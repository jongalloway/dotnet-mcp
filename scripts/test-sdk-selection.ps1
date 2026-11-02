[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$resolver = Join-Path $PSScriptRoot 'resolve-dotnet-sdk.ps1'
$baseline = (Get-Content (Join-Path $PSScriptRoot '../global.json') -Raw | ConvertFrom-Json).sdk.version

function New-Metadata([string[]] $Versions) {
    @{
        'channel-version' = '11.0'
        releases = @(@{ sdks = @($Versions | ForEach-Object { @{ version = $_ } }) })
    }
}
function Assert-Equal($Actual, $Expected) {
    if ($Actual -cne $Expected) { throw "Expected '$Expected', got '$Actual'." }
}
function Assert-Rejected($Metadata) {
    $rejected = $false
    try { $null = & $resolver -ReleaseMetadata $Metadata } catch { $rejected = $true }
    if (-not $rejected) { throw 'Invalid release metadata was accepted.' }
}

$rc3 = & $resolver -ReleaseMetadata (New-Metadata @($baseline))
Assert-Equal $rc3.'sdk-version' $baseline
Assert-Equal $rc3.lanes '["current"]'
Assert-Equal $rc3.phase 'rc3'
Assert-Equal (@($rc3.'compatibility-environments' | ConvertFrom-Json).Count) 4

$metadata = New-Metadata @($baseline, '11.0.100', '11.0.109', '11.0.110', '11.0.200', '12.0.100', '11.0.199-preview.1')
$ga = & $resolver -ReleaseMetadata $metadata
Assert-Equal $ga.'sdk-version' '11.0.110'
Assert-Equal $ga.phase 'ga'
Assert-Equal $ga.lanes '["current","rc3"]'
Assert-Equal (@($ga.'compatibility-environments' | ConvertFrom-Json).Count) 7
$exact = & $resolver -ReleaseMetadata $metadata -Lane rc3
Assert-Equal $exact.'sdk-version' $baseline
Assert-Equal $exact.'ga-sdk' '11.0.110'
$preview = & $resolver -ReleaseMetadata (New-Metadata @($baseline, '11.0.100-rc.4.12345.1', '11.0.200'))
Assert-Equal $preview.'sdk-version' $baseline
$singleSdk = & $resolver -ReleaseMetadata @{
    'channel-version' = '11.0'
    releases = @(@{ sdk = @{ version = $baseline } }, @{ sdk = @{ version = '11.0.100' } })
}
Assert-Equal $singleSdk.'sdk-version' '11.0.100'
Assert-Rejected @{}
Assert-Rejected @{ 'channel-version' = '12.0'; releases = @(@{ sdk = @{ version = $baseline } }) }
Assert-Rejected @{ 'channel-version' = '11.0'; releases = @() }
Assert-Rejected @{ 'channel-version' = '11.0'; releases = @(@{}) }
Assert-Rejected (New-Metadata @($baseline, 'not-a-version'))
Assert-Rejected (New-Metadata @('11.0.100'))
Write-Host 'SDK selection fixtures passed.'
