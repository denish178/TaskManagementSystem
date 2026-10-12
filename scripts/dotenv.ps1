function Import-TmsDotEnv {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Root
  )

  $path = Join-Path $Root ".env"
  if (-not (Test-Path $path)) {
    throw "Missing .env file. Copy .env.example to .env in the repo root and set values."
  }

  foreach ($line in Get-Content $path) {
    $trim = $line.Trim()
    if ($trim.Length -eq 0 -or $trim.StartsWith("#")) {
      continue
    }

    $eq = $trim.IndexOf("=")
    if ($eq -lt 1) {
      continue
    }

    $name = $trim.Substring(0, $eq).Trim()
    $value = $trim.Substring($eq + 1).Trim()
    if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
      $value = $value.Substring(1, $value.Length - 2)
    }

    Set-Item -Path "Env:$name" -Value $value
  }
}

function Get-TmsRequiredEnv {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Name
  )

  $value = [Environment]::GetEnvironmentVariable($Name)
  if ([string]::IsNullOrWhiteSpace($value)) {
    throw "Environment variable $Name is not set. Add it to your .env file (see .env.example)."
  }

  return $value
}
