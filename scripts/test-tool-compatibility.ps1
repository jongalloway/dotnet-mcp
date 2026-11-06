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

$script:rpcId = 0
$script:protocol = '2025-11-25'
function Invoke-Rpc($Process, [string] $Method, [hashtable] $Parameters) {
    $id = ++$script:rpcId
    if ($script:protocol -eq '2026-07-28') {
        if (-not $Parameters.ContainsKey('_meta')) { $Parameters['_meta'] = @{} }
        $Parameters['_meta']['io.modelcontextprotocol/protocolVersion'] = $script:protocol
        $Parameters['_meta']['io.modelcontextprotocol/clientInfo'] = @{ name = 'package-compatibility'; version = '1.0' }
        if (-not $Parameters['_meta'].ContainsKey('io.modelcontextprotocol/clientCapabilities')) {
            $Parameters['_meta']['io.modelcontextprotocol/clientCapabilities'] = @{}
        }
        if ($Method.StartsWith('tasks/')) {
            $Parameters['_meta']['io.modelcontextprotocol/clientCapabilities'] = @{ extensions = @{ 'io.modelcontextprotocol/tasks' = @{} } }
        }
    }
    $message = @{ jsonrpc = '2.0'; id = $id; method = $Method; params = $Parameters } | ConvertTo-Json -Depth 20 -Compress
    Add-Content -LiteralPath $script:transcript ">> $message"
    $Process.StandardInput.WriteLine($message)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $read = $Process.StandardOutput.ReadLineAsync()
        $remaining = [int] [Math]::Max(1, ($deadline - [DateTime]::UtcNow).TotalMilliseconds)
        if (-not $read.Wait($remaining)) { throw "Timed out waiting for $Method" }
        $line = $read.GetAwaiter().GetResult()
        if ($null -eq $line) { throw "Server exited before responding to $Method" }
        Add-Content -LiteralPath $script:transcript "<< $line"
        $response = $line | ConvertFrom-Json -AsHashtable
        if ($response.ContainsKey('id') -and $response.id -eq $id) {
            if ($response.ContainsKey('error')) { throw "$Method failed: $($response.error | ConvertTo-Json -Compress)" }
            return $response.result
        }
        if ([DateTime]::UtcNow -ge $deadline) { throw "Timed out waiting for $Method" }
    } while ($true)
}

function Assert-ToolResult($Result, [bool] $ExpectError = $false, [switch] $TaskResult) {
    if (-not $Result.ContainsKey('content') -or
        ($script:protocol -eq '2026-07-28' -and -not $TaskResult -and
            (-not $Result.ContainsKey('resultType') -or $Result.resultType -ne 'complete'))) {
        throw "Expected an inline tool result: $($Result | ConvertTo-Json -Depth 20 -Compress)"
    }
    $isError = $Result.ContainsKey('isError') -and $Result.isError -eq $true
    if ($isError -ne $ExpectError) {
        throw "Unexpected tool error state: $($Result | ConvertTo-Json -Depth 20 -Compress)"
    }
}

function Invoke-Tool($Process, [string] $Name, [hashtable] $Arguments) {
    $result = Invoke-Rpc $Process 'tools/call' @{ name = $Name; arguments = $Arguments }
    Assert-ToolResult $result
    $text = ($result.content | Where-Object type -EQ 'text' | ForEach-Object text) -join "`n"
    if (($result.ContainsKey('isError') -and $result.isError -eq $true) -or $text -match 'Exit Code: [1-9]|^Error:') {
        throw "${Name} failed: $text"
    }
    return $text
}

function Wait-Task($Process, [string] $TaskId) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $task = Invoke-Rpc $Process 'tasks/get' @{ taskId = $TaskId }
        if ($task.status -ne 'working') { return $task }
        $interval = if ($task.ContainsKey('pollIntervalMs')) { [Math]::Max(100, $task.pollIntervalMs) } else { 100 }
        Start-Sleep -Milliseconds $interval
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Task $TaskId did not finish."
}

function Start-Task($Process, [string] $Name, [hashtable] $Arguments) {
    $created = Invoke-Rpc $Process 'tools/call' @{
        name = $Name; arguments = $Arguments
        _meta = @{ 'io.modelcontextprotocol/clientCapabilities' = @{ extensions = @{ 'io.modelcontextprotocol/tasks' = @{} } } }
    }
    if ($created.resultType -ne 'task' -or [string]::IsNullOrWhiteSpace($created.taskId)) {
        throw "Expected a created task: $($created | ConvertTo-Json -Depth 20 -Compress)"
    }
    return $created.taskId
}

function Assert-Apps($Process, $Tools, $Capabilities) {
    if (-not $Capabilities.extensions.ContainsKey('io.modelcontextprotocol/ui')) { throw 'Apps extension is missing.' }
    $resources = Invoke-Rpc $Process 'resources/list' @{}
    foreach ($dashboard in @(
        @{ tool = 'dotnet_sdk'; uri = 'ui://dotnet-mcp/sdk-dashboard' },
        @{ tool = 'dotnet_server_metrics'; uri = 'ui://dotnet-mcp/server-metrics' }
    )) {
        $tool = @($Tools | Where-Object name -EQ $dashboard.tool)
        if ($tool.Count -ne 1 -or $tool[0]._meta.ui.resourceUri -ne $dashboard.uri -or
            $tool[0]._meta['ui/resourceUri'] -ne $dashboard.uri) { throw "Invalid Apps linkage: $($dashboard.tool)" }
        $resource = @($resources.resources | Where-Object uri -EQ $dashboard.uri)
        if ($resource.Count -ne 1 -or $resource[0].mimeType -ne 'text/html;profile=mcp-app' -or
            $resource[0]._meta.ui.prefersBorder -ne $true) { throw "Invalid Apps discovery: $($dashboard.uri)" }
        $read = Invoke-Rpc $Process 'resources/read' @{ uri = $dashboard.uri }
        if ($read.contents.Count -ne 1 -or $read.contents[0].uri -ne $dashboard.uri -or
            $read.contents[0].mimeType -ne $resource[0].mimeType -or
            $read.contents[0].text -notmatch '^<!DOCTYPE html>' -or
            $read.contents[0]._meta.ui.prefersBorder -ne $true) { throw "Invalid Apps resource: $($dashboard.uri)" }
    }
}

function Assert-Queries($Process, $Tools, [string] $Project) {
    foreach ($query in @(
        @{ name = 'dotnet_sdk_query'; actions = @('Version', 'Info', 'ListSdks', 'ListRuntimes', 'FrameworkInfo', 'CacheMetrics'); excluded = 'ClearTemplateCache' },
        @{ name = 'dotnet_project_query'; actions = @('Logs'); excluded = 'Stop' },
        @{ name = 'dotnet_server_metrics_query'; actions = @('Get', 'TokenSavingsGet'); excluded = 'Reset' }
    )) {
        $tool = @($Tools | Where-Object name -EQ $query.name)
        if ($tool.Count -ne 1) { throw "Missing query tool: $($query.name)" }
        $tool = $tool[0]
        if ($tool.annotations.readOnlyHint -ne $true -or $tool.annotations.destructiveHint -ne $false -or
            $tool.annotations.idempotentHint -ne $true -or $tool.annotations.openWorldHint -ne $false -or
            $tool._meta.ContainsKey('ui') -or $tool._meta.ContainsKey('ui/resourceUri') -or
            (Compare-Object @($tool.inputSchema.properties.action.enum) $query.actions)) {
            throw "Invalid query discovery: $($query.name)"
        }
        foreach ($action in @($query.excluded, 'NotAnAction')) {
            $arguments = @{ action = $action }
            if ($query.name -eq 'dotnet_project_query') { $arguments.sessionId = 'not-started' }
            $rejected = Invoke-Rpc $Process 'tools/call' @{ name = $query.name; arguments = $arguments }
            Assert-ToolResult $rejected -ExpectError $true
        }
        if ($query.name -ne 'dotnet_project_query') {
            foreach ($action in $query.actions) {
                $arguments = @{ action = $action }
                if ($action -eq 'FrameworkInfo') { $arguments.framework = $ExpectedFramework }
                $null = Invoke-Tool $Process $query.name $arguments
            }
        }
    }
    $sessionId = $null
    try {
        $run = Invoke-Tool $Process 'dotnet_project' @{
            action = 'Run'; project = $Project; noBuild = $true; startMode = 'Background'; workingDirectory = $projectRoot
        }
        if ($run -notmatch 'Session ID: ([^\r\n]+)') { throw "Background session missing: $run" }
        $sessionId = $Matches[1].Trim()
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        do {
            $logs = Invoke-Tool $Process 'dotnet_project_query' @{ action = 'Logs'; sessionId = $sessionId; tailLines = 10 }
            if ($logs -match 'compatibility-ready') { return }
            Start-Sleep -Milliseconds 100
        } while ([DateTime]::UtcNow -lt $deadline)
        throw "Background query did not retrieve application output: $logs"
    }
    finally {
        if ($sessionId) { $null = Invoke-Tool $Process 'dotnet_project' @{ action = 'Stop'; sessionId = $sessionId } }
    }
}

function Assert-Tasks($Process, [string] $ProjectDirectory, [string] $ApplicationDll) {
    foreach ($expectError in @($false, $true)) {
        $name = if ($expectError) { 'dotnet_project_query' } else { 'dotnet_server_capabilities' }
        $arguments = if ($expectError) { @{ action = 'Logs'; sessionId = 'not-started' } } else { @{} }
        $taskId = Start-Task $Process $name $arguments
        $completed = Wait-Task $Process $taskId
        if ($completed.status -ne 'completed') { throw "Task failed: $($completed | ConvertTo-Json -Depth 20 -Compress)" }
        Assert-ToolResult $completed.result -ExpectError $expectError -TaskResult
        if (-not $expectError -and $completed.result.structuredContent.supports.asyncTasks -ne $true) {
            throw 'Opted-in task did not report asyncTasks.'
        }
        $inline = Invoke-Rpc $Process 'tools/call' @{ name = 'dotnet_server_capabilities'; arguments = @{} }
        Assert-ToolResult $inline
        if ($inline.structuredContent.supports.asyncTasks -ne $false) { throw 'Tasks opt-in leaked into an ordinary call.' }
    }

    $marker = Join-Path $ProjectDirectory 'task-worker.pid'
    $project = Join-Path $ProjectDirectory 'cancellable.csproj'
    $command = [System.Security.SecurityElement]::Escape("`"$dotnet`" `"$ApplicationDll`" `"$marker`"")
    "<Project><Target Name=`"Build`"><Exec Command=`"$command`" /></Target></Project>" | Set-Content -LiteralPath $project
    $taskId = Start-Task $Process 'dotnet_project' @{
        action = 'Build'; project = $project; workingDirectory = $projectRoot; noRestore = $true
    }
    $worker = $null
    $taskCancelled = $false
    try {
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        while (-not (Test-Path -LiteralPath $marker) -or [string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $marker -Raw))) {
            if ([DateTime]::UtcNow -ge $deadline) { throw 'Task worker did not start.' }
            $working = Invoke-Rpc $Process 'tasks/get' @{ taskId = $taskId }
            if ($working.status -ne 'working') {
                throw "Task stopped before starting its worker: $($working | ConvertTo-Json -Depth 20 -Compress)"
            }
            $interval = if ($working.ContainsKey('pollIntervalMs')) { [Math]::Max(100, $working.pollIntervalMs) } else { 100 }
            Start-Sleep -Milliseconds $interval
        }
        $worker = [System.Diagnostics.Process]::GetProcessById([int](Get-Content -LiteralPath $marker -Raw))
        if ($worker.HasExited) { throw 'Task worker exited before cancellation.' }
        $working = Invoke-Rpc $Process 'tasks/get' @{ taskId = $taskId }
        if ($working.status -ne 'working') { throw "Expected working task, got $($working.status)." }
        $null = Invoke-Rpc $Process 'tasks/cancel' @{ taskId = $taskId }
        $cancelled = Wait-Task $Process $taskId
        if ($cancelled.status -ne 'cancelled') { throw "Expected cancelled task, got $($cancelled.status)." }
        $taskCancelled = $true
        if (-not $worker.WaitForExit($TimeoutSeconds * 1000)) { throw 'Task cancellation left the CLI worker running.' }
    }
    finally {
        try {
            if (-not $taskCancelled) { $null = Invoke-Rpc $Process 'tasks/cancel' @{ taskId = $taskId } }
        }
        finally {
            if ($null -ne $worker) {
                if (-not $worker.HasExited) { $worker.Kill($true); $worker.WaitForExit() }
                $worker.Dispose()
            }
        }
    }
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
    foreach ($protocol in @('2025-11-25', '2026-07-28')) {
        $script:protocol = $protocol
        $trace = Join-Path $OutputDirectory "$invocation-$protocol-host.txt"
        $script:transcript = Join-Path $OutputDirectory "$invocation-$protocol-rpc.txt"
        if ($invocation -eq 'dnx-script' -and $scriptSupportsGlobalJsonBypass) {
            @{ sdk = @{ version = '99.0.100'; rollForward = 'disable' } } |
                ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $scriptGlobalJson
        }
        $process = New-ChildProcess $command $arguments $trace -WorkingDirectory $startupDirectory -RawArguments $rawArguments
        $stderr = $process.StandardError.ReadToEndAsync()
        try {
            if ($protocol -eq '2025-11-25') {
                $discovery = Invoke-Rpc $process 'initialize' @{
                    protocolVersion = $protocol
                    capabilities = @{}
                    clientInfo = @{ name = 'package-compatibility'; version = '1.0' }
                }
                if ($discovery.protocolVersion -ne $protocol) { throw "Protocol negotiation failed: $($discovery.protocolVersion)" }
                $process.StandardInput.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
            }
            else {
                $discovery = Invoke-Rpc $process 'server/discover' @{}
                if ($protocol -notin $discovery.supportedVersions) { throw 'Current protocol is not supported.' }
                if (-not $discovery.capabilities.extensions.ContainsKey('io.modelcontextprotocol/tasks')) { throw 'Tasks extension is missing.' }
            }
            $tools = Invoke-Rpc $process 'tools/list' @{}
            foreach ($name in @('dotnet_sdk', 'dotnet_project', 'dotnet_package', 'dotnet_solution', 'dotnet_ef',
                'dotnet_tool', 'dotnet_workload', 'dotnet_dev_certs', 'dotnet_help', 'dotnet_server_capabilities')) {
                if ($name -notin $tools.tools.name) { throw "Missing MCP tool: $name" }
            }
            if ($invocation -eq 'dnx-script' -and $scriptSupportsGlobalJsonBypass) {
                # Startup proved the script bypasses the missing SDK. Child project commands still use global.json.
                @{ sdk = @{ version = $toolSdks[0].Name; rollForward = 'disable'; allowPrerelease = $true } } |
                    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $scriptGlobalJson
            }
            $snapshot = Invoke-Rpc $process 'tools/call' @{ name = 'dotnet_server_capabilities'; arguments = @{} }
            Assert-ToolResult $snapshot
            if ($snapshot.structuredContent.protocolVersion -ne $protocol -or
                $snapshot.structuredContent.supports.asyncTasks -ne $false) { throw 'Incorrect ordinary request capabilities.' }
            if ($snapshot.structuredContent.serverVersion.Split('+')[0] -ne $version.Split('+')[0]) {
                throw "Server version does not match package version $version."
            }
            Assert-Apps $process $tools.tools $discovery.capabilities
            $templates = Invoke-Tool $process 'dotnet_sdk' @{ action = 'ListTemplates' }
            if ($templates -notmatch 'console') { throw "Console template was not discovered: $templates" }
            $projectDir = Join-Path $projectRoot "sample-$invocation-$protocol"
            $null = Invoke-Tool $process 'dotnet_project' @{
                action = 'New'; template = 'console'; name = 'CompatibilitySample'
                output = $projectDir; framework = $projectFramework; workingDirectory = $projectRoot
            }
            $project = Join-Path $projectDir 'CompatibilitySample.csproj'
            @'
using System;
using System.IO;
using System.Threading;
Console.WriteLine("compatibility-ready");
if (args.Length != 0) File.WriteAllText(args[0], Environment.ProcessId.ToString());
Thread.Sleep(TimeSpan.FromMinutes(2));
'@ | Set-Content -LiteralPath (Join-Path $projectDir 'Program.cs')
            $analysis = Invoke-Tool $process 'dotnet_project' @{ action = 'Analyze'; projectPath = $project }
            if ($analysis -notmatch [regex]::Escape($projectFramework)) { throw "Missing framework in project analysis: $analysis" }
            $build = Invoke-Tool $process 'dotnet_project' @{ action = 'Build'; project = $project; workingDirectory = $projectRoot }
            if ($build -notmatch '"success"\s*:\s*true') { throw "Project build did not report success: $build" }
            Assert-Queries $process $tools.tools $project
            if ($protocol -eq '2026-07-28') {
                Assert-Tasks $process $projectDir (Join-Path $projectDir "bin/Debug/$projectFramework/CompatibilitySample.dll")
            }
        }
        finally {
            try {
                $process.StandardInput.Close()
                if (-not $process.WaitForExit(10000)) { $process.Kill($true); $process.WaitForExit() }
                $stderr.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $OutputDirectory "$invocation-$protocol-stderr.txt")
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
            serverVersion = $snapshot.structuredContent.serverVersion
            protocol = $protocol; features = @('inline', 'apps', 'queries', 'project-build')
            tasks = $(if ($protocol -eq '2026-07-28') { 'passed' } else { 'not-applicable-legacy-inline' })
            projectFramework = $projectFramework; projectSdk = $selectedProjectSdk; toolSdk = $selectedToolSdk
            allowRollForward = $false; passed = $true
            bypassedMissingGlobalSdk = ($invocation -eq 'dnx-script' -and $scriptSupportsGlobalJsonBypass)
        }
    }
}
@{
    package = [System.IO.Path]::GetFileName($PackagePath)
    version = $version
    sha256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
    sdkInventory = $sdkList; runtimeInventory = $runtimeList; results = $results
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json')
Write-Host "Packaged tool compatibility passed. Results: $OutputDirectory"
