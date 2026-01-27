using System.Text;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging;
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
    /// <param name="sessionId">Session ID for stop action (required when action is Stop)</param>
    /// <param name="startMode">Start mode for run action (Foreground or Background). Foreground blocks until exit, Background returns immediately with sessionId. Default: Foreground</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool(IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/File%20Folder/Flat/file_folder_flat.svg")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["New","Restore","Build","Run","Test","Publish","Clean","Analyze","Dependencies","Validate","Pack","Watch","Format","Stop"]""")]
    public async partial Task<string> DotnetProject(
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
        bool machineReadable = false)
    {
        return await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action parameter
            if (!ParameterValidator.ValidateAction<DotnetProjectAction>(action, out var errorMessage))
            {
                if (machineReadable)
                {
                    var validActions = Enum.GetNames(typeof(DotnetProjectAction));
                    var error = ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        validActions,
                        toolName: "dotnet_project");
                    return ErrorResultFactory.ToJson(error);
                }
                return $"Error: {errorMessage}";
            }

            // Route to appropriate handler based on action
            return action switch
            {
                DotnetProjectAction.New => await HandleNewAction(template, name, output, framework, additionalOptions, machineReadable),
                DotnetProjectAction.Restore => await HandleRestoreAction(project, machineReadable),
                DotnetProjectAction.Build => await HandleBuildAction(project, configuration, framework, machineReadable),
                DotnetProjectAction.Run => await HandleRunAction(project, configuration, appArgs, noBuild, startMode, machineReadable),
                DotnetProjectAction.Test => await HandleTestAction(project, configuration, filter, collect, resultsDirectory, logger, noBuild, noRestore, verbosity, framework, blame, listTests, testRunner, useLegacyProjectArgument, machineReadable),
                DotnetProjectAction.Publish => await HandlePublishAction(project, configuration, output, runtime, machineReadable),
                DotnetProjectAction.Clean => await HandleCleanAction(project, configuration, machineReadable),
                DotnetProjectAction.Analyze => await HandleAnalyzeAction(projectPath, machineReadable),
                DotnetProjectAction.Dependencies => await HandleDependenciesAction(projectPath, machineReadable),
                DotnetProjectAction.Validate => await HandleValidateAction(projectPath, machineReadable),
                DotnetProjectAction.Pack => await HandlePackAction(project, configuration, output, includeSymbols, includeSource, machineReadable),
                DotnetProjectAction.Watch => await HandleWatchAction(watchAction, project, configuration, appArgs, filter, noHotReload, machineReadable),
                DotnetProjectAction.Format => await HandleFormatAction(project, verify, includeGenerated, diagnostics, severity, machineReadable),
                DotnetProjectAction.Stop => await HandleStopAction(sessionId, machineReadable),
                _ => machineReadable
                    ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateValidationError(
                        $"Action '{action}' is not supported.",
                        parameterName: "action",
                        reason: "not supported"))
                    : $"Error: Action '{action}' is not supported."
            };
        });
    }

    private async Task<string> HandleNewAction(string? template, string? name, string? output, string? framework, string? additionalOptions, bool machineReadable)
    {
        // Route to existing DotnetProjectNew method
        return await DotnetProjectNew(
            template: template,
            name: name,
            output: output,
            framework: framework,
            additionalOptions: additionalOptions,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleRestoreAction(string? project, bool machineReadable)
    {
        // Route to existing DotnetProjectRestore method
        return await DotnetProjectRestore(
            project: project,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleBuildAction(string? project, string? configuration, string? framework, bool machineReadable)
    {
        // Route to existing DotnetProjectBuild method
        return await DotnetProjectBuild(
            project: project,
            configuration: configuration,
            framework: framework,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleRunAction(string? project, string? configuration, string? appArgs, bool? noBuild, StartMode? startMode, bool machineReadable)
    {
        var effectiveStartMode = startMode ?? StartMode.Foreground;

        // If foreground mode, use existing behavior
        if (effectiveStartMode == StartMode.Foreground)
        {
            return await DotnetProjectRun(
                project: project,
                configuration: configuration,
                appArgs: appArgs,
                noBuild: noBuild ?? false,
                machineReadable: machineReadable);
        }

        // Background mode - start process and return immediately with session metadata
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    projectError!,
                    parameterName: "project",
                    reason: "invalid extension");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {projectError}";
        }

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    configError!,
                    parameterName: "configuration",
                    reason: "invalid value");
                return ErrorResultFactory.ToJson(error);
            }
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

                    if (machineReadable)
                    {
                        var error = ErrorResultFactory.CreateValidationError(
                            "Failed to register process session. Session ID may already exist.",
                            parameterName: "sessionId",
                            reason: "duplicate");
                        return ErrorResultFactory.ToJson(error);
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
                if (machineReadable)
                {
                    var result = new SuccessResult
                    {
                        Success = true,
                        Output = $"Process started in background mode",
                        ExitCode = 0,
                        Metadata = new Dictionary<string, string>
                        {
                            ["sessionId"] = sessionId,
                            ["pid"] = process.Id.ToString(),
                            ["operationType"] = "run",
                            ["target"] = target,
                            ["startMode"] = "background"
                        }
                    };
                    return ErrorResultFactory.ToJson(result);
                }

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
            
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    $"Failed to start process: {ex.Message}",
                    parameterName: "startMode",
                    reason: "process start failed");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: Failed to start process: {ex.Message}";
        }
    }

    private async Task<string> HandleTestAction(string? project, string? configuration, string? filter, string? collect, string? resultsDirectory, string? logger, bool? noBuild, bool? noRestore, string? verbosity, string? framework, bool? blame, bool? listTests, TestRunner? testRunner, bool? useLegacyProjectArgument, bool machineReadable)
    {
        // Route to existing DotnetProjectTest method
        return await DotnetProjectTest(
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
            useLegacyProjectArgument: useLegacyProjectArgument ?? false,
            machineReadable: machineReadable);
    }

    private async Task<string> HandlePublishAction(string? project, string? configuration, string? output, string? runtime, bool machineReadable)
    {
        // Route to existing DotnetProjectPublish method
        return await DotnetProjectPublish(
            project: project,
            configuration: configuration,
            output: output,
            runtime: runtime,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleCleanAction(string? project, string? configuration, bool machineReadable)
    {
        // Route to existing DotnetProjectClean method
        return await DotnetProjectClean(
            project: project,
            configuration: configuration,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleAnalyzeAction(string? projectPath, bool machineReadable)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(projectPath, "projectPath", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "projectPath",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetProjectAnalyze method
        return await DotnetProjectAnalyze(projectPath: projectPath!);
    }

    private async Task<string> HandleDependenciesAction(string? projectPath, bool machineReadable)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(projectPath, "projectPath", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "projectPath",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetProjectDependencies method
        return await DotnetProjectDependencies(projectPath: projectPath!);
    }

    private async Task<string> HandleValidateAction(string? projectPath, bool machineReadable)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(projectPath, "projectPath", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "projectPath",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetProjectValidate method
        return await DotnetProjectValidate(projectPath: projectPath!);
    }

    private async Task<string> HandlePackAction(string? project, string? configuration, string? output, bool? includeSymbols, bool? includeSource, bool machineReadable)
    {
        // Route to existing DotnetPackCreate method
        return await DotnetPackCreate(
            project: project,
            configuration: configuration,
            output: output,
            includeSymbols: includeSymbols ?? false,
            includeSource: includeSource ?? false,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleWatchAction(string? watchAction, string? project, string? configuration, string? appArgs, string? filter, bool? noHotReload, bool machineReadable)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(watchAction, "watchAction", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "watchAction is required for Watch action. Valid values: run, test, build",
                    parameterName: "watchAction",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: watchAction is required for Watch action. Valid values: run, test, build";
        }

        // Validate watchAction value
        var validWatchActions = new[] { "run", "test", "build" };
        if (!validWatchActions.Contains(watchAction!.ToLowerInvariant()))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    $"Invalid watchAction '{watchAction}'. Valid values: run, test, build",
                    parameterName: "watchAction",
                    reason: "invalid value");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: Invalid watchAction '{watchAction}'. Valid values: run, test, build";
        }

        // Route to appropriate watch method based on watchAction
        return watchAction.ToLowerInvariant() switch
        {
            "run" => await DotnetWatchRun(project: project, appArgs: appArgs, noHotReload: noHotReload ?? false),
            "test" => await DotnetWatchTest(project: project, filter: filter),
            "build" => await DotnetWatchBuild(project: project, configuration: configuration),
            _ => machineReadable
                ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateValidationError(
                    $"Invalid watchAction '{watchAction}'. Valid values: run, test, build",
                    parameterName: "watchAction",
                    reason: "invalid value"))
                : $"Error: Invalid watchAction '{watchAction}'. Valid values: run, test, build"
        };
    }

    private async Task<string> HandleFormatAction(string? project, bool? verify, bool? includeGenerated, string? diagnostics, string? severity, bool machineReadable)
    {
        // Route to existing DotnetFormat method
        return await DotnetFormat(
            project: project,
            verify: verify ?? false,
            includeGenerated: includeGenerated ?? false,
            diagnostics: diagnostics,
            severity: severity,
            machineReadable: machineReadable);
    }

    private Task<string> HandleStopAction(string? sessionId, bool machineReadable)
    {
        // Validate required parameter
        if (!ParameterValidator.ValidateRequiredParameter(sessionId, "sessionId", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "sessionId",
                    reason: "required");
                return Task.FromResult(ErrorResultFactory.ToJson(error));
            }
            return Task.FromResult($"Error: {errorMessage}");
        }

        // Try to stop the session
        if (_processSessionManager.TryStopSession(sessionId!, out var stopError))
        {
            if (machineReadable)
            {
                var result = new SuccessResult
                {
                    Success = true,
                    Output = $"Successfully stopped session '{sessionId}'",
                    ExitCode = 0,
                    Metadata = new Dictionary<string, string>
                    {
                        ["sessionId"] = sessionId!,
                        ["stopped"] = "true"
                    }
                };
                return Task.FromResult(ErrorResultFactory.ToJson(result));
            }
            return Task.FromResult($"Successfully stopped session '{sessionId}'");
        }
        else
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    stopError!,
                    parameterName: "sessionId",
                    reason: "not found or already stopped");
                return Task.FromResult(ErrorResultFactory.ToJson(error));
            }
            return Task.FromResult($"Error: {stopError}");
        }
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
        string? additionalOptions = null,
        bool machineReadable = false)
    {
        // Validate additionalOptions first (security check before any other validation)
        if (!string.IsNullOrEmpty(additionalOptions) && !IsValidAdditionalOptions(additionalOptions))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "additionalOptions contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.",
                    parameterName: "additionalOptions",
                    reason: "invalid characters");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: additionalOptions contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";
        }

        // Validate template
        var templateValidation = await ParameterValidator.ValidateTemplateAsync(template, _logger);
        if (!templateValidation.IsValid)
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    templateValidation.ErrorMessage!,
                    parameterName: "template",
                    reason: string.IsNullOrWhiteSpace(template) ? "required" : "not found");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {templateValidation.ErrorMessage}";
        }

        // Validate framework
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    frameworkError!,
                    parameterName: "framework",
                    reason: "invalid format");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {frameworkError}";
        }

        var args = new StringBuilder($"new {template}");
        if (!string.IsNullOrEmpty(name)) args.Append($" -n \"{name}\"");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        if (!string.IsNullOrEmpty(additionalOptions)) args.Append($" {additionalOptions}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Restore the dependencies and tools of a .NET project.
    /// </summary>
    internal async Task<string> DotnetProjectRestore(
        string? project = null,
        bool machineReadable = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    projectError!,
                    parameterName: "project",
                    reason: "invalid extension");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {projectError}";
        }

        var args = "restore";
        if (!string.IsNullOrEmpty(project)) args += $" \"{project}\"";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    /// <summary>
    /// Build a .NET project and its dependencies.
    /// </summary>
    internal async Task<string> DotnetProjectBuild(
        string? project = null,
        string? configuration = null,
        string? framework = null,
        bool machineReadable = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    projectError!,
                    parameterName: "project",
                    reason: "invalid extension");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {projectError}";
        }

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";

        // Validate framework
        if (!ParameterValidator.ValidateFramework(framework, out var frameworkError))
            return $"Error: {frameworkError}";

        var args = new StringBuilder("build");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");

        // Capture working directory for concurrency target selection
        var workingDir = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        return await ExecuteWithConcurrencyCheck("build", GetOperationTarget(project, workingDir), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Build and run a .NET project.
    /// </summary>
    internal async Task<string> DotnetProjectRun(
        string? project = null,
        string? configuration = null,
        string? appArgs = null,
        bool noBuild = false,
        bool machineReadable = false)
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
        return await ExecuteWithConcurrencyCheck("run", GetOperationTarget(project, workingDir), args.ToString(), machineReadable);
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
        bool useLegacyProjectArgument = false,
        bool machineReadable = false)
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

        // Build the command
        var args = new StringBuilder("test");
        
        // Determine project argument style based on effective runner
        bool usePositionalArg = effectiveRunner == TestRunner.VSTest;
        string projectArgumentStyle;
        
        if (!string.IsNullOrEmpty(project))
        {
            if (usePositionalArg)
            {
                // VSTest: positional argument
                args.Append($" \"{project}\"");
                projectArgumentStyle = "positional";
            }
            else
            {
                // MTP: --project flag
                args.Append($" --project \"{project}\"");
                projectArgumentStyle = "--project";
            }
        }
        else
        {
            projectArgumentStyle = "none";
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

        // Store metadata for machine-readable output
        var metadata = new Dictionary<string, string>
        {
            ["selectedTestRunner"] = effectiveRunner == TestRunner.MicrosoftTestingPlatform ? "microsoft-testing-platform" : "vstest",
            ["projectArgumentStyle"] = projectArgumentStyle,
            ["selectionSource"] = selectionSource
        };
        
        // Use working directory for concurrency target selection
        var workingDirForTarget = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        return await ExecuteWithConcurrencyCheck("test", GetOperationTarget(project, workingDirForTarget), args.ToString(), machineReadable, metadata);
    }

    /// <summary>
    /// Publish a .NET project for deployment.
    /// </summary>
    internal async Task<string> DotnetProjectPublish(
        string? project = null,
        string? configuration = null,
        string? output = null,
        string? runtime = null,
        bool machineReadable = false)
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

        var args = new StringBuilder("publish");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(runtime)) args.Append($" -r {runtime}");

        // Capture working directory for concurrency target selection
        var workingDir = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        return await ExecuteWithConcurrencyCheck("publish", GetOperationTarget(project, workingDir), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Clean the output of a .NET project.
    /// </summary>
    internal async Task<string> DotnetProjectClean(
        string? project = null,
        string? configuration = null,
        bool machineReadable = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";

        var args = new StringBuilder("clean");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
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
}
