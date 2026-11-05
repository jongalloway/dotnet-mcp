[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackagePath,
    [Parameter(Mandatory)]
    [string] $DotnetRoot,
    [Parameter(Mandatory)]
    [ValidateSet('net10.0', 'net11.0')]
    [string] $ExpectedFramework,
    [Parameter(Mandatory)]
    [string] $ProjectSdkVersion,
    [Parameter(Mandatory)]
    [string] $ExpectedRuntimeVersion,
    [string] $OutputDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) "dotnet-mcp-compatibility-$([guid]::NewGuid().ToString('N'))"),
    [ValidateRange(1, 600)]
    [int] $TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$PackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$DotnetRoot = (Resolve-Path -LiteralPath $DotnetRoot).Path
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory must be new so tool caches cannot affect selection: $OutputDirectory"
}
$OutputDirectory = [System.IO.Directory]::CreateDirectory($OutputDirectory).FullName
$dotnet = Join-Path $DotnetRoot $(if ($IsWindows) { 'dotnet.exe' } else { 'dotnet' })
if (-not (Test-Path -LiteralPath $dotnet)) { throw "dotnet host not found: $dotnet" }

$archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
    if ($archive.Entries.FullName -match '(^|/)sdk-helper/|DotNetMcp\.SdkHelper') {
        throw 'The managed tool package must not include the native-only SDK helper.'
    }
    $nuspec = @($archive.Entries | Where-Object FullName -Like '*.nuspec')
    if ($nuspec.Count -ne 1) { throw 'Expected exactly one package manifest.' }
    $reader = [System.IO.StreamReader]::new($nuspec[0].Open())
    try { [xml] $manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $packageId = $manifest.package.metadata.id
    $version = $manifest.package.metadata.version
    if ($packageId -ne 'Community.Mcp.DotNet') { throw "Unexpected package: $packageId" }
    foreach ($tfm in @('net10.0', 'net11.0')) {
        $entry = $archive.GetEntry("tools/$tfm/any/DotNetMcp.runtimeconfig.json")
        if ($null -eq $entry) { throw "Package is missing $tfm runtimeconfig." }
        $reader = [System.IO.StreamReader]::new($entry.Open())
        try { $options = ($reader.ReadToEnd() | ConvertFrom-Json).runtimeOptions } finally { $reader.Dispose() }
        if ($options.tfm -ne $tfm -or $options.rollForward -ne 'Minor') {
            throw "Unexpected runtime contract for ${tfm}: $($options | ConvertTo-Json -Compress)"
        }
        $expectedFloor = if ($tfm -eq 'net10.0') { '10.0.0' } else { '11.0.0-rc.3.26522.148' }
        if ($options.framework.version -ne $expectedFloor) { throw "Unexpected runtime floor for $tfm" }
        if ($null -eq $archive.GetEntry("tools/$tfm/any/DotnetToolSettings.xml")) {
            throw "Package is missing $tfm tool settings."
        }
    }
}
finally { $archive.Dispose() }

$environment = @{
    DOTNET_ROOT = $DotnetRoot
    DOTNET_ROOT_X64 = $DotnetRoot
    DOTNET_ROOT_ARM64 = $DotnetRoot
    DOTNET_HOST_PATH = $dotnet
    DOTNET_MULTILEVEL_LOOKUP = '0'
    DOTNET_CLI_HOME = (Join-Path $OutputDirectory 'home')
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_NOLOGO = '1'
    NUGET_PACKAGES = (Join-Path $OutputDirectory 'packages')
    NUGET_HTTP_CACHE_PATH = (Join-Path $OutputDirectory 'http-cache')
    PATH = "$DotnetRoot$([System.IO.Path]::PathSeparator)$env:PATH"
}
$work = [System.IO.Directory]::CreateDirectory((Join-Path $OutputDirectory 'work')).FullName
$feed = [System.IO.Directory]::CreateDirectory((Join-Path $OutputDirectory 'feed')).FullName
Copy-Item -LiteralPath $PackagePath -Destination $feed
# Consumer tests deliberately override the checkout's RC3 SDK pin and package sources.
$toolMajor = $ExpectedFramework.Substring(3).Split('.')[0]
$toolSdks = @(Get-ChildItem (Join-Path $DotnetRoot 'sdk') -Directory | Where-Object Name -Like "$toolMajor.*")
if ($toolSdks.Count -ne 1) { throw "Expected one .NET $toolMajor SDK in the isolated root." }
@{ sdk = @{ version = $toolSdks[0].Name; rollForward = 'disable'; allowPrerelease = $true } } |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $work 'global.json')
$projectRoot = [System.IO.Directory]::CreateDirectory((Join-Path $work 'projects')).FullName
@{ sdk = @{ version = $ProjectSdkVersion; rollForward = 'disable'; allowPrerelease = $true } } |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $projectRoot 'global.json')
$config = Join-Path $work 'NuGet.Config'
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath $config

function New-ChildProcess([string] $FilePath, [string[]] $Arguments, [string] $TraceFile = '', [string] $WorkingDirectory = $work, [string] $RawArguments = '') {
    $info = [System.Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $FilePath
    $info.WorkingDirectory = $WorkingDirectory
    $info.UseShellExecute = $false
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    if ($RawArguments) { $info.Arguments = $RawArguments }
    else { foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) } }
    foreach ($key in $environment.Keys) { $info.Environment[$key] = $environment[$key] }
    foreach ($key in @('DOTNET_ROLL_FORWARD', 'DOTNET_ROLL_FORWARD_TO_PRERELEASE', 'DOTNET_ADDITIONAL_DEPS',
            'DOTNET_SHARED_STORE', 'DOTNET_STARTUP_HOOKS', 'MSBuildSDKsPath', 'DOTNET_HOST_TRACEFILE', 'COREHOST_TRACEFILE')) {
        $null = $info.Environment.Remove($key)
    }
    $info.Environment['DOTNET_HOST_TRACE'] = $(if ($TraceFile) { '1' } else { '0' })
    $info.Environment['COREHOST_TRACE'] = '0'
    if ($TraceFile) { $info.Environment['DOTNET_HOST_TRACEFILE'] = $TraceFile }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $info
    if (-not $process.Start()) { throw "Could not start $FilePath" }
    return $process
}

function Invoke-Checked([string[]] $Arguments, [string] $WorkingDirectory = $work) {
    $process = New-ChildProcess $dotnet $Arguments -WorkingDirectory $WorkingDirectory
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { throw "Timed out: dotnet $Arguments" }
        $text = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { throw "dotnet $Arguments failed ($($process.ExitCode)):`n$text" }
        return $text
    }
    finally {
        if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
        $process.Dispose()
    }
}

function Invoke-Rpc($Process, [int] $Id, [string] $Method, [hashtable] $Parameters) {
    $message = @{ jsonrpc = '2.0'; id = $Id; method = $Method; params = $Parameters } | ConvertTo-Json -Depth 20 -Compress
    $Process.StandardInput.WriteLine($message)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $read = $Process.StandardOutput.ReadLineAsync()
        $remaining = [int] [Math]::Max(1, ($deadline - [DateTime]::UtcNow).TotalMilliseconds)
        if (-not $read.Wait($remaining)) { throw "Timed out waiting for $Method" }
        $line = $read.GetAwaiter().GetResult()
        if ($null -eq $line) { throw "Server exited before responding to $Method" }
        $response = $line | ConvertFrom-Json -AsHashtable
        if ($response.ContainsKey('id') -and $response.id -eq $Id) {
            if ($response.ContainsKey('error')) { throw "$Method failed: $($response.error | ConvertTo-Json -Compress)" }
            return $response.result
        }
        if ([DateTime]::UtcNow -ge $deadline) { throw "Timed out waiting for $Method" }
    } while ($true)
}

function Invoke-Tool($Process, [int] $Id, [string] $Name, [hashtable] $Arguments) {
    $result = Invoke-Rpc $Process $Id 'tools/call' @{ name = $Name; arguments = $Arguments }
    $text = ($result.content | Where-Object type -EQ 'text' | ForEach-Object text) -join "`n"
    if (($result.ContainsKey('isError') -and $result.isError -eq $true) -or $text -match 'Exit Code: [1-9]|^Error:') {
        throw "${Name} failed: $text"
    }
    return $text
}

$sdkList = Invoke-Checked @('--list-sdks')
$runtimeList = Invoke-Checked @('--list-runtimes')
$sdkList | Set-Content -LiteralPath (Join-Path $OutputDirectory 'sdk-inventory.txt')
$runtimeList | Set-Content -LiteralPath (Join-Path $OutputDirectory 'runtime-inventory.txt')
$selectedProjectSdk = (Invoke-Checked @('--version') $projectRoot).Trim()
if ($selectedProjectSdk -ne $ProjectSdkVersion) {
    throw "Project SDK selection mismatch: expected $ProjectSdkVersion, got $selectedProjectSdk."
}
$selectedToolSdk = (Invoke-Checked @('--version')).Trim()
if ($selectedToolSdk -ne $toolSdks[0].Name) {
    throw "Tool SDK selection mismatch: expected $($toolSdks[0].Name), got $selectedToolSdk."
}
if ($runtimeList -notmatch [regex]::Escape("Microsoft.NETCore.App $ExpectedRuntimeVersion ")) {
    throw "Expected runtime is not installed in the isolated root: $ExpectedRuntimeVersion"
}
$projectFramework = 'net' + $ProjectSdkVersion.Split('.')[0] + '.0'
$scriptSupportsGlobalJsonBypass = [int]$toolSdks[0].Name.Split('.')[0] -ge 11
$results = @()
foreach ($invocation in @('tool', 'dnx', 'dnx-script')) {
    $trace = Join-Path $OutputDirectory "$invocation-host.txt"
    $startupDirectory = $work
    $rawArguments = ''
    if ($invocation -eq 'tool') {
        $toolPath = Join-Path $OutputDirectory 'tools'
        $installOutput = Invoke-Checked @('tool', 'install', "$packageId@$version", '--tool-path', $toolPath,
            '--configfile', $config, '--add-source', $feed, '--verbosity', 'quiet')
        $installOutput | Set-Content -LiteralPath (Join-Path $OutputDirectory 'install.txt')
        $command = Join-Path $toolPath $(if ($IsWindows) { 'dotnet-mcp.exe' } else { 'dotnet-mcp' })
        $arguments = @()
    }
    elseif ($invocation -eq 'dnx') {
        $command = $dotnet
        $arguments = @('dnx', "$packageId@$version", '--yes', '--configfile', $config, '--add-source', $feed)
    }
    else {
        if ($scriptSupportsGlobalJsonBypass) {
            $startupDirectory = [IO.Directory]::CreateDirectory((Join-Path $OutputDirectory 'dnx-script-work')).FullName
            $scriptGlobalJson = Join-Path $startupDirectory 'global.json'
            @{ sdk = @{ version = '99.0.100'; rollForward = 'disable' } } |
                ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $scriptGlobalJson
            $probe = New-ChildProcess $dotnet @('dnx', '--help') -WorkingDirectory $startupDirectory
            try {
                $probeOut = $probe.StandardOutput.ReadToEndAsync()
                $probeError = $probe.StandardError.ReadToEndAsync()
                if (-not $probe.WaitForExit($TimeoutSeconds * 1000)) { throw 'SDK selection probe timed out.' }
                if ($probe.ExitCode -eq 0) { throw 'dotnet dnx unexpectedly bypassed the missing SDK in global.json.' }
                ($probeOut.GetAwaiter().GetResult() + $probeError.GetAwaiter().GetResult()) |
                    Set-Content -LiteralPath (Join-Path $OutputDirectory 'dnx-script-sdk-selection.txt')
            }
            finally {
                if (-not $probe.HasExited) { $probe.Kill($true); $probe.WaitForExit() }
                $probe.Dispose()
            }
        }
        $script = Join-Path $DotnetRoot $(if ($IsWindows) { 'dnx.cmd' } else { 'dnx' })
        if (-not (Test-Path -LiteralPath $script -PathType Leaf)) { throw "dnx script not found: $script" }
        $arguments = @("$packageId@$version", '--yes', '--configfile', $config, '--add-source', $feed)
        if ($IsWindows) {
            $command = $env:ComSpec
            $rawArguments = '/d /s /c ""{0}" "{1}@{2}" --yes --configfile "{3}" --add-source "{4}""' -f $script, $packageId, $version, $config, $feed
            $arguments = @()
        }
        else { $command = $script }
    }
    $process = New-ChildProcess $command $arguments $trace -WorkingDirectory $startupDirectory -RawArguments $rawArguments
    $stderr = $process.StandardError.ReadToEndAsync()
    try {
        $null = Invoke-Rpc $process 1 'initialize' @{
            protocolVersion = '2025-11-25'
            capabilities = @{}
            clientInfo = @{ name = 'package-compatibility'; version = '1.0' }
        }
        $process.StandardInput.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
        $tools = Invoke-Rpc $process 2 'tools/list' @{}
        foreach ($name in @('dotnet_sdk', 'dotnet_project')) {
            if ($name -notin $tools.tools.name) { throw "Missing MCP tool: $name" }
        }
        if ($invocation -eq 'dnx-script' -and $scriptSupportsGlobalJsonBypass) {
            # Startup proved the script bypasses the missing SDK. Child project commands still use global.json.
            @{ sdk = @{ version = $toolSdks[0].Name; rollForward = 'disable'; allowPrerelease = $true } } |
                ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $scriptGlobalJson
        }
        $templates = Invoke-Tool $process 3 'dotnet_sdk' @{ action = 'ListTemplates' }
        if ($templates -notmatch 'console') { throw "Console template was not discovered: $templates" }
        $projectDir = Join-Path $projectRoot "sample-$invocation"
        $null = Invoke-Tool $process 4 'dotnet_project' @{
            action = 'New'; template = 'console'; name = 'CompatibilitySample'
            output = $projectDir; framework = $projectFramework; workingDirectory = $projectRoot
        }
        $project = Join-Path $projectDir 'CompatibilitySample.csproj'
        $analysis = Invoke-Tool $process 5 'dotnet_project' @{ action = 'Analyze'; projectPath = $project }
        if ($analysis -notmatch [regex]::Escape($projectFramework)) { throw "Missing framework in project analysis: $analysis" }
        $build = Invoke-Tool $process 6 'dotnet_project' @{ action = 'Build'; project = $project; workingDirectory = $projectRoot }
        if ($build -notmatch '"success"\s*:\s*true') { throw "Project build did not report success: $build" }
    }
    finally {
        try {
            $process.StandardInput.Close()
            if (-not $process.WaitForExit(10000)) { $process.Kill($true); $process.WaitForExit() }
            $stderr.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $OutputDirectory "$invocation-stderr.txt")
        }
        finally { $process.Dispose() }
    }
    $hostTrace = Get-Content -LiteralPath $trace -Raw
    $assetPattern = 'tools[/\\]' + [regex]::Escape($ExpectedFramework) + '[/\\]any[/\\]DotNetMcp.dll'
    if ($hostTrace -notmatch $assetPattern) { throw "Host did not select $ExpectedFramework assets; see $trace" }
    $runtimePattern = [regex]::Escape([System.IO.Path]::Join(
        $DotnetRoot, 'shared', 'Microsoft.NETCore.App', $ExpectedRuntimeVersion, 'System.Private.CoreLib.dll'))
    if ($hostTrace -notmatch $runtimePattern) { throw "Host did not load expected runtime; see $trace" }
    $results += @{
        invocation = $invocation; framework = $ExpectedFramework; runtime = $ExpectedRuntimeVersion
        projectFramework = $projectFramework; projectSdk = $selectedProjectSdk; toolSdk = $selectedToolSdk
        allowRollForward = $false; passed = $true
        bypassedMissingGlobalSdk = ($invocation -eq 'dnx-script' -and $scriptSupportsGlobalJsonBypass)
    }
}
@{
    package = [System.IO.Path]::GetFileName($PackagePath)
    sha256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
    sdkInventory = $sdkList; runtimeInventory = $runtimeList; results = $results
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json')
Write-Host "Packaged tool compatibility passed. Results: $OutputDirectory"
