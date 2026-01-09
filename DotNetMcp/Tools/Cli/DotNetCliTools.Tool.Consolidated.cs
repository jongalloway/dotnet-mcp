using System.ComponentModel;
using System.Text;
using DotNetMcp.Actions;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Consolidated .NET tool management commands.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Manage .NET tools (global, local, and tool manifests).
    /// Provides a unified interface for all .NET tool operations including installation,
    /// updates, searching, running, and manifest management.
    /// </summary>
    /// <param name="action">The tool operation to perform</param>
    /// <param name="packageId">NuGet package ID of the tool for install/update/uninstall operations (e.g., 'dotnet-ef', 'dotnet-format')</param>
    /// <param name="global">Install/update/uninstall as global tool (system-wide); otherwise operates on local tools</param>
    /// <param name="version">Specific version to install or update to</param>
    /// <param name="framework">Target framework for installation</param>
    /// <param name="toolPath">Custom tool installation path</param>
    /// <param name="searchTerm">Search query for finding tools on NuGet.org</param>
    /// <param name="detail">Show detailed information in search results</param>
    /// <param name="take">Maximum number of search results to return (1-100)</param>
    /// <param name="skip">Skip the first N search results for pagination</param>
    /// <param name="prerelease">Include prerelease tool versions in search</param>
    /// <param name="toolName">Tool command name to run (e.g., 'dotnet-ef', 'dotnet-format')</param>
    /// <param name="args">Arguments to pass to the tool when running</param>
    /// <param name="output">Output directory for manifest creation (defaults to current directory)</param>
    /// <param name="force">Force manifest creation even if one already exists</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [Description("Manage .NET tools (global, local, and tool manifests). Supports install, list, update, uninstall, restore, search, run, and manifest creation.")]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Install","List","Update","Uninstall","Restore","CreateManifest","Search","Run"]""")]
    public async Task<string> DotnetTool(
        DotnetToolAction action,
        string? packageId = null,
        bool? global = null,
        string? version = null,
        string? framework = null,
        string? toolPath = null,
        string? searchTerm = null,
        bool? detail = null,
        int? take = null,
        int? skip = null,
        bool? prerelease = null,
        string? toolName = null,
        string? args = null,
        string? output = null,
        bool? force = null,
        string? workingDirectory = null,
        bool machineReadable = false)
    {
        return await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action parameter
            if (!ParameterValidator.ValidateAction<DotnetToolAction>(action, out var errorMessage))
            {
                if (machineReadable)
                {
                    var validActions = Enum.GetNames(typeof(DotnetToolAction));
                    var error = ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        validActions,
                        toolName: "dotnet_tool");
                    return ErrorResultFactory.ToJson(error);
                }
                return $"Error: {errorMessage}";
            }

            // Route to appropriate handler based on action
            return action switch
            {
                DotnetToolAction.Install => await HandleInstallAction(packageId, global ?? false, version, framework, toolPath, machineReadable),
                DotnetToolAction.List => await HandleListAction(global ?? false, machineReadable),
                DotnetToolAction.Update => await HandleUpdateAction(packageId, global ?? false, version, machineReadable),
                DotnetToolAction.Uninstall => await HandleUninstallAction(packageId, global ?? false, machineReadable),
                DotnetToolAction.Restore => await HandleRestoreAction(machineReadable),
                DotnetToolAction.CreateManifest => await HandleCreateManifestAction(output, force ?? false, machineReadable),
                DotnetToolAction.Search => await HandleSearchAction(searchTerm, detail ?? false, take, skip, prerelease ?? false, machineReadable),
                DotnetToolAction.Run => await HandleRunAction(toolName, args, machineReadable),
                _ => machineReadable
                    ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateValidationError(
                        $"Action '{action}' is not supported.",
                        parameterName: "action",
                        reason: "not supported"))
                    : $"Error: Action '{action}' is not supported."
            };
        });
    }

    private async Task<string> HandleInstallAction(string? packageId, bool global, string? version, string? framework, string? toolPath, bool machineReadable)
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

        var command = new StringBuilder($"tool install \"{packageId}\"");
        if (global) command.Append(" --global");
        if (!string.IsNullOrEmpty(version)) command.Append($" --version {version}");
        if (!string.IsNullOrEmpty(framework)) command.Append($" --framework {framework}");
        if (!string.IsNullOrEmpty(toolPath)) command.Append($" --tool-path \"{toolPath}\"");

        return await ExecuteDotNetCommand(command.ToString(), machineReadable);
    }

    private async Task<string> HandleListAction(bool global, bool machineReadable)
    {
        var command = "tool list";
        if (global) command += " --global";
        return await ExecuteDotNetCommand(command, machineReadable);
    }

    private async Task<string> HandleUpdateAction(string? packageId, bool global, string? version, bool machineReadable)
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

        var command = new StringBuilder($"tool update \"{packageId}\"");
        if (global) command.Append(" --global");
        if (!string.IsNullOrEmpty(version)) command.Append($" --version {version}");

        return await ExecuteDotNetCommand(command.ToString(), machineReadable);
    }

    private async Task<string> HandleUninstallAction(string? packageId, bool global, bool machineReadable)
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

        var command = new StringBuilder($"tool uninstall \"{packageId}\"");
        if (global) command.Append(" --global");

        return await ExecuteDotNetCommand(command.ToString(), machineReadable);
    }

    private async Task<string> HandleRestoreAction(bool machineReadable)
    {
        return await ExecuteDotNetCommand("tool restore", machineReadable);
    }

    private async Task<string> HandleCreateManifestAction(string? output, bool force, bool machineReadable)
    {
        var command = new StringBuilder("new tool-manifest");
        if (!string.IsNullOrEmpty(output)) command.Append($" -o \"{output}\"");
        if (force) command.Append(" --force");

        return await ExecuteDotNetCommand(command.ToString(), machineReadable);
    }

    private async Task<string> HandleSearchAction(string? searchTerm, bool detail, int? take, int? skip, bool prerelease, bool machineReadable)
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

        var command = new StringBuilder($"tool search \"{searchTerm}\"");
        if (detail) command.Append(" --detail");
        if (take.HasValue) command.Append($" --take {take.Value}");
        if (skip.HasValue) command.Append($" --skip {skip.Value}");
        if (prerelease) command.Append(" --prerelease");

        return await ExecuteDotNetCommand(command.ToString(), machineReadable);
    }

    private async Task<string> HandleRunAction(string? toolName, string? args, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(toolName, "toolName", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "toolName",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        // Validate args if provided
        if (!string.IsNullOrEmpty(args) && !IsValidAdditionalOptions(args))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "args contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.",
                    parameterName: "args",
                    reason: "invalid characters");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: args contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";
        }

        var command = new StringBuilder($"tool run \"{toolName}\"");
        if (!string.IsNullOrEmpty(args)) command.Append($" -- {args}");

        return await ExecuteDotNetCommand(command.ToString(), machineReadable);
    }

    // ===== Tool helper methods (moved from DotNetCliTools.Tool.cs) =====
    /// <summary>
    /// Install a .NET tool globally or locally.
    /// Global tools are available system-wide. Local tools require a manifest file (.config/dotnet-tools.json).
    /// </summary>
    /// <param name="packageName">NuGet package name of the tool (e.g., 'dotnet-ef', 'dotnet-format')</param>
    /// <param name="global">Install as global tool (system-wide); otherwise installs as local tool</param>
    /// <param name="version">Specific version to install</param>
    /// <param name="framework">Target framework to install for</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "tool")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["tool","install","global","local","cli"]""")]
    private async Task<string> DotnetToolInstall(
        string packageName,
        bool global = false,
        string? version = null,
        string? framework = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "packageName parameter is required.",
                    parameterName: "packageName",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: packageName parameter is required.";
        }

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
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    private async Task<string> DotnetToolList(
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
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    private async Task<string> DotnetToolUpdate(
        string packageName,
        bool global = false,
        string? version = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "packageName parameter is required.",
                    parameterName: "packageName",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: packageName parameter is required.";
        }

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
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    private async Task<string> DotnetToolUninstall(
        string packageName,
        bool global = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "packageName parameter is required.",
                    parameterName: "packageName",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: packageName parameter is required.";
        }

        var args = new StringBuilder($"tool uninstall \"{packageName}\"");
        if (global) args.Append(" --global");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Restore tools from the tool manifest (.config/dotnet-tools.json).
    /// Installs all tools listed in the manifest; essential for project setup after cloning.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    private async Task<string> DotnetToolRestore(bool machineReadable = false)
        => await ExecuteDotNetCommand("tool restore", machineReadable);

    /// <summary>
    /// Create a .NET tool manifest file (.config/dotnet-tools.json).
    /// Required before installing local tools. Creates the manifest in the current directory or specified output location.
    /// </summary>
    /// <param name="output">Output directory for the manifest (defaults to current directory)</param>
    /// <param name="force">Force creation even if manifest already exists</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    private async Task<string> DotnetToolManifestCreate(
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
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    private async Task<string> DotnetToolSearch(
        string searchTerm,
        bool detail = false,
        int? take = null,
        int? skip = null,
        bool prerelease = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "searchTerm parameter is required.",
                    parameterName: "searchTerm",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: searchTerm parameter is required.";
        }

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
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    private async Task<string> DotnetToolRun(
        string toolName,
        string? args = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "toolName parameter is required.",
                    parameterName: "toolName",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: toolName parameter is required.";
        }

        if (!string.IsNullOrEmpty(args) && !IsValidAdditionalOptions(args))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "args contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.",
                    parameterName: "args",
                    reason: "invalid characters");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: args contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";
        }

        var commandArgs = new StringBuilder($"tool run \"{toolName}\"");
        if (!string.IsNullOrEmpty(args)) commandArgs.Append($" -- {args}");
        return await ExecuteDotNetCommand(commandArgs.ToString(), machineReadable);
    }
}
