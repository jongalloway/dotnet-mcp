#!/usr/bin/env pwsh

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [switch] $CreateGitHubRelease,

    [switch] $Draft,

    [string] $NotesFile,

    [switch] $GenerateNotes
)

function Fail([string] $message)
{
    throw $message
}

function Invoke-Git([Parameter(Mandatory = $true)][string[]] $Args)
{
    & git @Args
    if ($LASTEXITCODE -ne 0)
    {
        Fail ("git failed: git " + ($Args -join ' '))
    }
}

function Invoke-Gh([Parameter(Mandatory = $true)][string[]] $Args)
{
    & gh @Args
    if ($LASTEXITCODE -ne 0)
    {
        Fail ("gh failed: gh " + ($Args -join ' '))
    }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue))
{
    Fail "git is required but was not found on PATH."
}

if ($CreateGitHubRelease -and -not (Get-Command gh -ErrorAction SilentlyContinue))
{
    Fail "gh is required for -CreateGitHubRelease but was not found on PATH."
}

if ($CreateGitHubRelease)
{
    if ($GenerateNotes -and -not [string]::IsNullOrWhiteSpace($NotesFile))
    {
        Fail "Specify either -GenerateNotes or -NotesFile (not both)."
    }

    if (-not $GenerateNotes -and [string]::IsNullOrWhiteSpace($NotesFile))
    {
        Write-Host "No release notes option specified; defaulting to -GenerateNotes." -ForegroundColor DarkGray
        $GenerateNotes = $true
    }
}

$normalizedVersion = $Version.Trim()
if (-not $normalizedVersion.StartsWith('v'))
{
    $normalizedVersion = 'v' + $normalizedVersion
}

if ($normalizedVersion -notmatch '^v\d+\.\d+\.\d+([-.][0-9A-Za-z][0-9A-Za-z.-]*)?$')
{
    Fail "Version '$Version' does not look like a semver tag. Expected like v1.2.3 or v1.2.3-rc.1"
}

Write-Host "Cutting release tag $normalizedVersion" -ForegroundColor Cyan

Invoke-Git @('fetch', '--tags', 'origin')

# Ensure we're on main and up-to-date
$branch = (& git rev-parse --abbrev-ref HEAD).Trim()
if ($LASTEXITCODE -ne 0)
{
    Fail "Unable to determine current branch."
}

if ($branch -ne 'main')
{
    Fail "Refusing to tag from branch '$branch'. Switch to 'main' first."
}

Invoke-Git @('pull', '--ff-only')

# Require clean working tree
$statusPorcelain = (& git status --porcelain).Trim()
if ($LASTEXITCODE -ne 0)
{
    Fail "Unable to read git status."
}

if (-not [string]::IsNullOrWhiteSpace($statusPorcelain))
{
    Fail "Working tree is not clean. Commit/stash changes before cutting a release."
}

# Refuse if tag already exists
& git rev-parse -q --verify "refs/tags/$normalizedVersion" | Out-Null
if ($LASTEXITCODE -eq 0)
{
    Fail "Tag '$normalizedVersion' already exists."
}

$head = (& git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($head))
{
    Fail "Unable to determine HEAD commit."
}

Write-Host "Will tag main at $head" -ForegroundColor DarkGray

if ($PSCmdlet.ShouldProcess("origin/$normalizedVersion", "Create annotated tag and push"))
{
    Invoke-Git @('tag', '-a', $normalizedVersion, '-m', $normalizedVersion)

    Write-Host "Verifying tag containment..." -ForegroundColor DarkGray
    $contains = (& git branch -r --contains $normalizedVersion) | ForEach-Object { $_.Trim() }
    if ($LASTEXITCODE -ne 0)
    {
        Fail "Unable to verify branch containment for '$normalizedVersion'."
    }

    if ($contains -notcontains 'origin/main')
    {
        Fail "Safety check failed: '$normalizedVersion' is not contained in origin/main. Refusing to push."
    }

    Invoke-Git @('push', 'origin', $normalizedVersion)
}

if ($CreateGitHubRelease)
{
    $releaseArgs = @('release', 'create', $normalizedVersion)

    if ($Draft)
    {
        $releaseArgs += '--draft'
    }

    if ($GenerateNotes)
    {
        $releaseArgs += '--generate-notes'
    }
    elseif (-not [string]::IsNullOrWhiteSpace($NotesFile))
    {
        $releaseArgs += @('--notes-file', $NotesFile)
    }

    if ($PSCmdlet.ShouldProcess("GitHub release $normalizedVersion", "Create release"))
    {
        Invoke-Gh $releaseArgs
    }
}

Write-Host "Done." -ForegroundColor Green