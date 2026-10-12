# One-command local stack: Docker infra + backend services.
param(
  [switch]$SkipBuild,
  [switch]$InfraOnly,
  [switch]$BackendOnly
)

$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $Root

if (-not $BackendOnly) {
  Write-Host "Starting Docker infrastructure (SQL Server + MongoDB)..."
  docker compose -f docker-compose.dev.yml up -d
  & (Join-Path $PSScriptRoot "wait-sql.ps1")
}

if (-not $InfraOnly) {
  & (Join-Path $PSScriptRoot "start-backend.ps1") @PSBoundParameters
  Write-Host ""
  Write-Host "Frontend (separate terminal):"
  Write-Host "  cd frontend"
  Write-Host "  copy .env.example .env   # VITE_API_BASE_URL only"
  Write-Host "  npm install"
  Write-Host "  npm run dev"
  Write-Host ""
  Write-Host "UI: http://localhost:5173"
  Write-Host "API Gateway: http://localhost:5204"
  Write-Host "Smoke test: powershell -File scripts/e2e-gateway.ps1"
}
