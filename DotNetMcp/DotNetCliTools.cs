using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

[McpServerToolType]
public sealed class DotNetCliTools
{
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;
    private const string MachineReadableDescription = "Return structured JSON output for both success and error responses instead of plain text";

    public DotNetCliTools(ILogger<DotNetCliTools> logger, ConcurrencyManager concurrencyManager)
    {
        // DI guarantees logger is never null
        _logger = logger!;
        _concurrencyManager = concurrencyManager!;
    }

    [McpServerTool, Description("List all installed .NET templates with their metadata using the Template Engine. Provides structured information about available project templates.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("priority", 10.0)]
    [McpMeta("tags", JsonValue = """["template","list","discovery","project-creation"]""")]
    public async Task<string> DotnetTemplateList(
        [Description("If true, bypasses cache and reloads templates from disk")] bool forceReload = false)
          => await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload, _logger);

    [McpServerTool, Description("Search for .NET templates by name or description. Returns matching templates with their details.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async Task<string> DotnetTemplateSearch(
        [Description("Search term to find templates (searches in name, short name, and description)")] string searchTerm,
        [Description("If true, bypasses cache and reloads templates from disk")] bool forceReload = false)
        => await TemplateEngineHelper.SearchTemplatesAsync(searchTerm, forceReload, _logger);

    [McpServerTool, Description("Get detailed information about a specific template including available parameters and options.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async Task<string> DotnetTemplateInfo(
 [Description("The template short name (e.g., 'console', 'webapi', 'classlib')")] string templateShortName,
        [Description("If true, bypasses cache and reloads templates from disk")] bool forceReload = false)
        => await TemplateEngineHelper.GetTemplateDetailsAsync(templateShortName, forceReload, _logger);

    [McpServerTool, Description("Clear all caches (templates, SDK, runtime) to force reload from disk. Use this after installing or uninstalling templates or SDK versions. Also resets all cache metrics.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async Task<string> DotnetTemplateClearCache()
    {
        await DotNetResources.ClearAllCachesAsync();
        return "All caches (templates, SDK, runtime) and metrics cleared successfully. Next query will reload from disk.";
    }

    [McpServerTool, Description("Get cache metrics showing hit/miss statistics for templates, SDK, and runtime information.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public Task<string> DotnetCacheMetrics()
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine("Cache Metrics:");
        result.AppendLine();
        result.AppendLine($"Templates: {TemplateEngineHelper.Metrics}");
        result.AppendLine($"SDK Info: {DotNetResources.GetSdkMetrics()}");
        result.AppendLine($"Runtime Info: {DotNetResources.GetRuntimeMetrics()}");
        return Task.FromResult(result.ToString());
    }

    [McpServerTool, Description("Get information about .NET framework versions, including which are LTS releases. Useful for understanding framework compatibility.")]
    [McpMeta("category", "framework")]
    [McpMeta("usesFrameworkHelper", true)]
    public Task<string> DotnetFrameworkInfo(
     [Description("Optional: specific framework to get info about (e.g., 'net8.0', 'net6.0')")] string? framework = null)
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
            result.AppendLine("Modern .NET Frameworks (5.0+):");
            foreach (var fw in FrameworkHelper.GetSupportedModernFrameworks())
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

        return Task.FromResult(result.ToString());
    }

    [McpServerTool, Description("Create a new .NET project or file from a template. Common templates: console, classlib, web, webapi, mvc, blazor, xunit, nunit, mstest.")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["project","create","new","template","initialization"]""")]
    public async Task<string> DotnetProjectNew(
  [Description("The template to use (e.g., 'console', 'classlib', 'webapi')")] string? template = null,
        [Description("The name for the project")] string? name = null,
        [Description("The output directory")] string? output = null,
        [Description("The target framework (e.g., 'net9.0', 'net8.0')")] string? framework = null,
        [Description("Additional template-specific options (e.g., '--format slnx', '--use-program-main', '--aot')")] string? additionalOptions = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Restore the dependencies and tools of a .NET project")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["project","restore","dependencies","packages","setup"]""")]
    public async Task<string> DotnetProjectRestore(
        [Description("The project file or solution file to restore")] string? project = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = "restore";
        if (!string.IsNullOrEmpty(project)) args += $" \"{project}\"";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    [McpServerTool, Description("Build a .NET project and its dependencies")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","build","compile","compilation"]""")]
    public async Task<string> DotnetProjectBuild(
        [Description("The project file or solution file to build")] string? project = null,
        [Description("The configuration to build (Debug or Release)")] string? configuration = null,
        [Description("Build for a specific framework")] string? framework = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("build");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        
        return await ExecuteWithConcurrencyCheck("build", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Build and run a .NET project")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","run","execute","launch","development"]""")]
    public async Task<string> DotnetProjectRun(
      [Description("The project file to run")] string? project = null,
           [Description("The configuration to use (Debug or Release)")] string? configuration = null,
           [Description("Arguments to pass to the application")] string? appArgs = null,
           [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("run");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");
        
        return await ExecuteWithConcurrencyCheck("run", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Run unit tests in a .NET project")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","test","testing","unit-test","validation"]""")]
    public async Task<string> DotnetProjectTest(
        [Description("The project file or solution file to test")] string? project = null,
        [Description("The configuration to test (Debug or Release)")] string? configuration = null,
        [Description("Filter to run specific tests")] string? filter = null,
        [Description("The friendly name of the data collector (e.g., 'XPlat Code Coverage')")] string? collect = null,
        [Description("The directory where test results will be placed")] string? resultsDirectory = null,
      [Description("The logger to use for test results (e.g., 'trx', 'console;verbosity=detailed')")] string? logger = null,
    [Description("Do not build the project before testing")] bool noBuild = false,
   [Description("Do not restore the project before building")] bool noRestore = false,
        [Description("Set the MSBuild verbosity level (quiet, minimal, normal, detailed, diagnostic)")] string? verbosity = null,
   [Description("The target framework to test for")] string? framework = null,
        [Description("Run tests in blame mode to isolate problematic tests")] bool blame = false,
        [Description("List discovered tests without running them")] bool listTests = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Publish a .NET project for deployment")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 7.0)]
    [McpMeta("isLongRunning", true)]
    public async Task<string> DotnetProjectPublish(
     [Description("The project file to publish")] string? project = null,
        [Description("The configuration to publish (Debug or Release)")] string? configuration = null,
      [Description("The output directory for published files")] string? output = null,
        [Description("The target runtime identifier (e.g., 'linux-x64', 'win-x64')")] string? runtime = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("publish");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(runtime)) args.Append($" -r {runtime}");
        
        return await ExecuteWithConcurrencyCheck("publish", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Create a NuGet package from a .NET project. Use this to pack projects for distribution on NuGet.org or private feeds.")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetPackCreate(
      [Description("The project file to pack")] string? project = null,
      [Description("The configuration to pack (Debug or Release)")] string? configuration = null,
        [Description("The output directory for the package")] string? output = null,
        [Description("Include symbols package")] bool includeSymbols = false,
[Description("Include source files in the package")] bool includeSource = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("pack");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (includeSymbols) args.Append(" --include-symbols");
        if (includeSource) args.Append(" --include-source");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Clean the output of a .NET project")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetProjectClean(
     [Description("The project file or solution file to clean")] string? project = null,
        [Description("The configuration to clean (Debug or Release)")] string? configuration = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("clean");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Run a .NET project with file watching and hot reload. Note: This is a long-running command that watches for file changes and automatically restarts the application. It should be terminated by the user when no longer needed.")]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public Task<string> DotnetWatchRun(
        [Description("The project file to run")] string? project = null,
        [Description("Arguments to pass to the application")] string? appArgs = null,
        [Description("Disable hot reload")] bool noHotReload = false)
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

    [McpServerTool, Description("Run unit tests with file watching and automatic test re-runs. Note: This is a long-running command that watches for file changes. It should be terminated by the user when no longer needed.")]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public Task<string> DotnetWatchTest(
        [Description("The project file or solution file to test")] string? project = null,
        [Description("Filter to run specific tests")] string? filter = null)
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

    [McpServerTool, Description("Build a .NET project with file watching and automatic rebuild. Note: This is a long-running command that watches for file changes. It should be terminated by the user when no longer needed.")]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public Task<string> DotnetWatchBuild(
      [Description("The project file or solution file to build")] string? project = null,
        [Description("The configuration to build (Debug or Release)")] string? configuration = null)
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

    [McpServerTool, Description("Add a NuGet package reference to a .NET project")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["package","add","nuget","dependency","install"]""")]
    public async Task<string> DotnetPackageAdd(
 [Description("The name of the NuGet package to add")] string packageName,
    [Description("The project file to add the package to")] string? project = null,
        [Description("The version of the package")] string? version = null,
        [Description("Include prerelease packages")] bool prerelease = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Add a project-to-project reference")]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetReferenceAdd(
        [Description("The project file to add the reference from")] string project,
   [Description("The project file to reference")] string reference,
        [Description(MachineReadableDescription)] bool machineReadable = false)
        => await ExecuteDotNetCommand($"add \"{project}\" reference \"{reference}\"", machineReadable);

    [McpServerTool, Description("List package references for a .NET project")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetPackageList(
[Description("The project file or solution file")] string? project = null,
     [Description("Show outdated packages")] bool outdated = false,
        [Description("Show deprecated packages")] bool deprecated = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("list");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append(" package");
        if (outdated) args.Append(" --outdated");
        if (deprecated) args.Append(" --deprecated");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Remove a NuGet package reference from a .NET project")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetPackageRemove(
        [Description("The name of the NuGet package to remove")] string packageName,
  [Description("The project file to remove the package from")] string? project = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("remove");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Search for NuGet packages on nuget.org. Returns matching packages with descriptions and download counts.")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 7.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["package","search","nuget","discovery","find"]""")]
    public async Task<string> DotnetPackageSearch(
        [Description("Search term to find packages")] string searchTerm,
        [Description("Maximum number of results to return (1-100)")] int? take = null,
        [Description("Skip the first N results")] int? skip = null,
        [Description("Include prerelease packages")] bool prerelease = false,
[Description("Show exact matches only")] bool exactMatch = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder($"package search {searchTerm}");
        if (take.HasValue) args.Append($" --take {take.Value}");
        if (skip.HasValue) args.Append($" --skip {skip.Value}");
        if (prerelease) args.Append(" --prerelease");
        if (exactMatch) args.Append(" --exact-match");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Update a NuGet package reference to a newer version in a .NET project. Note: This uses 'dotnet add package' which updates the package when a newer version is specified.")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetPackageUpdate(
        [Description("The name of the NuGet package to update")] string packageName,
        [Description("The project file to update the package in")] string? project = null,
        [Description("The version to update to")] string? version = null,
        [Description("Update to the latest prerelease version")] bool prerelease = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("List project references")]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetReferenceList(
     [Description("The project file")] string? project = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = "list";
        if (!string.IsNullOrEmpty(project)) args += $" \"{project}\"";
        args += " reference";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    [McpServerTool, Description("Remove a project-to-project reference")]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetReferenceRemove(
            [Description("The project file to remove the reference from")] string project,
            [Description("The project file to unreference")] string reference,
            [Description(MachineReadableDescription)] bool machineReadable = false)
            => await ExecuteDotNetCommand($"remove \"{project}\" reference \"{reference}\"", machineReadable);

    [McpServerTool, Description("Create a new .NET solution file. A solution file organizes multiple related projects.")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["solution","create","new","organization","multi-project"]""")]
    public async Task<string> DotnetSolutionCreate(
        [Description("The name for the solution file")] string name,
        [Description("The output directory for the solution file")] string? output = null,
        [Description("The solution file format: 'sln' (classic) or 'slnx' (XML-based). Default is 'sln'.")] string? format = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Add one or more projects to a .NET solution file")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetSolutionAdd(
           [Description("The solution file to add projects to")] string solution,
           [Description("Array of project file paths to add to the solution")] string[] projects,
           [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("List all projects in a .NET solution file")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSolutionList(
        [Description("The solution file to list projects from")] string solution,
        [Description(MachineReadableDescription)] bool machineReadable = false)
        => await ExecuteDotNetCommand($"solution \"{solution}\" list", machineReadable);

    [McpServerTool, Description("Remove one or more projects from a .NET solution file")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetSolutionRemove(
        [Description("The solution file to remove projects from")] string solution,
  [Description("Array of project file paths to remove from the solution")] string[] projects,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Get information about installed .NET SDKs and runtimes")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSdkInfo([Description(MachineReadableDescription)] bool machineReadable = false) 
        => await ExecuteDotNetCommand("--info", machineReadable);

    [McpServerTool, Description("Get the version of the .NET SDK")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSdkVersion([Description(MachineReadableDescription)] bool machineReadable = false) 
        => await ExecuteDotNetCommand("--version", machineReadable);

    [McpServerTool, Description("List installed .NET SDKs")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSdkList([Description(MachineReadableDescription)] bool machineReadable = false) 
        => await ExecuteDotNetCommand("--list-sdks", machineReadable);

    [McpServerTool, Description("List installed .NET runtimes")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetRuntimeList([Description(MachineReadableDescription)] bool machineReadable = false) 
        => await ExecuteDotNetCommand("--list-runtimes", machineReadable);

    [McpServerTool, Description("Get help for a specific dotnet command. Use this to discover available options for any dotnet command.")]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetHelp(
        [Description("The dotnet command to get help for (e.g., 'build', 'new', 'run'). If not specified, shows general dotnet help.")] string? command = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
  => await ExecuteDotNetCommand(command != null ? $"{command} --help" : "--help", machineReadable);

    [McpServerTool, Description("Get information about .NET MCP Server capabilities including supported features, concurrency safety, and available resources. Provides guidance for AI orchestrators on parallel execution.")]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public Task<string> DotnetServerCapabilities()
    {
        var result = new StringBuilder();
        result.AppendLine("=== .NET MCP Server Capabilities ===");
        result.AppendLine();
        result.AppendLine("Version: 1.0+");
        result.AppendLine("Protocol: Model Context Protocol (MCP)");
        result.AppendLine("Transport: stdio");
        result.AppendLine();
        
        result.AppendLine("FEATURES:");
        result.AppendLine("  • 49 MCP Tools across 13 categories");
        result.AppendLine("  • 4 MCP Resources (SDK, Runtime, Templates, Frameworks)");
        result.AppendLine("  • Direct .NET SDK integration via NuGet packages");
        result.AppendLine("  • Template Engine integration with caching (5-min TTL)");
        result.AppendLine("  • Framework validation and LTS identification");
        result.AppendLine("  • Thread-safe caching with metrics tracking");
        result.AppendLine();
        
        result.AppendLine("TOOL CATEGORIES:");
        result.AppendLine("  • Template (5 tools): List, search, info, cache management");
        result.AppendLine("  • Project (7 tools): New, build, run, test, publish, clean, restore");
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

    [McpServerTool, Description("Format code according to .editorconfig and style rules. Available since .NET 6 SDK. Useful for enforcing consistent code style across projects.")]
    [McpMeta("category", "format")]
    [McpMeta("priority", 6.0)]
    [McpMeta("minimumSdkVersion", "6.0")]
    public async Task<string> DotnetFormat(
        [Description("The project or solution file to format")] string? project = null,
  [Description("Verify formatting without making changes")] bool verify = false,
        [Description("Include generated code files")] bool includeGenerated = false,
        [Description("Comma-separated list of diagnostic IDs to fix")] string? diagnostics = null,
        [Description("Severity level to fix (info, warn, error)")] string? severity = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("format");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (verify) args.Append(" --verify-no-changes");
        if (includeGenerated) args.Append(" --include-generated");
        if (!string.IsNullOrEmpty(diagnostics)) args.Append($" --diagnostics {diagnostics}");
        if (!string.IsNullOrEmpty(severity)) args.Append($" --severity {severity}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Manage NuGet local caches. List or clear the global-packages, http-cache, temp, and plugins-cache folders. Useful for troubleshooting NuGet issues.")]
    [McpMeta("category", "nuget")]
    [McpMeta("priority", 4.0)]
    public async Task<string> DotnetNugetLocals(
        [Description("The cache location to manage: all, http-cache, global-packages, temp, or plugins-cache")] string cacheLocation,
        [Description("List the cache location path")] bool list = false,
      [Description("Clear the specified cache location")] bool clear = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Trust the HTTPS development certificate. Installs the certificate to the trusted root store. May require elevation on Windows/macOS. Essential for local ASP.NET Core HTTPS development.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    [McpMeta("requiresElevation", true)]
    public async Task<string> DotnetCertificateTrust([Description(MachineReadableDescription)] bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --trust", machineReadable);

    [McpServerTool, Description("Check if the HTTPS development certificate exists and is trusted. Returns certificate status and validity information.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetCertificateCheck([Description(MachineReadableDescription)] bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --check --trust", machineReadable);

    [McpServerTool, Description("Remove all HTTPS development certificates. Use this to clean up old or invalid certificates before creating new ones.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetCertificateClean([Description(MachineReadableDescription)] bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --clean", machineReadable);

    [McpServerTool, Description("Export the HTTPS development certificate to a file. Useful for Docker containers or sharing certificates across environments. Supports PFX and PEM formats with optional password protection.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetCertificateExport(
        [Description("Path to export the certificate file")] string path,
        [Description("Certificate password for protection (optional, but recommended for PFX format)")] string? password = null,
     [Description("Export format: Pfx or Pem (defaults to Pfx if not specified)")] string? format = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable);
    }

    [McpServerTool, Description("Initialize user secrets for a project. Creates a unique secrets ID and enables secret storage. This is the first step to using user secrets in your project.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 8.0)]
    public async Task<string> DotnetSecretsInit(
        [Description("Project file to initialize secrets for (optional, uses current directory if not specified)")] string? project = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets init");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Set a user secret value. Stores sensitive configuration outside of the project. Supports hierarchical keys (e.g., 'ConnectionStrings:DefaultConnection'). DEVELOPMENT ONLY - not for production deployment.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    public async Task<string> DotnetSecretsSet(
        [Description("Secret key (supports hierarchical keys like 'ConnectionStrings:DefaultConnection')")] string key,
        [Description("Secret value (will not be logged for security)")] string value,
        [Description("Project file (optional, uses current directory if not specified)")] string? project = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable);
    }

    [McpServerTool, Description("List all user secrets for a project. Displays secret keys and values. Useful for debugging configuration.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetSecretsList(
        [Description("Project file (optional, uses current directory if not specified)")] string? project = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Remove a specific user secret by key. Deletes the secret from local storage.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSecretsRemove(
        [Description("Secret key to remove")] string key,
        [Description("Project file (optional, uses current directory if not specified)")] string? project = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Error: key parameter is required.";

        var args = new StringBuilder($"user-secrets remove \"{key}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Clear all user secrets for a project. Removes all stored secrets. Use this for a fresh start when debugging configuration issues.")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetSecretsClear(
        [Description("Project file (optional, uses current directory if not specified)")] string? project = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets clear");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Install a .NET tool globally or locally to a tool manifest. Global tools are available system-wide, local tools are project-specific and tracked in .config/dotnet-tools.json.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["tool","install","global","local","cli"]""")]
    public async Task<string> DotnetToolInstall(
        [Description("Package name of the tool (e.g., 'dotnet-ef', 'dotnet-format')")] string packageName,
        [Description("Install globally (system-wide), otherwise installs locally to tool manifest")] bool global = false,
        [Description("Specific version to install")] string? version = null,
        [Description("Target framework to install for")] string? framework = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool install \"{packageName}\"");
        if (global) args.Append(" --global");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("List installed .NET tools. Shows global tools (system-wide) or local tools (from .config/dotnet-tools.json manifest) with their versions and commands.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetToolList(
        [Description("List global tools (system-wide), otherwise lists local tools from manifest")] bool global = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = "tool list";
        if (global) args += " --global";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    [McpServerTool, Description("Update a .NET tool to a newer version. Can update to latest, a specific version, or latest prerelease.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetToolUpdate(
        [Description("Package name of the tool to update")] string packageName,
        [Description("Update global tool (system-wide), otherwise updates local tool")] bool global = false,
        [Description("Update to specific version, otherwise updates to latest")] string? version = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool update \"{packageName}\"");
        if (global) args.Append(" --global");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Uninstall a .NET tool. Removes a global tool (system-wide) or removes from local tool manifest.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetToolUninstall(
        [Description("Package name of the tool to uninstall")] string packageName,
        [Description("Uninstall global tool (system-wide), otherwise uninstalls from local manifest")] bool global = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool uninstall \"{packageName}\"");
        if (global) args.Append(" --global");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Restore tools from the tool manifest (.config/dotnet-tools.json). Installs all tools listed in the manifest, essential for project setup after cloning.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetToolRestore([Description(MachineReadableDescription)] bool machineReadable = false)
        => await ExecuteDotNetCommand("tool restore", machineReadable);

    [McpServerTool, Description("Create a .NET tool manifest file (.config/dotnet-tools.json). Required before installing local tools. Creates the manifest in the current directory or specified output location.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetToolManifestCreate(
        [Description("Output directory for the manifest (defaults to current directory)")] string? output = null,
        [Description("Force creation even if manifest already exists")] bool force = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("new tool-manifest");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (force) args.Append(" --force");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Search for .NET tools on NuGet.org. Finds available tools by name or description with download counts and package information.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetToolSearch(
        [Description("Search term to find tools")] string searchTerm,
        [Description("Show detailed information including description and versions")] bool detail = false,
        [Description("Maximum number of results to return (1-100)")] int? take = null,
        [Description("Skip the first N results for pagination")] int? skip = null,
        [Description("Include prerelease tool versions in search")] bool prerelease = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Run a .NET tool by its command name. Executes an installed local or global tool with optional arguments.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetToolRun(
        [Description("Tool command name to run (e.g., 'dotnet-ef', 'dotnet-format')")] string toolName,
        [Description("Arguments to pass to the tool (e.g., 'migrations add Initial')")] string? args = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Create a new Entity Framework Core migration. Generates migration files for database schema changes. Requires Microsoft.EntityFrameworkCore.Design package and dotnet-ef tool.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","schema"]""")]
    public async Task<string> DotnetEfMigrationsAdd(
        [Description("Name of the migration (e.g., 'InitialCreate', 'AddProductEntity')")] string name,
        [Description("Project file containing the DbContext")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("The DbContext class to use (if multiple contexts exist)")] string? context = null,
        [Description("Output directory for migration files")] string? outputDir = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("List all Entity Framework Core migrations. Shows applied and pending migrations with their status. Useful for understanding migration history.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","list"]""")]
    public async Task<string> DotnetEfMigrationsList(
        [Description("Project file containing the DbContext")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("The DbContext class to use (if multiple contexts exist)")] string? context = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description("Show connection string used")] bool connection = false,
        [Description("Do not build the project before listing")] bool noBuild = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Remove the last Entity Framework Core migration. Removes the most recent unapplied migration. Useful for cleaning up mistakes before applying to database.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","remove"]""")]
    public async Task<string> DotnetEfMigrationsRemove(
        [Description("Project file containing the DbContext")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("The DbContext class to use (if multiple contexts exist)")] string? context = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description("Force removal (reverts migration if already applied)")] bool force = false,
        [Description("Do not build the project before removing")] bool noBuild = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Generate SQL script from Entity Framework Core migrations. Exports migration changes to SQL file for deployment or review. Useful for production deployments.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","sql","script"]""")]
    public async Task<string> DotnetEfMigrationsScript(
        [Description("Starting migration (default: 0 for all migrations)")] string? from = null,
        [Description("Target migration (default: last migration)")] string? to = null,
        [Description("Output file path for SQL script")] string? output = null,
        [Description("Project file containing the DbContext")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("The DbContext class to use (if multiple contexts exist)")] string? context = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description("Generate idempotent script (can be run multiple times)")] bool idempotent = false,
        [Description("Do not build the project before scripting")] bool noBuild = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Apply Entity Framework Core migrations to the database. Updates database schema to the latest or specified migration. Essential for database updates.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","database","update","migration","apply"]""")]
    public async Task<string> DotnetEfDatabaseUpdate(
        [Description("Target migration name (default: latest migration). Use '0' to rollback all migrations.")] string? migration = null,
        [Description("Project file containing the DbContext")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("The DbContext class to use (if multiple contexts exist)")] string? context = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description("Connection string (overrides configured connection)")] string? connection = null,
        [Description("Do not build the project before updating")] bool noBuild = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("Drop the Entity Framework Core database. WARNING: This permanently deletes the database. Use with extreme caution, typically only for development. Set force=true to execute without confirmation prompt.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 5.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","database","drop","delete"]""")]
    public async Task<string> DotnetEfDatabaseDrop(
        [Description("Project file containing the DbContext")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("The DbContext class to use (if multiple contexts exist)")] string? context = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description("Force drop without confirmation prompt (set to true to execute)")] bool force = false,
        [Description("Perform a dry run without actually dropping")] bool dryRun = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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

    [McpServerTool, Description("List all Entity Framework Core DbContext classes in the project. Shows available database contexts. Useful for multi-context applications.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","list"]""")]
    public async Task<string> DotnetEfDbContextList(
        [Description("Project file containing the DbContext classes")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description("Do not build the project before listing")] bool noBuild = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("ef dbcontext list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Get Entity Framework Core DbContext information. Shows connection string and provider details for a specific DbContext.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","info","connection-string"]""")]
    public async Task<string> DotnetEfDbContextInfo(
        [Description("Project file containing the DbContext")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("The DbContext class to use (if multiple contexts exist)")] string? context = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description("Do not build the project before getting info")] bool noBuild = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
    {
        var args = new StringBuilder("ef dbcontext info");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    [McpServerTool, Description("Reverse engineer (scaffold) Entity Framework Core entities from existing database. Generates DbContext and entity classes from database schema. Essential for database-first development.")]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","scaffold","reverse-engineer","database-first"]""")]
    public async Task<string> DotnetEfDbContextScaffold(
        [Description("Database connection string (e.g., 'Server=localhost;Database=MyDb;...')")] string connection,
        [Description("Database provider (e.g., 'Microsoft.EntityFrameworkCore.SqlServer', 'Npgsql.EntityFrameworkCore.PostgreSQL')")] string provider,
        [Description("Project file to add generated files to")] string? project = null,
        [Description("Startup project file (if different from DbContext project)")] string? startupProject = null,
        [Description("Output directory for generated entity classes (default: project root)")] string? outputDir = null,
        [Description("Directory for the generated DbContext class (default: same as outputDir)")] string? contextDir = null,
        [Description("Target framework for the project")] string? framework = null,
        [Description("Specific tables to scaffold (comma-separated, default: all tables)")] string? tables = null,
        [Description("Specific schemas to scaffold (comma-separated)")] string? schemas = null,
        [Description("Use database names directly instead of pluralization")] bool useDatabaseNames = false,
        [Description("Force overwrite of existing files")] bool force = false,
        [Description("Do not build the project before scaffolding")] bool noBuild = false,
        [Description(MachineReadableDescription)] bool machineReadable = false)
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
        => await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _logger, machineReadable, cancellationToken);

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
        var operationId = Guid.NewGuid().ToString();
        
        // Try to acquire the operation
        if (!_concurrencyManager.TryAcquireOperation(operationType, target, operationId, out var conflictingOperation))
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
            return await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _logger, machineReadable, cancellationToken);
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
}
