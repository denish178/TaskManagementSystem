# Seeds teams, projects, sprints, tasks, subtasks, assignments, and comments for demos.
# Requires gateway + all services running.
# Idempotent: skips only when demo project "Customer Mobile App" already exists.
# If the Acme Digital team exists without projects, continues seeding.
$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
. (Join-Path $PSScriptRoot "dotenv.ps1")
Import-TmsDotEnv -Root $Root
$adminPassword = Get-TmsRequiredEnv -Name "TMS_DEV_ADMIN_PASSWORD"
$base = "http://localhost:5204"
$demoTeamName = "Acme Digital"
$demoMarkerProject = "Customer Mobile App"

# Matches DevelopmentDataSeeder (fallback if GET /users JSON parsing fails on Windows PS)
$devUserIds = @{
  "admin@local.test" = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
  "user@local.test" = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
  "ananya.iyer@acme.test" = "cccccccc-cccc-cccc-cccc-cccccccccc01"
  "vikram.singh@acme.test" = "cccccccc-cccc-cccc-cccc-cccccccccc02"
  "neha.kapoor@acme.test" = "cccccccc-cccc-cccc-cccc-cccccccccc03"
  "arjun.desai@acme.test" = "cccccccc-cccc-cccc-cccc-cccccccccc04"
  "sofia.martinez@acme.test" = "cccccccc-cccc-cccc-cccc-cccccccccc05"
}

Write-Host "=== Waiting for API gateway ==="
& (Join-Path $PSScriptRoot "wait-health.ps1") -Url "$base/health" -TimeoutSeconds 120

function Invoke-TmsJson {
  param(
    [string]$Method,
    [string]$Uri,
    [hashtable]$Headers = @{},
    [object]$Body = $null
  )

  $irm = @{
    Uri = $Uri
    Method = $Method
    TimeoutSec = 120
  }
  if ($Headers.Count -gt 0) {
    $irm.Headers = $Headers
  }
  if ($null -ne $Body) {
    $irm.ContentType = "application/json"
    $irm.Body = ($Body | ConvertTo-Json -Depth 6)
  }

  try {
    return Invoke-RestMethod @irm
  } catch {
    if ($_.ErrorDetails.Message) {
      throw $_.ErrorDetails.Message
    }
    throw $_.Exception.Message
  }
}

function Convert-ToGuidString($raw) {
  if ($raw -is [System.Array]) {
    if ($raw.Length -lt 1) {
      throw "Empty id array."
    }
    $raw = $raw[0]
  }
  return ([guid]$raw).ToString("D")
}

function Get-EntityId($entity) {
  if ($null -eq $entity) {
    throw "API entity is null."
  }
  if ($entity -is [System.Array]) {
    if ($entity.Length -lt 1) {
      throw "Empty entity array."
    }
    $entity = $entity[0]
  }
  if ($entity -is [System.Collections.IDictionary]) {
    foreach ($key in $entity.Keys) {
      if ("$key" -ieq "id") {
        return Convert-ToGuidString $entity[$key]
      }
    }
  }
  $prop = $entity.PSObject.Properties | Where-Object { $_.Name -ieq "id" } | Select-Object -First 1
  if ($null -ne $prop -and $null -ne $prop.Value -and "$($prop.Value)".Length -gt 0) {
    return Convert-ToGuidString $prop.Value
  }
  if ($null -ne $entity.id) {
    return Convert-ToGuidString $entity.id
  }
  if ($null -ne $entity.Id) {
    return Convert-ToGuidString $entity.Id
  }
  throw "API entity has no id property."
}

Write-Host "=== Login (admin) ==="
$admin = Invoke-TmsJson -Method POST -Uri "$base/api/auth/login" -Body @{
  email = "admin@local.test"
  password = $adminPassword
}
$token = if ($null -ne $admin.token) { $admin.token } else { $admin.Token }
$headers = @{ Authorization = "Bearer $token" }
$profile = Invoke-TmsJson -Method GET -Uri "$base/api/auth/profile" -Headers $headers
$adminId = Get-EntityId $profile

Write-Host "=== Load users ==="
$usersResponse = Invoke-WebRequest -Uri "$base/api/auth/users" -Headers $headers -UseBasicParsing
$usersParsed = $usersResponse.Content | ConvertFrom-Json
if ($usersParsed -is [System.Array]) {
  $users = @($usersParsed)
} else {
  $users = @($usersParsed)
}

$projects = @(Invoke-TmsJson -Method GET -Uri "$base/api/projects" -Headers $headers)
if ($projects | Where-Object { $_.name -eq $demoMarkerProject -or $_.Name -eq $demoMarkerProject }) {
  Write-Host "Demo data already present (project '$demoMarkerProject' found) - nothing to do."
  exit 0
}

$teams = @(Invoke-TmsJson -Method GET -Uri "$base/api/teams" -Headers $headers)
$matchingTeams = @($teams | Where-Object { $_.name -eq $demoTeamName -or $_.Name -eq $demoTeamName })
$existingTeam = if ($matchingTeams.Count -gt 0) { $matchingTeams[0] } else { $null }
function Get-UserEmail($candidate) {
  if ($null -ne $candidate.email) { return [string]$candidate.email }
  if ($null -ne $candidate.Email) { return [string]$candidate.Email }
  return ""
}

function Get-UserId([string]$email) {
  $want = $email.Trim().ToLowerInvariant()
  foreach ($candidate in $users) {
    if ($candidate -is [System.Array]) {
      foreach ($inner in $candidate) {
        $em = Get-UserEmail $inner
        if ($em.Trim().ToLowerInvariant() -eq $want) {
          return Get-EntityId $inner
        }
      }
      continue
    }
    $em = Get-UserEmail $candidate
    if ($em.Trim().ToLowerInvariant() -eq $want) {
      return Get-EntityId $candidate
    }
  }
  if ($devUserIds.ContainsKey($want)) {
    return $devUserIds[$want]
  }
  throw "User not found: $email. Restart Auth (scripts/start-local.ps1) so DevelopmentDataSeeder runs with TMS_DEV_* passwords in .env."
}

$idRahul = Get-UserId "user@local.test"
$idAnanya = Get-UserId "ananya.iyer@acme.test"
$idVikram = Get-UserId "vikram.singh@acme.test"
$idNeha = Get-UserId "neha.kapoor@acme.test"
$idArjun = Get-UserId "arjun.desai@acme.test"
$idSofia = Get-UserId "sofia.martinez@acme.test"

Write-Host "=== Team + members ==="
$teamId = $null
if ($existingTeam) {
  $teamId = Get-EntityId $existingTeam
  try {
    Invoke-TmsJson -Method GET -Uri "$base/api/teams/$teamId" -Headers $headers | Out-Null
    Write-Host "Using existing team '$demoTeamName' (finishing incomplete seed)."
  } catch {
    Write-Host "Team '$demoTeamName' id is stale; creating a new team."
    $teamId = $null
  }
}

if (-not $teamId) {
  $team = Invoke-TmsJson -Method POST -Uri "$base/api/teams" -Headers $headers -Body @{
    name = $demoTeamName
    description = "Product engineering - mobile, web, and analytics"
    createdBy = $adminId
  }
  $teamId = Get-EntityId $team
}

foreach ($memberId in @($idRahul, $idAnanya, $idVikram, $idNeha, $idArjun, $idSofia)) {
  try {
    Invoke-TmsJson -Method POST -Uri "$base/api/teams/$teamId/members" -Headers $headers -Body @{
      userId = $memberId
      role = "Member"
    } | Out-Null
  } catch {
    $msg = $_.Exception.Message
    if ($_.ErrorDetails.Message) { $msg = $_.ErrorDetails.Message }
    if ($msg -match "409|already a team member|Conflict") { continue }
    throw $msg
  }
}

Write-Host "=== Projects ==="
function New-Project([string]$name, [string]$description) {
  $r = Invoke-TmsJson -Method POST -Uri "$base/api/projects" -Headers $headers -Body @{
    teamId = [string]$teamId
    name = $name
    description = $description
    createdBy = [string]$adminId
  }
  return Get-EntityId $r
}

$projectMobile = New-Project "Customer Mobile App" "iOS and Android experience for end customers"
$projectWeb = New-Project "Partner Web Portal" "B2B partner onboarding, billing, and reports"
$projectAnalytics = New-Project "Analytics Dashboard" "Internal KPIs and sales intelligence"

Write-Host "=== Sprints ==="
function New-Sprint([string]$projectId, [string]$name, [string]$goal, [int]$startOffsetDays, [int]$lengthDays) {
  $start = (Get-Date).Date.AddDays($startOffsetDays).ToUniversalTime()
  $end = $start.AddDays($lengthDays)
  $r = Invoke-TmsJson -Method POST -Uri "$base/api/sprints" -Headers $headers -Body @{
    projectId = [string]$projectId
    name = $name
    goal = $goal
    startDate = $start.ToString("o")
    endDate = $end.ToString("o")
  }
  return Get-EntityId $r
}

$sprintMobile = New-Sprint $projectMobile "Sprint 12 - Onboarding" "Ship guided onboarding and notification MVP" -3 14
$sprintWeb = New-Sprint $projectWeb "Sprint 8 - Billing" "SSO for partners and CSV exports" -7 14
$sprintAnalytics = New-Sprint $projectAnalytics "Sprint 3 - KPIs" "Dashboard widgets and data migration" 0 21

Write-Host "=== Tasks ==="
function New-Task {
  param(
    [string]$ProjectId,
    [string]$SprintId,
    [string]$Title,
    [string]$Description,
    [string]$Priority,
    [int]$DueInDays
  )
  $due = (Get-Date).Date.AddDays($DueInDays).ToUniversalTime().ToString("o")
  $r = Invoke-TmsJson -Method POST -Uri "$base/api/tasks" -Headers $headers -Body @{
    projectId = [string]$ProjectId
    sprintId = [string]$SprintId
    title = $Title
    description = $Description
    priority = $Priority
    dueDate = $due
  }
  return Get-EntityId $r
}

function Set-TaskStatus([string]$TaskId, [string]$Status) {
  Invoke-TmsJson -Method PATCH -Uri "$base/api/tasks/$TaskId/status" -Headers $headers -Body @{ status = $Status } | Out-Null
}

function Assign-Task([string]$TaskId, [string]$AssigneeId) {
  Invoke-TmsJson -Method PATCH -Uri "$base/api/tasks/$TaskId/assign" -Headers $headers -Body @{ assigneeId = [string]$AssigneeId } | Out-Null
}

function Add-Subtask([string]$TaskId, [string]$Title, [bool]$Completed = $false) {
  $sub = Invoke-TmsJson -Method POST -Uri "$base/api/tasks/$TaskId/subtasks" -Headers $headers -Body @{ title = $Title }
  $subId = Get-EntityId $sub
  if ($Completed) {
    Invoke-TmsJson -Method PUT -Uri "$base/api/tasks/$TaskId/subtasks/$subId" -Headers $headers -Body @{
      title = $Title
      isCompleted = $true
    } | Out-Null
  }
}

$t1 = New-Task $projectMobile $sprintMobile "Onboarding walkthrough screens" "Carousel + permissions flow for new installs" "High" 10
Set-TaskStatus $t1 "InProgress"
Assign-Task $t1 $idRahul
Add-Subtask $t1 "Design Figma screens" $true
Add-Subtask $t1 "Implement ViewPager steps" $false
Add-Subtask $t1 "QA on Android 14" $false

$t2 = New-Task $projectMobile $sprintMobile "Push notification service" "FCM integration and preference center" "Medium" 14
Set-TaskStatus $t2 "Todo"
Assign-Task $t2 $idAnanya

$t3 = New-Task $projectMobile $sprintMobile "App Store release checklist" "Screenshots, privacy labels, rollout plan" "High" 7
Set-TaskStatus $t3 "Testing"
Assign-Task $t3 $idSofia

$t4 = New-Task $projectWeb $sprintWeb "Partner SSO login" "SAML + session timeout handling" "High" 12
Set-TaskStatus $t4 "InProgress"
Assign-Task $t4 $idVikram
Add-Subtask $t4 "Configure IdP metadata" $true
Add-Subtask $t4 "Map partner roles to claims" $false

$t5 = New-Task $projectWeb $sprintWeb "Export billing reports (CSV)" "Monthly usage export for finance" "Medium" 5
Set-TaskStatus $t5 "Done"
Assign-Task $t5 $idNeha

$t6 = New-Task $projectAnalytics $sprintAnalytics "Sales KPI dashboard widgets" "Revenue, churn, and pipeline tiles" "High" 18
Set-TaskStatus $t6 "InProgress"
Assign-Task $t6 $idArjun

$t7 = New-Task $projectAnalytics $sprintAnalytics "Migrate legacy sales data" "One-time ETL from old CRM" "Medium" 25
Set-TaskStatus $t7 "Todo"
Assign-Task $t7 $idRahul

Write-Host "=== Comments (as assignees) ==="
function Add-Comment([string]$TaskId, [string]$Token, [string]$Text) {
  Invoke-TmsJson -Method POST -Uri "$base/api/comments" -Headers @{ Authorization = "Bearer $Token" } -Body @{
    taskId = [string]$TaskId
    content = $Text
  } | Out-Null
}

$rahulLogin = Invoke-TmsJson -Method POST -Uri "$base/api/auth/login" -Body @{
  email = "user@local.test"
  password = (Get-TmsRequiredEnv -Name "TMS_DEV_USER_PASSWORD")
}
$rahulToken = $rahulLogin.token

$vikramLogin = Invoke-TmsJson -Method POST -Uri "$base/api/auth/login" -Body @{
  email = "vikram.singh@acme.test"
  password = (Get-TmsRequiredEnv -Name "TMS_DEV_USER_PASSWORD")
}
$vikramToken = $vikramLogin.token

Add-Comment $t1 $rahulToken "Onboarding copy approved by marketing - implementing step 2 next."
Add-Comment $t4 $vikramToken "IdP certificate rotates Friday; I'll add a reminder in the sprint notes."

Write-Host ""
Write-Host "PRESENTATION DEMO READY"
Write-Host "------------------------"
Write-Host "Admin login:  admin@local.test  (Priya Sharma)"
Write-Host "User login:   user@local.test   (Rahul Mehta) - same TMS_DEV_USER_PASSWORD as other team users"
Write-Host "Team:         $demoTeamName"
Write-Host "Projects:     Customer Mobile App | Partner Web Portal | Analytics Dashboard"
Write-Host ""
Write-Host "Suggested flow:"
Write-Host "  1. Admin - Projects list, open Mobile App, Sprints / Tasks / Kanban"
Write-Host "  2. Admin - assign or reassign a task; show Members"
Write-Host "  3. Log in as Rahul - open assigned task (comments, subtasks, status)"
Write-Host "  4. Dashboard + notifications bell"
Write-Host ""
Write-Host "Project IDs: mobile=$projectMobile web=$projectWeb analytics=$projectAnalytics"
