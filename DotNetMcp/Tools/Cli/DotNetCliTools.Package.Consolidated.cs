using System.Text;
using DotNetMcp.Actions;
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "package")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Add","Remove","Search","Update","List","AddReference","RemoveReference","ListReferences","ClearCache"]""")]
    public async partial Task<string> DotnetPackage(
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
        bool machineReadable = false)
    {
        return await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action parameter
            if (!ParameterValidator.ValidateAction<DotnetPackageAction>(action, out var errorMessage))
            {
                if (machineReadable)
                {
                    var validActions = Enum.GetNames(typeof(DotnetPackageAction));
                    var error = ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        validActions,
                        toolName: "dotnet_package");
                    return ErrorResultFactory.ToJson(error);
                }
                return $"Error: {errorMessage}";
            }

            // Route to appropriate handler based on action
            return action switch
            {
                DotnetPackageAction.Add => await HandleAddAction(packageId, project, version, source, framework, prerelease ?? false, machineReadable),
                DotnetPackageAction.Remove => await HandleRemoveAction(packageId, project, machineReadable),
                DotnetPackageAction.Search => await HandleSearchAction(searchTerm, take, skip, prerelease ?? false, exactMatch ?? false, machineReadable),
                DotnetPackageAction.Update => await HandleUpdateAction(packageId, project, version, prerelease ?? false, machineReadable),
                DotnetPackageAction.List => await HandleListAction(project, outdated ?? false, deprecated ?? false, machineReadable),
                DotnetPackageAction.AddReference => await HandleAddReferenceAction(project, referencePath, machineReadable),
                DotnetPackageAction.RemoveReference => await HandleRemoveReferenceAction(project, referencePath, machineReadable),
                DotnetPackageAction.ListReferences => await HandleListReferencesAction(project, machineReadable),
                DotnetPackageAction.ClearCache => await HandleClearCacheAction(cacheType, machineReadable),
                _ => machineReadable
                    ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateValidationError(
                        $"Action '{action}' is not supported.",
                        parameterName: "action",
                        reason: "not supported"))
                    : $"Error: Action '{action}' is not supported."
            };
        });
    }

    private async Task<string> HandleAddAction(string? packageId, string? project, string? version, string? source, string? framework, bool prerelease, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(packageId, "packageId", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "packageId",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // If no source/framework specified, preserve existing behavior by routing to DotnetPackageAdd
        if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(framework))
        {
            return await DotnetPackageAdd(
                packageName: packageId!,
                project: project,
                version: version,
                prerelease: prerelease,
                machineReadable: machineReadable);
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

        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    private async Task<string> HandleRemoveAction(string? packageId, string? project, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(packageId, "packageId", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "packageId",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetPackageRemove method
        return await DotnetPackageRemove(
            packageName: packageId!,
            project: project,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleSearchAction(string? searchTerm, int? take, int? skip, bool prerelease, bool exactMatch, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(searchTerm, "searchTerm", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "searchTerm",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetPackageSearch method
        return await DotnetPackageSearch(
            searchTerm: searchTerm!,
            take: take,
            skip: skip,
            prerelease: prerelease,
            exactMatch: exactMatch,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleUpdateAction(string? packageId, string? project, string? version, bool prerelease, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(packageId, "packageId", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "packageId",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // Route to existing DotnetPackageUpdate method
        return await DotnetPackageUpdate(
            packageName: packageId!,
            project: project,
            version: version,
            prerelease: prerelease,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleListAction(string? project, bool outdated, bool deprecated, bool machineReadable)
    {
        // Route to existing DotnetPackageList method
        return await DotnetPackageList(
            project: project,
            outdated: outdated,
            deprecated: deprecated,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleAddReferenceAction(string? project, string? referencePath, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    projectError!,
                    parameterName: "project",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {projectError}";
        }

        if (!ParameterValidator.ValidateRequiredParameter(referencePath, "referencePath", out var referenceError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    referenceError!,
                    parameterName: "referencePath",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {referenceError}";
        }

        // Route to existing DotnetReferenceAdd method
        return await DotnetReferenceAdd(
            project: project!,
            reference: referencePath!,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleRemoveReferenceAction(string? project, string? referencePath, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(project, "project", out var projectError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    projectError!,
                    parameterName: "project",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {projectError}";
        }

        if (!ParameterValidator.ValidateRequiredParameter(referencePath, "referencePath", out var referenceError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    referenceError!,
                    parameterName: "referencePath",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {referenceError}";
        }

        // Route to existing DotnetReferenceRemove method
        return await DotnetReferenceRemove(
            project: project!,
            reference: referencePath!,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleListReferencesAction(string? project, bool machineReadable)
    {
        // Route to existing DotnetReferenceList method
        return await DotnetReferenceList(
            project: project,
            machineReadable: machineReadable);
    }

    private async Task<string> HandleClearCacheAction(string? cacheType, bool machineReadable)
    {
        // Default to "all" if not specified
        var cacheLocation = cacheType ?? "all";

        // Route to existing DotnetNugetLocals method with clear=true
        return await DotnetNugetLocals(
            cacheLocation: cacheLocation,
            list: false,
            clear: true,
            machineReadable: machineReadable);
    }

    // ===== Reference helper methods (moved from DotNetCliTools.Reference.cs) =====

    /// <summary>
    /// Add a project-to-project reference.
    /// </summary>
    private async Task<string> DotnetReferenceAdd(
        string project,
        string reference,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"add \"{project}\" reference \"{reference}\"", machineReadable);

    /// <summary>
    /// List project references.
    /// </summary>
    private async Task<string> DotnetReferenceList(
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
    private async Task<string> DotnetReferenceRemove(
        string project,
        string reference,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"remove \"{project}\" reference \"{reference}\"", machineReadable);

    // ===== Package helper methods (moved from DotNetCliTools.Package.cs) =====

    /// <summary>
    /// Create a NuGet package from a .NET project.
    /// </summary>
    internal async Task<string> DotnetPackCreate(
        string? project = null,
        string? configuration = null,
        string? output = null,
        bool includeSymbols = false,
        bool includeSource = false,
        bool machineReadable = false)
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
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Add a NuGet package reference to a .NET project.
    /// </summary>
    private async Task<string> DotnetPackageAdd(
        string packageName,
        string? project = null,
        string? version = null,
        bool prerelease = false,
        bool machineReadable = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List package references for a .NET project.
    /// </summary>
    private async Task<string> DotnetPackageList(
        string? project = null,
        bool outdated = false,
        bool deprecated = false,
        bool machineReadable = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

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
    private async Task<string> DotnetPackageRemove(
        string packageName,
        string? project = null,
        bool machineReadable = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        var args = new StringBuilder("remove");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Search for NuGet packages on nuget.org. Returns matching packages with descriptions and download counts.
    /// </summary>
    private async Task<string> DotnetPackageSearch(
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
    private async Task<string> DotnetPackageUpdate(
        string packageName,
        string? project = null,
        string? version = null,
        bool prerelease = false,
        bool machineReadable = false)
    {
        // Validate project path if provided
        if (!ParameterValidator.ValidateProjectPath(project, out var projectError))
            return $"Error: {projectError}";

        var args = new StringBuilder("add");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        args.Append($" package {packageName}");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        else if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Manage local NuGet caches. List or clear the local NuGet HTTP request cache, global packages folder, or temp folder.
    /// </summary>
    private async Task<string> DotnetNugetLocals(
        string cacheLocation,
        bool list = false,
        bool clear = false,
        bool machineReadable = false)
    {
        if (!list && !clear)
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "Either 'list' or 'clear' must be true.",
                    parameterName: "list/clear",
                    reason: "at least one required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: Either 'list' or 'clear' must be true.";
        }

        if (list && clear)
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "Cannot specify both 'list' and 'clear'.",
                    parameterName: "list/clear",
                    reason: "mutually exclusive");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: Cannot specify both 'list' and 'clear'.";
        }

        var validLocations = new[] { "all", "http-cache", "global-packages", "temp", "plugins-cache" };
        var normalizedCacheLocation = cacheLocation.ToLowerInvariant();
        if (!validLocations.Contains(normalizedCacheLocation))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    $"Invalid cache location. Must be one of: {string.Join(", ", validLocations)}",
                    parameterName: "cacheLocation",
                    reason: "invalid value");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: Invalid cache location. Must be one of: {string.Join(", ", validLocations)}";
        }

        var args = $"nuget locals {normalizedCacheLocation}";
        if (list) args += " --list";
        if (clear) args += " --clear";
        return await ExecuteDotNetCommand(args, machineReadable);
    }
}
