using System.Text;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Watch tools for file-watching and automatic rebuild/re-run scenarios.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Run a .NET project with file watching and hot reload. 
    /// Note: This is a long-running command that watches for file changes and automatically restarts the application. 
    /// It should be terminated by the user when no longer needed.
    /// </summary>
    /// <param name="project">The project file to run</param>
    /// <param name="appArgs">Arguments to pass to the application</param>
    /// <param name="noHotReload">Disable hot reload</param>
    [McpServerTool]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public partial Task<string> DotnetWatchRun(
        string? project = null,
        string? appArgs = null,
        bool noHotReload = false)
    {
        var args = new StringBuilder("watch");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        args.Append(" run");
        if (noHotReload) args.Append(" --no-hot-reload");
        if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");
        return Task.FromResult("Warning: 'dotnet watch run' is a long-running command that requires interactive terminal support. " +
      "It will watch for file changes and automatically restart the application. " +
        "This command is best run directly in a terminal. " +
             $"Command that would be executed: dotnet {args}");
    }

    /// <summary>
    /// Run unit tests with file watching and automatic test re-runs. 
    /// Note: This is a long-running command that watches for file changes. It should be terminated by the user when no longer needed.
    /// </summary>
    /// <param name="project">The project file or solution file to test</param>
    /// <param name="filter">Filter to run specific tests</param>
    [McpServerTool]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public partial Task<string> DotnetWatchTest(
        string? project = null,
        string? filter = null)
    {
        var args = new StringBuilder("watch");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        args.Append(" test");
        if (!string.IsNullOrEmpty(filter)) args.Append($" --filter \"{filter}\"");
        return Task.FromResult("Warning: 'dotnet watch test' is a long-running command that requires interactive terminal support. " +
               "It will watch for file changes and automatically re-run tests. " +
    "This command is best run directly in a terminal. " +
  $"Command that would be executed: dotnet {args}");
    }

    /// <summary>
    /// Build a .NET project with file watching and automatic rebuild. 
    /// Note: This is a long-running command that watches for file changes. It should be terminated by the user when no longer needed.
    /// </summary>
    /// <param name="project">The project file or solution file to build</param>
    /// <param name="configuration">The configuration to build (Debug or Release)</param>
    [McpServerTool]
    [McpMeta("category", "watch")]
    [McpMeta("isLongRunning", true)]
    [McpMeta("requiresInteractive", true)]
    public partial Task<string> DotnetWatchBuild(
        string? project = null,
        string? configuration = null)
    {
        var args = new StringBuilder("watch");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        args.Append(" build");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        return Task.FromResult("Warning: 'dotnet watch build' is a long-running command that requires interactive terminal support. " +
   "It will watch for file changes and automatically rebuild. " +
         "This command is best run directly in a terminal. " +
   $"Command that would be executed: dotnet {args}");
    }
}
