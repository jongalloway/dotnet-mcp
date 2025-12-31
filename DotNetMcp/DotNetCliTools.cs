using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

[McpServerToolType]
public sealed partial class DotNetCliTools
{
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    // Constants for server capability discovery
    private const string DefaultServerVersion = "1.0.0";
    private const string ProtocolVersion = "0.5.0-preview.1";

    public DotNetCliTools(ILogger<DotNetCliTools> logger, ConcurrencyManager concurrencyManager)
    {
        // DI guarantees logger is never null
        _logger = logger!;
        _concurrencyManager = concurrencyManager!;
    }

    /// <summary>
    /// List all installed .NET templates with their metadata using the Template Engine. 
    /// Provides structured information about available project templates.
    /// </summary>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("priority", 10.0)]
    [McpMeta("tags", JsonValue = """["template","list","discovery","project-creation"]""")]
    public async partial Task<string> DotnetTemplateList(bool forceReload = false)
          => await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload, _logger);

    /// <summary>
    /// Search for .NET templates by name or description. Returns matching templates with their details.
    /// </summary>
    /// <param name="searchTerm">Search term to find templates (searches in name, short name, and description)</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async partial Task<string> DotnetTemplateSearch(string searchTerm, bool forceReload = false)
        => await TemplateEngineHelper.SearchTemplatesAsync(searchTerm, forceReload, _logger);

    /// <summary>
    /// Get detailed information about a specific template including available parameters and options.
    /// </summary>
    /// <param name="templateShortName">The template short name (e.g., 'console', 'webapi', 'classlib')</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async partial Task<string> DotnetTemplateInfo(string templateShortName, bool forceReload = false)
        => await TemplateEngineHelper.GetTemplateDetailsAsync(templateShortName, forceReload, _logger);

    /// <summary>
    /// Clear all caches (templates, SDK, runtime) to force reload from disk. 
    /// Use this after installing or uninstalling templates or SDK versions. Also resets all cache metrics.
    /// </summary>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async partial Task<string> DotnetTemplateClearCache()
    {
        await DotNetResources.ClearAllCachesAsync();
        return "All caches (templates, SDK, runtime) and metrics cleared successfully. Next query will reload from disk.";
    }

    /// <summary>
    /// Get cache metrics showing hit/miss statistics for templates, SDK, and runtime information.
    /// </summary>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public partial Task<string> DotnetCacheMetrics()
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine("Cache Metrics:");
        result.AppendLine();
        result.AppendLine($"Templates: {TemplateEngineHelper.Metrics}");
        result.AppendLine($"SDK Info: {DotNetResources.GetSdkMetrics()}");
        result.AppendLine($"Runtime Info: {DotNetResources.GetRuntimeMetrics()}");
        return Task.FromResult(result.ToString());
    }

    /// <summary>
    /// Get information about .NET framework versions, including which are LTS releases. 
    /// Useful for understanding framework compatibility.
    /// </summary>
    /// <param name="framework">Optional: specific framework to get info about (e.g., 'net8.0', 'net6.0')</param>
    [McpServerTool]
    [McpMeta("category", "framework")]
    [McpMeta("usesFrameworkHelper", true)]
    public async partial Task<string> DotnetFrameworkInfo(string? framework = null)
    {
        var result = new StringBuilder();

        if (!string.IsNullOrEmpty(framework))
        {
            result.AppendLine($"Framework: {framework}");
            result.AppendLine($"Description: {FrameworkHelper.GetFrameworkDescription(framework)}");
            result.AppendLine($"Is LTS: {FrameworkHelper.IsLtsFramework(framework)}");
            result.AppendLine($"Is Modern .NET: {FrameworkHelper.IsModernNet(framework)}");
            result.AppendLine($"Is .NET Core: {FrameworkHelper.IsNetCore(framework)}");
            result.AppendLine($"Is .NET Framework: {FrameworkHelper.IsNetFramework(framework)}");
            result.AppendLine($"Is .NET Standard: {FrameworkHelper.IsNetStandard(framework)}");
        }
        else
        {
            var supportedModernFrameworks = FrameworkHelper.GetSupportedModernFrameworks().ToList();

            // Only show preview TFMs when the SDK major version is installed.
            try
            {
                var sdkList = await DotNetCommandExecutor.ExecuteCommandForResourceAsync("--list-sdks", _logger);
                var hasNet11Sdk = sdkList
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Any(line => line.TrimStart().StartsWith("11.", StringComparison.Ordinal));
                if (hasNet11Sdk)
                {
                    supportedModernFrameworks.Insert(0, DotNetSdkConstants.TargetFrameworks.Net110);
                }
            }
            catch
            {
                // If SDK discovery fails, fall back to stable list.
            }

            result.AppendLine("Modern .NET Frameworks (5.0+):");
            foreach (var fw in supportedModernFrameworks)
            {
                var ltsMarker = FrameworkHelper.IsLtsFramework(fw) ? " (LTS)" : string.Empty;
                result.AppendLine($"  {fw}{ltsMarker} - {FrameworkHelper.GetFrameworkDescription(fw)}");
            }

            result.AppendLine();
            result.AppendLine(".NET Core Frameworks:");
            foreach (var fw in FrameworkHelper.GetSupportedNetCoreFrameworks())
            {
                var ltsMarker = FrameworkHelper.IsLtsFramework(fw) ? " (LTS)" : string.Empty;
                result.AppendLine($"  {fw}{ltsMarker} - {FrameworkHelper.GetFrameworkDescription(fw)}");
            }

            result.AppendLine();
            result.AppendLine($"Latest Recommended: {FrameworkHelper.GetLatestRecommendedFramework()}");
            result.AppendLine($"Latest LTS: {FrameworkHelper.GetLatestLtsFramework()}");
        }

        return result.ToString();
    }

    /// <summary>
    /// Create a new .NET project or file from a template. 
    /// Common templates: console, classlib, web, webapi, mvc, blazor, xunit, nunit, mstest.
    /// </summary>
    /// <param name="template">The template to use (e.g., 'console', 'classlib', 'webapi')</param>
    /// <param name="name">The name for the project</param>
    /// <param name="output">The output directory</param>
    /// <param name="framework">The target framework (e.g., 'net10.0', 'net8.0')</param>
    /// <param name="additionalOptions">Additional template-specific options (e.g., '--format slnx', '--use-program-main', '--aot')</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["project","create","new","template","initialization"]""")]
    public async partial Task<string> DotnetProjectNew(
        string? template = null,
        string? name = null,
        string? output = null,
        string? framework = null,
        string? additionalOptions = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(template))
            return "Error: template parameter is required.";

        // Validate additionalOptions to prevent injection attempts
        if (!string.IsNullOrEmpty(additionalOptions) && !IsValidAdditionalOptions(additionalOptions))
            return "Error: additionalOptions contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";

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
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["project","restore","dependencies","packages","setup"]""")]
    public async partial Task<string> DotnetProjectRestore(
        string? project = null,
        bool machineReadable = false)
    {
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
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","build","compile","compilation"]""")]
    public async partial Task<string> DotnetProjectBuild(
        string? project = null,
        string? configuration = null,
        string? framework = null,
        bool machineReadable = false)
    {
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
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","run","execute","launch","development"]""")]
    public async partial Task<string> DotnetProjectRun(
        string? project = null,
        string? configuration = null,
        string? appArgs = null,
        bool machineReadable = false)
    {
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
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","test","testing","unit-test","validation"]""")]
    public async partial Task<string> DotnetProjectTest(
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
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 7.0)]
    [McpMeta("isLongRunning", true)]
    public async partial Task<string> DotnetProjectPublish(
        string? project = null,
        string? configuration = null,
        string? output = null,
        string? runtime = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("publish");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(runtime)) args.Append($" -r {runtime}");

        return await ExecuteWithConcurrencyCheck("publish", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Create a NuGet package from a .NET project. Use this to pack projects for distribution on NuGet.org or private feeds.
    /// </summary>
    /// <param name="project">The project file to pack</param>
    /// <param name="configuration">The configuration to pack (Debug or Release)</param>
    /// <param name="output">The output directory for the package</param>
    /// <param name="includeSymbols">Include symbols package</param>
    /// <param name="includeSource">Include source files in the package</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "package")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetPackCreate(
        string? project = null,
        string? configuration = null,
        string? output = null,
        bool includeSymbols = false,
        bool includeSource = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("pack");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (includeSymbols) args.Append(" --include-symbols");
        if (includeSource) args.Append(" --include-source");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Clean the output of a .NET project.
    /// </summary>
    /// <param name="project">The project file or solution file to clean</param>
    /// <param name="configuration">The configuration to clean (Debug or Release)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetProjectClean(
        string? project = null,
        string? configuration = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("clean");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Run a .NET project with file watching and hot reload. 
    /// Note: This is a long-running command that watches for file changes and automatically restarts the application. 
    /// It should be terminated by the user when no longer needed.
    /// </summary>
    /// <param name="project">The project file to run</param>
    /// <param name="appArgs">Arguments to pass to the application</param>
    /// <param name="noHotReload">Disable hot reload</param>
    [McpServerTool]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public partial Task<string> DotnetWatchRun(
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
    /// <param name="project">The project file or solution file to test</param>
    /// <param name="filter">Filter to run specific tests</param>
    [McpServerTool]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public partial Task<string> DotnetWatchTest(
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
    /// <param name="project">The project file or solution file to build</param>
    /// <param name="configuration">The configuration to build (Debug or Release)</param>
    [McpServerTool]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public partial Task<string> DotnetWatchBuild(
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

    /// <summary>
    /// Add a NuGet package reference to a .NET project.
    /// </summary>
    /// <param name="packageName">The name of the NuGet package to add</param>
    /// <param name="project">The project file to add the package to</param>
    /// <param name="version">The version of the package</param>
    /// <param name="prerelease">Include prerelease packages</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "package")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["package","add","nuget","dependency","install"]""")]
    public async partial Task<string> DotnetPackageAdd(
        string packageName,
        string? project = null,
        string? version = null,
        bool prerelease = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Add a project-to-project reference.
    /// </summary>
    /// <param name="project">The project file to add the reference from</param>
    /// <param name="reference">The project file to reference</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetReferenceAdd(
        string project,
        string reference,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"add \"{project}\" reference \"{reference}\"", machineReadable);

    /// <summary>
    /// List package references for a .NET project.
    /// </summary>
    /// <param name="project">The project file or solution file</param>
    /// <param name="outdated">Show outdated packages</param>
    /// <param name="deprecated">Show deprecated packages</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "package")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetPackageList(
        string? project = null,
        bool outdated = false,
        bool deprecated = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("list");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append(" package");
        if (outdated) args.Append(" --outdated");
        if (deprecated) args.Append(" --deprecated");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Remove a NuGet package reference from a .NET project.
    /// </summary>
    /// <param name="packageName">The name of the NuGet package to remove</param>
    /// <param name="project">The project file to remove the package from</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "package")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetPackageRemove(
        string packageName,
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("remove");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Search for NuGet packages on nuget.org. Returns matching packages with descriptions and download counts.
    /// </summary>
    /// <param name="searchTerm">Search term to find packages</param>
    /// <param name="take">Maximum number of results to return (1-100)</param>
    /// <param name="skip">Skip the first N results</param>
    /// <param name="prerelease">Include prerelease packages</param>
    /// <param name="exactMatch">Show exact matches only</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "package")]
    [McpMeta("priority", 7.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["package","search","nuget","discovery","find"]""")]
    public async partial Task<string> DotnetPackageSearch(
        string searchTerm,
        int? take = null,
        int? skip = null,
        bool prerelease = false,
        bool exactMatch = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder($"package search {searchTerm}");
        if (take.HasValue) args.Append($" --take {take.Value}");
        if (skip.HasValue) args.Append($" --skip {skip.Value}");
        if (prerelease) args.Append(" --prerelease");
        if (exactMatch) args.Append(" --exact-match");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Update a NuGet package reference to a newer version in a .NET project. 
    /// Note: This uses 'dotnet add package' which updates the package when a newer version is specified.
    /// </summary>
    /// <param name="packageName">The name of the NuGet package to update</param>
    /// <param name="project">The project file to update the package in</param>
    /// <param name="version">The version to update to</param>
    /// <param name="prerelease">Update to the latest prerelease version</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "package")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetPackageUpdate(
        string packageName,
        string? project = null,
        string? version = null,
        bool prerelease = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List project references.
    /// </summary>
    /// <param name="project">The project file</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetReferenceList(
        string? project = null,
        bool machineReadable = false)
    {
        var args = "list";
        if (!string.IsNullOrEmpty(project)) args += $" \"{project}\"";
        args += " reference";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    /// <summary>
    /// Remove a project-to-project reference.
    /// </summary>
    /// <param name="project">The project file to remove the reference from</param>
    /// <param name="reference">The project file to unreference</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetReferenceRemove(
        string project,
        string reference,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"remove \"{project}\" reference \"{reference}\"", machineReadable);

    /// <summary>
    /// Create a new .NET solution file. A solution file organizes multiple related projects.
    /// </summary>
    /// <param name="name">The name for the solution file</param>
    /// <param name="output">The output directory for the solution file</param>
    /// <param name="format">The solution file format: 'sln' (classic) or 'slnx' (XML-based). Default is 'sln'.</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["solution","create","new","organization","multi-project"]""")]
    public async partial Task<string> DotnetSolutionCreate(
        string name,
        string? output = null,
        string? format = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("new sln");
        args.Append($" -n \"{name}\"");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(format))
        {
            if (format != "sln" && format != "slnx")
                return "Error: format must be either 'sln' or 'slnx'.";
            args.Append($" --format {format}");
        }
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Add one or more projects to a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to add projects to</param>
    /// <param name="projects">Array of project file paths to add to the solution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetSolutionAdd(
        string solution,
        string[] projects,
        bool machineReadable = false)
    {
        if (projects == null || projects.Length == 0)
            return "Error: at least one project path is required.";

        var args = new StringBuilder($"solution \"{solution}\" add");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List all projects in a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to list projects from</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSolutionList(
        string solution,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"solution \"{solution}\" list", machineReadable);

    /// <summary>
    /// Remove one or more projects from a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to remove projects from</param>
    /// <param name="projects">Array of project file paths to remove from the solution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetSolutionRemove(
        string solution,
        string[] projects,
        bool machineReadable = false)
    {
        if (projects == null || projects.Length == 0)
            return "Error: at least one project path is required.";

        var args = new StringBuilder($"solution \"{solution}\" remove");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Get information about installed .NET SDKs and runtimes.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSdkInfo(bool machineReadable = false)
        => await ExecuteDotNetCommand("--info", machineReadable);

    /// <summary>
    /// Get the version of the .NET SDK.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSdkVersion(bool machineReadable = false)
        => await ExecuteDotNetCommand("--version", machineReadable);

    /// <summary>
    /// List installed .NET SDKs.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSdkList(bool machineReadable = false)
        => await ExecuteDotNetCommand("--list-sdks", machineReadable);

    /// <summary>
    /// List installed .NET runtimes.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetRuntimeList(bool machineReadable = false)
        => await ExecuteDotNetCommand("--list-runtimes", machineReadable);

    /// <summary>
    /// Get help for a specific dotnet command. Use this to discover available options for any dotnet command.
    /// </summary>
    /// <param name="command">The dotnet command to get help for (e.g., 'build', 'new', 'run'). If not specified, shows general dotnet help.</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetHelp(
        string? command = null,
        bool machineReadable = false)
        => await ExecuteDotNetCommand(command != null ? $"{command} --help" : "--help", machineReadable);

    /// <summary>
    /// Get a machine-readable JSON snapshot of server capabilities, versions, and supported features for agent orchestration and discovery.
    /// </summary>
    [McpServerTool]
    [McpMeta("category", "help")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["capabilities","version","discovery","orchestration","metadata"]""")]
    public async partial Task<string> DotnetServerCapabilities()
    {
        // Get the assembly version
        var assembly = typeof(DotNetCliTools).Assembly;
        var version = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? DefaultServerVersion;

        // Parse installed SDKs from dotnet --list-sdks
        var sdksOutput = await ExecuteDotNetCommand("--list-sdks", machineReadable: false);
        var installedSdks = ParseInstalledSdks(sdksOutput);

        // Create the capabilities snapshot
        var capabilities = new ServerCapabilities
        {
            ServerVersion = version,
            ProtocolVersion = ProtocolVersion,
            SupportedCategories = new[]
            {
                "template",
                "project",
                "package",
                "solution",
                "reference",
                "tool",
                "watch",
                "sdk",
                "security",
                "framework",
                "format",
                "nuget",
                "help",
                "efcore"
            },
            Supports = new ServerFeatureSupport
            {
                StructuredErrors = true,
                MachineReadable = true,
                Cancellation = true,
                Telemetry = false  // Future feature
            },
            SdkVersions = new SdkVersionInfo
            {
                Installed = installedSdks,
                Recommended = FrameworkHelper.GetLatestRecommendedFramework(),
                Lts = FrameworkHelper.GetLatestLtsFramework()
            }
        };

        return ErrorResultFactory.ToJson(capabilities);
    }

    /// <summary>
    /// Get detailed human-readable information about .NET MCP Server capabilities, including supported features, concurrency safety, and available resources.
    /// Provides guidance for AI orchestrators on parallel execution.
    /// </summary>
    [McpServerTool]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public partial Task<string> DotnetServerInfo()
    {
        var result = new StringBuilder();
        result.AppendLine("=== .NET MCP Server Capabilities ===");
        result.AppendLine();
        result.AppendLine("Version: 1.0+");
        result.AppendLine("Protocol: Model Context Protocol (MCP)");
        result.AppendLine("Transport: stdio");
        result.AppendLine();

        result.AppendLine("FEATURES:");
        result.AppendLine("  • 52 MCP Tools across 13 categories");
        result.AppendLine("  • 4 MCP Resources (SDK, Runtime, Templates, Frameworks)");
        result.AppendLine("  • Direct .NET SDK integration via NuGet packages");
        result.AppendLine("  • Template Engine integration with caching (5-min TTL)");
        result.AppendLine("  • Framework validation and LTS identification");
        result.AppendLine("  • MSBuild integration for project analysis");
        result.AppendLine("  • Thread-safe caching with metrics tracking");
        result.AppendLine();

        result.AppendLine("TOOL CATEGORIES:");
        result.AppendLine("  • Template (5 tools): List, search, info, cache management");
        result.AppendLine("  • Project (10 tools): New, build, run, test, publish, clean, restore, analyze, dependencies, validate");
        result.AppendLine("  • Package (6 tools): Add, remove, update, list, search, pack");
        result.AppendLine("  • Solution (4 tools): Create, add, remove, list");
        result.AppendLine("  • Reference (3 tools): Add, remove, list");
        result.AppendLine("  • Tool (7 tools): Install, uninstall, update, list, search, restore, run");
        result.AppendLine("  • Watch (3 tools): Watch run, watch test, watch build");
        result.AppendLine("  • SDK (4 tools): Version, info, list SDKs, list runtimes");
        result.AppendLine("  • Security (4 tools): Certificate trust, check, clean, export");
        result.AppendLine("  • Framework (1 tool): Framework information and LTS status");
        result.AppendLine("  • Format (1 tool): Code formatting");
        result.AppendLine("  • NuGet (1 tool): Cache management");
        result.AppendLine("  • Help (2 tools): Command help, server capabilities");
        result.AppendLine();

        result.AppendLine("CONCURRENCY SAFETY:");
        result.AppendLine("  ✅ Read-only operations: Always safe for parallel execution");
        result.AppendLine("     (Info, List, Search, Check, Help, Metrics tools)");
        result.AppendLine("  ⚠️  Mutating operations: Safe on different targets only");
        result.AppendLine("     (Build, Add, Remove operations on different projects)");
        result.AppendLine("  ❌ Global/Long-running: Never run in parallel");
        result.AppendLine("     (Watch commands, Run, Certificate operations, Cache clearing)");
        result.AppendLine();
        result.AppendLine("  📖 See documentation: doc/concurrency.md");
        result.AppendLine("     Full concurrency safety matrix with detailed guidance");
        result.AppendLine();

        result.AppendLine("CACHING:");
        result.AppendLine("  • Templates: 5-minute TTL, thread-safe with metrics");
        result.AppendLine("  • SDK Info: 5-minute TTL, thread-safe with metrics");
        result.AppendLine("  • Runtime Info: 5-minute TTL, thread-safe with metrics");
        result.AppendLine("  • Force reload available on template tools");
        result.AppendLine("  • Use dotnet_cache_metrics for hit/miss statistics");
        result.AppendLine();

        result.AppendLine("RESOURCES (Read-Only Access):");
        result.AppendLine("  • dotnet://sdk-info - Installed SDKs with versions and paths");
        result.AppendLine("  • dotnet://runtime-info - Installed runtimes with metadata");
        result.AppendLine("  • dotnet://templates - Complete template catalog");
        result.AppendLine("  • dotnet://frameworks - Framework information with LTS status");
        result.AppendLine();

        result.AppendLine("DOCUMENTATION:");
        result.AppendLine("  • README: https://github.com/jongalloway/dotnet-mcp");
        result.AppendLine("  • SDK Integration: doc/sdk-integration.md");
        result.AppendLine("  • Advanced Topics: doc/advanced-topics.md");
        result.AppendLine("  • Concurrency Safety: doc/concurrency.md");
        result.AppendLine();

        result.AppendLine("For detailed concurrency guidance and parallel execution patterns,");
        result.AppendLine("see the Concurrency Safety Matrix at: doc/concurrency.md");

        return Task.FromResult(result.ToString());
    }

    /// <summary>
    /// Format code according to .editorconfig and style rules. Available since .NET 6 SDK.
    /// Useful for enforcing consistent code style across projects.
    /// </summary>
    /// <param name="project">The project or solution file to format</param>
    /// <param name="verify">Verify formatting without making changes</param>
    /// <param name="includeGenerated">Include generated code files</param>
    /// <param name="diagnostics">Comma-separated list of diagnostic IDs to fix</param>
    /// <param name="severity">Severity level to fix (info, warn, error)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "format")]
    [McpMeta("priority", 6.0)]
    [McpMeta("minimumSdkVersion", "6.0")]
    public async partial Task<string> DotnetFormat(
        string? project = null,
        bool verify = false,
        bool includeGenerated = false,
        string? diagnostics = null,
        string? severity = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("format");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (verify) args.Append(" --verify-no-changes");
        if (includeGenerated) args.Append(" --include-generated");
        if (!string.IsNullOrEmpty(diagnostics)) args.Append($" --diagnostics {diagnostics}");
        if (!string.IsNullOrEmpty(severity)) args.Append($" --severity {severity}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Manage NuGet local caches. List or clear the global-packages, http-cache, temp, and plugins-cache folders.
    /// Useful for troubleshooting NuGet issues.
    /// </summary>
    /// <param name="cacheLocation">The cache location to manage: all, http-cache, global-packages, temp, or plugins-cache</param>
    /// <param name="list">List the cache location path</param>
    /// <param name="clear">Clear the specified cache location</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "nuget")]
    [McpMeta("priority", 4.0)]
    public async partial Task<string> DotnetNugetLocals(
        string cacheLocation,
        bool list = false,
        bool clear = false,
        bool machineReadable = false)
    {
        if (!list && !clear)
            return "Error: Either 'list' or 'clear' must be true.";

        if (list && clear)
            return "Error: Cannot specify both 'list' and 'clear'.";

        var validLocations = new[] { "all", "http-cache", "global-packages", "temp", "plugins-cache" };
        var normalizedCacheLocation = cacheLocation.ToLowerInvariant();
        if (!validLocations.Contains(normalizedCacheLocation))
            return $"Error: Invalid cache location. Must be one of: {string.Join(", ", validLocations)}";

        var args = $"nuget locals {normalizedCacheLocation}";
        if (list) args += " --list";
        if (clear) args += " --clear";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    /// <summary>
    /// Trust the HTTPS development certificate. Installs the certificate to the trusted root store.
    /// May require elevation on Windows/macOS. Essential for local ASP.NET Core HTTPS development.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    [McpMeta("requiresElevation", true)]
    public async partial Task<string> DotnetCertificateTrust(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --trust", machineReadable);

    /// <summary>
    /// Check if the HTTPS development certificate exists and is trusted.
    /// Returns certificate status and validity information.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetCertificateCheck(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --check", machineReadable);

    /// <summary>
    /// Remove all HTTPS development certificates.
    /// Use this to clean up old or invalid certificates before creating new ones.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetCertificateClean(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --clean", machineReadable);

    /// <summary>
    /// Export the HTTPS development certificate to a file.
    /// Useful for Docker containers or sharing certificates across environments. Supports PFX and PEM formats with optional password protection.
    /// </summary>
    /// <param name="path">Path to export the certificate file</param>
    /// <param name="password">Certificate password for protection (optional, but recommended for PFX format)</param>
    /// <param name="format">Export format: Pfx or Pem (defaults to Pfx if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetCertificateExport(
        string path,
        string? password = null,
        string? format = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Error: path parameter is required.";

        // Validate and normalize format if provided
        string? normalizedFormat = null;
        if (!string.IsNullOrEmpty(format))
        {
            normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat != "pfx" && normalizedFormat != "pem")
                return "Error: format must be either 'pfx' or 'pem' (case-insensitive).";
        }

        // Security Note: The password must be passed as a command-line argument to dotnet dev-certs,
        // which is the standard .NET CLI behavior. While this stores the password temporarily in memory
        // (CodeQL alert cs/cleartext-storage-of-sensitive-information), this is:
        // 1. Required by the .NET CLI interface - there's no alternative secure input method
        // 2. Mitigated by passing logger: null below, which prevents logging of the password
        // 3. Not persisted to disk or stored long-term
        // 4. Consistent with how developers manually use the dotnet dev-certs command
        var args = new StringBuilder("dev-certs https");
        args.Append($" --export-path \"{path}\"");

        if (!string.IsNullOrEmpty(normalizedFormat))
            args.Append($" --format {normalizedFormat}");

        if (!string.IsNullOrEmpty(password))
            args.Append($" --password \"{password}\"");

        // Pass logger: null to prevent DotNetCommandExecutor from logging the password
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    /// <summary>
    /// Initialize user secrets for a project.
    /// Creates a unique secrets ID and enables secret storage. This is the first step to using user secrets in your project.
    /// </summary>
    /// <param name="project">Project file to initialize secrets for (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 8.0)]
    public async partial Task<string> DotnetSecretsInit(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets init");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Set a user secret value.
    /// Stores sensitive configuration outside of the project. Supports hierarchical keys (e.g., 'ConnectionStrings:DefaultConnection').
    /// DEVELOPMENT ONLY - not for production deployment.
    /// </summary>
    /// <param name="key">Secret key (supports hierarchical keys like 'ConnectionStrings:DefaultConnection')</param>
    /// <param name="value">Secret value (will not be logged for security)</param>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    public async partial Task<string> DotnetSecretsSet(
        string key,
        string value,
        string? project = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Error: key parameter is required.";

        if (string.IsNullOrWhiteSpace(value))
            return "Error: value parameter is required.";

        // Security Note: The secret value must be passed as a command-line argument to dotnet user-secrets,
        // which is the standard .NET CLI behavior. While this stores the value temporarily in memory
        // (similar to dev-certs password handling), this is:
        // 1. Required by the .NET CLI interface - there's no alternative secure input method
        // 2. Mitigated by passing logger: null below, which prevents logging of the secret value
        // 3. Not persisted to disk in logs or command history by our code
        // 4. Consistent with how developers manually use the dotnet user-secrets command
        // 5. User secrets are ONLY for development, never for production deployment
        var args = new StringBuilder("user-secrets set");
        args.Append($" \"{key}\" \"{value}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");

        // Pass logger: null to prevent DotNetCommandExecutor from logging the secret value
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    /// <summary>
    /// List all user secrets for a project. Displays secret keys and values.
    /// Useful for debugging configuration.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetSecretsList(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Remove a specific user secret by key. Deletes the secret from local storage.
    /// </summary>
    /// <param name="key">Secret key to remove</param>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSecretsRemove(
        string key,
        string? project = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Error: key parameter is required.";

        var args = new StringBuilder($"user-secrets remove \"{key}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Clear all user secrets for a project. Removes all stored secrets.
    /// Use this for a fresh start when debugging configuration issues.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetSecretsClear(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets clear");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Install a .NET tool globally or locally to a tool manifest.
    /// Global tools are available system-wide; local tools are project-specific and tracked in .config/dotnet-tools.json.
    /// </summary>
    /// <param name="packageName">Package name of the tool (e.g., 'dotnet-ef', 'dotnet-format')</param>
    /// <param name="global">Install globally (system-wide); otherwise installs locally to tool manifest</param>
    /// <param name="version">Specific version to install</param>
    /// <param name="framework">Target framework to install for</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["tool","install","global","local","cli"]""")]
    public async partial Task<string> DotnetToolInstall(
        string packageName,
        bool global = false,
        string? version = null,
        string? framework = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool install \"{packageName}\"");
        if (global) args.Append(" --global");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List installed .NET tools.
    /// Shows global tools (system-wide) or local tools (from .config/dotnet-tools.json manifest) with their versions and commands.
    /// </summary>
    /// <param name="global">List global tools (system-wide); otherwise lists local tools from manifest</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolList(
        bool global = false,
        bool machineReadable = false)
    {
        var args = "tool list";
        if (global) args += " --global";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    /// <summary>
    /// Update a .NET tool to a newer version.
    /// Can update to latest or a specific version.
    /// </summary>
    /// <param name="packageName">Package name of the tool to update</param>
    /// <param name="global">Update global tool (system-wide); otherwise updates local tool</param>
    /// <param name="version">Update to specific version; otherwise updates to latest</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolUpdate(
        string packageName,
        bool global = false,
        string? version = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool update \"{packageName}\"");
        if (global) args.Append(" --global");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Uninstall a .NET tool.
    /// Removes a global tool (system-wide) or removes from local tool manifest.
    /// </summary>
    /// <param name="packageName">Package name of the tool to uninstall</param>
    /// <param name="global">Uninstall global tool (system-wide); otherwise uninstalls from local manifest</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolUninstall(
        string packageName,
        bool global = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool uninstall \"{packageName}\"");
        if (global) args.Append(" --global");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Restore tools from the tool manifest (.config/dotnet-tools.json).
    /// Installs all tools listed in the manifest; essential for project setup after cloning.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolRestore(bool machineReadable = false)
        => await ExecuteDotNetCommand("tool restore", machineReadable);

    /// <summary>
    /// Create a .NET tool manifest file (.config/dotnet-tools.json).
    /// Required before installing local tools. Creates the manifest in the current directory or specified output location.
    /// </summary>
    /// <param name="output">Output directory for the manifest (defaults to current directory)</param>
    /// <param name="force">Force creation even if manifest already exists</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolManifestCreate(
        string? output = null,
        bool force = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("new tool-manifest");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (force) args.Append(" --force");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Search for .NET tools on NuGet.org.
    /// Finds available tools by name or description with download counts and package information.
    /// </summary>
    /// <param name="searchTerm">Search term to find tools</param>
    /// <param name="detail">Show detailed information including description and versions</param>
    /// <param name="take">Maximum number of results to return (1-100)</param>
    /// <param name="skip">Skip the first N results for pagination</param>
    /// <param name="prerelease">Include prerelease tool versions in search</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolSearch(
        string searchTerm,
        bool detail = false,
        int? take = null,
        int? skip = null,
        bool prerelease = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return "Error: searchTerm parameter is required.";

        var args = new StringBuilder($"tool search \"{searchTerm}\"");
        if (detail) args.Append(" --detail");
        if (take.HasValue) args.Append($" --take {take.Value}");
        if (skip.HasValue) args.Append($" --skip {skip.Value}");
        if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Run a .NET tool by its command name.
    /// Executes an installed local or global tool with optional arguments.
    /// </summary>
    /// <param name="toolName">Tool command name to run (e.g., 'dotnet-ef', 'dotnet-format')</param>
    /// <param name="args">Arguments to pass to the tool (e.g., 'migrations add Initial')</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolRun(
        string toolName,
        string? args = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return "Error: toolName parameter is required.";

        if (!string.IsNullOrEmpty(args) && !IsValidAdditionalOptions(args))
            return "Error: args contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";

        var commandArgs = new StringBuilder($"tool run \"{toolName}\"");
        if (!string.IsNullOrEmpty(args)) commandArgs.Append($" -- {args}");
        return await ExecuteDotNetCommand(commandArgs.ToString(), machineReadable);
    }

    // Entity Framework Core CLI Tools
    // Note: Requires dotnet-ef tool to be installed (dotnet tool install dotnet-ef --global or locally)

    /// <summary>
    /// Create a new Entity Framework Core migration.
    /// Generates migration files for database schema changes. Requires Microsoft.EntityFrameworkCore.Design package and dotnet-ef tool.
    /// </summary>
    /// <param name="name">Name of the migration (e.g., 'InitialCreate', 'AddProductEntity')</param>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="outputDir">Output directory for migration files</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","schema"]""")]
    public async partial Task<string> DotnetEfMigrationsAdd(
        string name,
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? outputDir = null,
        string? framework = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Error: name parameter is required.";

        var args = new StringBuilder($"ef migrations add \"{name}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(outputDir)) args.Append($" --output-dir \"{outputDir}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List all Entity Framework Core migrations.
    /// Shows applied and pending migrations with their status. Useful for understanding migration history.
    /// </summary>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="connection">Show connection string used</param>
    /// <param name="noBuild">Do not build the project before listing</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","list"]""")]
    public async partial Task<string> DotnetEfMigrationsList(
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool connection = false,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef migrations list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (connection) args.Append(" --connection");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Remove the last Entity Framework Core migration.
    /// Removes the most recent unapplied migration. Useful for cleaning up mistakes before applying to database.
    /// </summary>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="force">Force removal (reverts migration if already applied)</param>
    /// <param name="noBuild">Do not build the project before removing</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","remove"]""")]
    public async partial Task<string> DotnetEfMigrationsRemove(
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool force = false,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef migrations remove");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (force) args.Append(" --force");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Generate SQL script from Entity Framework Core migrations.
    /// Exports migration changes to SQL file for deployment or review. Useful for production deployments.
    /// </summary>
    /// <param name="from">Starting migration (default: 0 for all migrations)</param>
    /// <param name="to">Target migration (default: last migration)</param>
    /// <param name="output">Output file path for SQL script</param>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="idempotent">Generate idempotent script (can be run multiple times)</param>
    /// <param name="noBuild">Do not build the project before scripting</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","sql","script"]""")]
    public async partial Task<string> DotnetEfMigrationsScript(
        string? from = null,
        string? to = null,
        string? output = null,
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool idempotent = false,
        bool noBuild = false,
        bool machineReadable = false)
    {
        // Validate parameter order: if 'to' is specified without 'from', use empty string for from
        var args = new StringBuilder("ef migrations script");
        if (!string.IsNullOrEmpty(from))
        {
            args.Append($" \"{from}\"");
            if (!string.IsNullOrEmpty(to))
                args.Append($" \"{to}\"");
        }
        else if (!string.IsNullOrEmpty(to))
        {
            // If only 'to' is specified, EF Core expects 'from' to be empty string (all migrations up to 'to')
            args.Append($" \"\" \"{to}\"");
        }

        if (!string.IsNullOrEmpty(output)) args.Append($" --output \"{output}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (idempotent) args.Append(" --idempotent");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Apply Entity Framework Core migrations to the database.
    /// Updates database schema to the latest or specified migration. Essential for database updates.
    /// </summary>
    /// <param name="migration">Target migration name (default: latest migration). Use '0' to rollback all migrations.</param>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="connection">Connection string (overrides configured connection)</param>
    /// <param name="noBuild">Do not build the project before updating</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","database","update","migration","apply"]""")]
    public async partial Task<string> DotnetEfDatabaseUpdate(
        string? migration = null,
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        string? connection = null,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef database update");
        if (!string.IsNullOrEmpty(migration)) args.Append($" \"{migration}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (!string.IsNullOrEmpty(connection)) args.Append($" --connection \"{connection}\"");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Drop the Entity Framework Core database.
    /// WARNING: This permanently deletes the database. Use with extreme caution (typically only for development).
    /// Set force=true to execute without confirmation prompt.
    /// </summary>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="force">Force drop without confirmation prompt (set to true to execute)</param>
    /// <param name="dryRun">Perform a dry run without actually dropping</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 5.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","database","drop","delete"]""")]
    public async partial Task<string> DotnetEfDatabaseDrop(
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool force = false,
        bool dryRun = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef database drop");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (force) args.Append(" --force");
        if (dryRun) args.Append(" --dry-run");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List all Entity Framework Core DbContext classes in the project.
    /// Shows available database contexts. Useful for multi-context applications.
    /// </summary>
    /// <param name="project">Project file containing the DbContext classes</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="noBuild">Do not build the project before listing</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","list"]""")]
    public async partial Task<string> DotnetEfDbContextList(
        string? project = null,
        string? startupProject = null,
        string? framework = null,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef dbcontext list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Get Entity Framework Core DbContext information.
    /// Shows connection string and provider details for a specific DbContext.
    /// </summary>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="noBuild">Do not build the project before getting info</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","info","connection-string"]""")]
    public async partial Task<string> DotnetEfDbContextInfo(
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef dbcontext info");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Reverse engineer (scaffold) Entity Framework Core entities from an existing database.
    /// Generates DbContext and entity classes from database schema. Essential for database-first development.
    /// </summary>
    /// <param name="connection">Database connection string (e.g., 'Server=localhost;Database=MyDb;...')</param>
    /// <param name="provider">Database provider (e.g., 'Microsoft.EntityFrameworkCore.SqlServer', 'Npgsql.EntityFrameworkCore.PostgreSQL')</param>
    /// <param name="project">Project file to add generated files to</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="outputDir">Output directory for generated entity classes (default: project root)</param>
    /// <param name="contextDir">Directory for the generated DbContext class (default: same as outputDir)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="tables">Specific tables to scaffold (comma-separated; default: all tables)</param>
    /// <param name="schemas">Specific schemas to scaffold (comma-separated)</param>
    /// <param name="useDatabaseNames">Use database names directly instead of pluralization</param>
    /// <param name="force">Force overwrite of existing files</param>
    /// <param name="noBuild">Do not build the project before scaffolding</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","scaffold","reverse-engineer","database-first"]""")]
    public async partial Task<string> DotnetEfDbContextScaffold(
        string connection,
        string provider,
        string? project = null,
        string? startupProject = null,
        string? outputDir = null,
        string? contextDir = null,
        string? framework = null,
        string? tables = null,
        string? schemas = null,
        bool useDatabaseNames = false,
        bool force = false,
        bool noBuild = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(connection))
            return "Error: connection parameter is required.";

        if (string.IsNullOrWhiteSpace(provider))
            return "Error: provider parameter is required.";

        var args = new StringBuilder($"ef dbcontext scaffold \"{connection}\" \"{provider}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(outputDir)) args.Append($" --output-dir \"{outputDir}\"");
        if (!string.IsNullOrEmpty(contextDir)) args.Append($" --context-dir \"{contextDir}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");

        // Handle multiple tables - split by comma and add --table for each
        if (!string.IsNullOrEmpty(tables))
        {
            foreach (var table in tables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                args.Append($" --table \"{table}\"");
            }
        }

        // Handle multiple schemas - split by comma and add --schema for each
        if (!string.IsNullOrEmpty(schemas))
        {
            foreach (var schema in schemas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                args.Append($" --schema \"{schema}\"");
            }
        }

        if (useDatabaseNames) args.Append(" --use-database-names");
        if (force) args.Append(" --force");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    private async Task<string> ExecuteDotNetCommand(string arguments, bool machineReadable = false, CancellationToken cancellationToken = default)
        => await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _logger, machineReadable, unsafeOutput: false, cancellationToken);

    /// <summary>
    /// Execute a command with concurrency control. Returns error if there's a conflict.
    /// </summary>
    private async Task<string> ExecuteWithConcurrencyCheck(
        string operationType,
        string target,
        string arguments,
        bool machineReadable = false,
        CancellationToken cancellationToken = default)
    {
        // Try to acquire the operation
        if (!_concurrencyManager.TryAcquireOperation(operationType, target, out var conflictingOperation))
        {
            // Conflict detected - return error
            var errorResponse = ErrorResultFactory.CreateConcurrencyConflict(operationType, target, conflictingOperation!);
            return machineReadable
                ? ErrorResultFactory.ToJson(errorResponse)
                : $"Error: {errorResponse.Errors[0].Message}\nHint: {errorResponse.Errors[0].Hint}";
        }

        try
        {
            // Execute the command
            return await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _logger, machineReadable, unsafeOutput: false, cancellationToken);
        }
        finally
        {
            // Always release the operation lock
            _concurrencyManager.ReleaseOperation(operationType, target);
        }
    }

    /// <summary>
    /// Gets the operation target for concurrency control. Returns the project path if specified, 
    /// otherwise returns the current directory.
    /// </summary>
    private static string GetOperationTarget(string? project)
        => project ?? Directory.GetCurrentDirectory();

    private static bool IsValidAdditionalOptions(string options)
    {
        // Validation rationale (see PR #42 and follow-up refinement in PR #60):
        // We intentionally use a simple foreach + pattern match instead of LINQ (All) or a HashSet/FrozenSet.
        // Reasons:
        //1. Readability: The allowlist is tiny (5 chars); the loop is explicit and easy to audit for security.
        //2. Performance: Differences among foreach, LINQ, HashSet, or FrozenSet for short CLI option strings are negligible.
        // Avoiding LINQ prevents enumerator/delegate allocations; HashSet/FrozenSet adds unnecessary static initialization.
        //3. Security clarity: A positive allowlist (alphanumeric + specific safe punctuation) makes the policy obvious.
        //4. Modern C# pattern matching (c is '-' or '_' ...) is concise and self-documenting.
        // If additional safe characters are ever required, extend the pattern below and update the comment.
        // Rejected shell/metacharacters: &, |, ;, <, >, `, $, (, ), {, }, [, ], \, ", '
        foreach (var c in options)
        {
            if (!(char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or ' ' or '='))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Parse the output of 'dotnet --list-sdks' to extract SDK versions.
    /// Expected format: "9.0.306 [/usr/share/dotnet/sdk]"
    /// </summary>
    private static string[] ParseInstalledSdks(string sdksOutput)
    {
        if (string.IsNullOrWhiteSpace(sdksOutput))
            return Array.Empty<string>();

        var sdks = new List<string>();
        var lines = sdksOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Each line format: "version [path]"
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var version = parts[0].Trim();
                // Skip empty lines, error messages, and exit code lines - only keep lines starting with a digit (SDK versions)
                if (!string.IsNullOrEmpty(version) &&
                    !version.StartsWith("Exit", StringComparison.OrdinalIgnoreCase) &&
                    !version.StartsWith("Error", StringComparison.OrdinalIgnoreCase) &&
                    char.IsDigit(version[0]))
                {
                    sdks.Add(version);
                }
            }
        }

        return sdks.ToArray();
    }

    /// <summary>
    /// Analyze a .csproj file to extract comprehensive project information including target frameworks, 
    /// package references, project references, and build properties. Returns structured JSON.
    /// Does not require building the project.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file to analyze</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["project","analyze","introspection","metadata"]""")]
    public async partial Task<string> DotnetProjectAnalyze(string projectPath)
    {
        _logger.LogDebug("Analyzing project file: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.AnalyzeProjectAsync(projectPath, _logger);
    }

    /// <summary>
    /// Analyze project dependencies to build a dependency graph showing direct package and project dependencies.
    /// Returns structured JSON with dependency information. For transitive dependencies, use CLI commands.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file to analyze</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["project","dependencies","analyze","packages"]""")]
    public async partial Task<string> DotnetProjectDependencies(string projectPath)
    {
        _logger.LogDebug("Analyzing dependencies for: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.AnalyzeDependenciesAsync(projectPath, _logger);
    }

    /// <summary>
    /// Validate a .csproj file for common issues, deprecated packages, and configuration problems.
    /// Returns structured JSON with errors, warnings, and recommendations. Does not require building.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file to validate</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["project","validate","health-check","diagnostics"]""")]
    public async partial Task<string> DotnetProjectValidate(string projectPath)
    {
        _logger.LogDebug("Validating project: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.ValidateProjectAsync(projectPath, _logger);
    }
}
