[CmdletBinding()]
param(
    [string] $GlobalJsonPath = (Join-Path $PSScriptRoot '../global.json'),
    [ValidateSet('current')]
    [string] $Lane = 'current',
    [object] $ReleaseMetadata,
    [string] $GitHubOutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$baseline = (Get-Content -LiteralPath $GlobalJsonPath -Raw | ConvertFrom-Json).sdk.version
if ($baseline -notmatch '^(\d+)\.(\d+)\.(\d{3})$') {
    throw "Expected a stable SDK baseline, got '$baseline'."
}
$major = $Matches[1]
$minor = $Matches[2]
$featureBand = [int] [Math]::Floor([int] $Matches[3] / 100)
$minimum = [version] $baseline

if ($null -eq $ReleaseMetadata) {
    $ReleaseMetadata = Invoke-RestMethod "https://raw.githubusercontent.com/dotnet/core/main/release-notes/$major.$minor/releases.json"
}
$metadata = $ReleaseMetadata | ConvertTo-Json -Depth 30 | ConvertFrom-Json -AsHashtable
if ($metadata['channel-version'] -ne "$major.$minor" -or @($metadata['releases']).Count -eq 0) {
    throw 'Release metadata must contain the expected channel and a nonempty releases array.'
}

$versions = @(
    foreach ($release in $metadata['releases']) {
        $sdks = if ($release.ContainsKey('sdks')) { @($release['sdks']) } else { @($release['sdk']) }
        if ($sdks.Count -eq 0) { throw 'Release metadata contains a release without SDKs.' }
        foreach ($sdk in $sdks) {
            if ($null -eq $sdk -or $sdk['version'] -notmatch '^\d+\.\d+\.\d{3}(-[0-9A-Za-z.-]+)?$') {
                throw 'Release metadata contains an invalid SDK version.'
            }
            $sdk['version']
        }
    }
)
if ($baseline -notin $versions) { throw "Release metadata is missing the SDK baseline $baseline." }
$stable = @($versions | Where-Object {
    $_ -notmatch '-' -and
    ([version] $_).Major -eq [int] $major -and
    ([version] $_).Minor -eq [int] $minor -and
    [int] [Math]::Floor(([version] $_).Build / 100) -eq $featureBand -and
    ([version] $_) -ge $minimum
} | Sort-Object { [version] $_ } -Descending -Unique)
$selected = $stable[0]
$environments = @('net10', 'ga', 'ga-side-by-side-net10', 'ga-side-by-side-net11')
$result = [ordered]@{
    'baseline-sdk' = $baseline
    'sdk-version' = $selected
    'ga-sdk' = $selected
    'phase' = 'ga'
    'lanes' = '["current"]'
    'compatibility-environments' = (ConvertTo-Json -InputObject $environments -Compress)
}
Write-Host "SDK lane ${Lane}: $selected (GA minimum: $baseline)"
if ($GitHubOutputPath) {
    foreach ($entry in $result.GetEnumerator()) {
        "$($entry.Key)=$($entry.Value)" | Out-File -LiteralPath $GitHubOutputPath -Append -Encoding utf8
    }
}
[pscustomobject] $result
