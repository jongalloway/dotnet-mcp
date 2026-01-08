using System.ComponentModel;
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [Description("Manage NuGet packages and project references. Supports add, remove, search, update, list packages, add/remove/list references, and clear cache.")]
    [McpMeta("category", "package")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Add","Remove","Search","Update","List","AddReference","RemoveReference","ListReferences","ClearCache"]""")]
    public async Task<string> DotnetPackage(
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
        bool machineReadable = false)
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

        // Route to existing DotnetPackageAdd method
        return await DotnetPackageAdd(
            packageName: packageId!,
            project: project,
            version: version,
            prerelease: prerelease,
            machineReadable: machineReadable);
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
}
