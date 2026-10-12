param(
  [Parameter(Mandatory = $true)]
  [string]$Url,
  [int]$TimeoutSeconds = 90
)

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
  try {
    $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
    if ($r.StatusCode -eq 200) {
      Write-Host "Ready: $Url"
      return
    }
  } catch {
    Start-Sleep -Seconds 3
  }
}

throw "Timed out waiting for $Url"
