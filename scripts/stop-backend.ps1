$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$pidFile = Join-Path $Root ".local\backend-pids.json"

if (Test-Path $pidFile) {
  $entries = Get-Content $pidFile | ConvertFrom-Json
  foreach ($entry in $entries) {
    $procId = $entry.id
    if ($procId -match '^\d+$') {
      Stop-Process -Id ([int]$procId) -Force -ErrorAction SilentlyContinue
    }
  }
  Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
  Write-Host "Stopped backend processes from $pidFile"
}

$ports = @(5007, 5012, 5266, 5260, 5144, 5209, 5287, 5290, 5204)
foreach ($port in $ports) {
  $connections = netstat -ano | Select-String ":$port\s"
  foreach ($line in $connections) {
    $parts = ($line -split "\s+") | Where-Object { $_ -ne "" }
    $procId = $parts[-1]
    if ($procId -match '^\d+$') {
      Stop-Process -Id ([int]$procId) -Force -ErrorAction SilentlyContinue
    }
  }
}

Write-Host "Stopped backend listeners on ports: $($ports -join ', ')"
