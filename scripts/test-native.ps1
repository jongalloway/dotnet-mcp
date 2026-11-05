[CmdletBinding()]
param(
    [string] $NativeDirectory = (Join-Path $PSScriptRoot '..\artifacts\native\win-x64'),
    [switch] $SkipReleaseScenarios
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsWindows) { throw 'Native distribution validation currently requires Windows x64.' }
$NativeDirectory = (Resolve-Path -LiteralPath $NativeDirectory).Path
$relocated = Join-Path ([IO.Path]::GetTempPath()) "dotnet-mcp-native-validation-$([guid]::NewGuid().ToString('N'))"
$names = @('DOTNET_MCP_TEST_SERVER_PATH', 'DOTNET_MCP_SCENARIO_TESTS', 'DOTNET_MCP_RELEASE_SCENARIO_TESTS')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name) }
Push-Location (Join-Path $PSScriptRoot '..')
try {
    Copy-Item -LiteralPath $NativeDirectory -Destination $relocated -Recurse
    $executable = Join-Path $relocated 'DotNetMcp.exe'
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $relocated 'sdk-helper\DotNetMcp.SdkHelper.dll') -PathType Leaf)) {
        throw "The native bundle is incomplete: $NativeDirectory"
    }
    $env:DOTNET_MCP_TEST_SERVER_PATH = $executable
    $env:DOTNET_MCP_SCENARIO_TESTS = '1'
    $env:DOTNET_MCP_RELEASE_SCENARIO_TESTS = '1'
    dotnet build (Join-Path (Get-Location) 'DotNetMcp\DotNetMcp.csproj') -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Managed reference builds failed.' }
    dotnet test --solution DotNetMcp.slnx -c Release --framework net11.0 -- `
        --filter-class '*NativeDistributionTests' --filter-class '*McpConformanceTests' `
        --filter-class '*McpAppsMetaWireFormatTest'
    if ($LASTEXITCODE -ne 0) { throw 'Native identity, discovery parity, or MCP conformance failed.' }
    if (-not $SkipReleaseScenarios) {
        & (Join-Path $PSScriptRoot 'run-release-gate-tests.ps1') -Framework net11.0 -NoBuild -NoRestore
    }
}
finally {
    Pop-Location
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
    if (Test-Path -LiteralPath $relocated) { Remove-Item -LiteralPath $relocated -Recurse -Force }
}
