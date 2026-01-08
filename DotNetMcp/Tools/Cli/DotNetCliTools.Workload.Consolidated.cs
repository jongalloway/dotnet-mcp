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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "workload")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", false)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["List","Info","Search","Install","Update","Uninstall"]""")]
    [McpMeta("tags", JsonValue = """["workload","consolidated","sdk","mobile","maui","wasm"]""")]
    public async Task<string> DotnetWorkload(
        DotnetWorkloadAction action,
        string? searchTerm = null,
        string[]? workloadIds = null,
        bool skipManifestUpdate = false,
        bool includePreviews = false,
        string? source = null,
        string? configFile = null,
        bool machineReadable = false)
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
        // Validate workloadIds is provided and not empty
        if (workloadIds == null || workloadIds.Length == 0)
        {
            var errorMessage = "The 'workloadIds' parameter is required for Install action and must contain at least one workload ID.";
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
                            Hint = "Provide one or more workload IDs to install (e.g., ['maui-android', 'wasm-tools'])",
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

        var args = new StringBuilder("workload install");
        
        // Add each workload ID
        foreach (var id in workloadIds)
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
        // Validate workloadIds is provided and not empty
        if (workloadIds == null || workloadIds.Length == 0)
        {
            var errorMessage = "The 'workloadIds' parameter is required for Uninstall action and must contain at least one workload ID.";
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
                            Hint = "Provide one or more workload IDs to uninstall (e.g., ['maui-android', 'wasm-tools'])",
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

        var args = new StringBuilder("workload uninstall");
        
        // Add each workload ID
        foreach (var id in workloadIds)
        {
            args.Append($" {id}");
        }

        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }
}
