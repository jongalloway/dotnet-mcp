[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [ValidateSet('net10.0', 'net11.0')]
    [string[]] $Framework = @('net10.0', 'net11.0'),

    [switch] $NoRestore,
    [switch] $NoBuild,
    [switch] $SkipServerJsonValidation,

    # Override test filters if needed.
    [string] $ScenarioTestFilterNamespace = '*DotNetMcp.Tests.Scenarios*',
    [string] $ReleaseScenarioTestFilterNamespace = '*DotNetMcp.Tests.ReleaseScenarios*'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Checked([string] $FilePath, [string[]] $Arguments) {
    Write-Host "> $FilePath $($Arguments -join ' ')"
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $repoRoot

try {
    # Ensure these tests are opted-in.
    $env:DOTNET_MCP_SCENARIO_TESTS = '1'
    $env:DOTNET_MCP_RELEASE_SCENARIO_TESTS = '1'

    if (-not $NoRestore) {
        Invoke-Checked 'dotnet' @('restore', 'DotNetMcp.slnx')
    }

    if (-not $NoBuild) {
        $buildArgs = @('build', 'DotNetMcp.slnx', '--configuration', $Configuration)
        if ($NoRestore) { $buildArgs += '--no-restore' }
        Invoke-Checked 'dotnet' $buildArgs
    }

    if (-not $SkipServerJsonValidation) {
        Invoke-Checked 'pwsh' @('-File', 'scripts/validate-server-json.ps1')
    }

    $baseTestArgs = @(
        'test',
        '--solution', 'DotNetMcp.slnx',
        '--configuration', $Configuration,
        '--verbosity', 'normal'
    )

    if ($NoBuild) { $baseTestArgs += '--no-build' }
    if ($NoRestore) { $baseTestArgs += '--no-restore' }

    # Tool installs share SDK caches, so do not run the two frameworks concurrently.
    foreach ($targetFramework in $Framework) {
        $frameworkTestArgs = $baseTestArgs + @('--framework', $targetFramework)
        Invoke-Checked 'dotnet' ($frameworkTestArgs + @('--', '--filter-namespace', $ScenarioTestFilterNamespace))
        Invoke-Checked 'dotnet' ($frameworkTestArgs + @('--', '--filter-namespace', $ReleaseScenarioTestFilterNamespace))
    }
}
finally {
    Pop-Location
}
