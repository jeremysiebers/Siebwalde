<#
.SYNOPSIS
  Validate the Siebwalde Workflow v1 Active State Manifest and compare it with actual Git reality.

.DESCRIPTION
  Read-only helper. It validates required fields and enum values, then compares
  working_branch, verified_revision and the tracked working-tree state against the
  real repository. It emits one of: CONSISTENT, STATE_DRIFT, INVALID_MANIFEST,
  MANIFEST_MISSING, plus resume guidance.

  It never modifies the manifest or the repository, and performs no destructive,
  remote or hardware actions.

.PARAMETER ManifestPath
  Path to the manifest. Defaults to active-state.json next to this script.

.PARAMETER Json
  Emit the result as JSON instead of human-readable text.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/workflow/active-state-check.ps1
#>
[CmdletBinding()]
param(
  [string]$ManifestPath,
  [switch]$Json
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ManifestPath) { $ManifestPath = Join-Path $PSScriptRoot 'active-state.json' }

function Invoke-Git {
  param([string[]]$GitArgs)
  try {
    $out = & git -C $repoRoot @GitArgs 2>$null
    if ($null -eq $out) { return '' }
    return ($out | Out-String).Trim()
  } catch { return '' }
}

function Test-GitRevisionExists {
  param([string]$Rev)
  & git -C $repoRoot cat-file -e "$Rev^{commit}" 2>$null | Out-Null
  return ($LASTEXITCODE -eq 0)
}

$primaryStates = @('INTAKE','CLASSIFY','PLAN','ANALYZE_DESIGN','IMPLEMENT','SELF_VERIFY','INDEPENDENT_REVIEW','VALIDATION_PREP','VALIDATE','EVIDENCE_COMPLETE','PR_PREP','PR_ACTIVE','MERGE_READY','POST_MERGE_VERIFY','CLOSURE','RETROSPECTIVE','DONE','ABORTED')
$execStatuses = @('ACTIVE','PAUSED','BLOCKED','WAITING_AUTHORITY','WAITING_PRODUCT_DECISION','UNKNOWN_EXECUTION_STATE')
$evidenceStatuses = @('PROVEN','PARTIALLY_PROVEN','NOT_PROVEN','NOT_APPLICABLE')
$authorityTypes = @('PRODUCT_DECISION','LIVE_HARDWARE','FIRMWARE_FLASH','DESTRUCTIVE_ACTION','REMOTE_PUSH','CREATE_PR','HISTORY_REWRITE','MERGE','EVIDENCE_BRANCH_DELETE')
$authorityStatuses = @('PENDING','GRANTED','DENIED','INVALIDATED','CONSUMED')
$riskClasses = @('NORMAL','ELEVATED','SAFETY_RELEVANT')
$validationClasses = @('V0','V1','V2','V3','V4')
$requiredFields = @('format_version','updated_at','increment','objective','target_branch','working_branch','verified_revision','primary_state','execution_status','autonomy_envelope','change_class','risk_class','review_class','validation_class','acceptance_criteria','role_status','evidence_completed','evidence_invalidated','open_findings','pending_authority','authority_validity','blockers','working_tree_state','next_safe_action','references')

$result = [ordered]@{
  manifest_path      = $ManifestPath
  manifest_present   = $false
  schema_ok          = $false
  schema_errors      = @()
  branch_match       = $false
  revision_match     = $false
  working_tree_clean = $false
  drift              = 'UNKNOWN'
  drift_reasons      = @()
  primary_state      = $null
  execution_status   = $null
  next_safe_action   = $null
  resume_guidance    = $null
}

function Write-Result {
  param($R)
  if ($Json) {
    $R | ConvertTo-Json -Depth 6
    return
  }
  "manifest_path:      $($R.manifest_path)"
  "manifest_present:   $($R.manifest_present)"
  "schema_ok:          $($R.schema_ok)"
  "drift:              $($R.drift)"
  "primary_state:      $($R.primary_state)"
  "execution_status:   $($R.execution_status)"
  "branch_match:       $($R.branch_match)"
  "revision_match:     $($R.revision_match)"
  "working_tree_clean: $($R.working_tree_clean)"
  if ($R.schema_errors.Count) { 'schema_errors:'; $R.schema_errors | ForEach-Object { "  - $_" } }
  if ($R.drift_reasons.Count) { 'drift_reasons:'; $R.drift_reasons | ForEach-Object { "  - $_" } }
  "resume_guidance:    $($R.resume_guidance)"
  "next_safe_action:   $($R.next_safe_action)"
}

if (-not (Test-Path -LiteralPath $ManifestPath)) {
  $result.drift = 'MANIFEST_MISSING'
  $result.drift_reasons += 'Active State Manifest not found.'
  $result.resume_guidance = 'No manifest: do not assume the workflow has no history. Reconstruct the active increment from Git, the Increment Contract, review/evidence records, PR state and durable docs before resuming.'
  Write-Result $result
  exit 0
}
$result.manifest_present = $true

try {
  $raw = Get-Content -LiteralPath $ManifestPath -Raw
  $m = $raw | ConvertFrom-Json
} catch {
  $result.drift = 'INVALID_MANIFEST'
  $result.schema_errors += "JSON parse error: $($_.Exception.Message)"
  $result.resume_guidance = 'Manifest is not valid JSON. Repair or reconstruct it before trusting recorded state.'
  Write-Result $result
  exit 0
}

foreach ($f in $requiredFields) {
  if (-not ($m.PSObject.Properties.Name -contains $f)) { $result.schema_errors += "missing required field: $f" }
}
if ($m.primary_state -and ($primaryStates -notcontains $m.primary_state)) { $result.schema_errors += "invalid primary_state: $($m.primary_state)" }
if ($m.execution_status -and ($execStatuses -notcontains $m.execution_status)) { $result.schema_errors += "invalid execution_status: $($m.execution_status)" }
if ($m.risk_class -and ($riskClasses -notcontains $m.risk_class)) { $result.schema_errors += "invalid risk_class: $($m.risk_class)" }
if ($m.validation_class -and ($validationClasses -notcontains $m.validation_class)) { $result.schema_errors += "invalid validation_class: $($m.validation_class)" }
foreach ($ac in @($m.acceptance_criteria)) {
  if ($null -eq $ac) { continue }
  foreach ($k in @('id','description','status')) {
    if (-not ($ac.PSObject.Properties.Name -contains $k)) { $result.schema_errors += "acceptance_criteria entry missing '$k'" }
  }
  if ($ac.status -and ($evidenceStatuses -notcontains $ac.status)) { $result.schema_errors += "AC $($ac.id): invalid status '$($ac.status)'" }
  if (($ac.PSObject.Properties.Name -contains 'disposition') -and $ac.disposition -and $ac.disposition -ne 'ACCEPTED_EXCEPTION') {
    $result.schema_errors += "AC $($ac.id): invalid disposition '$($ac.disposition)'"
  }
}
foreach ($a in @($m.pending_authority)) {
  if ($null -eq $a) { continue }
  foreach ($k in @('type','scope','bound_revision','status')) {
    if (-not ($a.PSObject.Properties.Name -contains $k)) { $result.schema_errors += "pending_authority entry missing '$k'" }
  }
  if ($a.type -and ($authorityTypes -notcontains $a.type)) { $result.schema_errors += "authority: invalid type '$($a.type)'" }
  if ($a.status -and ($authorityStatuses -notcontains $a.status)) { $result.schema_errors += "authority: invalid status '$($a.status)'" }
}
foreach ($b in @($m.blockers)) {
  if ($null -eq $b) { continue }
  foreach ($k in @('reason','blocked_state','evidence','unblock_condition','independent_work_remaining','resume_action')) {
    if (-not ($b.PSObject.Properties.Name -contains $k)) { $result.schema_errors += "blocker entry missing '$k'" }
  }
}
$result.schema_ok = ($result.schema_errors.Count -eq 0)
$result.primary_state = $m.primary_state
$result.execution_status = $m.execution_status
$result.next_safe_action = $m.next_safe_action

$currentBranch = Invoke-Git -GitArgs @('branch','--show-current')
$head = Invoke-Git -GitArgs @('rev-parse','HEAD')
$porcelain = Invoke-Git -GitArgs @('status','--porcelain','--untracked-files=no')

$result.branch_match = ($currentBranch -eq $m.working_branch)
$result.revision_match = ($head -eq $m.verified_revision)
$result.working_tree_clean = [string]::IsNullOrWhiteSpace($porcelain)

if (-not $result.branch_match) { $result.drift_reasons += "branch mismatch: manifest '$($m.working_branch)' vs actual '$currentBranch'" }
if (-not $result.revision_match) { $result.drift_reasons += "revision mismatch: manifest '$($m.verified_revision)' vs actual '$head'" }
if (-not $result.working_tree_clean) { $result.drift_reasons += 'unexpected tracked modifications in the working tree' }
if ($m.verified_revision -and -not (Test-GitRevisionExists $m.verified_revision)) {
  $result.drift_reasons += "missing referenced revision: $($m.verified_revision)"
}

if (-not $result.schema_ok) {
  $result.drift = 'INVALID_MANIFEST'
} elseif ($result.drift_reasons.Count -gt 0) {
  $result.drift = 'STATE_DRIFT'
} else {
  $result.drift = 'CONSISTENT'
}

switch ($m.execution_status) {
  'ACTIVE'                   { $result.resume_guidance = 'Verify evidence freshness, then resume from the earliest state whose requirements are not yet proven.' }
  'PAUSED'                   { $result.resume_guidance = 'Re-verify repository and evidence reality before resuming deliberately.' }
  'BLOCKED'                  { $result.resume_guidance = 'Resolve or confirm the blocker; independent safe work may continue.' }
  'WAITING_AUTHORITY'        { $result.resume_guidance = 'Do not perform the gated action; present a bounded, scoped authority request and wait.' }
  'WAITING_PRODUCT_DECISION' { $result.resume_guidance = 'Present options and the specific decision needed; do not choose for the Product Owner.' }
  'UNKNOWN_EXECUTION_STATE'  { $result.resume_guidance = 'Observe current reality before any retry (inspect remote / hardware / process state); do not blindly retry.' }
  default                    { $result.resume_guidance = 'Unknown execution_status; re-derive workflow state from evidence before acting.' }
}
if ($result.drift -ne 'CONSISTENT') {
  $result.resume_guidance += ' Do not blindly overwrite the manifest to match Git; classify the difference first.'
}

Write-Result $result
exit 0
