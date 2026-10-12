param(
  [int]$TimeoutSeconds = 120,
  [int]$Port = 1433
)

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
Write-Host "Waiting for SQL Server on port $Port..."

while ((Get-Date) -lt $deadline) {
  $containerRunning = $false
  $health = docker inspect -f "{{.State.Health.Status}}" tms-sql 2>$null
  if ($LASTEXITCODE -eq 0) {
    if ($health -eq "healthy") {
      Write-Host "tms-sql container is healthy."
      Start-Sleep -Seconds 5
      exit 0
    }
    $state = docker inspect -f "{{.State.Status}}" tms-sql 2>$null
    if ($state -eq "running" -and -not $health) {
      $containerRunning = $true
    }
    if ($state -eq "exited" -or $state -eq "dead") {
      Write-Host "tms-sql is not running (state: $state). Check: docker logs tms-sql"
      throw "SQL container tms-sql stopped. Reset with: docker compose --project-directory <repo-root> -f deploy/docker/docker-compose.dev.yml down -v"
    }
  }

  if ((Test-NetConnection -ComputerName localhost -Port $Port -WarningAction SilentlyContinue).TcpTestSucceeded) {
    if ($containerRunning -or -not $health) {
      Write-Host "SQL port is open; waiting for login readiness..."
      Start-Sleep -Seconds 15
      exit 0
    }
  }
  Start-Sleep -Seconds 3
}

throw "SQL Server did not become ready on port $Port within $TimeoutSeconds seconds."
