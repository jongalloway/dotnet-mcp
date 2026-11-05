[CmdletBinding()]
param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\native\win-x64'),
    [switch] $UseEnvironmentalTools
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsWindows) { throw 'The native publishing target currently supports Windows x64 only.' }
if ([Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -ne 'X64') {
    throw 'Run native publishing from an x64 PowerShell process.'
}

$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\DotNetMcp\DotNetMcp.csproj'))
$artifacts = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\artifacts\native-build'))
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$arguments = @('publish', $project, '-c', 'Release', '-f', 'net11.0', '-r', 'win-x64',
    '-p:PublishAot=true', '-p:NativeDebugSymbols=false',
    '--artifacts-path', $artifacts, '--output', $OutputDirectory, '--nologo')
if ($UseEnvironmentalTools) {
    if (-not (Get-Command link.exe -ErrorAction SilentlyContinue) -or -not $env:LIB) {
        throw 'UseEnvironmentalTools requires the Windows linker on PATH and its CRT/Windows SDK libraries in LIB.'
    }
    $arguments += '-p:IlcUseEnvironmentalTools=true'
}

& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Native publish failed with exit code $LASTEXITCODE." }

$executable = Join-Path $OutputDirectory 'DotNetMcp.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Native publish did not produce the expected executable: $executable"
}
foreach ($file in @('DotNetMcp.SdkHelper.dll', 'DotNetMcp.SdkHelper.runtimeconfig.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $OutputDirectory "sdk-helper\$file") -PathType Leaf)) {
        throw "The native bundle is missing its SDK helper file: $file"
    }
}
$stream = [IO.File]::OpenRead($executable)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
try {
    if ($pe.PEHeaders.CorHeader -or $pe.PEHeaders.CoffHeader.Machine -ne 'Amd64') {
        throw 'Published executable is not a native Windows x64 PE.'
    }
}
finally { $pe.Dispose(); $stream.Dispose() }

$identityText = & $executable --native-info
if ($LASTEXITCODE -ne 0) { throw 'Published executable failed its runtime identity check.' }
$identity = $identityText | ConvertFrom-Json
if ($identity.DynamicCodeSupported -ne $false -or $identity.ProcessArchitecture -ne 'X64' -or
    $identity.FrameworkDescription -notlike '.NET 11.*') {
    throw "Unexpected native runtime identity: $identityText"
}
Write-Host "Verified native executable: $executable"
