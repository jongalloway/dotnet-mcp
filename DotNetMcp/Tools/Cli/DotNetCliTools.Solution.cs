using System.Text;
using DotNetMcp.Actions;
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
    internal async Task<string> DotnetSolutionCreate(
        string name,
        string? output = null,
        string? format = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("new sln");
        args.Append($" -n \"{name}\"");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        
        // Determine the format to use
        var effectiveFormat = format ?? "sln"; // Default to 'sln' for backward compatibility
        
        if (effectiveFormat != "sln" && effectiveFormat != "slnx")
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
        
        args.Append($" --format {effectiveFormat}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Add one or more projects to a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to add projects to</param>
    /// <param name="projects">Array of project file paths to add to the solution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    internal async Task<string> DotnetSolutionAdd(
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
    internal async Task<string> DotnetSolutionList(
        string solution,
        bool machineReadable = false)
        => await ExecuteDotNetCommand($"solution \"{solution}\" list", machineReadable);

    /// <summary>
    /// Remove one or more projects from a .NET solution file.
    /// </summary>
    /// <param name="solution">The solution file to remove projects from</param>
    /// <param name="projects">Array of project file paths to remove from the solution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    internal async Task<string> DotnetSolutionRemove(
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Create","Add","List","Remove"]""")]
    [McpMeta("tags", JsonValue = """["solution","consolidated","create","add","list","remove","organization","multi-project"]""")]
    public async partial Task<string> DotnetSolution(
        DotnetSolutionAction action,
        string? solution = null,
        string? name = null,
        string? output = null,
        string? format = null,
        string[]? projects = null,
        bool machineReadable = false)
    {
        // Validate action parameter
        if (!ParameterValidator.ValidateAction<DotnetSolutionAction>(action, out var actionError))
        {
            if (machineReadable)
            {
                var validActions = Enum.GetNames(typeof(DotnetSolutionAction));
                var error = ErrorResultFactory.CreateActionValidationError(
                    action.ToString(),
                    validActions,
                    toolName: "dotnet_solution");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {actionError}";
        }

        // Route to appropriate method based on action
        return action switch
        {
            DotnetSolutionAction.Create => await HandleCreateAction(name, output, format, machineReadable),
            DotnetSolutionAction.Add => await HandleAddAction(solution, projects, machineReadable),
            DotnetSolutionAction.List => await HandleListAction(solution, machineReadable),
            DotnetSolutionAction.Remove => await HandleRemoveAction(solution, projects, machineReadable),
            _ => throw new InvalidOperationException($"Unsupported action '{action}'. This should have been caught by validation.")
        };
    }

    private async Task<string> HandleCreateAction(string? name, string? output, string? format, bool machineReadable)
    {
        // Validate required parameter for create action
        if (!ParameterValidator.ValidateRequiredParameter(name, "name", out var nameError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateRequiredParameterError("name", "dotnet_solution (action=Create)");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {nameError}";
        }

        return await DotnetSolutionCreate(name!, output, format, machineReadable);
    }

    private async Task<string> HandleAddAction(string? solution, string[]? projects, bool machineReadable)
    {
        // Validate required parameters for add action
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateRequiredParameterError("solution", "dotnet_solution (action=Add)");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {solutionError}";
        }

        if (projects == null || projects.Length == 0)
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "at least one project path is required for the 'add' action.",
                    parameterName: "projects",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: at least one project path is required for the 'add' action.";
        }

        return await DotnetSolutionAdd(solution!, projects, machineReadable);
    }

    private async Task<string> HandleListAction(string? solution, bool machineReadable)
    {
        // Validate required parameter for list action
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateRequiredParameterError("solution", "dotnet_solution (action=List)");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {solutionError}";
        }

        return await DotnetSolutionList(solution!, machineReadable);
    }

    private async Task<string> HandleRemoveAction(string? solution, string[]? projects, bool machineReadable)
    {
        // Validate required parameters for remove action
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateRequiredParameterError("solution", "dotnet_solution (action=Remove)");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {solutionError}";
        }

        if (projects == null || projects.Length == 0)
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "at least one project path is required for the 'remove' action.",
                    parameterName: "projects",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: at least one project path is required for the 'remove' action.";
        }

        return await DotnetSolutionRemove(solution!, projects, machineReadable);
    }
}
