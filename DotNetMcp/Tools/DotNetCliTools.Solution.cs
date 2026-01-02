using System.Text;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Solution management tools for organizing multiple projects.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Create a new .NET solution file. A solution file organizes multiple related projects.
    /// </summary>
    /// <param name="name">The name for the solution file</param>
    /// <param name="output">The output directory for the solution file</param>
    /// <param name="format">The solution file format: 'sln' (classic) or 'slnx' (XML-based). Default is 'sln'.</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["solution","create","new","organization","multi-project"]""")]
    public async partial Task<string> DotnetSolutionCreate(
        string name,
        string? output = null,
        string? format = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("new sln");
        args.Append($" -n \"{name}\"");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(format))
        {
            if (format != "sln" && format != "slnx")
            {
                if (machineReadable)
                {
                    var error = ErrorResultFactory.CreateValidationError(
                        "format must be either 'sln' or 'slnx'.",
                        parameterName: "format",
                        reason: "invalid value");
                    return ErrorResultFactory.ToJson(error);
                }
                return "Error: format must be either 'sln' or 'slnx'.";
            }
            args.Append($" --format {format}");
        }
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Add one or more projects to a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to add projects to</param>
    /// <param name="projects">Array of project file paths to add to the solution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetSolutionAdd(
        string solution,
        string[] projects,
        bool machineReadable = false)
    {
        if (projects == null || projects.Length == 0)
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "at least one project path is required.",
                    parameterName: "projects",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: at least one project path is required.";
        }

        var args = new StringBuilder($"solution \"{solution}\" add");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List all projects in a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to list projects from</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSolutionList(
        string solution,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"solution \"{solution}\" list", machineReadable);

    /// <summary>
    /// Remove one or more projects from a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to remove projects from</param>
    /// <param name="projects">Array of project file paths to remove from the solution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetSolutionRemove(
        string solution,
        string[] projects,
        bool machineReadable = false)
    {
        if (projects == null || projects.Length == 0)
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "at least one project path is required.",
                    parameterName: "projects",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: at least one project path is required.";
        }

        var args = new StringBuilder($"solution \"{solution}\" remove");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }
}
