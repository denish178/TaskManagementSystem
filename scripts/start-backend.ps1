# Starts all Gateway-backed microservices in Development (run from repo root).
param(
  [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
. (Join-Path $PSScriptRoot "dotenv.ps1")
Import-TmsDotEnv -Root $Root

$env:ASPNETCORE_ENVIRONMENT = "Development"

$sqlPassword = Get-TmsRequiredEnv -Name "MSSQL_SA_PASSWORD"
$jwtKey = Get-TmsRequiredEnv -Name "JWT_SIGNING_KEY"
if ($jwtKey.Length -lt 32) {
  throw "JWT_SIGNING_KEY must be at least 32 characters (see .env.example)."
}

# Override User Secrets / Azure connection strings for local Docker SQL (per-process below).
# Use string indexers — DataSource/UserID properties fail on some .NET/PowerShell combos.
$sqlBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
$sqlBuilder["Server"] = "127.0.0.1,1433"
$sqlBuilder["User ID"] = "sa"
$sqlBuilder["Password"] = $sqlPassword
$sqlBase = $sqlBuilder.ConnectionString.TrimEnd(';') + ";TrustServerCertificate=True;Encrypt=False"
$env:Jwt__Key = $jwtKey
$env:Jwt__Issuer = "TaskManagement.Auth"
$env:Jwt__Audience = "TaskManagementSystem"

$pidFile = Join-Path $Root ".local\backend-pids.json"
$logDir = Join-Path $Root "logs"
$waitHealth = Join-Path $PSScriptRoot "wait-health.ps1"

New-Item -ItemType Directory -Force -Path (Join-Path $Root ".local") | Out-Null
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$waitSql = Join-Path $PSScriptRoot "wait-sql.ps1"
if (Test-Path $waitSql) {
  Write-Host "Waiting for SQL Server on port 1433 (start deploy/docker/docker-compose.dev.yml if this hangs)..."
  & $waitSql
}

$redisSignalR = $false
try {
  $redisClient = New-Object System.Net.Sockets.TcpClient
  $redisClient.Connect("127.0.0.1", 6379)
  $redisSignalR = $redisClient.Connected
  $redisClient.Close()
} catch {
  $redisSignalR = $false
}
if ($redisSignalR) {
  Write-Host "Redis detected on :6379 - enabling SignalR backplane for Task, Comment, Notification."
}

$services = @(
  @{ Name = "Auth"; Project = "backend\Services\Auth\TaskManagement.Auth\TaskManagement.Auth.csproj"; Health = "http://localhost:5007/health" },
  @{ Name = "Project"; Project = "backend\Services\Project\TaskManagement.Project\TaskManagement.Project.csproj"; Health = "http://localhost:5012/health" },
  @{ Name = "Task"; Project = "backend\Services\Task\TaskManagement.Task\TaskManagement.Task.csproj"; Health = "http://localhost:5266/health" },
  @{ Name = "Comment"; Project = "backend\Services\Comment\TaskManagement.Comment\TaskManagement.Comment.csproj"; Health = "http://localhost:5260/health" },
  @{ Name = "File"; Project = "backend\Services\File\TaskManagement.File\TaskManagement.File.csproj"; Health = "http://localhost:5144/health" },
  @{ Name = "Dashboard"; Project = "backend\Services\Dashboard\TaskManagement.Dashboard\TaskManagement.Dashboard.csproj"; Health = "http://localhost:5209/health" },
  @{ Name = "AI"; Project = "backend\Services\AI\TaskManagement.AI\TaskManagement.AI.csproj"; Health = "http://localhost:5287/health" },
  @{ Name = "Notification"; Project = "backend\Services\Notification\TaskManagement.Notification\TaskManagement.Notification.csproj"; Health = "http://localhost:5290/health" },
  @{ Name = "Gateway"; Project = "backend\Gateway\TaskManagement.Gateway\TaskManagement.Gateway.csproj"; Health = "http://localhost:5204/health" }
)

if (-not $SkipBuild) {
  Write-Host "Building solution..."
  dotnet build (Join-Path $Root "TaskManagement.slnx") -v minimal
}

$pids = @()
foreach ($service in $services) {
  $projectPath = Join-Path $Root $service.Project
  $logPath = Join-Path $logDir "$($service.Name).log"
  $errPath = Join-Path $logDir "$($service.Name).err.log"
  Write-Host "Starting $($service.Name) -> $logPath"

  switch ($service.Name) {
    "Auth" {
      $env:ConnectionStrings__AuthDatabase = "$sqlBase;Database=TMS_Auth"
      Remove-Item Env:ConnectionStrings__AzureSql -ErrorAction SilentlyContinue
      Remove-Item Env:ConnectionStrings__FileDb -ErrorAction SilentlyContinue
    }
    "Project" {
      $env:ConnectionStrings__AzureSql = "$sqlBase;Database=TMS_Project"
      $env:ServiceUrls__Task = "http://localhost:5266"
      Remove-Item Env:ConnectionStrings__AuthDatabase -ErrorAction SilentlyContinue
      Remove-Item Env:ConnectionStrings__FileDb -ErrorAction SilentlyContinue
    }
    "Task" {
      $env:ConnectionStrings__AzureSql = "$sqlBase;Database=TMS_Task"
      $env:ServiceUrls__Project = "http://localhost:5012"
      $env:ServiceUrls__Notification = "http://localhost:5290"
      if ($redisSignalR) { $env:SignalR__Redis__Configuration = "localhost:6379" }
      Remove-Item Env:ConnectionStrings__AuthDatabase -ErrorAction SilentlyContinue
      Remove-Item Env:ConnectionStrings__FileDb -ErrorAction SilentlyContinue
    }
    "Notification" {
      $env:ConnectionStrings__NotificationDatabase = "$sqlBase;Database=TMS_Notification"
      if ($redisSignalR) { $env:SignalR__Redis__Configuration = "localhost:6379" }
      Remove-Item Env:ConnectionStrings__AzureSql -ErrorAction SilentlyContinue
    }
    "Comment" {
      $env:MongoDb__ConnectionString = "mongodb://127.0.0.1:27018"
      if ($redisSignalR) { $env:SignalR__Redis__Configuration = "localhost:6379" }
      Remove-Item Env:ConnectionStrings__AzureSql -ErrorAction SilentlyContinue
    }
    "File" {
      $env:ConnectionStrings__FileDb = "$sqlBase;Database=TMS_File"
      $env:ServiceUrls__Task = "http://localhost:5266"
      $env:FileStorage__Mode = "Local"
      $env:AzureBlobStorage__ConnectionString = ""
      Remove-Item Env:ConnectionStrings__AzureSql -ErrorAction SilentlyContinue
    }
    "Dashboard" {
      Remove-Item Env:ConnectionStrings__AzureSql -ErrorAction SilentlyContinue
    }
    "AI" {
      $env:ConnectionStrings__ProjectDatabase = "$sqlBase;Database=TMS_Project"
      $env:ConnectionStrings__TaskDatabase = "$sqlBase;Database=TMS_Task"
      Remove-Item Env:ConnectionStrings__AzureSql -ErrorAction SilentlyContinue
      if ($env:GEMINI_API_KEY) {
        $env:Gemini__ApiKey = $env:GEMINI_API_KEY
      }
    }
    "Gateway" {
      Remove-Item Env:ConnectionStrings__AzureSql -ErrorAction SilentlyContinue
    }
  }

  $proc = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList "run", "--project", $projectPath `
    -WorkingDirectory $Root `
    -RedirectStandardOutput $logPath `
    -RedirectStandardError $errPath `
    -PassThru `
    -WindowStyle Hidden

  $pids += @{ name = $service.Name; id = $proc.Id }

  if ($service.Health) {
    & $waitHealth -Url $service.Health -TimeoutSeconds 120
  }
}

@($pids) | ConvertTo-Json | Set-Content $pidFile
Write-Host "Backend processes started. PIDs saved to $pidFile"
Write-Host "Gateway: http://localhost:5204/health"
