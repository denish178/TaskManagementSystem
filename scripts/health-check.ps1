$endpoints = @(
  @{ Name = "Gateway"; Url = "http://localhost:5204/health" },
  @{ Name = "Auth"; Url = "http://localhost:5007/health" },
  @{ Name = "Project"; Url = "http://localhost:5012/health" },
  @{ Name = "Task"; Url = "http://localhost:5266/health" },
  @{ Name = "Comment"; Url = "http://localhost:5260/health" },
  @{ Name = "File"; Url = "http://localhost:5144/health" },
  @{ Name = "Dashboard"; Url = "http://localhost:5209/health" },
  @{ Name = "AI"; Url = "http://localhost:5287/health" },
  @{ Name = "Notification"; Url = "http://localhost:5290/health" }
)

$failed = 0
foreach ($ep in $endpoints) {
  try {
    $r = Invoke-WebRequest -Uri $ep.Url -UseBasicParsing -TimeoutSec 5
    if ($r.StatusCode -eq 200) {
      Write-Host "[OK] $($ep.Name)"
    } else {
      Write-Host "[FAIL] $($ep.Name) HTTP $($r.StatusCode)"
      $failed++
    }
  } catch {
    Write-Host "[FAIL] $($ep.Name) $($_.Exception.Message)"
    $failed++
  }
}

if ($failed -gt 0) { exit 1 }
