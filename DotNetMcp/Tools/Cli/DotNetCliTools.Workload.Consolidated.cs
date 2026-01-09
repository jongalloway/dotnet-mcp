using System.Text;
using DotNetMcp.Actions;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Consolidated .NET workload management tool.
/// Provides unified interface for all workload operations including install, update, list, search, and uninstall.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Manage .NET workloads for specialized development scenarios (MAUI, WASM, etc.).
    /// Provides a consolidated interface for installing, updating, listing, searching, and uninstalling workloads.
    /// </summary>
    /// <param name="action">The workload operation to perform</param>
    /// <param name="searchTerm">Optional search term to filter workloads (used with Search action)</param>
    /// <param name="workloadIds">Array of workload IDs to install or uninstall (required for Install and Uninstall actions)</param>
    /// <param name="skipManifestUpdate">Skip updating the workload manifests during installation (used with Install action)</param>
    /// <param name="includePreviews">Allow prerelease workload manifests (used with Install and Update actions)</param>
    /// <param name="source">NuGet package source to use during restore (used with Install and Update actions)</param>
    /// <param name="configFile">Path to NuGet configuration file to use (used with Install and Update actions)</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "workload")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", false)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["List","Info","Search","Install","Update","Uninstall"]""")]
    [McpMeta("tags", JsonValue = """["workload","consolidated","sdk","mobile","maui","wasm"]""")]
    public async partial Task<string> DotnetWorkload(
        DotnetWorkloadAction action,
        string? searchTerm = null,
        string[]? workloadIds = null,
        bool skipManifestUpdate = false,
        bool includePreviews = false,
        string? source = null,
        string? configFile = null,
        string? workingDirectory = null,
        bool machineReadable = false)
    {
        return await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action enum
            if (!ParameterValidator.ValidateAction<DotnetWorkloadAction>(action, out var actionError))
            {
                if (machineReadable)
                {
                    var validActions = Enum.GetNames(typeof(DotnetWorkloadAction));
                    var error = ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        validActions,
                        toolName: "dotnet_workload");
                    return ErrorResultFactory.ToJson(error);
                }
                return $"Error: {actionError}";
            }

            // Route to appropriate action handler
            return action switch
            {
                DotnetWorkloadAction.List => await HandleListAction(machineReadable),
                DotnetWorkloadAction.Info => await HandleInfoAction(machineReadable),
                DotnetWorkloadAction.Search => await HandleSearchAction(searchTerm, machineReadable),
                DotnetWorkloadAction.Install => await HandleInstallAction(workloadIds, skipManifestUpdate, includePreviews, source, configFile, machineReadable),
                DotnetWorkloadAction.Update => await HandleUpdateAction(includePreviews, source, configFile, machineReadable),
                DotnetWorkloadAction.Uninstall => await HandleUninstallAction(workloadIds, machineReadable),
                _ => machineReadable
                    ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        Enum.GetNames(typeof(DotnetWorkloadAction)),
                        toolName: "dotnet_workload"))
                    : $"Error: Unsupported action '{action}'"
            };
        });
    }

    private async Task<string> HandleListAction(bool machineReadable)
    {
        return await ExecuteDotNetCommand("workload list", machineReadable);
    }

    private async Task<string> HandleInfoAction(bool machineReadable)
    {
        return await ExecuteDotNetCommand("workload --info", machineReadable);
    }

    private async Task<string> HandleSearchAction(string? searchTerm, bool machineReadable)
    {
        var args = "workload search";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            args += $" \"{searchTerm}\"";
        }
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    private async Task<string> HandleInstallAction(
        string[]? workloadIds,
        bool skipManifestUpdate,
        bool includePreviews,
        string? source,
        string? configFile,
        bool machineReadable)
    {
        // Validate workload IDs using shared validation
        var validationError = ValidateWorkloadIds(workloadIds, "Install", machineReadable);
        if (validationError != null)
        {
            return validationError;
        }

        var args = new StringBuilder("workload install");

        // Add each workload ID
        foreach (var id in workloadIds!)
        {
            args.Append($" {id}");
        }

        if (skipManifestUpdate) args.Append(" --skip-manifest-update");
        if (includePreviews) args.Append(" --include-previews");
        if (!string.IsNullOrEmpty(source)) args.Append($" --source \"{source}\"");
        if (!string.IsNullOrEmpty(configFile)) args.Append($" --configfile \"{configFile}\"");

        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    private async Task<string> HandleUpdateAction(
        bool includePreviews,
        string? source,
        string? configFile,
        bool machineReadable)
    {
        var args = new StringBuilder("workload update");

        if (includePreviews) args.Append(" --include-previews");
        if (!string.IsNullOrEmpty(source)) args.Append($" --source \"{source}\"");
        if (!string.IsNullOrEmpty(configFile)) args.Append($" --configfile \"{configFile}\"");

        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    private async Task<string> HandleUninstallAction(string[]? workloadIds, bool machineReadable)
    {
        // Validate workload IDs using shared validation
        var validationError = ValidateWorkloadIds(workloadIds, "Uninstall", machineReadable);
        if (validationError != null)
        {
            return validationError;
        }

        var args = new StringBuilder("workload uninstall");

        // Add each workload ID
        foreach (var id in workloadIds!)
        {
            args.Append($" {id}");
        }

        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Validates workload IDs array and returns error message if validation fails.
    /// </summary>
    /// <param name="workloadIds">Array of workload IDs to validate</param>
    /// <param name="actionName">Name of the action (for error messages)</param>
    /// <param name="machineReadable">Whether to return JSON-formatted errors</param>
    /// <returns>Error message if validation fails, null if validation succeeds</returns>
    private string? ValidateWorkloadIds(string[]? workloadIds, string actionName, bool machineReadable)
    {
        // Validate workloadIds is provided and not empty
        if (workloadIds == null || workloadIds.Length == 0)
        {
            var errorMessage = $"The 'workloadIds' parameter is required for {actionName} action and must contain at least one workload ID.";
            if (machineReadable)
            {
                var error = new ErrorResponse
                {
                    Success = false,
                    Errors = new List<ErrorResult>
                    {
                        new ErrorResult
                        {
                            Code = "MISSING_PARAMETER",
                            Message = errorMessage,
                            Category = "Validation",
                            Hint = "Provide one or more workload IDs (e.g., ['maui-android', 'wasm-tools'])",
                            McpErrorCode = McpErrorCodes.InvalidParams
                        }
                    },
                    ExitCode = -1
                };
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // Validate each workload ID
        foreach (var id in workloadIds)
        {
            if (!ParameterValidator.ValidateWorkloadId(id, out var validationError))
            {
                if (machineReadable)
                {
                    var error = new ErrorResponse
                    {
                        Success = false,
                        Errors = new List<ErrorResult>
                        {
                            new ErrorResult
                            {
                                Code = "INVALID_PARAMETER",
                                Message = validationError!,
                                Category = "Validation",
                                Hint = "Workload IDs must contain only alphanumeric characters, hyphens, and underscores",
                                McpErrorCode = McpErrorCodes.InvalidParams
                            }
                        },
                        ExitCode = -1
                    };
                    return ErrorResultFactory.ToJson(error);
                }
                return $"Error: {validationError}";
            }
        }

        return null; // Validation succeeded
    }

    // ===== Workload helper methods (moved from DotNetCliTools.Workload.cs) =====
    /// <summary>
    /// List all installed .NET workloads with their versions and manifest information.
    /// Shows workloads currently installed for mobile, MAUI, Blazor WASM, and other specialized development.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "workload")]
    [McpMeta("priority", 7.0)]
    [McpMeta("commonlyUsed", false)]
    [McpMeta("tags", JsonValue = """["workload","list","installed","sdk"]""")]
    internal async Task<string> DotnetWorkloadList(bool machineReadable = false)
        => await ExecuteDotNetCommand("workload list", machineReadable);

    /// <summary>
    /// Get detailed information about installed .NET workloads.
    /// Shows comprehensive details including manifest versions, installation paths, and installation sources for each workload.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "workload")]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["workload","info","details","installed","manifest"]""")]
    internal async Task<string> DotnetWorkloadInfo(bool machineReadable = false)
        => await ExecuteDotNetCommand("workload --info", machineReadable);

    /// <summary>
    /// Search for available .NET workloads by name or description.
    /// Finds workloads for mobile (iOS, Android), MAUI, Blazor WebAssembly, and other platform-specific development.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter workloads (searches in IDs and descriptions). If not provided, shows all available workloads.</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "workload")]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["workload","search","available","discovery"]""")]
    internal async Task<string> DotnetWorkloadSearch(
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
    [McpMeta("category", "workload")]
    [McpMeta("priority", 8.0)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresElevation", "sometimes")]
    [McpMeta("tags", JsonValue = """["workload","install","setup","mobile","maui","wasm"]""")]
    internal async Task<string> DotnetWorkloadInstall(
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
    [McpMeta("category", "workload")]
    [McpMeta("priority", 7.0)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["workload","update","upgrade","maintenance"]""")]
    internal async Task<string> DotnetWorkloadUpdate(
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
    [McpMeta("category", "workload")]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["workload","uninstall","remove","cleanup"]""")]
    internal async Task<string> DotnetWorkloadUninstall(
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
