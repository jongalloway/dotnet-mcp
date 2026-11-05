using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging;
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
    /// <param name="format">The solution file format: 'sln', 'slnx', or 'slnf'. Default is 'sln'.</param>
    /// <param name="parentSolution">Parent .sln or .slnx file, required only for 'slnf' (SDK 11+)</param>
    internal async Task<string> DotnetSolutionCreate(
        string name,
        string? output = null,
        string? format = null,
        string? parentSolution = null)
    {
        var effectiveFormat = format ?? "sln";
        if (effectiveFormat is not ("sln" or "slnx" or "slnf"))
            return "Error: format must be 'sln', 'slnx', or 'slnf'.";

        if (effectiveFormat == "slnf")
        {
            if (!ParameterValidator.ValidateRequiredParameter(parentSolution, "parentSolution", out var parentError))
                return $"Error: {parentError}";
            var parentExtension = Path.GetExtension(parentSolution);
            if (!string.Equals(parentExtension, ".sln", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(parentExtension, ".slnx", StringComparison.OrdinalIgnoreCase))
                return "Error: parentSolution must be a .sln or .slnx file.";
            if (parentSolution!.Any(char.IsControl))
                return "Error: parentSolution must not contain control characters.";
            try
            {
                var baseDirectory = DotNetCommandExecutor.WorkingDirectoryOverride.Value ?? Directory.GetCurrentDirectory();
                var filterDirectory = string.IsNullOrEmpty(output) ? baseDirectory : Path.GetFullPath(output, baseDirectory);
                if (!File.Exists(Path.GetFullPath(parentSolution!, filterDirectory)))
                    return "Error: parentSolution does not exist. Relative parent paths are resolved from the filter's output directory.";
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
            {
                return $"Error: Invalid parentSolution or output path: {ex.Message}";
            }
            if (await SdkFeatureSupport.RequireSdk11Async("Solution filters", _logger) is { } sdkError)
                return sdkError;
        }
        else if (parentSolution is not null)
        {
            return "Error: parentSolution is only used with format 'slnf'.";
        }

        var args = new StringBuilder(effectiveFormat == "slnf" ? "new slnf" : "new sln");
        args.Append($" -n {CommandLineArgument.Quote(name)}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o {CommandLineArgument.Quote(output)}");
        if (effectiveFormat == "slnf")
            args.Append($" --parent-solution {CommandLineArgument.Quote(parentSolution!)}");
        else
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

        if (string.Equals(Path.GetExtension(solution), ".slnf", StringComparison.OrdinalIgnoreCase))
            return await RemoveFilterProjectsAsync(solution, projects);

        var args = new StringBuilder($"solution \"{solution}\" remove");
        foreach (var project in projects)
        {
            args.Append($" \"{project}\"");
        }
        return await ExecuteDotNetCommand(args.ToString());
    }

    private async Task<string> RemoveFilterProjectsAsync(string solution, string[] projects)
    {
        // SDK 11 RC3 compares filter-relative removal paths with parent-relative entries.
        // Edit the filter itself rather than pass fictitious project paths to the CLI.
        string? temporaryPath = null;
        try
        {
            var cancellationToken = DotNetCommandExecutor.RequestCancellationToken.Value;
            var workingDirectory = DotNetCommandExecutor.WorkingDirectoryOverride.Value ?? Directory.GetCurrentDirectory();
            var filterPath = Path.GetFullPath(solution, workingDirectory);
            await using var stream = new FileStream(filterPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
                bufferSize: 4096, useAsync: true);
            using var reader = new StreamReader(stream, leaveOpen: true);
            var originalContent = await reader.ReadToEndAsync(cancellationToken);
            var root = JsonNode.Parse(originalContent);
            if (root?["solution"] is not JsonObject filter
                || filter["path"] is not JsonValue parentValue || !parentValue.TryGetValue<string>(out var parent)
                || string.IsNullOrWhiteSpace(parent) || filter["projects"] is not JsonArray entries)
                return "Error: Invalid solution filter. Expected solution.path and solution.projects.";

            var parentPath = Path.GetFullPath(parent.Replace('\\', Path.DirectorySeparatorChar), Path.GetDirectoryName(filterPath)!);
            if (!string.Equals(Path.GetExtension(parentPath), ".sln", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(Path.GetExtension(parentPath), ".slnx", StringComparison.OrdinalIgnoreCase))
                return "Error: The solution filter's parent must be a .sln or .slnx file.";
            if (!File.Exists(parentPath))
                return "Error: The solution filter's parent solution does not exist.";
            var parentDirectory = Path.GetDirectoryName(parentPath)!;
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var requestedPaths = new HashSet<string>(comparer);
            foreach (var project in projects)
            {
                var fullPath = Path.GetFullPath(project, workingDirectory);
                if (Directory.Exists(fullPath))
                {
                    var candidates = Directory.GetFiles(fullPath).Where(path =>
                        Path.GetExtension(path) is ".csproj" or ".fsproj" or ".vbproj").ToArray();
                    if (candidates.Length != 1)
                        return $"Error: Expected exactly one project in '{project}'.";
                    fullPath = candidates[0];
                }
                requestedPaths.Add(fullPath);
            }

            var indices = new List<int>();
            var missingPaths = new HashSet<string>(requestedPaths, comparer);
            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index] is not JsonValue entry || !entry.TryGetValue<string>(out var path)
                    || string.IsNullOrWhiteSpace(path))
                    return "Error: Invalid solution filter project path.";
                var fullPath = Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar), parentDirectory);
                if (requestedPaths.Contains(fullPath))
                {
                    missingPaths.Remove(fullPath);
                    indices.Add(index);
                }
            }
            if (missingPaths.Count > 0)
                return "Error: One or more requested projects are not present in the solution filter. No changes were made.";

            foreach (var index in indices.AsEnumerable().Reverse())
                entries.RemoveAt(index);
            var content = root!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            temporaryPath = filterPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, content, cancellationToken);
            if (await File.ReadAllTextAsync(filterPath, cancellationToken) != originalContent)
                return "Error: The solution filter changed during removal. No changes were made; retry with the current file.";
            cancellationToken.ThrowIfCancellationRequested();
            File.Replace(temporaryPath, filterPath, destinationBackupFileName: null);
            return $"Removed {indices.Count} project(s) from solution filter '{solution}'.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Failed to remove projects from solution filter {Solution}", solution);
            return $"Error: Could not update solution filter: {ex.Message}";
        }
        finally
        {
            if (temporaryPath is not null)
                File.Delete(temporaryPath);
        }
    }

    /// <summary>
    /// Manage solution files and project membership. A solution file organizes multiple related projects.
    /// This is a consolidated tool that routes to specific solution operations based on the action parameter.
    /// </summary>
    /// <param name="action">The solution operation to perform: Create, Add, List, or Remove</param>
    /// <param name="solution">Path to solution file. Required for 'add', 'list', and 'remove' actions. Not used for 'create' action (which uses the name parameter).</param>
    /// <param name="name">Solution name (required for 'create' action)</param>
    /// <param name="output">Output directory for solution file (optional, used with 'create' action)</param>
    /// <param name="format">Solution file format: 'sln', 'slnx', or 'slnf' (SDK 11+). Default is 'sln'. Used with Create.</param>
    /// <param name="parentSolution">Explicit parent .sln or .slnx path, required for Create with format 'slnf'</param>
    /// <param name="projects">Array of project file paths (required for 'add' and 'remove' actions)</param>
    /// <param name="context">Injected MCP request context for destructive-operation confirmation</param>
    [McpServerTool(Title = ".NET Solution Manager", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true, UseStructuredContent = true, OutputSchemaType = typeof(SolutionListResult), IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Card%20File%20Box/Flat/card_file_box_flat.svg")]
    [McpMeta("category", "solution")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Create","Add","List","Remove","Analyze","Dependencies","Validate"]""")]
    [McpMeta("tags", JsonValue = """["solution","consolidated","create","add","list","remove","organization","multi-project"]""")]
    public async partial Task<CallToolResult> DotnetSolution(
        DotnetSolutionAction action,
        string? solution = null,
        string? name = null,
        string? output = null,
        string? format = null,
        string[]? projects = null,
        string? parentSolution = null,
        McpServer? server = null,
        RequestContext<CallToolRequestParams>? context = null)
    {
        // Validate action parameter
        if (!ParameterValidator.ValidateAction<DotnetSolutionAction>(action, out var actionError))
        {
            return StructuredContentHelper.ApplicationError($"Error: {actionError}");
        }

        if (parentSolution is not null && action != DotnetSolutionAction.Create)
            return StructuredContentHelper.ApplicationError("Error: parentSolution is only used with Create.");

        if (string.Equals(Path.GetExtension(solution), ".slnf", StringComparison.OrdinalIgnoreCase))
        {
            if (action is DotnetSolutionAction.Analyze or DotnetSolutionAction.Dependencies or DotnetSolutionAction.Validate)
                return StructuredContentHelper.ApplicationError("Error: Solution filter analysis is not supported. Use the parent .sln or .slnx file.");
            if (await SdkFeatureSupport.RequireSdk11Async("Solution filters", _logger) is { } sdkError)
                return StructuredContentHelper.ApplicationError(sdkError);
        }

        // Auto-detect solution from workspace roots when not explicitly specified.
        // Only applicable for actions that require a solution path (Add, List, Remove);
        // Create uses the name parameter instead.
        var effectiveSolution = solution;
        if (string.IsNullOrEmpty(effectiveSolution) && action is
            DotnetSolutionAction.Add or DotnetSolutionAction.List or DotnetSolutionAction.Remove
            or DotnetSolutionAction.Analyze or DotnetSolutionAction.Dependencies or DotnetSolutionAction.Validate)
        {
            effectiveSolution = await WorkspaceDiscovery.TryFindSolutionInRootsAsync(server, DotNetCommandExecutor.RequestCancellationToken.Value);
        }

        // Route to appropriate method based on action
        var textResult = action switch
        {
            DotnetSolutionAction.Create => await HandleCreateAction(name, output, format, parentSolution),
            DotnetSolutionAction.Add => await HandleAddAction(effectiveSolution, projects),
            DotnetSolutionAction.List => await HandleListAction(effectiveSolution),
            DotnetSolutionAction.Remove => await HandleRemoveAction(effectiveSolution, projects, server, context),
            DotnetSolutionAction.Analyze => await HandleSolutionAnalyzeAction(effectiveSolution),
            DotnetSolutionAction.Dependencies => await HandleSolutionDependenciesAction(effectiveSolution),
            DotnetSolutionAction.Validate => await HandleSolutionValidateAction(effectiveSolution),
            _ => throw new InvalidOperationException($"Unsupported action '{action}'. This should have been caught by validation.")
        };

        // Add structured content for List action
        object? structured = action == DotnetSolutionAction.List && !StructuredContentHelper.IsFailure(textResult)
            ? BuildSolutionListStructuredContent(textResult)
            : null;

        return StructuredContentHelper.ActionResult(action, textResult, structured);
    }

    private async Task<string> HandleCreateAction(string? name, string? output, string? format, string? parentSolution)
    {
        // Validate required parameter for create action
        if (!ParameterValidator.ValidateRequiredParameter(name, "name", out var nameError))
        {
            return $"Error: {nameError}";
        }

        return await DotnetSolutionCreate(name!, output, format, parentSolution);
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

    private async Task<string> HandleRemoveAction(string? solution, string[]? projects, McpServer? server = null, RequestContext<CallToolRequestParams>? context = null)
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

        var projectList = string.Join(", ", projects.Select(project => $"'{project}'"));
        if (!await DestructiveOperationConfirmation.ConfirmAsync(
            server, context,
            $"This will remove {projectList} from solution '{solution}'. Do you want to proceed?",
            "Confirm remove",
            "I understand this will remove the project(s) from the solution",
            DotNetCommandExecutor.RequestCancellationToken.Value))
        {
            return "Remove operation cancelled.";
        }

        return await DotnetSolutionRemove(solution!, projects);
    }

    private async Task<string> HandleSolutionAnalyzeAction(string? solution)
    {
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            return $"Error: {solutionError}";
        }

        return await SolutionAnalysisHelper.AnalyzeSolutionAsync(solution!, args => ExecuteDotNetCommand(args), _logger);
    }

    private async Task<string> HandleSolutionDependenciesAction(string? solution)
    {
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            return $"Error: {solutionError}";
        }

        return await SolutionAnalysisHelper.GetSolutionDependenciesAsync(solution!, args => ExecuteDotNetCommand(args), _logger);
    }

    private async Task<string> HandleSolutionValidateAction(string? solution)
    {
        if (!ParameterValidator.ValidateRequiredParameter(solution, "solution", out var solutionError))
        {
            return $"Error: {solutionError}";
        }

        return await SolutionAnalysisHelper.ValidateSolutionAsync(solution!, args => ExecuteDotNetCommand(args), _logger);
    }

    private static SolutionListResult? BuildSolutionListStructuredContent(string textResult)
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
        return new SolutionListResult { Projects = projects };
    }
}
