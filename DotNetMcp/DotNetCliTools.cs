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

    public DotNetCliTools(ILogger<DotNetCliTools> logger)
    {
        // DI guarantees logger is never null
        _logger = logger!;
    }

    [McpServerTool, Description("List all installed .NET templates with their metadata using the Template Engine. Provides structured information about available project templates.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async Task<string> DotnetTemplateList()
          => await TemplateEngineHelper.GetInstalledTemplatesAsync(_logger);

    [McpServerTool, Description("Search for .NET templates by name or description. Returns matching templates with their details.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async Task<string> DotnetTemplateSearch(
        [Description("Search term to find templates (searches in name, short name, and description)")] string searchTerm)
        => await TemplateEngineHelper.SearchTemplatesAsync(searchTerm, _logger);

    [McpServerTool, Description("Get detailed information about a specific template including available parameters and options.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async Task<string> DotnetTemplateInfo(
 [Description("The template short name (e.g., 'console', 'webapi', 'classlib')")] string templateShortName)
        => await TemplateEngineHelper.GetTemplateDetailsAsync(templateShortName, _logger);

    [McpServerTool, Description("Clear the template cache to force reload from disk. Use this after installing or uninstalling templates.")]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async Task<string> DotnetTemplateClearCache()
    {
        await TemplateEngineHelper.ClearCacheAsync(_logger);
        return "Template cache cleared successfully. Next template query will reload from disk.";
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
    public async Task<string> DotnetProjectNew(
  [Description("The template to use (e.g., 'console', 'classlib', 'webapi')")] string? template = null,
        [Description("The name for the project")] string? name = null,
        [Description("The output directory")] string? output = null,
        [Description("The target framework (e.g., 'net9.0', 'net8.0')")] string? framework = null,
        [Description("Additional template-specific options (e.g., '--format slnx', '--use-program-main', '--aot')")] string? additionalOptions = null)
    {
        if (string.IsNullOrWhiteSpace(template))
            return "Error: template parameter is required.";

        // Validate additionalOptions to prevent injection attempts
        if (!string.IsNullOrEmpty(additionalOptions))
        {
            if (!IsValidAdditionalOptions(additionalOptions))
                return "Error: additionalOptions contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, and spaces are allowed.";
        }

        var args = new StringBuilder($"new {template}");
        if (!string.IsNullOrEmpty(name)) args.Append($" -n \"{name}\"");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        if (!string.IsNullOrEmpty(additionalOptions)) args.Append($" {additionalOptions}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Restore the dependencies and tools of a .NET project")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 8.0)]
    public async Task<string> DotnetProjectRestore(
        [Description("The project file or solution file to restore")] string? project = null)
    {
        var args = "restore";
        if (!string.IsNullOrEmpty(project)) args += $" \"{project}\"";
        return await ExecuteDotNetCommand(args);
    }

    [McpServerTool, Description("Build a .NET project and its dependencies")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    public async Task<string> DotnetProjectBuild(
        [Description("The project file or solution file to build")] string? project = null,
        [Description("The configuration to build (Debug or Release)")] string? configuration = null,
        [Description("Build for a specific framework")] string? framework = null)
    {
        var args = new StringBuilder("build");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Build and run a .NET project")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    public async Task<string> DotnetProjectRun(
      [Description("The project file to run")] string? project = null,
           [Description("The configuration to use (Debug or Release)")] string? configuration = null,
           [Description("Arguments to pass to the application")] string? appArgs = null)
    {
        var args = new StringBuilder("run");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Run unit tests in a .NET project")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
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
        [Description("List discovered tests without running them")] bool listTests = false)
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
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Publish a .NET project for deployment")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetProjectPublish(
     [Description("The project file to publish")] string? project = null,
        [Description("The configuration to publish (Debug or Release)")] string? configuration = null,
      [Description("The output directory for published files")] string? output = null,
        [Description("The target runtime identifier (e.g., 'linux-x64', 'win-x64')")] string? runtime = null)
    {
        var args = new StringBuilder("publish");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(runtime)) args.Append($" -r {runtime}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Create a NuGet package from a .NET project. Use this to pack projects for distribution on NuGet.org or private feeds.")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetPackCreate(
      [Description("The project file to pack")] string? project = null,
      [Description("The configuration to pack (Debug or Release)")] string? configuration = null,
        [Description("The output directory for the package")] string? output = null,
        [Description("Include symbols package")] bool includeSymbols = false,
[Description("Include source files in the package")] bool includeSource = false)
    {
        var args = new StringBuilder("pack");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (includeSymbols) args.Append(" --include-symbols");
        if (includeSource) args.Append(" --include-source");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Clean the output of a .NET project")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetProjectClean(
     [Description("The project file or solution file to clean")] string? project = null,
        [Description("The configuration to clean (Debug or Release)")] string? configuration = null)
    {
        var args = new StringBuilder("clean");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        return await ExecuteDotNetCommand(args.ToString());
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
    public async Task<string> DotnetPackageAdd(
 [Description("The name of the NuGet package to add")] string packageName,
    [Description("The project file to add the package to")] string? project = null,
        [Description("The version of the package")] string? version = null,
        [Description("Include prerelease packages")] bool prerelease = false)
    {
        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Add a project-to-project reference")]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetReferenceAdd(
        [Description("The project file to add the reference from")] string project,
   [Description("The project file to reference")] string reference)
        => await ExecuteDotNetCommand($"add \"{project}\" reference \"{reference}\"");

    [McpServerTool, Description("List package references for a .NET project")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetPackageList(
[Description("The project file or solution file")] string? project = null,
     [Description("Show outdated packages")] bool outdated = false,
        [Description("Show deprecated packages")] bool deprecated = false)
    {
        var args = new StringBuilder("list");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append(" package");
        if (outdated) args.Append(" --outdated");
        if (deprecated) args.Append(" --deprecated");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Remove a NuGet package reference from a .NET project")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetPackageRemove(
        [Description("The name of the NuGet package to remove")] string packageName,
  [Description("The project file to remove the package from")] string? project = null)
    {
        var args = new StringBuilder("remove");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Search for NuGet packages on nuget.org. Returns matching packages with descriptions and download counts.")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 7.0)]
    [McpMeta("commonlyUsed", true)]
    public async Task<string> DotnetPackageSearch(
        [Description("Search term to find packages")] string searchTerm,
        [Description("Maximum number of results to return (1-100)")] int? take = null,
        [Description("Skip the first N results")] int? skip = null,
        [Description("Include prerelease packages")] bool prerelease = false,
[Description("Show exact matches only")] bool exactMatch = false)
    {
        var args = new StringBuilder($"package search {searchTerm}");
        if (take.HasValue) args.Append($" --take {take.Value}");
        if (skip.HasValue) args.Append($" --skip {skip.Value}");
        if (prerelease) args.Append(" --prerelease");
        if (exactMatch) args.Append(" --exact-match");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Update a NuGet package reference to a newer version in a .NET project. Note: This uses 'dotnet add package' which updates the package when a newer version is specified.")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetPackageUpdate(
        [Description("The name of the NuGet package to update")] string packageName,
        [Description("The project file to update the package in")] string? project = null,
        [Description("The version to update to")] string? version = null,
        [Description("Update to the latest prerelease version")] bool prerelease = false)
    {
        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("List project references")]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetReferenceList(
     [Description("The project file")] string? project = null)
    {
        var args = "list";
        if (!string.IsNullOrEmpty(project)) args += $" \"{project}\"";
        args += " reference";
        return await ExecuteDotNetCommand(args);
    }

    [McpServerTool, Description("Remove a project-to-project reference")]
    [McpMeta("category", "reference")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetReferenceRemove(
            [Description("The project file to remove the reference from")] string project,
            [Description("The project file to unreference")] string reference)
            => await ExecuteDotNetCommand($"remove \"{project}\" reference \"{reference}\"");

    [McpServerTool, Description("Create a new .NET solution file. A solution file organizes multiple related projects.")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 8.0)]
    public async Task<string> DotnetSolutionCreate(
        [Description("The name for the solution file")] string name,
        [Description("The output directory for the solution file")] string? output = null,
        [Description("The solution file format: 'sln' (classic) or 'slnx' (XML-based). Default is 'sln'.")] string? format = null)
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
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Add one or more projects to a .NET solution file")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetSolutionAdd(
           [Description("The solution file to add projects to")] string solution,
           [Description("Array of project file paths to add to the solution")] string[] projects)
    {
        if (projects == null || projects.Length == 0)
            return "Error: at least one project path is required.";

        var args = new StringBuilder($"solution \"{solution}\" add");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("List all projects in a .NET solution file")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSolutionList(
        [Description("The solution file to list projects from")] string solution)
        => await ExecuteDotNetCommand($"solution \"{solution}\" list");

    [McpServerTool, Description("Remove one or more projects from a .NET solution file")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetSolutionRemove(
        [Description("The solution file to remove projects from")] string solution,
  [Description("Array of project file paths to remove from the solution")] string[] projects)
    {
        if (projects == null || projects.Length == 0)
            return "Error: at least one project path is required.";

        var args = new StringBuilder($"solution \"{solution}\" remove");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Get information about installed .NET SDKs and runtimes")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSdkInfo() => await ExecuteDotNetCommand("--info");

    [McpServerTool, Description("Get the version of the .NET SDK")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSdkVersion() => await ExecuteDotNetCommand("--version");

    [McpServerTool, Description("List installed .NET SDKs")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetSdkList() => await ExecuteDotNetCommand("--list-sdks");

    [McpServerTool, Description("List installed .NET runtimes")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async Task<string> DotnetRuntimeList() => await ExecuteDotNetCommand("--list-runtimes");

    [McpServerTool, Description("Get help for a specific dotnet command. Use this to discover available options for any dotnet command.")]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetHelp(
        [Description("The dotnet command to get help for (e.g., 'build', 'new', 'run'). If not specified, shows general dotnet help.")] string? command = null)
  => await ExecuteDotNetCommand(command != null ? $"{command} --help" : "--help");

    [McpServerTool, Description("Format code according to .editorconfig and style rules. Available since .NET 6 SDK. Useful for enforcing consistent code style across projects.")]
    [McpMeta("category", "format")]
    [McpMeta("priority", 6.0)]
    [McpMeta("minimumSdkVersion", "6.0")]
    public async Task<string> DotnetFormat(
        [Description("The project or solution file to format")] string? project = null,
  [Description("Verify formatting without making changes")] bool verify = false,
        [Description("Include generated code files")] bool includeGenerated = false,
        [Description("Comma-separated list of diagnostic IDs to fix")] string? diagnostics = null,
        [Description("Severity level to fix (info, warn, error)")] string? severity = null)
    {
        var args = new StringBuilder("format");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (verify) args.Append(" --verify-no-changes");
        if (includeGenerated) args.Append(" --include-generated");
        if (!string.IsNullOrEmpty(diagnostics)) args.Append($" --diagnostics {diagnostics}");
        if (!string.IsNullOrEmpty(severity)) args.Append($" --severity {severity}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Manage NuGet local caches. List or clear the global-packages, http-cache, temp, and plugins-cache folders. Useful for troubleshooting NuGet issues.")]
    [McpMeta("category", "nuget")]
    [McpMeta("priority", 4.0)]
    public async Task<string> DotnetNugetLocals(
        [Description("The cache location to manage: all, http-cache, global-packages, temp, or plugins-cache")] string cacheLocation,
        [Description("List the cache location path")] bool list = false,
      [Description("Clear the specified cache location")] bool clear = false)
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
        return await ExecuteDotNetCommand(args);
    }

    private async Task<string> ExecuteDotNetCommand(string arguments)
        => await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _logger);

    private static bool IsValidAdditionalOptions(string options)
    {
        // Allow alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs
        // This covers standard CLI option patterns like: --option-name value --flag --key=value
        // Reject shell metacharacters that could be used for injection: &, |, ;, <, >, `, $, (, ), {, }, [, ], \, ", '
        return !options.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.' && c != ' ' && c != '=');
    }
}
