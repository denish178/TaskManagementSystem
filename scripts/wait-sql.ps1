param(
  [int]$TimeoutSeconds = 180,
  [int]$Port = 1433
)

function Get-TmsSqlContainerName {
  $named = docker ps -a --filter "name=^tms-sql$" --format "{{.Names}}" 2>$null
  if ($named) {
    return $named.Trim()
  }

  $suffix = docker ps -a --filter "name=tms-sql" --format "{{.Names}}" 2>$null | Select-Object -First 1
  if ($suffix) {
    return $suffix.Trim()
  }

  return "tms-sql"
}

function Test-SqlPortOpen {
  param([int]$TargetPort)

  foreach ($hostName in @("127.0.0.1", "localhost")) {
    if ((Test-NetConnection -ComputerName $hostName -Port $TargetPort -WarningAction SilentlyContinue).TcpTestSucceeded) {
      return $true
    }
  }

  return $false
}

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$sqlContainer = Get-TmsSqlContainerName
Write-Host "Waiting for SQL Server on port $Port (container: $sqlContainer)..."

while ((Get-Date) -lt $deadline) {
  $state = docker inspect -f "{{.State.Status}}" $sqlContainer 2>$null
  if ($LASTEXITCODE -eq 0) {
    if ($state -eq "exited" -or $state -eq "dead") {
      Write-Host "$sqlContainer is not running (state: $state). Check: docker logs $sqlContainer"
      throw "SQL container stopped. Reset with: docker compose --project-directory <repo-root> -f deploy/docker/docker-compose.dev.yml down -v"
    }

    $health = docker inspect -f "{{.State.Health.Status}}" $sqlContainer 2>$null
    if ($health -eq "healthy") {
      Write-Host "$sqlContainer is healthy."
      Start-Sleep -Seconds 3
      exit 0
    }
  }

  if ($state -eq "running" -and (Test-SqlPortOpen -TargetPort $Port)) {
    Write-Host "SQL port $Port is open (health: $health); waiting for login readiness..."
    Start-Sleep -Seconds 15
    exit 0
  }

  Start-Sleep -Seconds 3
}

throw "SQL Server did not become ready on port $Port within $TimeoutSeconds seconds. Check: docker logs $sqlContainer"
