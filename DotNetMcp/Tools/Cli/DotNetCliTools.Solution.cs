using System.Text;
using DotNetMcp.Actions;
using ModelContextProtocol.Protocol;
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
    internal async Task<string> DotnetSolutionCreate(
        string name,
        string? output = null,
        string? format = null)
    {
        var args = new StringBuilder("new sln");
        args.Append($" -n \"{name}\"");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        
        // Determine the format to use
        var effectiveFormat = format ?? "sln"; // Default to 'sln' for backward compatibility
        
        if (effectiveFormat != "sln" && effectiveFormat != "slnx")
        {
            return "Error: format must be either 'sln' or 'slnx'.";
        }
        
        args.Append($" --format {effectiveFormat}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Add one or more projects to a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to add projects to</param>
    /// <param name="projects">Array of project file paths to add to the solution</param>
    internal async Task<string> DotnetSolutionAdd(
        string solution,
        string[] projects)
    {
        if (projects == null || projects.Length == 0)
        {
            return "Error: at least one project path is required.";
        }

        var args = new StringBuilder($"solution \"{solution}\" add");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// List all projects in a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to list projects from</param>
    internal async Task<string> DotnetSolutionList(
        string solution)
        => await ExecuteDotNetCommand($"solution \"{solution}\" list");

    /// <summary>
    /// Remove one or more projects from a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to remove projects from</param>
    /// <param name="projects">Array of project file paths to remove from the solution</param>
    internal async Task<string> DotnetSolutionRemove(
        string solution,
        string[] projects)
    {
        if (projects == null || projects.Length == 0)
        {
            return "Error: at least one project path is required.";
        }

        var args = new StringBuilder($"solution \"{solution}\" remove");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Manage solution files and project membership. A solution file organizes multiple related projects.
    /// This is a consolidated tool that routes to specific solution operations based on the action parameter.
    /// </summary>
    /// <param name="action">The solution operation to perform: Create, Add, List, or Remove</param>
    /// <param name="solution">Path to solution file. Required for 'add', 'list', and 'remove' actions. Not used for 'create' action (which uses the name parameter).</param>
    /// <param name="name">Solution name (required for 'create' action)</param>
    /// <param name="output">Output directory for solution file (optional, used with 'create' action)</param>
    /// <param name="format">Solution file format: 'sln' (classic) or 'slnx' (XML-based). Default is 'sln'. (optional, used with 'create' action)</param>
    /// <param name="projects">Array of project file paths (required for 'add' and 'remove' actions)</param>
    [McpServerTool(Title = ".NET Solution Manager", Destructive = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Card%20File%20Box/Flat/card_file_box_flat.svg")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Create","Add","List","Remove"]""")]
    [McpMeta("tags", JsonValue = """["solution","consolidated","create","add","list","remove","organization","multi-project"]""")]
    public async partial Task<CallToolResult> DotnetSolution(
        DotnetSolutionAction action,
        string? solution = null,
        string? name = null,
        string? output = null,
        string? format = null,
        string[]? projects = null)
    {
        // Validate action parameter
        if (!ParameterValidator.ValidateAction<DotnetSolutionAction>(action, out var actionError))
        {
            return StructuredContentHelper.ToCallToolResult($"Error: {actionError}");
        }

        // Route to appropriate method based on action
        var textResult = action switch
        {
            DotnetSolutionAction.Create => await HandleCreateAction(name, output, format),
            DotnetSolutionAction.Add => await HandleAddAction(solution, projects),
            DotnetSolutionAction.List => await HandleListAction(solution),
            DotnetSolutionAction.Remove => await HandleRemoveAction(solution, projects),
            _ => throw new InvalidOperationException($"Unsupported action '{action}'. This should have been caught by validation.")
        };

        // Add structured content for List action
        object? structured = action == DotnetSolutionAction.List
            ? BuildSolutionListStructuredContent(textResult)
            : null;

        return StructuredContentHelper.ToCallToolResult(textResult, structured);
    }

    private async Task<string> HandleCreateAction(string? name, string? output, string? format)
    {
        // Validate required parameter for create action
        if (!ParameterValidator.ValidateRequiredParameter(name, "name", out var nameError))
        {
            return $"Error: {nameError}";
        }

        return await DotnetSolutionCreate(name!, output, format);
    }

    private async Task<string> HandleAddAction(string? solution, string[]? projects)
    {
        // Validate required parameters for add action
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            return $"Error: {solutionError}";
        }

        if (projects == null || projects.Length == 0)
        {
            return "Error: at least one project path is required for the 'add' action.";
        }

        return await DotnetSolutionAdd(solution!, projects);
    }

    private async Task<string> HandleListAction(string? solution)
    {
        // Validate required parameter for list action
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            return $"Error: {solutionError}";
        }

        return await DotnetSolutionList(solution!);
    }

    private async Task<string> HandleRemoveAction(string? solution, string[]? projects)
    {
        // Validate required parameters for remove action
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            return $"Error: {solutionError}";
        }

        if (projects == null || projects.Length == 0)
        {
            return "Error: at least one project path is required for the 'remove' action.";
        }

        return await DotnetSolutionRemove(solution!, projects);
    }

    private static object? BuildSolutionListStructuredContent(string textResult)
    {
        // Parse project paths from dotnet solution list output
        // Output format: lines with .csproj/.fsproj/.vbproj paths
        var lines = textResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var projects = lines
            .Where(l => !l.StartsWith("Exit Code:", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("Error", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("Project(s)", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("---", StringComparison.OrdinalIgnoreCase)
                && (l.Trim().EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                    || l.Trim().EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)
                    || l.Trim().EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)))
            .Select(l => l.Trim())
            .ToArray();
        return new { projects };
    }
}
