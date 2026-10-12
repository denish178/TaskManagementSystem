# End-to-end smoke tests through API Gateway (requires all services running).
$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
. (Join-Path $PSScriptRoot "dotenv.ps1")
Import-TmsDotEnv -Root $Root
$adminPassword = Get-TmsRequiredEnv -Name "TMS_DEV_ADMIN_PASSWORD"
$userPassword = Get-TmsRequiredEnv -Name "TMS_DEV_USER_PASSWORD"
$base = "http://localhost:5204"

function Assert-Status($response, $expected) {
  if ($response.StatusCode -ne $expected) {
    throw "Expected HTTP $expected but got $($response.StatusCode): $($response.Content)"
  }
}

Write-Host "=== Auth: admin login ==="
$loginBody = @{ email = "admin@local.test"; password = $adminPassword } | ConvertTo-Json
$adminLogin = Invoke-WebRequest -Uri "$base/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json" -UseBasicParsing
Assert-Status $adminLogin 200
$adminJson = $adminLogin.Content | ConvertFrom-Json
$adminToken = $adminJson.token
$adminHeaders = @{ Authorization = "Bearer $adminToken" }

Write-Host "=== Auth: user login ==="
$userLoginBody = @{ email = "user@local.test"; password = $userPassword } | ConvertTo-Json
$userLogin = Invoke-WebRequest -Uri "$base/api/auth/login" -Method POST -Body $userLoginBody -ContentType "application/json" -UseBasicParsing
$userJson = $userLogin.Content | ConvertFrom-Json
$userToken = $userJson.token
$userHeaders = @{ Authorization = "Bearer $userToken" }
$userId = $userJson.user.id

Write-Host "=== Team + Project ==="
$teamBody = @{ name = "E2E Team"; description = "test"; createdBy = $adminJson.user.id } | ConvertTo-Json
$team = Invoke-WebRequest -Uri "$base/api/teams" -Method POST -Body $teamBody -Headers $adminHeaders -ContentType "application/json" -UseBasicParsing
$teamId = ($team.Content | ConvertFrom-Json).id
$projectBody = @{ teamId = $teamId; name = "E2E Project"; description = "e2e"; createdBy = $adminJson.user.id } | ConvertTo-Json
$project = Invoke-WebRequest -Uri "$base/api/projects" -Method POST -Body $projectBody -Headers $adminHeaders -ContentType "application/json" -UseBasicParsing
$projectId = ($project.Content | ConvertFrom-Json).id

Write-Host "=== Task create ==="
$taskBody = @{ projectId = $projectId; title = "E2E Task"; description = "d"; priority = "High"; dueDate = $null; sprintId = $null } | ConvertTo-Json
$task = Invoke-WebRequest -Uri "$base/api/tasks" -Method POST -Body $taskBody -Headers $adminHeaders -ContentType "application/json" -UseBasicParsing
$taskId = ($task.Content | ConvertFrom-Json).id

Write-Host "=== Kanban statuses ==="
foreach ($status in @("Todo", "InProgress", "Testing", "Done")) {
  $patch = @{ status = $status } | ConvertTo-Json
  $r = Invoke-WebRequest -Uri "$base/api/tasks/$taskId/status" -Method PATCH -Body $patch -Headers $adminHeaders -ContentType "application/json" -UseBasicParsing
  Assert-Status $r 200
}

Write-Host "=== Admin assign ==="
$assignBody = @{ assigneeId = $userId } | ConvertTo-Json
$assign = Invoke-WebRequest -Uri "$base/api/tasks/$taskId/assign" -Method PATCH -Body $assignBody -Headers $adminHeaders -ContentType "application/json" -UseBasicParsing
Assert-Status $assign 200

Write-Host "=== User assign forbidden ==="
try {
  Invoke-WebRequest -Uri "$base/api/tasks/$taskId/assign" -Method PATCH -Body $assignBody -Headers $userHeaders -ContentType "application/json" -UseBasicParsing
  throw "User assign should have failed"
} catch {
  if ($_.Exception.Response.StatusCode.value__ -ne 403) {
    throw "Expected 403 for user assign, got $($_.Exception.Response.StatusCode)"
  }
}

Write-Host "=== User edit preserves assignee ==="
$getBefore = Invoke-WebRequest -Uri "$base/api/tasks/$taskId" -Headers $userHeaders -UseBasicParsing
$assigneeBefore = ($getBefore.Content | ConvertFrom-Json).assigneeId
$updateBody = @{ title = "E2E Task Updated"; description = "x"; sprintId = $null; priority = "Medium"; dueDate = $null } | ConvertTo-Json
Invoke-WebRequest -Uri "$base/api/tasks/$taskId" -Method PUT -Body $updateBody -Headers $userHeaders -ContentType "application/json" -UseBasicParsing | Out-Null
$getAfter = Invoke-WebRequest -Uri "$base/api/tasks/$taskId" -Headers $userHeaders -UseBasicParsing
$assigneeAfter = ($getAfter.Content | ConvertFrom-Json).assigneeId
if ($assigneeBefore -ne $assigneeAfter) { throw "Assignee changed after user edit" }

Write-Host "=== Comment ==="
$commentBody = @{ taskId = $taskId; content = "E2E comment" } | ConvertTo-Json
$c = Invoke-WebRequest -Uri "$base/api/comments" -Method POST -Body $commentBody -Headers $userHeaders -ContentType "application/json" -UseBasicParsing
Assert-Status $c 201

Write-Host "=== Sprint + member ==="
$sprintBody = @{
  projectId = $projectId
  name = "Sprint 1"
  goal = "test"
  startDate = (Get-Date).ToUniversalTime().ToString("o")
  endDate = (Get-Date).AddDays(7).ToUniversalTime().ToString("o")
} | ConvertTo-Json
Invoke-WebRequest -Uri "$base/api/sprints" -Method POST -Body $sprintBody -Headers $adminHeaders -ContentType "application/json" -UseBasicParsing | Out-Null
$memberBody = @{ userId = $userId; role = "Member" } | ConvertTo-Json
Invoke-WebRequest -Uri "$base/api/teams/$teamId/members" -Method POST -Body $memberBody -Headers $adminHeaders -ContentType "application/json" -UseBasicParsing | Out-Null

Write-Host "=== File upload ==="
$temp = New-TemporaryFile
Rename-Item $temp "$temp.txt"
$temp = "$temp.txt"
Set-Content -Path $temp -Value "e2e attachment" -NoNewline
try {
  curl.exe -s -f -X POST "$base/api/files/upload" `
    -H "Authorization: Bearer $adminToken" `
    -F "file=@$temp;type=text/plain" `
    -F "taskId=$taskId" `
    -F "uploadedBy=$($adminJson.user.id)" | Out-Null
} finally {
  Remove-Item $temp -Force -ErrorAction SilentlyContinue
}

Write-Host "=== Dashboard ==="
Invoke-WebRequest -Uri "$base/api/dashboard/project/$projectId" -Headers $adminHeaders -UseBasicParsing | Out-Null

Write-Host "ALL E2E CHECKS PASSED"
