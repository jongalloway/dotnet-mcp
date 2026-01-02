using System.Text;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// .NET tool management for installing, updating, and running CLI tools.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Install a .NET tool globally or locally.
    /// Global tools are available system-wide. Local tools require a manifest file (.config/dotnet-tools.json).
    /// </summary>
    /// <param name="packageName">NuGet package name of the tool (e.g., 'dotnet-ef', 'dotnet-format')</param>
    /// <param name="global">Install as global tool (system-wide); otherwise installs as local tool</param>
    /// <param name="version">Specific version to install</param>
    /// <param name="framework">Target framework to install for</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["tool","install","global","local","cli"]""")]
    public async partial Task<string> DotnetToolInstall(
        string packageName,
        bool global = false,
        string? version = null,
        string? framework = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

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
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolList(
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
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolUpdate(
        string packageName,
        bool global = false,
        string? version = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

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
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolUninstall(
        string packageName,
        bool global = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool uninstall \"{packageName}\"");
        if (global) args.Append(" --global");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Restore tools from the tool manifest (.config/dotnet-tools.json).
    /// Installs all tools listed in the manifest; essential for project setup after cloning.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolRestore(bool machineReadable = false)
        => await ExecuteDotNetCommand("tool restore", machineReadable);

    /// <summary>
    /// Create a .NET tool manifest file (.config/dotnet-tools.json).
    /// Required before installing local tools. Creates the manifest in the current directory or specified output location.
    /// </summary>
    /// <param name="output">Output directory for the manifest (defaults to current directory)</param>
    /// <param name="force">Force creation even if manifest already exists</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolManifestCreate(
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
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolSearch(
        string searchTerm,
        bool detail = false,
        int? take = null,
        int? skip = null,
        bool prerelease = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return "Error: searchTerm parameter is required.";

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
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolRun(
        string toolName,
        string? args = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return "Error: toolName parameter is required.";

        if (!string.IsNullOrEmpty(args) && !IsValidAdditionalOptions(args))
            return "Error: args contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";

        var commandArgs = new StringBuilder($"tool run \"{toolName}\"");
        if (!string.IsNullOrEmpty(args)) commandArgs.Append($" -- {args}");
        return await ExecuteDotNetCommand(commandArgs.ToString(), machineReadable);
    }
}
