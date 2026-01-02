using System.Text;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// .NET workload management for mobile, MAUI, and WASM development.
/// Enables installation and management of SDK workloads for specialized development scenarios.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// List all installed .NET workloads with their versions and manifest information.
    /// Shows workloads currently installed for mobile, MAUI, Blazor WASM, and other specialized development.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "workload")]
    [McpMeta("priority", 7.0)]
    [McpMeta("commonlyUsed", false)]
    [McpMeta("tags", JsonValue = """["workload","list","installed","sdk"]""")]
    public async partial Task<string> DotnetWorkloadList(bool machineReadable = false)
        => await ExecuteDotNetCommand("workload list", machineReadable);

    /// <summary>
    /// Search for available .NET workloads by name or description.
    /// Finds workloads for mobile (iOS, Android), MAUI, Blazor WebAssembly, and other platform-specific development.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter workloads (searches in IDs and descriptions). If not provided, shows all available workloads.</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "workload")]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["workload","search","available","discovery"]""")]
    public async partial Task<string> DotnetWorkloadSearch(
        string? searchTerm = null,
        bool machineReadable = false)
    {
        var args = "workload search";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            args += $" \"{searchTerm}\"";
        }
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    /// <summary>
    /// Install one or more .NET workloads to enable specialized development scenarios.
    /// WARNING: This operation can download large packages (GB) and may require elevated permissions.
    /// Common workloads: maui-android, maui-ios, maui-windows, wasm-tools, android, ios.
    /// </summary>
    /// <param name="workloadIds">One or more workload IDs to install (comma-separated for multiple, e.g., 'maui-android,wasm-tools')</param>
    /// <param name="skipManifestUpdate">Skip updating the workload manifests during installation</param>
    /// <param name="includePreviews">Allow prerelease workload manifests</param>
    /// <param name="source">NuGet package source to use during restore (can specify multiple by repeating)</param>
    /// <param name="configFile">Path to NuGet configuration file to use</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "workload")]
    [McpMeta("priority", 8.0)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresElevation", "sometimes")]
    [McpMeta("tags", JsonValue = """["workload","install","setup","mobile","maui","wasm"]""")]
    public async partial Task<string> DotnetWorkloadInstall(
        string workloadIds,
        bool skipManifestUpdate = false,
        bool includePreviews = false,
        string? source = null,
        string? configFile = null,
        bool machineReadable = false)
    {
        // Parse and validate workload IDs
        if (!ParameterValidator.ParseWorkloadIds(workloadIds, out var ids, out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        var args = new StringBuilder("workload install");
        
        // Add each workload ID
        foreach (var id in ids)
        {
            args.Append($" {id}");
        }

        if (skipManifestUpdate) args.Append(" --skip-manifest-update");
        if (includePreviews) args.Append(" --include-previews");
        if (!string.IsNullOrEmpty(source)) args.Append($" --source \"{source}\"");
        if (!string.IsNullOrEmpty(configFile)) args.Append($" --configfile \"{configFile}\"");

        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Update all installed .NET workloads to their latest versions.
    /// WARNING: This operation can download large packages and may take significant time.
    /// Ensures all installed workloads are up-to-date with the latest SDK version.
    /// </summary>
    /// <param name="includePreviews">Allow prerelease workload manifests</param>
    /// <param name="source">NuGet package source to use during restore</param>
    /// <param name="configFile">Path to NuGet configuration file to use</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "workload")]
    [McpMeta("priority", 7.0)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["workload","update","upgrade","maintenance"]""")]
    public async partial Task<string> DotnetWorkloadUpdate(
        bool includePreviews = false,
        string? source = null,
        string? configFile = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("workload update");
        
        if (includePreviews) args.Append(" --include-previews");
        if (!string.IsNullOrEmpty(source)) args.Append($" --source \"{source}\"");
        if (!string.IsNullOrEmpty(configFile)) args.Append($" --configfile \"{configFile}\"");

        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Uninstall one or more .NET workloads.
    /// Removes workload components that are no longer needed, freeing disk space.
    /// </summary>
    /// <param name="workloadIds">One or more workload IDs to uninstall (comma-separated for multiple, e.g., 'maui-android,wasm-tools')</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "workload")]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["workload","uninstall","remove","cleanup"]""")]
    public async partial Task<string> DotnetWorkloadUninstall(
        string workloadIds,
        bool machineReadable = false)
    {
        // Parse and validate workload IDs
        if (!ParameterValidator.ParseWorkloadIds(workloadIds, out var ids, out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        var args = new StringBuilder("workload uninstall");
        
        // Add each workload ID
        foreach (var id in ids)
        {
            args.Append($" {id}");
        }

        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }
}
