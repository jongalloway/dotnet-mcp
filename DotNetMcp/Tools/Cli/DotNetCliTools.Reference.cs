using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Reference management tools for project-to-project references.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Add a project-to-project reference.
    /// </summary>
    /// <param name="project">The project file to add the reference from</param>
    /// <param name="reference">The project file to reference</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "reference")]
    [McpMeta("priority", 7.0)]
    public async Task<string> DotnetReferenceAdd(
        string project,
        string reference,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"add \"{project}\" reference \"{reference}\"", machineReadable);

    /// <summary>
    /// List project references.
    /// </summary>
    /// <param name="project">The project file</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "reference")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetReferenceList(
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
    /// <param name="project">The project file to remove the reference from</param>
    /// <param name="reference">The project file to unreference</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "reference")]
    [McpMeta("priority", 5.0)]
    public async Task<string> DotnetReferenceRemove(
        string project,
        string reference,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"remove \"{project}\" reference \"{reference}\"", machineReadable);
}
