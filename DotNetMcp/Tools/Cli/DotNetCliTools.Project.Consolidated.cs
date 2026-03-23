using System.ComponentModel;
using System.Text;
using DotNetMcp.Actions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Consolidated .NET project lifecycle management commands.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Manage .NET project lifecycle including creation, building, testing, running, and publishing.
    /// Provides a unified interface for all project operations including new project creation,
    /// building, running, testing, publishing, cleaning, code formatting, packaging, and project analysis.
    /// </summary>
    /// <param name="action">The project operation to perform</param>
    /// <param name="project">Path to project file for most operations</param>
    /// <param name="template">Template short name for new action (e.g., 'console', 'webapi', 'classlib')</param>
    /// <param name="name">Project name for new action</param>
    /// <param name="output">Output directory for new/publish/pack actions</param>
    /// <param name="framework">Target framework (e.g., 'net10.0', 'net8.0')</param>
    /// <param name="configuration">Build configuration (Debug or Release)</param>
    /// <param name="runtime">Target runtime identifier for publish action (e.g., 'linux-x64', 'win-x64')</param>
    /// <param name="additionalOptions">Template-specific options for new action (e.g., '--use-program-main', '--aot')</param>
    /// <param name="appArgs">Arguments to pass to the application for run action</param>
    /// <param name="filter">Test filter expression for test action</param>
    /// <param name="collect">Data collector friendly name for test action (e.g., 'XPlat Code Coverage')</param>
    /// <param name="resultsDirectory">Directory for test results</param>
    /// <param name="logger">Logger for test results (e.g., 'trx', 'console;verbosity=detailed')</param>
    /// <param name="noBuild">Skip building before test/run actions</param>
    /// <param name="noRestore">Skip restore before build/test actions</param>
    /// <param name="verbosity">MSBuild verbosity level (quiet, minimal, normal, detailed, diagnostic)</param>
    /// <param name="blame">Run tests in blame mode for test action</param>
    /// <param name="listTests">List discovered tests without running them for test action</param>
    /// <param name="includeSymbols">Include symbols package for pack action</param>
    /// <param name="includeSource">Include source files in the package for pack action</param>
    /// <param name="watchAction">Sub-action for watch (run, test, or build)</param>
    /// <param name="noHotReload">Disable hot reload for watch action</param>
    /// <param name="projectPath">Project path for analyze/dependencies/validate actions (required for these actions)</param>
    /// <param name="verify">Verify formatting without making changes for format action</param>
    /// <param name="includeGenerated">Include generated code files for format action</param>
    /// <param name="diagnostics">Comma-separated list of diagnostic IDs to fix for format action</param>
    /// <param name="severity">Severity level to fix for format action (info, warn, error)</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    /// <param name="testRunner">Test runner selection for test action (Auto, MicrosoftTestingPlatform, or VSTest). Auto detects from global.json. Default: Auto</param>
    /// <param name="useLegacyProjectArgument">DEPRECATED: Use testRunner parameter instead. When true, uses positional project argument (VSTest mode)</param>
    /// <param name="sessionId">Session ID for stop/logs actions (required when action is Stop or Logs)</param>
    /// <param name="startMode">Start mode for run/watch actions (Foreground or Background). Foreground blocks until exit, Background returns immediately with sessionId. Default: Foreground</param>
    /// <param name="tailLines">Number of most recent log lines to return for logs action (optional, returns all if not specified)</param>
    /// <param name="since">Return logs only after this timestamp (ISO 8601 format) for logs action (optional)</param>
    /// <param name="propertyName">MSBuild property name for SetProperty/GetProperty/RemoveProperty actions (e.g., 'OutputType')</param>
    /// <param name="propertyValue">Value to set for SetProperty action (e.g., 'Exe')</param>
    /// <param name="selfContained">Publish as self-contained deployment (true) or framework-dependent (false) for publish action</param>
    /// <param name="arch">Target architecture for publish action (e.g., 'x64', 'arm64')</param>
    /// <param name="os">Target operating system for publish action (e.g., 'win', 'linux', 'osx')</param>
    /// <param name="source">NuGet package source URL or local path for restore action</param>
    /// <param name="lockedMode">Run restore in locked mode (fail if lock file is out of date) for restore action</param>
    /// <param name="configFile">NuGet.config file to use for restore action</param>
    /// <param name="itemType">Item type for AddItem/RemoveItem/ListItems actions (e.g., 'Using', 'Content', 'None')</param>
    /// <param name="include">The Include attribute value for AddItem/RemoveItem actions</param>
    [McpServerTool(Title = ".NET Project", Destructive = true, TaskSupport = ToolTaskSupport.Optional, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/File%20Folder/Flat/file_folder_flat.svg")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["New","Restore","Build","Run","Test","Publish","Clean","Analyze","Dependencies","Validate","Pack","Watch","Format","Stop","Logs","SetProperty","GetProperty","RemoveProperty","AddItem","RemoveItem","ListItems"]""")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://dotnet-mcp/project-dashboard"}""")]
    [McpMeta("ui/resourceUri", "ui://dotnet-mcp/project-dashboard")]
    public async partial Task<CallToolResult> DotnetProject(
        DotnetProjectAction action,
        string? project = null,
        string? template = null,
        string? name = null,
        string? output = null,
        string? framework = null,
        string? configuration = null,
        string? runtime = null,
        string? additionalOptions = null,
        string? appArgs = null,
        string? filter = null,
        string? collect = null,
        string? resultsDirectory = null,
        string? logger = null,
        bool? noBuild = null,
        bool? noRestore = null,
        string? verbosity = null,
        bool? blame = null,
        bool? listTests = null,
        bool? includeSymbols = null,
        bool? includeSource = null,
        string? watchAction = null,
        bool? noHotReload = null,
        string? projectPath = null,
        bool? verify = null,
        bool? includeGenerated = null,
        string? diagnostics = null,
        string? severity = null,
        string? workingDirectory = null,
        TestRunner? testRunner = null,
        bool? useLegacyProjectArgument = null,
        string? sessionId = null,
        StartMode? startMode = null,
        int? tailLines = null,
        string? since = null,
        string? propertyName = null,
        string? propertyValue = null,
        bool? selfContained = null,
        string? arch = null,
        string? os = null,
        string? source = null,
        bool? lockedMode = null,
        string? configFile = null,
        string? itemType = null,
        string? include = null,
        IProgress<ProgressNotificationValue>? progress = null,
        McpServer? server = null)
    {
        // Auto-detect project from workspace roots when not explicitly specified.
        // Only attempt discovery for actions that use the project parameter; skip New,
        // Stop, Logs, ListTemplateOptions, Analyze, Dependencies, and Validate which
        // either don't need a project path or use projectPath instead.
        var effectiveProject = project;
        if (string.IsNullOrEmpty(effectiveProject) && action is
            DotnetProjectAction.Restore or DotnetProjectAction.Build or DotnetProjectAction.Run or
            DotnetProjectAction.Test or DotnetProjectAction.Publish or DotnetProjectAction.Clean or
            DotnetProjectAction.Pack or DotnetProjectAction.Watch or DotnetProjectAction.Format or
            DotnetProjectAction.SetProperty or DotnetProjectAction.GetProperty or
            DotnetProjectAction.RemoveProperty or DotnetProjectAction.AddItem or
            DotnetProjectAction.RemoveItem or DotnetProjectAction.ListItems)
        {
            effectiveProject = await WorkspaceDiscovery.TryFindProjectInRootsAsync(server);
        }

        var textResult = await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action parameter
            if (!ParameterValidator.ValidateAction<DotnetProjectAction>(action, out var errorMessage))
            {
                return $"Error: {errorMessage}";
            }

            // Route to appropriate handler based on action
            return action switch
            {
                DotnetProjectAction.New => await HandleNewAction(template, name, output, framework, additionalOptions),
                DotnetProjectAction.Restore => await ExecuteWithProgress(progress, "Restoring packages...", "Restore complete", () => HandleRestoreAction(effectiveProject, verbosity, source, lockedMode, configFile, server)),
                DotnetProjectAction.Build => await ExecuteWithProgress(progress, "Building project...", "Build complete", () => HandleBuildAction(effectiveProject, configuration, framework, noRestore, verbosity, output, server)),
                DotnetProjectAction.Run => await ExecuteWithProgress(progress, "Building and starting application...", "Run complete", () => HandleRunAction(effectiveProject, configuration, appArgs, noBuild, startMode)),
                DotnetProjectAction.Test => await ExecuteWithProgress(progress, "Running tests...", "Tests complete", () => HandleTestAction(effectiveProject, configuration, filter, collect, resultsDirectory, logger, noBuild, noRestore, verbosity, framework, blame, listTests, testRunner, useLegacyProjectArgument, server)),
                DotnetProjectAction.Publish => await ExecuteWithProgress(progress, "Publishing project...", "Publish complete", () => HandlePublishAction(effectiveProject, configuration, output, runtime, framework, noRestore, noBuild, selfContained, arch, os, verbosity, server)),
                DotnetProjectAction.Clean => await ExecuteWithProgress(progress, "Cleaning output directories...", "Clean complete", () => HandleCleanAction(effectiveProject, configuration, framework, verbosity, server)),
                DotnetProjectAction.Analyze => await HandleAnalyzeAction(projectPath),
                DotnetProjectAction.Dependencies => await HandleDependenciesAction(projectPath),
                DotnetProjectAction.Validate => await HandleValidateAction(projectPath),
                DotnetProjectAction.Pack => await ExecuteWithProgress(progress, "Packing project...", "Pack complete", () => HandlePackAction(effectiveProject, configuration, output, includeSymbols, includeSource)),
                DotnetProjectAction.Watch => await HandleWatchAction(watchAction, effectiveProject, configuration, appArgs, filter, noHotReload, startMode),
                DotnetProjectAction.Format => await HandleFormatAction(effectiveProject, verify, includeGenerated, diagnostics, severity),
                DotnetProjectAction.Stop => await HandleStopAction(sessionId),
                DotnetProjectAction.Logs => await HandleLogsAction(sessionId, tailLines, since),
                DotnetProjectAction.ListTemplateOptions => await HandleListTemplateOptionsAction(template),
                DotnetProjectAction.SetProperty => await HandleSetPropertyAction(effectiveProject, propertyName, propertyValue),
                DotnetProjectAction.GetProperty => await HandleGetPropertyAction(effectiveProject, propertyName),
                DotnetProjectAction.RemoveProperty => await HandleRemovePropertyAction(effectiveProject, propertyName),
                DotnetProjectAction.AddItem => await HandleAddItemAction(effectiveProject, itemType, include),
                DotnetProjectAction.RemoveItem => await HandleRemoveItemAction(effectiveProject, itemType, include),
                DotnetProjectAction.ListItems => await HandleListItemsAction(effectiveProject, itemType),
                _ => $"Error: Action '{action}' is not supported."
            };
        });

        // Build structured content for actions that benefit from dashboard display.
        object? structured = action switch
        {
            DotnetProjectAction.New => BuildNewProjectStructuredContent(textResult, template, name, output, framework),
            DotnetProjectAction.Build => BuildBuildStructuredContent(textResult, effectiveProject, configuration),
            DotnetProjectAction.Analyze => BuildAnalyzeStructuredContent(textResult),
            DotnetProjectAction.ListTemplateOptions => BuildTemplateOptionsStructuredContent(textResult, template),
            _ => null
        };

        var displayText = structured != null
            ? $"[This data is displayed in the project dashboard UI. Summarize briefly or refer the user to it rather than repeating all details.]\n\n{textResult}"
            : textResult;

        return StructuredContentHelper.ToCallToolResult(displayText, structured);
    }

    private async Task<string> HandleNewAction(string? template, string? name, string? output, string? framework, string? additionalOptions)
    {
        // Route to existing DotnetProjectNew method
        return await DotnetProjectNew(
            template: template,
            name: name,
            output: output,
            framework: framework,
            additionalOptions: additionalOptions);
    }

    private async Task<string> HandleRestoreAction(string? project, string? verbosity, string? source, bool? lockedMode, string? configFile, McpServer? server = null)
    {
        // Validate before sending the notification so clients don't see misleading messages
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        var target = string.IsNullOrEmpty(project) ? "project" : $"\"{Path.GetFileName(project)}\"";
        await SendMcpLogAsync(server, $"Restoring NuGet packages for {target}...");
        return await DotnetProjectRestore(
            project: project,
            verbosity: verbosity,
            source: source,
            lockedMode: lockedMode ?? false,
            configFile: configFile);
    }

    private async Task<string> HandleBuildAction(string? project, string? configuration, string? framework, bool? noRestore, string? verbosity, string? output, McpServer? server = null)
    {
        // Validate before sending the notification so clients don't see misleading messages
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        var target = string.IsNullOrEmpty(project) ? "project" : $"\"{Path.GetFileName(project)}\"";
        var config = string.IsNullOrEmpty(configuration) ? "" : $" ({configuration})";
        await SendMcpLogAsync(server, $"Building {target}{config}...");
        var result = await DotnetProjectBuild(
            project: project,
            configuration: configuration,
            framework: framework,
            noRestore: noRestore ?? false,
            verbosity: verbosity,
            output: output);

        // Use sampling for AI-assisted error interpretation when build fails and client supports sampling.
        // Note: the result string has already had SecretRedactor applied by DotNetCommandExecutor.
        return await AppendAiAnalysisOnFailureAsync(result, server,
            "Summarize these .NET build errors and suggest fixes (be concise):");
    }

    private async Task<string> HandleRunAction(string? project, string? configuration, string? appArgs, bool? noBuild, StartMode? startMode)
    {
        var effectiveStartMode = startMode ?? StartMode.Foreground;

        // If foreground mode, use existing behavior
        if (effectiveStartMode == StartMode.Foreground)
        {
            return await DotnetProjectRun(
                project: project,
                configuration: configuration,
                appArgs: appArgs,
                noBuild: noBuild ?? false);
        }

        // Background mode - start process and return immediately with session metadata
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
        {
            return $"Error: {projectError}";
        }

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
        {
            return $"Error: {configError}";
        }

        // Build the command arguments
        var args = new StringBuilder("run");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (noBuild ?? false) args.Append(" --no-build");
        if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");

        // Determine the target for session tracking
        var workingDir = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        var target = GetOperationTarget(project, workingDir);

        // Generate a session ID
        var sessionId = Guid.NewGuid().ToString();

        try
        {
            // Start the process without waiting
            var process = DotNetCommandExecutor.StartProcess(args.ToString(), _logger, workingDir);
            var sessionRegistered = false;

            try
            {
                // Register the session
                if (!_processSessionManager.RegisterSession(sessionId, process, "run", target))
                {
                    // Registration failed - unlikely but handle it
                    // Kill and dispose the process since we couldn't register it
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        // Best effort cleanup - log for troubleshooting
                        _logger.LogDebug(ex, "Failed to kill process during registration failure for session {SessionId}", sessionId);
                    }
                    finally
                    {
                        process.Dispose();
                    }

                    return "Error: Failed to register process session. Session ID may already exist.";
                }

                sessionRegistered = true;

                // Attach cleanup continuation for when process exits
                // Note: This is a fire-and-forget task by design. The process lifetime is independent
                // of the API call that started it. The cleanup will run when the process exits,
                // or be orphaned if the server shuts down (which is acceptable for a background process).
                // The ProcessSessionManager owns the process and will dispose it during cleanup.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await process.WaitForExitAsync();
                        // Safely capture exit code before logging, in case the process is disposed
                        int exitCode;
                        try
                        {
                            exitCode = process.ExitCode;
                            _logger.LogDebug("Background run process {SessionId} exited with code {ExitCode}", sessionId, exitCode);
                        }
                        catch (InvalidOperationException)
                        {
                            // Process was disposed before we could get the exit code
                            _logger.LogDebug("Background run process {SessionId} exited (exit code unavailable)", sessionId);
                        }
                        
                        // Clean up the session
                        _processSessionManager.CleanupCompletedSessions();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in background process cleanup for session {SessionId}", sessionId);
                    }
                });

                // Return success with session metadata

                return $"Process started in background mode\nSession ID: {sessionId}\nPID: {process.Id}\nTarget: {target}\n\nUse 'dotnet_project' with action 'Stop' and sessionId '{sessionId}' to terminate the process.";
            }
            finally
            {
                // Only dispose the process if registration failed
                // When registration succeeds, the ProcessSessionManager owns the process and handles disposal
                if (!sessionRegistered)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Best effort disposal; log at debug to avoid noisy failures during cleanup
                        _logger.LogDebug(ex, "Failed to dispose process for session {SessionId}", sessionId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start background run process");
            
            return $"Error: Failed to start process: {ex.Message}";
        }
    }

    private async Task<string> HandleTestAction(string? project, string? configuration, string? filter, string? collect, string? resultsDirectory, string? logger, bool? noBuild, bool? noRestore, string? verbosity, string? framework, bool? blame, bool? listTests, TestRunner? testRunner, bool? useLegacyProjectArgument, McpServer? server = null)
    {
        // Validate before sending the notification so clients don't see misleading messages
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";

        // Route to existing DotnetProjectTest method
        var target = string.IsNullOrEmpty(project) ? "project" : $"\"{Path.GetFileName(project)}\"";
        var filterInfo = string.IsNullOrEmpty(filter) ? "" : $" (filter: {filter})";
        await SendMcpLogAsync(server, $"Running tests for {target}{filterInfo}...");
        var result = await DotnetProjectTest(
            project: project,
            configuration: configuration,
            filter: filter,
            collect: collect,
            resultsDirectory: resultsDirectory,
            logger: logger,
            noBuild: noBuild ?? false,
            noRestore: noRestore ?? false,
            verbosity: verbosity,
            framework: framework,
            blame: blame ?? false,
            listTests: listTests ?? false,
            testRunner: testRunner,
            useLegacyProjectArgument: useLegacyProjectArgument ?? false);

        // Use sampling for AI-assisted failure analysis when tests fail and client supports sampling.
        // Note: the result string has already had SecretRedactor applied by DotNetCommandExecutor.
        return await AppendAiAnalysisOnFailureAsync(result, server,
            "Summarize these .NET test results and suggest which tests need attention (be concise):");
    }

    private async Task<string> HandlePublishAction(string? project, string? configuration, string? output, string? runtime, string? framework, bool? noRestore, bool? noBuild, bool? selfContained, string? arch, string? os, string? verbosity, McpServer? server = null)
    {
        // Validate before sending the notification so clients don't see misleading messages
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";
        if (!ParameterValidator.ValidateRuntimeIdentifier(runtime, out var runtimeError))
            return $"Error: {runtimeError}";
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        // Route to existing DotnetProjectPublish method
        var target = string.IsNullOrEmpty(project) ? "project" : $"\"{Path.GetFileName(project)}\"";
        var runtimeInfo = string.IsNullOrEmpty(runtime) ? "" : $" for {runtime}";
        await SendMcpLogAsync(server, $"Publishing {target}{runtimeInfo}...");
        return await DotnetProjectPublish(
            project: project,
            configuration: configuration,
            output: output,
            runtime: runtime,
            framework: framework,
            noRestore: noRestore ?? false,
            noBuild: noBuild ?? false,
            selfContained: selfContained,
            arch: arch,
            os: os,
            verbosity: verbosity);
    }

    private async Task<string> HandleCleanAction(string? project, string? configuration, string? framework, string? verbosity, McpServer? server = null)
    {
        // Validate before sending the notification so clients don't see misleading messages
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        // Request confirmation via elicitation when client supports it
        if (server != null && server.ClientCapabilities?.Elicitation != null)
        {
            var target = project ?? "the current project/solution";
            var elicitResult = await server.ElicitAsync(new ElicitRequestParams
            {
                Message = $"This will delete all build output artifacts for {target}. Do you want to proceed?",
                RequestedSchema = new ElicitRequestParams.RequestSchema
                {
                    Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                    {
                        ["confirmed"] = new ElicitRequestParams.BooleanSchema
                        {
                            Title = "Confirm clean",
                            Description = "I understand this will delete build artifacts"
                        }
                    }
                }
            }, default);

            if (!elicitResult.IsAccepted)
            {
                return "Clean operation cancelled.";
            }
        }

        // Route to existing DotnetProjectClean method
        return await DotnetProjectClean(
            project: project,
            configuration: configuration,
            framework: framework,
            verbosity: verbosity);
    }

    private async Task<string> HandleAnalyzeAction(string? projectPath)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(projectPath, "projectPath", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetProjectAnalyze method
        return await DotnetProjectAnalyze(projectPath: projectPath!);
    }

    private async Task<string> HandleDependenciesAction(string? projectPath)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(projectPath, "projectPath", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetProjectDependencies method
        return await DotnetProjectDependencies(projectPath: projectPath!);
    }

    private async Task<string> HandleValidateAction(string? projectPath)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(projectPath, "projectPath", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetProjectValidate method
        return await DotnetProjectValidate(projectPath: projectPath!);
    }

    private async Task<string> HandlePackAction(string? project, string? configuration, string? output, bool? includeSymbols, bool? includeSource)
    {
        // Route to existing DotnetPackCreate method
        return await DotnetPackCreate(
            project: project,
            configuration: configuration,
            output: output,
            includeSymbols: includeSymbols ?? false,
            includeSource: includeSource ?? false);
    }

    private async Task<string> HandleWatchAction(string? watchAction, string? project, string? configuration, string? appArgs, string? filter, bool? noHotReload, StartMode? startMode = null)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(watchAction, "watchAction", out var errorMessage))
        {
            return "Error: watchAction is required for Watch action. Valid values: run, test, build";
        }

        // Validate watchAction value
        var validWatchActions = new[] { "run", "test", "build" };
        if (!validWatchActions.Contains(watchAction!.ToLowerInvariant()))
        {
            return $"Error: Invalid watchAction '{watchAction}'. Valid values: run, test, build";
        }

        var effectiveStartMode = startMode ?? StartMode.Foreground;

        // Background mode - start the watch process and return immediately with session metadata
        if (effectiveStartMode == StartMode.Background)
        {
            // Validate project path if provided
            if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            {
                return $"Error: {projectError}";
            }

            // Validate configuration if provided
            if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            {
                return $"Error: {configError}";
            }

            // Build the watch command arguments
            var args = new StringBuilder("watch");
            if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");

            switch (watchAction.ToLowerInvariant())
            {
                case "run":
                    args.Append(" run");
                    if (noHotReload ?? false) args.Append(" --no-hot-reload");
                    if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");
                    break;
                case "test":
                    args.Append(" test");
                    if (!string.IsNullOrEmpty(filter)) args.Append($" --filter \"{filter}\"");
                    break;
                case "build":
                    args.Append(" build");
                    if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
                    break;
            }

            // Determine the target for session tracking
            var workingDir = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
            var target = GetOperationTarget(project, workingDir);

            // Generate a session ID
            var sessionId = Guid.NewGuid().ToString();

            try
            {
                // Start the process without waiting
                var process = DotNetCommandExecutor.StartProcess(args.ToString(), _logger, workingDir);

                // Register the session; if registration fails, dispose the process
                if (!_processSessionManager.RegisterSession(sessionId, process, "watch", target))
                {
                    using (process)
                    {
                        try
                        {
                            process.Kill(entireProcessTree: true);
                        }
                        catch (Win32Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to kill process during registration failure for watch session {SessionId}", sessionId);
                        }
                        catch (InvalidOperationException ex)
                        {
                            _logger.LogDebug(ex, "Failed to kill process during registration failure for watch session {SessionId}", sessionId);
                        }
                    }

                    return "Error: Failed to register watch process session. Session ID may already exist.";
                }

                // Attach cleanup continuation for when process exits
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await process.WaitForExitAsync();
                        try
                        {
                            var exitCode = process.ExitCode;
                            _logger.LogDebug("Background watch process {SessionId} exited with code {ExitCode}", sessionId, exitCode);
                        }
                        catch (InvalidOperationException)
                        {
                            _logger.LogDebug("Background watch process {SessionId} exited (exit code unavailable)", sessionId);
                        }

                        _processSessionManager.CleanupCompletedSessions();
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError(ex, "Error in background watch process cleanup for session {SessionId}", sessionId);
                    }
                });

                return $"Watch process started in background mode\nSession ID: {sessionId}\nPID: {process.Id}\nTarget: {target}\nWatch Action: {watchAction}\n\nUse 'dotnet_project' with action 'Stop' and sessionId '{sessionId}' to terminate the watch process.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to start background watch process");
                return $"Error: Failed to start watch process: {ex.Message}";
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Failed to start background watch process");
                return $"Error: Failed to start watch process: {ex.Message}";
            }
        }

        // Foreground mode - route to appropriate watch method based on watchAction
        return watchAction.ToLowerInvariant() switch
        {
            "run" => await DotnetWatchRun(project: project, appArgs: appArgs, noHotReload: noHotReload ?? false),
            "test" => await DotnetWatchTest(project: project, filter: filter),
            "build" => await DotnetWatchBuild(project: project, configuration: configuration),
            _ => $"Error: Invalid watchAction '{watchAction}'. Valid values: run, test, build"
        };
    }

    private async Task<string> HandleFormatAction(string? project, bool? verify, bool? includeGenerated, string? diagnostics, string? severity)
    {
        // Route to existing DotnetFormat method
        return await DotnetFormat(
            project: project,
            verify: verify ?? false,
            includeGenerated: includeGenerated ?? false,
            diagnostics: diagnostics,
            severity: severity);
    }

    private Task<string> HandleStopAction(string? sessionId)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(sessionId, "sessionId", out var errorMessage))
        {
            return Task.FromResult($"Error: {errorMessage}");
        }

        // Try to stop the session
        if (_processSessionManager.TryStopSession(sessionId!, out var stopError))
        {
            return Task.FromResult($"Successfully stopped session '{sessionId}'");
        }
        else
        {
            return Task.FromResult($"Error: {stopError}");
        }
    }

    private Task<string> HandleLogsAction(string? sessionId, int? tailLines, string? since)
    {
        // Validate sessionId
        if (!ParameterValidator.ValidateRequiredParameter(sessionId, "sessionId", out var errorMessage))
        {
            return Task.FromResult($"Error: {errorMessage}");
        }

        // Parse since parameter if provided
        DateTime? sinceTimestamp = null;
        if (!string.IsNullOrWhiteSpace(since))
        {
            if (!DateTime.TryParse(since, out var parsedSince))
            {
                return Task.FromResult($"Error: Invalid 'since' timestamp format. Expected ISO 8601 format (e.g., '2024-01-01T12:00:00Z').");
            }
            sinceTimestamp = parsedSince.ToUniversalTime();
        }

        // Validate tailLines if provided
        if (tailLines.HasValue && tailLines.Value < 1)
        {
            return Task.FromResult("Error: tailLines must be a positive integer.");
        }

        // Get logs from ProcessSessionManager
        var logs = _processSessionManager.GetSessionLogs(sessionId!, tailLines, sinceTimestamp);

        if (logs == null)
        {
            return Task.FromResult($"Error: Session '{sessionId}' not found. It may have already completed or been stopped.");
        }

        // Format the response

        // Plain text format
        var textResult = new StringBuilder();
        textResult.AppendLine($"Logs for session '{logs.SessionId}':");
        textResult.AppendLine($"Operation Type: {logs.OperationType}");
        textResult.AppendLine($"Target: {logs.Target}");
        textResult.AppendLine($"Start Time: {logs.StartTime:O}");
        textResult.AppendLine($"Status: {(logs.IsRunning ? "Running" : "Completed")}");
        textResult.AppendLine($"Total Output Lines: {logs.TotalOutputLines}");
        textResult.AppendLine($"Total Error Lines: {logs.TotalErrorLines}");
        
        if (tailLines.HasValue || sinceTimestamp.HasValue)
        {
            textResult.AppendLine($"Returned Output Lines: {logs.OutputLines.Length}");
            textResult.AppendLine($"Returned Error Lines: {logs.ErrorLines.Length}");
        }

        textResult.AppendLine();
        textResult.AppendLine("=== Output ===");

        // Combine output and error lines in chronological order
        var allLines = logs.OutputLines
            .Select(line => new { line.Timestamp, line.Content, IsError = false })
            .Concat(logs.ErrorLines.Select(line => new { line.Timestamp, line.Content, IsError = true }))
            .OrderBy(x => x.Timestamp)
            .ToList();

        if (allLines.Count == 0)
        {
            textResult.AppendLine("(no output yet)");
        }
        else
        {
            foreach (var line in allLines)
            {
                if (line.IsError)
                {
                    textResult.AppendLine($"[stderr] {line.Content}");
                }
                else
                {
                    textResult.AppendLine(line.Content);
                }
            }
        }

        return Task.FromResult(textResult.ToString());
    }

    private async Task<string> HandleListTemplateOptionsAction(string? template)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(template, "template", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        // Validate that the template exists
        var templateValidation = await ParameterValidator.ValidateTemplateAsync(template, _logger);
        if (!templateValidation.IsValid)
        {
            return $"Error: {templateValidation.ErrorMessage}";
        }

        return await ExecuteDotNetCommand($"new {template} --help");
    }

    private async Task<string> HandleSetPropertyAction(string? project, string? propertyName, string? propertyValue)
    {
        // Validate required project parameter
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            return $"Error: {projectError}";
        }

        // Validate required propertyName parameter
        if (!ParameterValidator.ValidateRequiredParameter(propertyName, "propertyName", out var nameError))
        {
            return $"Error: {nameError}";
        }

        // Validate required propertyValue parameter
        if (propertyValue is null)
        {
            return "Error: propertyValue is required for SetProperty action.";
        }

        return await ProjectAnalysisHelper.SetPropertyAsync(project!, propertyName!, propertyValue, _logger);
    }

    private async Task<string> HandleGetPropertyAction(string? project, string? propertyName)
    {
        // Validate required project parameter
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            return $"Error: {projectError}";
        }

        // Validate required propertyName parameter
        if (!ParameterValidator.ValidateRequiredParameter(propertyName, "propertyName", out var nameError))
        {
            return $"Error: {nameError}";
        }

        return await ProjectAnalysisHelper.GetPropertyAsync(project!, propertyName!, _logger);
    }

    private async Task<string> HandleRemovePropertyAction(string? project, string? propertyName)
    {
        // Validate required project parameter
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            return $"Error: {projectError}";
        }

        // Validate required propertyName parameter
        if (!ParameterValidator.ValidateRequiredParameter(propertyName, "propertyName", out var nameError))
        {
            return $"Error: {nameError}";
        }

        return await ProjectAnalysisHelper.RemovePropertyAsync(project!, propertyName!, _logger);
    }

    private async Task<string> HandleAddItemAction(string? project, string? itemType, string? include)
    {
        // Validate required project parameter
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            return $"Error: {projectError}";
        }

        // Validate required itemType parameter
        if (!ParameterValidator.ValidateRequiredParameter(itemType, "itemType", out var typeError))
        {
            return $"Error: {typeError}";
        }

        // Validate required include parameter
        if (!ParameterValidator.ValidateRequiredParameter(include, "include", out var includeError))
        {
            return $"Error: {includeError}";
        }

        return await ProjectAnalysisHelper.AddItemAsync(project!, itemType!, include!, logger: _logger);
    }

    private async Task<string> HandleRemoveItemAction(string? project, string? itemType, string? include)
    {
        // Validate required project parameter
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            return $"Error: {projectError}";
        }

        // Validate required itemType parameter
        if (!ParameterValidator.ValidateRequiredParameter(itemType, "itemType", out var typeError))
        {
            return $"Error: {typeError}";
        }

        // Validate required include parameter
        if (!ParameterValidator.ValidateRequiredParameter(include, "include", out var includeError))
        {
            return $"Error: {includeError}";
        }

        return await ProjectAnalysisHelper.RemoveItemAsync(project!, itemType!, include!, _logger);
    }

    private async Task<string> HandleListItemsAction(string? project, string? itemType)
    {
        // Validate required project parameter
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            return $"Error: {projectError}";
        }

        return await ProjectAnalysisHelper.ListItemsAsync(project!, itemType, _logger);
    }

    // ===== Watch helper methods (moved from DotNetCliTools.Watch.cs) =====

    /// <summary>
    /// Run a .NET project with file watching and hot reload. 
    /// Note: This is a long-running command that watches for file changes and automatically restarts the application. 
    /// It should be terminated by the user when no longer needed.
    /// </summary>
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    internal Task<string> DotnetWatchRun(
        string? project = null,
        string? appArgs = null,
        bool noHotReload = false)
    {
        var args = new StringBuilder("watch");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        args.Append(" run");
        if (noHotReload) args.Append(" --no-hot-reload");
        if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");
        return Task.FromResult("Warning: 'dotnet watch run' is a long-running command that requires interactive terminal support. " +
      "It will watch for file changes and automatically restart the application. " +
        "This command is best run directly in a terminal. " +
             $"Command that would be executed: dotnet {args}");
    }

    /// <summary>
    /// Run unit tests with file watching and automatic test re-runs. 
    /// Note: This is a long-running command that watches for file changes. It should be terminated by the user when no longer needed.
    /// </summary>
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    internal Task<string> DotnetWatchTest(
        string? project = null,
        string? filter = null)
    {
        var args = new StringBuilder("watch");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        args.Append(" test");
        if (!string.IsNullOrEmpty(filter)) args.Append($" --filter \"{filter}\"");
        return Task.FromResult("Warning: 'dotnet watch test' is a long-running command that requires interactive terminal support. " +
               "It will watch for file changes and automatically re-run tests. " +
    "This command is best run directly in a terminal. " +
  $"Command that would be executed: dotnet {args}");
    }

    /// <summary>
    /// Build a .NET project with file watching and automatic rebuild. 
    /// Note: This is a long-running command that watches for file changes. It should be terminated by the user when no longer needed.
    /// </summary>
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    internal Task<string> DotnetWatchBuild(
        string? project = null,
        string? configuration = null)
    {
        var args = new StringBuilder("watch");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        args.Append(" build");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        return Task.FromResult("Warning: 'dotnet watch build' is a long-running command that requires interactive terminal support. " +
   "It will watch for file changes and automatically rebuild. " +
         "This command is best run directly in a terminal. " +
   $"Command that would be executed: dotnet {args}");
    }

    // ===== Project helper methods (moved from DotNetCliTools.Project.cs) =====
    // Note: Some methods kept McpMeta attributes as they are referenced from helper methods
    // and might be useful for future maintenance

    /// <summary>
    /// Create a new .NET project from a template using the <c>dotnet new</c> command.
    /// Common templates: console, classlib, web, webapi, mvc, blazor, xunit, nunit, mstest.
    /// </summary>
    internal async Task<string> DotnetProjectNew(
        string? template = null,
        string? name = null,
        string? output = null,
        string? framework = null,
        string? additionalOptions = null)
    {
        // Validate additionalOptions first (security check before any other validation)
        if (!string.IsNullOrEmpty(additionalOptions) && !IsValidAdditionalOptions(additionalOptions))
        {
            return "Error: additionalOptions contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";
        }

        // Validate template
        var templateValidation = await ParameterValidator.ValidateTemplateAsync(template, _logger);
        if (!templateValidation.IsValid)
        {
            return $"Error: {templateValidation.ErrorMessage}";
        }

        // Validate framework
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
        {
            return $"Error: {frameworkError}";
        }

        var args = new StringBuilder($"new {template}");
        if (!string.IsNullOrEmpty(name)) args.Append($" -n \"{name}\"");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        if (!string.IsNullOrEmpty(additionalOptions)) args.Append($" {additionalOptions}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Restore the dependencies and tools of a .NET project.
    /// </summary>
    internal async Task<string> DotnetProjectRestore(
        string? project = null,
        string? verbosity = null,
        string? source = null,
        bool lockedMode = false,
        string? configFile = null)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
        {
            return $"Error: {projectError}";
        }

        // Validate verbosity
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        var args = new StringBuilder("restore");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(verbosity)) args.Append($" -v {verbosity}");
        if (!string.IsNullOrEmpty(source)) args.Append($" --source \"{source}\"");
        if (lockedMode) args.Append(" --locked-mode");
        if (!string.IsNullOrEmpty(configFile)) args.Append($" --configfile \"{configFile}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Build a .NET project and its dependencies.
    /// </summary>
    internal async Task<string> DotnetProjectBuild(
        string? project = null,
        string? configuration = null,
        string? framework = null,
        bool noRestore = false,
        string? verbosity = null,
        string? output = null)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
        {
            return $"Error: {projectError}";
        }

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";

        // Validate framework
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";

        // Validate verbosity
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        var args = new StringBuilder("build");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        if (noRestore) args.Append(" --no-restore");
        if (!string.IsNullOrEmpty(verbosity)) args.Append($" -v {verbosity}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");

        // Capture working directory for concurrency target selection
        var workingDir = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        return await ExecuteWithConcurrencyCheck("build", GetOperationTarget(project, workingDir), args.ToString());
    }

    /// <summary>
    /// Build and run a .NET project.
    /// </summary>
    internal async Task<string> DotnetProjectRun(
        string? project = null,
        string? configuration = null,
        string? appArgs = null,
        bool noBuild = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";

        var args = new StringBuilder("run");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (noBuild) args.Append(" --no-build");
        if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");

        // Capture working directory for concurrency target selection
        var workingDir = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        return await ExecuteWithConcurrencyCheck("run", GetOperationTarget(project, workingDir), args.ToString());
    }

    /// <summary>
    /// Run unit tests in a .NET project.
    /// Test runner can be explicitly specified or auto-detected from global.json.
    /// Auto mode (default) detects MTP from global.json config, otherwise uses VSTest for legacy compatibility.
    /// </summary>
    internal async Task<string> DotnetProjectTest(
        string? project = null,
        string? configuration = null,
        string? filter = null,
        string? collect = null,
        string? resultsDirectory = null,
        string? logger = null,
        bool noBuild = false,
        bool noRestore = false,
        string? verbosity = null,
        string? framework = null,
        bool blame = false,
        bool listTests = false,
        TestRunner? testRunner = null,
        bool useLegacyProjectArgument = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";

        // Validate verbosity
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        // Validate framework
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";

        // Determine the effective test runner
        TestRunner effectiveRunner;
        string selectionSource;
        
        // If useLegacyProjectArgument is true, use VSTest mode for backward compatibility
        if (useLegacyProjectArgument)
        {
            effectiveRunner = TestRunner.VSTest;
            selectionSource = "useLegacyProjectArgument-parameter";
        }
        // If testRunner is explicitly specified, use it
        else if (testRunner.HasValue && testRunner.Value != TestRunner.Auto)
        {
            effectiveRunner = testRunner.Value;
            selectionSource = "testRunner-parameter";
        }
        // Otherwise, auto-detect or default to Auto behavior
        else
        {
            var workingDirForDetection = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
            var (detectedRunner, detectionSource) = SdkIntegration.TestRunnerDetector.DetectTestRunner(
                workingDirectory: workingDirForDetection,
                projectPath: project,
                logger: _logger);
            effectiveRunner = detectedRunner;
            selectionSource = detectionSource;
        }

        // When MTP is detected and a project path is provided, ensure the working
        // directory is the project's directory so that `dotnet test` also walks up and
        // discovers global.json with the MTP runner config.  Without this, the CLI may
        // fall back to VSTest internally and choke on the `--project` flag (MSB1001).
        if (effectiveRunner == TestRunner.MicrosoftTestingPlatform
            && !string.IsNullOrEmpty(project)
            && string.IsNullOrEmpty(DotNetCommandExecutor.WorkingDirectoryOverride.Value))
        {
            var projectDir = Path.GetDirectoryName(Path.GetFullPath(project));
            if (!string.IsNullOrEmpty(projectDir))
            {
                DotNetCommandExecutor.WorkingDirectoryOverride.Value = projectDir;
            }
        }

        // Build the command
        var args = new StringBuilder("test");
        
        // Determine project argument style based on effective runner
        bool usePositionalArg = effectiveRunner == TestRunner.VSTest;
        
        if (!string.IsNullOrEmpty(project))
        {
            if (usePositionalArg)
            {
                // VSTest: positional argument
                args.Append($" \"{project}\"");
            }
            else
            {
                // MTP: --project flag
                args.Append($" --project \"{project}\"");
            }
        }
        
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(filter)) args.Append($" --filter \"{filter}\"");
        if (!string.IsNullOrEmpty(collect)) args.Append($" --collect \"{collect}\"");
        if (!string.IsNullOrEmpty(resultsDirectory)) args.Append($" --results-directory \"{resultsDirectory}\"");
        if (!string.IsNullOrEmpty(logger)) args.Append($" --logger \"{logger}\"");
        if (noBuild) args.Append(" --no-build");
        if (noRestore) args.Append(" --no-restore");
        if (!string.IsNullOrEmpty(verbosity)) args.Append($" --verbosity {verbosity}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (blame) args.Append(" --blame");
        if (listTests) args.Append(" --list-tests");

        // Use working directory for concurrency target selection
        var workingDirForTarget = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        return await ExecuteWithConcurrencyCheck("test", GetOperationTarget(project, workingDirForTarget), args.ToString());
    }

    /// <summary>
    /// Publish a .NET project for deployment.
    /// </summary>
    internal async Task<string> DotnetProjectPublish(
        string? project = null,
        string? configuration = null,
        string? output = null,
        string? runtime = null,
        string? framework = null,
        bool noRestore = false,
        bool noBuild = false,
        bool? selfContained = null,
        string? arch = null,
        string? os = null,
        string? verbosity = null)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";

        // Validate runtime identifier
        if (!ParameterValidator.ValidateRuntimeIdentifier(runtime, out var runtimeError))
            return $"Error: {runtimeError}";

        // Validate framework
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";

        // Validate verbosity
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        var args = new StringBuilder("publish");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(runtime)) args.Append($" -r {runtime}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        if (noRestore) args.Append(" --no-restore");
        if (noBuild) args.Append(" --no-build");
        if (selfContained.HasValue) args.Append($" --self-contained {(selfContained.Value ? "true" : "false")}");
        if (!string.IsNullOrEmpty(arch)) args.Append($" --arch {arch}");
        if (!string.IsNullOrEmpty(os)) args.Append($" --os {os}");
        if (!string.IsNullOrEmpty(verbosity)) args.Append($" -v {verbosity}");

        // Capture working directory for concurrency target selection
        var workingDir = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        return await ExecuteWithConcurrencyCheck("publish", GetOperationTarget(project, workingDir), args.ToString());
    }

    /// <summary>
    /// Clean the output of a .NET project.
    /// </summary>
    internal async Task<string> DotnetProjectClean(
        string? project = null,
        string? configuration = null,
        string? framework = null,
        string? verbosity = null)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";

        // Validate framework
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";

        // Validate verbosity
        if (!ParameterValidator.ValidateVerbosity(verbosity, out var verbosityError))
            return $"Error: {verbosityError}";

        var args = new StringBuilder("clean");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        if (!string.IsNullOrEmpty(verbosity)) args.Append($" -v {verbosity}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Analyze a .csproj file to extract comprehensive project information including target frameworks, 
    /// package references, project references, and build properties. Returns structured JSON.
    /// Does not require building the project.
    /// </summary>
    internal async Task<string> DotnetProjectAnalyze(string projectPath)
    {
        // Validate project path
        if (!ParameterValidator.ValidateProjectPath(projectPath, out var projectError))
            return $"Error: {projectError}";

        _logger.LogDebug("Analyzing project file: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.AnalyzeProjectAsync(projectPath, _logger);
    }

    /// <summary>
    /// Analyze project dependencies to build a dependency graph showing direct package and project dependencies.
    /// Returns structured JSON with dependency information. For transitive dependencies, use CLI commands.
    /// </summary>
    internal async Task<string> DotnetProjectDependencies(string projectPath)
    {
        // Validate project path
        if (!ParameterValidator.ValidateProjectPath(projectPath, out var projectError))
            return $"Error: {projectError}";

        _logger.LogDebug("Analyzing dependencies for: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.AnalyzeDependenciesAsync(projectPath, _logger);
    }

    /// <summary>
    /// Validate a .csproj file for common issues, deprecated packages, and configuration problems.
    /// Returns structured JSON with errors, warnings, and recommendations. Does not require building.
    /// </summary>
    internal async Task<string> DotnetProjectValidate(string projectPath)
    {
        // Validate project path
        if (!ParameterValidator.ValidateProjectPath(projectPath, out var projectError))
            return $"Error: {projectError}";

        _logger.LogDebug("Validating project: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.ValidateProjectAsync(projectPath, _logger);
    }

    /// <summary>
    /// Appends an AI-generated analysis section to a command result when the command failed
    /// and the MCP client supports sampling. Falls back gracefully when sampling is unavailable.
    /// </summary>
    private static async Task<string> AppendAiAnalysisOnFailureAsync(string result, McpServer? server, string promptPrefix)
    {
        if (server?.ClientCapabilities?.Sampling == null || !IsCommandFailure(result))
            return result;

        var chatClient = server.AsSamplingChatClient();
        try
        {
            var prompt = result.Length > MaxSamplingPromptLength ? result[^MaxSamplingPromptLength..] : result;
            var suggestion = await chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, $"{promptPrefix}\n\n{prompt}")],
                new ChatOptions { MaxOutputTokens = MaxSamplingResponseTokens });
            if (!string.IsNullOrWhiteSpace(suggestion.Text))
                result += $"\n\nAI Analysis:\n{suggestion.Text}";
        }
        catch (Exception)
        {
            // Sampling is best-effort; fall back gracefully to raw output
        }

        return result;
    }

    // ── Structured content builders for project dashboard ─────────────

    private static object? BuildNewProjectStructuredContent(string textResult, string? template, string? name, string? output, string? framework)
    {
        var success = textResult.Contains("was created successfully", StringComparison.OrdinalIgnoreCase);
        return new
        {
            action = "new",
            success,
            template = template ?? "unknown",
            projectName = name,
            outputDirectory = output,
            framework,
            summary = success ? "Project created successfully" : "Project creation failed"
        };
    }

    private static object? BuildBuildStructuredContent(string textResult, string? project, string? configuration)
    {
        var success = textResult.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase);
        var failed = textResult.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);
        if (!success && !failed) return null;

        // Extract warning and error counts
        var warningCount = 0;
        var errorCount = 0;
        foreach (var line in textResult.Split('\n'))
        {
            if (line.Contains("Warning(s)", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out var w)) warningCount = w;
            }
            else if (line.Contains("Error(s)", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out var e)) errorCount = e;
            }
        }

        return new
        {
            action = "build",
            buildResult = new
            {
                success,
                project,
                configuration = configuration ?? "Debug",
                warningCount,
                errorCount,
                summary = success
                    ? $"Build succeeded ({warningCount} warnings)"
                    : $"Build FAILED ({errorCount} errors, {warningCount} warnings)"
            }
        };
    }

    private static object? BuildAnalyzeStructuredContent(string textResult)
    {
        if (string.IsNullOrWhiteSpace(textResult) || textResult.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
            return null;

        return new
        {
            action = "analyze",
            analysisText = textResult
        };
    }

    private static object? BuildTemplateOptionsStructuredContent(string textResult, string? template)
    {
        if (string.IsNullOrWhiteSpace(textResult) || textResult.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
            return null;

        return new
        {
            action = "listTemplateOptions",
            template = template ?? "unknown",
            optionsText = textResult
        };
    }

    /// <summary>
    /// Detects whether a dotnet command output string represents a non-zero (failure) exit code.
    /// Parses the "Exit Code: N" line that DotNetCommandExecutor appends to every result.
    /// </summary>
    private static bool IsCommandFailure(string commandOutput)
    {
        const string prefix = "Exit Code: ";
        var idx = commandOutput.LastIndexOf(prefix, StringComparison.Ordinal);
        if (idx < 0) return false;
        var start = idx + prefix.Length;
        var end = commandOutput.IndexOfAny(['\r', '\n'], start);
        var codeSpan = (end >= 0 ? commandOutput[start..end] : commandOutput[start..]).AsSpan().Trim();
        return int.TryParse(codeSpan, out var code) && code != 0;
    }
}
