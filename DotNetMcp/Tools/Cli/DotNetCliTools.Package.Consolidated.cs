using System.Text;
using DotNetMcp.Actions;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Consolidated .NET package management commands.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Manage NuGet packages and project references.
    /// Provides a unified interface for all package and reference operations including
    /// adding/removing packages, searching NuGet.org, updating packages, managing project-to-project
    /// references, and clearing local caches.
    /// </summary>
    /// <param name="action">The package operation to perform</param>
    /// <param name="packageId">NuGet package ID for add/remove/update operations (e.g., 'Newtonsoft.Json', 'Serilog')</param>
    /// <param name="version">Specific package version to install or update to</param>
    /// <param name="project">Path to project file for package/reference operations</param>
    /// <param name="source">NuGet source URL for package operations</param>
    /// <param name="framework">Target framework for package operations</param>
    /// <param name="prerelease">Include prerelease package versions</param>
    /// <param name="searchTerm">Search query for finding packages on NuGet.org</param>
    /// <param name="take">Maximum number of search results to return (1-100)</param>
    /// <param name="skip">Skip the first N search results for pagination</param>
    /// <param name="exactMatch">Show exact matches only in search results</param>
    /// <param name="outdated">Show only outdated packages in list</param>
    /// <param name="deprecated">Show only deprecated packages in list</param>
    /// <param name="referencePath">Path to referenced project for add/remove reference operations</param>
    /// <param name="cacheType">Cache location to clear: all, http-cache, global-packages, temp, plugins-cache</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    [McpServerTool(Title = "NuGet Package Manager", Destructive = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Package/Flat/package_flat.svg")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Add","Remove","Search","Update","List","AddReference","RemoveReference","ListReferences","ClearCache"]""")]
    public async partial Task<CallToolResult> DotnetPackage(
        DotnetPackageAction action,
        string? packageId = null,
        string? version = null,
        string? project = null,
        string? source = null,
        string? framework = null,
        bool? prerelease = null,
        string? searchTerm = null,
        int? take = null,
        int? skip = null,
        bool? exactMatch = null,
        bool? outdated = null,
        bool? deprecated = null,
        string? referencePath = null,
        string? cacheType = null,
        string? workingDirectory = null,
        IProgress<ProgressNotificationValue>? progress = null)
    {
        var textResult = await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action parameter
            if (!ParameterValidator.ValidateAction<DotnetPackageAction>(action, out var errorMessage))
            {
                return $"Error: {errorMessage}";
            }

            // Route to appropriate handler based on action
            return action switch
            {
                DotnetPackageAction.Add => await ExecuteWithProgress(progress, "Adding package...", "Package added", () => HandleAddAction(packageId, project, version, source, framework, prerelease ?? false)),
                DotnetPackageAction.Remove => await HandleRemoveAction(packageId, project),
                DotnetPackageAction.Search => await HandleSearchAction(searchTerm, take, skip, prerelease ?? false, exactMatch ?? false),
                DotnetPackageAction.Update => await ExecuteWithProgress(progress, "Updating packages...", "Update complete", () => HandleUpdateAction(packageId, project, version, prerelease ?? false)),
                DotnetPackageAction.List => await HandleListAction(project, outdated ?? false, deprecated ?? false),
                DotnetPackageAction.AddReference => await HandleAddReferenceAction(project, referencePath),
                DotnetPackageAction.RemoveReference => await HandleRemoveReferenceAction(project, referencePath),
                DotnetPackageAction.ListReferences => await HandleListReferencesAction(project),
                DotnetPackageAction.ClearCache => await HandleClearCacheAction(cacheType),
                _ => $"Error: Action '{action}' is not supported."
            };
        });

        // Add structured content for List action
        object? structured = action == DotnetPackageAction.List
            ? BuildPackageListStructuredContent(textResult)
            : null;

        return StructuredContentHelper.ToCallToolResult(textResult, structured);
    }

    private async Task<string> HandleAddAction(string? packageId, string? project, string? version, string? source, string? framework, bool prerelease)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(packageId, "packageId", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        // If no source/framework specified, preserve existing behavior by routing to DotnetPackageAdd
        if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(framework))
        {
            return await DotnetPackageAdd(
                packageName: packageId!,
                project: project,
                version: version,
                prerelease: prerelease);
        }

        // When source or framework are specified, execute 'dotnet add package' directly so those options are honored
        var args = new StringBuilder("add");

        if (!string.IsNullOrWhiteSpace(project))
        {
            args.Append($" \"{project}\"");
        }

        args.Append(" package");
        args.Append($" \"{packageId}\"");

        if (!string.IsNullOrWhiteSpace(version))
        {
            args.Append($" --version \"{version}\"");
        }

        if (prerelease)
        {
            args.Append(" --prerelease");
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            args.Append($" --source \"{source}\"");
        }

        if (!string.IsNullOrWhiteSpace(framework))
        {
            args.Append($" --framework \"{framework}\"");
        }

        return await ExecuteDotNetCommand(args.ToString());
    }

    private async Task<string> HandleRemoveAction(string? packageId, string? project)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(packageId, "packageId", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetPackageRemove method
        return await DotnetPackageRemove(
            packageName: packageId!,
            project: project);
    }

    private async Task<string> HandleSearchAction(string? searchTerm, int? take, int? skip, bool prerelease, bool exactMatch)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(searchTerm, "searchTerm", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetPackageSearch method
        return await DotnetPackageSearch(
            searchTerm: searchTerm!,
            take: take,
            skip: skip,
            prerelease: prerelease,
            exactMatch: exactMatch);
    }

    private async Task<string> HandleUpdateAction(string? packageId, string? project, string? version, bool prerelease)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(packageId, "packageId", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetPackageUpdate method
        return await DotnetPackageUpdate(
            packageName: packageId!,
            project: project,
            version: version,
            prerelease: prerelease);
    }

    private async Task<string> HandleListAction(string? project, bool outdated, bool deprecated)
    {
        // Route to existing DotnetPackageList method
        return await DotnetPackageList(
            project: project,
            outdated: outdated,
            deprecated: deprecated);
    }

    private async Task<string> HandleAddReferenceAction(string? project, string? referencePath)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            return $"Error: {projectError}";
        }

        if (!ParameterValidator.ValidateRequiredParameter(referencePath, "referencePath", out var referenceError))
        {
            return $"Error: {referenceError}";
        }

        // Route to existing DotnetReferenceAdd method
        return await DotnetReferenceAdd(
            project: project!,
            reference: referencePath!);
    }

    private async Task<string> HandleRemoveReferenceAction(string? project, string? referencePath)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            return $"Error: {projectError}";
        }

        if (!ParameterValidator.ValidateRequiredParameter(referencePath, "referencePath", out var referenceError))
        {
            return $"Error: {referenceError}";
        }

        // Route to existing DotnetReferenceRemove method
        return await DotnetReferenceRemove(
            project: project!,
            reference: referencePath!);
    }

    private async Task<string> HandleListReferencesAction(string? project)
    {
        // Route to existing DotnetReferenceList method
        return await DotnetReferenceList(
            project: project);
    }

    private async Task<string> HandleClearCacheAction(string? cacheType)
    {
        // Default to "all" if not specified
        var cacheLocation = cacheType ?? "all";

        // Route to existing DotnetNugetLocals method with clear=true
        return await DotnetNugetLocals(
            cacheLocation: cacheLocation,
            list: false,
            clear: true);
    }

    private static object? BuildPackageListStructuredContent(string textResult)
    {
        // Parse package list from 'dotnet list package' output
        var lines = textResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var packages = lines
            .Where(l => l.TrimStart().StartsWith(">", StringComparison.Ordinal))
            .Select(l =>
            {
                var parts = l.Trim().TrimStart('>').Trim()
                    .Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                    return new { name = parts[0], requestedVersion = (string?)parts[1], resolvedVersion = (string?)parts[2] };
                if (parts.Length == 2)
                    return new { name = parts[0], requestedVersion = (string?)parts[1], resolvedVersion = (string?)null };
                return parts.Length == 1
                    ? new { name = parts[0], requestedVersion = (string?)null, resolvedVersion = (string?)null }
                    : null;
            })
            .Where(p => p != null)
            .ToArray();
        return new { packages };
    }

    // ===== Reference helper methods (moved from DotNetCliTools.Reference.cs) =====

    /// <summary>
    /// Add a project-to-project reference.
    /// </summary>
    internal async Task<string> DotnetReferenceAdd(
        string project,
        string reference)
        => await ExecuteDotNetCommand($"add \"{project}\" reference \"{reference}\"");

    /// <summary>
    /// List project references.
    /// </summary>
    internal async Task<string> DotnetReferenceList(
        string? project = null)
    {
        var args = "list";
        if (!string.IsNullOrEmpty(project)) args += $" \"{project}\"";
        args += " reference";
        return await ExecuteDotNetCommand(args);
    }

    /// <summary>
    /// Remove a project-to-project reference.
    /// </summary>
    internal async Task<string> DotnetReferenceRemove(
        string project,
        string reference)
        => await ExecuteDotNetCommand($"remove \"{project}\" reference \"{reference}\"");

    // ===== Package helper methods (moved from DotNetCliTools.Package.cs) =====

    /// <summary>
    /// Create a NuGet package from a .NET project.
    /// </summary>
    internal async Task<string> DotnetPackCreate(
        string? project = null,
        string? configuration = null,
        string? output = null,
        bool includeSymbols = false,
        bool includeSource = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        // Validate configuration
        if (!ParameterValidator.ValidateConfiguration(configuration, out var configError))
            return $"Error: {configError}";

        var args = new StringBuilder("pack");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (includeSymbols) args.Append(" --include-symbols");
        if (includeSource) args.Append(" --include-source");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Add a NuGet package reference to a .NET project.
    /// </summary>
    internal async Task<string> DotnetPackageAdd(
        string packageName,
        string? project = null,
        string? version = null,
        bool prerelease = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// List package references for a .NET project.
    /// </summary>
    internal async Task<string> DotnetPackageList(
        string? project = null,
        bool outdated = false,
        bool deprecated = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        var args = new StringBuilder("list");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append(" package");
        if (outdated) args.Append(" --outdated");
        if (deprecated) args.Append(" --deprecated");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Remove a NuGet package reference from a .NET project.
    /// </summary>
    internal async Task<string> DotnetPackageRemove(
        string packageName,
        string? project = null)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        var args = new StringBuilder("remove");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Search for NuGet packages on nuget.org. Returns matching packages with descriptions and download counts.
    /// </summary>
    internal async Task<string> DotnetPackageSearch(
        string searchTerm,
        int? take = null,
        int? skip = null,
        bool prerelease = false,
        bool exactMatch = false)
    {
        var args = new StringBuilder($"package search {searchTerm}");
        if (take.HasValue) args.Append($" --take {take.Value}");
        if (skip.HasValue) args.Append($" --skip {skip.Value}");
        if (prerelease) args.Append(" --prerelease");
        if (exactMatch) args.Append(" --exact-match");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Update a NuGet package reference to a newer version in a .NET project. 
    /// Note: This uses 'dotnet add package' which updates the package when a newer version is specified.
    /// </summary>
    internal async Task<string> DotnetPackageUpdate(
        string packageName,
        string? project = null,
        string? version = null,
        bool prerelease = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Manage local NuGet caches. List or clear the local NuGet HTTP request cache, global packages folder, or temp folder.
    /// </summary>
    internal async Task<string> DotnetNugetLocals(
        string cacheLocation,
        bool list = false,
        bool clear = false)
    {
        if (!list && !clear)
        {
            return "Error: Either 'list' or 'clear' must be true.";
        }

        if (list && clear)
        {
            return "Error: Cannot specify both 'list' and 'clear'.";
        }

        var validLocations = new[] { "all", "http-cache", "global-packages", "temp", "plugins-cache" };
        var normalizedCacheLocation = cacheLocation.ToLowerInvariant();
        if (!validLocations.Contains(normalizedCacheLocation))
        {
            return $"Error: Invalid cache location. Must be one of: {string.Join(", ", validLocations)}";
        }

        var args = $"nuget locals {normalizedCacheLocation}";
        if (list) args += " --list";
        if (clear) args += " --clear";
        return await ExecuteDotNetCommand(args);
    }
}
