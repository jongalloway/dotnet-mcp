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
}
