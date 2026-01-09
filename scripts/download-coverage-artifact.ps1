<#
.SYNOPSIS
Downloads the GitHub Actions Cobertura coverage artifact and prints a quick summary.

.DESCRIPTION
This repo's CI workflow uploads a coverage artifact named "coverage-cobertura" containing
"coverage.cobertura.xml". This script downloads that artifact from a specific workflow
run (or the latest successful run) and prints overall line/branch rates plus the lowest-
covered source files.

Requires GitHub CLI (gh) and authentication ("gh auth login").

.PARAMETER RunId
GitHub Actions run ID (e.g. 20865330584).

.PARAMETER PullRequest
Pull request number. If provided, the script will locate the latest workflow run for the PR's
head SHA and download the coverage artifact from that run.

.PARAMETER NoBaseCompare
When using -PullRequest, disables downloading and comparing against the latest successful run
of the base branch (defaults to -Branch, typically 'main').

.PARAMETER Workflow
Workflow file name or workflow name to query. Defaults to "build.yml".

.PARAMETER Branch
Branch to query when RunId is not provided. Defaults to "main".

.PARAMETER ArtifactName
Artifact name to download. Defaults to "coverage-cobertura".

.PARAMETER Repo
GitHub repository in "owner/name" form. If omitted, attempts to infer from git remote.

.PARAMETER OutDir
Directory to place downloaded artifacts. Defaults to "artifacts/coverage".

.EXAMPLE
pwsh -File scripts/download-coverage-artifact.ps1

.EXAMPLE
pwsh -File scripts/download-coverage-artifact.ps1 -RunId 20865330584

.EXAMPLE
pwsh -File scripts/download-coverage-artifact.ps1 -PullRequest 285
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false)]
  [long]$RunId,

  [Parameter(Mandatory = $false)]
  [int]$PullRequest,

  [Parameter(Mandatory = $false)]
  [switch]$NoBaseCompare,

  [Parameter(Mandatory = $false)]
  [string]$Workflow = "build.yml",

  [Parameter(Mandatory = $false)]
  [string]$Branch = "main",

  [Parameter(Mandatory = $false)]
  [string]$ArtifactName = "coverage-cobertura",

  [Parameter(Mandatory = $false)]
  [string]$Repo,

  [Parameter(Mandatory = $false)]
  [string]$OutDir = "artifacts/coverage"
)

$ErrorActionPreference = 'Stop'

function Write-Info([string]$message) {
  Write-Host $message
}

function Require-Command([string]$name) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
    throw "Required command '$name' not found on PATH. Install it and try again."
  }
}

function Infer-RepoFromGitRemote() {
  try {
    $remote = (git config --get remote.origin.url 2>$null)
    if ([string]::IsNullOrWhiteSpace($remote)) {
      return $null
    }

    # Supports:
    # - https://github.com/owner/repo.git
    # - git@github.com:owner/repo.git
    $remote = $remote.Trim()

    if ($remote -match '^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?$') {
      return "$($Matches.owner)/$($Matches.repo)"
    }

    if ($remote -match '^git@github\.com:(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?$') {
      return "$($Matches.owner)/$($Matches.repo)"
    }

    return $null
  }
  catch {
    return $null
  }
}

function Get-LatestSuccessfulRunId([string]$repo, [string]$workflow, [string]$branch) {
  $json = gh run list -R $repo -w $workflow -b $branch -s success -L 1 --json databaseId,url,displayTitle,headSha,updatedAt 2>$null
  if ([string]::IsNullOrWhiteSpace($json)) {
    throw "Unable to query workflow runs. Ensure 'gh auth status' succeeds and you have access to $repo."
  }

  $runs = $json | ConvertFrom-Json
  if (-not $runs -or $runs.Count -eq 0) {
    throw "No successful runs found for workflow '$workflow' on branch '$branch' in $repo."
  }

  return [long]$runs[0].databaseId
}

function Get-RunCandidatesForPullRequest([string]$repo, [string]$workflow, [int]$pullRequest) {
  $json = gh pr view $pullRequest -R $repo --json number,headRefName,headRefOid,state,url 2>$null
  if ([string]::IsNullOrWhiteSpace($json)) {
    throw "Unable to query PR #$pullRequest. Ensure you have access to $repo and that the PR exists."
  }

  $pr = $json | ConvertFrom-Json
  if (-not $pr -or -not $pr.headRefOid) {
    throw "Unable to determine head SHA for PR #$pullRequest."
  }

  Write-Info "PR #$($pr.number) ($($pr.state)) head=$($pr.headRefName) sha=$($pr.headRefOid)"

  # gh doesn't offer a single built-in command to fetch "the run that produced artifact X".
  # We'll pull recent runs for this workflow and match on head SHA.
  $runsJson = gh run list -R $repo -w $workflow -L 100 --json databaseId,headSha,headBranch,event,status,conclusion,createdAt,updatedAt,url 2>$null
  if ([string]::IsNullOrWhiteSpace($runsJson)) {
    throw "Unable to query workflow runs. Ensure 'gh auth status' succeeds and you have access to $repo."
  }

  $runs = $runsJson | ConvertFrom-Json
  if (-not $runs) {
    throw "No runs found for workflow '$workflow' in $repo."
  }

  $shaMatches = @($runs | Where-Object { $_.headSha -eq $pr.headRefOid -and $_.event -eq 'pull_request' })
  if ($shaMatches.Count -gt 0) {
    return $shaMatches | Sort-Object updatedAt -Descending
  }

  # Fallback: match by branch name (useful if the run list doesn't include the exact SHA).
  $branchMatches = @($runs | Where-Object { $_.headBranch -eq $pr.headRefName -and $_.event -eq 'pull_request' })
  if ($branchMatches.Count -gt 0) {
    return $branchMatches | Sort-Object updatedAt -Descending
  }

  throw "No matching workflow runs found for PR #$pullRequest (workflow '$workflow')."
}

function Download-CoverageArtifactForRun([long]$runId, [string]$repo, [string]$artifactName, [string]$outPath) {
  try {
    $null = gh run download $runId -R $repo -n $artifactName -D $outPath 2>$null
    return $true
  }
  catch {
    return $false
  }
}

function Get-CoverageSummary([string]$coverageFile, [string]$repoRoot) {
  [xml]$xml = Get-Content -Path $coverageFile

  $lineRate = [double]$xml.coverage.'line-rate'
  $branchRate = [double]$xml.coverage.'branch-rate'

  Write-Info "Overall (Cobertura): line-rate=$([math]::Round($lineRate * 100, 2))% branch-rate=$([math]::Round($branchRate * 100, 2))%"

  $classes = @($xml.coverage.packages.package.classes.class)

  # Aggregate by filename (Cobertura can repeat filenames across multiple <class> nodes).
  $byFile = @{}
  foreach ($c in $classes) {
    $file = [string]$c.filename
    if ([string]::IsNullOrWhiteSpace($file)) { continue }

    # Skip generated/obj output.
    if ($file -match '[\\/]obj[\\/]') { continue }

    $lines = @($c.lines.line)
    $total = $lines.Count
    if ($total -le 0) { continue }

    $covered = @($lines | Where-Object { [int]$_.hits -gt 0 }).Count

    if (-not $byFile.ContainsKey($file)) {
      $byFile[$file] = [pscustomobject]@{ File = $file; Total = 0; Covered = 0 }
    }

    $byFile[$file].Total += $total
    $byFile[$file].Covered += $covered
  }

  $stats = $byFile.Values | ForEach-Object {
    $rate = if ($_.Total -gt 0) { $_.Covered / $_.Total } else { 0 }
    $relative = $_.File
    if (-not [string]::IsNullOrWhiteSpace($repoRoot)) {
      $normalizedRoot = [System.IO.Path]::GetFullPath($repoRoot)
      try {
        $full = [System.IO.Path]::GetFullPath($_.File)
        if ($full.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
          $relative = $full.Substring($normalizedRoot.Length).TrimStart('\','/')
        }
      }
      catch {
        # Best-effort only
      }
    }

    [pscustomobject]@{ LineRate = $rate; Covered = $_.Covered; Total = $_.Total; File = $relative }
  }

  Write-Info ""
  Write-Info "Lowest-covered files (excluding obj/):"
  $stats |
    Sort-Object LineRate, Total |
    Select-Object -First 15 |
    ForEach-Object {
      " - {0,6:P1}  {1,6}/{2,-6}  {3}" -f $_.LineRate, $_.Covered, $_.Total, $_.File
    } | Write-Host

  return [pscustomobject]@{
    LineRate = $lineRate
    BranchRate = $branchRate
  }
}

function Find-CoverageFile([string]$outPath) {
  $coverageFile = Join-Path $outPath 'coverage.cobertura.xml'
  if (Test-Path $coverageFile) {
    return $coverageFile
  }

  $found = Get-ChildItem -Path $outPath -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($found) {
    return $found.FullName
  }

  return $null
}

# --- Main ---
Require-Command gh

if ([string]::IsNullOrWhiteSpace($Repo)) {
  $Repo = Infer-RepoFromGitRemote
}

if ([string]::IsNullOrWhiteSpace($Repo)) {
  throw "Unable to infer repo. Provide -Repo in 'owner/name' form (e.g. jongalloway/dotnet-mcp)."
}

if ($RunId -and $PullRequest) {
  throw "Specify only one of -RunId or -PullRequest."
}

if ($PullRequest -and $PullRequest -le 0) {
  throw "-PullRequest must be a positive integer."
}

if ($RunId -and $RunId -le 0) {
  throw "-RunId must be a positive integer."
}

$repoRoot = (Resolve-Path -Path .).Path
$outRoot = Join-Path $repoRoot $OutDir

$compareToBase = $false

if ($PullRequest) {
  Write-Info "PullRequest provided; locating workflow run for PR #$PullRequest..."
  $candidates = Get-RunCandidatesForPullRequest -repo $Repo -workflow $Workflow -pullRequest $PullRequest
  foreach ($candidate in $candidates) {
    $candidateRunId = [long]$candidate.databaseId
    $outPath = Join-Path $outRoot "run-$candidateRunId"
    New-Item -ItemType Directory -Force -Path $outPath | Out-Null

    Write-Info "Trying run $candidateRunId ($($candidate.conclusion))..."
    if (Download-CoverageArtifactForRun -runId $candidateRunId -repo $Repo -artifactName $ArtifactName -outPath $outPath) {
      $RunId = $candidateRunId
      break
    }
  }

  if (-not $RunId) {
    throw "Unable to download artifact '$ArtifactName' for PR #$PullRequest. The workflow may not have produced artifacts (e.g. failed before upload)."
  }

  $compareToBase = (-not $NoBaseCompare)
}
else {
  if (-not $RunId) {
    Write-Info "RunId not provided; finding latest successful run for '$Workflow' on '$Branch'..."
    $RunId = Get-LatestSuccessfulRunId -repo $Repo -workflow $Workflow -branch $Branch
  }

  $outPath = Join-Path $outRoot "run-$RunId"
  New-Item -ItemType Directory -Force -Path $outPath | Out-Null

  Write-Info "Downloading artifact '$ArtifactName' from run $RunId ($Repo) to '$OutPath'..."
  if (-not (Download-CoverageArtifactForRun -runId $RunId -repo $Repo -artifactName $ArtifactName -outPath $outPath)) {
    throw "Failed to download artifact '$ArtifactName' from run $RunId."
  }
}

$coverageFile = Find-CoverageFile -outPath $outPath
if ([string]::IsNullOrWhiteSpace($coverageFile) -or -not (Test-Path $coverageFile)) {
  throw "Downloaded artifact, but coverage.cobertura.xml was not found under '$outPath'."
}

Write-Info "Found coverage file: $coverageFile"
$prSummary = Get-CoverageSummary -coverageFile $coverageFile -repoRoot $repoRoot

if ($compareToBase) {
  Write-Info ""
  Write-Info "Downloading baseline coverage for '$Branch' (latest successful run)..."

  $baseRunId = Get-LatestSuccessfulRunId -repo $Repo -workflow $Workflow -branch $Branch
  $baseOutPath = Join-Path (Join-Path $outRoot ("base-" + $Branch)) ("run-" + $baseRunId)
  New-Item -ItemType Directory -Force -Path $baseOutPath | Out-Null

  if (-not (Download-CoverageArtifactForRun -runId $baseRunId -repo $Repo -artifactName $ArtifactName -outPath $baseOutPath)) {
    Write-Info "Warning: Failed to download baseline artifact '$ArtifactName' from run $baseRunId."
  }
  else {
    $baseCoverageFile = Find-CoverageFile -outPath $baseOutPath
    if ([string]::IsNullOrWhiteSpace($baseCoverageFile) -or -not (Test-Path $baseCoverageFile)) {
      Write-Info "Warning: Downloaded baseline artifact, but coverage.cobertura.xml was not found under '$baseOutPath'."
    }
    else {
      Write-Info ""
      Write-Info "Baseline coverage file: $baseCoverageFile"
      $baseSummary = Get-CoverageSummary -coverageFile $baseCoverageFile -repoRoot $repoRoot

      $lineDelta = ($prSummary.LineRate - $baseSummary.LineRate) * 100
      $branchDelta = ($prSummary.BranchRate - $baseSummary.BranchRate) * 100

      Write-Info ""
      Write-Info ("Delta vs '{0}' (percentage points): line={1:N2}pp branch={2:N2}pp" -f $Branch, $lineDelta, $branchDelta)
    }
  }
}

Write-Info ""
Write-Info "Done. Coverage artifact saved at: $outPath"
