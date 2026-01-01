using System.Text;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Package management tools for NuGet packages.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Create a NuGet package from a .NET project.
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
    /// Manage local NuGet caches. List or clear the local NuGet HTTP request cache, global packages folder, or temp folder.
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
}
