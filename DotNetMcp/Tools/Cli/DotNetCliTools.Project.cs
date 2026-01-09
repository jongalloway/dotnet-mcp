using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Project tools for building, running, testing, and managing .NET projects.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Create a new .NET project from a template using the <c>dotnet new</c> command.
    /// Common templates: console, classlib, web, webapi, mvc, blazor, xunit, nunit, mstest.
    /// </summary>
    /// <param name="template">The template to use (e.g., 'console', 'classlib', 'webapi')</param>
    /// <param name="name">The name for the project</param>
    /// <param name="output">The output directory</param>
    /// <param name="framework">The target framework (e.g., 'net10.0', 'net8.0')</param>
    /// <param name="additionalOptions">Additional template-specific options (e.g., '--format slnx', '--use-program-main', '--aot')</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["project","create","new","template","initialization"]""")]
    public async Task<string> DotnetProjectNew(
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
    /// <param name="project">The project file or solution file to restore</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "project")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["project","restore","dependencies","packages","setup"]""")]
    public async Task<string> DotnetProjectRestore(
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
    /// <param name="project">The project file or solution file to build</param>
    /// <param name="configuration">The configuration to build (Debug or Release)</param>
    /// <param name="framework">Build for a specific framework</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","build","compile","compilation"]""")]
    public async Task<string> DotnetProjectBuild(
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

        return await ExecuteWithConcurrencyCheck("build", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Build and run a .NET project.
    /// </summary>
    /// <param name="project">The project file to run</param>
    /// <param name="configuration">The configuration to use (Debug or Release)</param>
    /// <param name="appArgs">Arguments to pass to the application</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","run","execute","launch","development"]""")]
    public async Task<string> DotnetProjectRun(
        string? project = null,
        string? configuration = null,
        string? appArgs = null,
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
        if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");

        return await ExecuteWithConcurrencyCheck("run", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Run unit tests in a .NET project.
    /// </summary>
    /// <param name="project">The project file or solution file to test</param>
    /// <param name="configuration">The configuration to test (Debug or Release)</param>
    /// <param name="filter">Filter to run specific tests</param>
    /// <param name="collect">The friendly name of the data collector (e.g., 'XPlat Code Coverage')</param>
    /// <param name="resultsDirectory">The directory where test results will be placed</param>
    /// <param name="logger">The logger to use for test results (e.g., 'trx', 'console;verbosity=detailed')</param>
    /// <param name="noBuild">Do not build the project before testing</param>
    /// <param name="noRestore">Do not restore the project before building</param>
    /// <param name="verbosity">Set the MSBuild verbosity level (quiet, minimal, normal, detailed, diagnostic)</param>
    /// <param name="framework">The target framework to test for</param>
    /// <param name="blame">Run tests in blame mode to isolate problematic tests</param>
    /// <param name="listTests">List discovered tests without running them</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","test","testing","unit-test","validation"]""")]
    public async Task<string> DotnetProjectTest(
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

        var args = new StringBuilder("test");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
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

        return await ExecuteWithConcurrencyCheck("test", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Publish a .NET project for deployment.
    /// </summary>
    /// <param name="project">The project file to publish</param>
    /// <param name="configuration">The configuration to publish (Debug or Release)</param>
    /// <param name="output">The output directory for published files</param>
    /// <param name="runtime">The target runtime identifier (e.g., 'linux-x64', 'win-x64')</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "project")]
    [McpMeta("priority", 7.0)]
    [McpMeta("isLongRunning", true)]
    public async Task<string> DotnetProjectPublish(
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

        return await ExecuteWithConcurrencyCheck("publish", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Clean the output of a .NET project.
    /// </summary>
    /// <param name="project">The project file or solution file to clean</param>
    /// <param name="configuration">The configuration to clean (Debug or Release)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "project")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetProjectClean(
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
    /// <param name="projectPath">Path to the .csproj file to analyze</param>
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["project","analyze","introspection","metadata"]""")]
    public async Task<string> DotnetProjectAnalyze(string projectPath)
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
    /// <param name="projectPath">Path to the .csproj file to analyze</param>
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["project","dependencies","analyze","packages"]""")]
    public async Task<string> DotnetProjectDependencies(string projectPath)
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
    /// <param name="projectPath">Path to the .csproj file to validate</param>
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["project","validate","health-check","diagnostics"]""")]
    public async Task<string> DotnetProjectValidate(string projectPath)
    {
        // Validate project path
        if (!ParameterValidator.ValidateProjectPath(projectPath, out var projectError))
            return $"Error: {projectError}";

        _logger.LogDebug("Validating project: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.ValidateProjectAsync(projectPath, _logger);
    }
}
