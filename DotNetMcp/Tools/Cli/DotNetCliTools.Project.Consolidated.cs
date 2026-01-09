using System.Text;
using DotNetMcp.Actions;
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["New","Restore","Build","Run","Test","Publish","Clean","Analyze","Dependencies","Validate","Pack","Watch","Format"]""")]
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
                DotnetProjectAction.Run => await HandleRunAction(project, configuration, appArgs, machineReadable),
                DotnetProjectAction.Test => await HandleTestAction(project, configuration, filter, collect, resultsDirectory, logger, noBuild, noRestore, verbosity, framework, blame, listTests, machineReadable),
                DotnetProjectAction.Publish => await HandlePublishAction(project, configuration, output, runtime, machineReadable),
                DotnetProjectAction.Clean => await HandleCleanAction(project, configuration, machineReadable),
                DotnetProjectAction.Analyze => await HandleAnalyzeAction(projectPath, machineReadable),
                DotnetProjectAction.Dependencies => await HandleDependenciesAction(projectPath, machineReadable),
                DotnetProjectAction.Validate => await HandleValidateAction(projectPath, machineReadable),
                DotnetProjectAction.Pack => await HandlePackAction(project, configuration, output, includeSymbols, includeSource, machineReadable),
                DotnetProjectAction.Watch => await HandleWatchAction(watchAction, project, configuration, appArgs, filter, noHotReload, machineReadable),
                DotnetProjectAction.Format => await HandleFormatAction(project, verify, includeGenerated, diagnostics, severity, machineReadable),
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

    private async Task<string> HandleRunAction(string? project, string? configuration, string? appArgs, bool machineReadable)
    {
        // Route to existing DotnetProjectRun method
        return await DotnetProjectRun(
            project: project,
            configuration: configuration,
            appArgs: appArgs,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleTestAction(string? project, string? configuration, string? filter, string? collect, string? resultsDirectory, string? logger, bool? noBuild, bool? noRestore, string? verbosity, string? framework, bool? blame, bool? listTests, bool machineReadable)
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
}
