using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Core infrastructure for DotNetCliTools including class declaration, 
/// dependency injection, and private helper methods.
/// </summary>
[McpServerToolType]
public sealed partial class DotNetCliTools
{
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;
    private readonly ProcessSessionManager _processSessionManager;
    private readonly ToolMetricsAccumulator? _metricsAccumulator;
    private readonly ResourceSubscriptionManager? _subscriptions;
    private readonly IMcpTaskStore? _taskStore;

    // Constants for server capability discovery
    private const string DefaultServerVersion = "1.0.0";
    private const string ProtocolVersion = "2025-11-25";

    // Constants for sampling (AI-assisted error interpretation)
    private const int MaxSamplingPromptLength = 4000;
    private const int MaxSamplingResponseTokens = 256;

    public DotNetCliTools(ILogger<DotNetCliTools> logger, ConcurrencyManager concurrencyManager, ProcessSessionManager processSessionManager, ToolMetricsAccumulator? metricsAccumulator = null, ResourceSubscriptionManager? subscriptions = null, IMcpTaskStore? taskStore = null)
    {
        // DI guarantees logger is never null
        _logger = logger!;
        _concurrencyManager = concurrencyManager!;
        _processSessionManager = processSessionManager!;
        _metricsAccumulator = metricsAccumulator;
        _subscriptions = subscriptions;
        _taskStore = taskStore;
    }

    private async Task<string> ExecuteDotNetCommand(string arguments, CancellationToken cancellationToken = default, string? workingDirectory = null)
        => await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _logger, unsafeOutput: false, cancellationToken: cancellationToken, workingDirectory: workingDirectory);

    /// <summary>
    /// Execute a command with concurrency control. Returns error if there's a conflict.
    /// </summary>
    private async Task<string> ExecuteWithConcurrencyCheck(
        string operationType,
        string target,
        string arguments,
        CancellationToken cancellationToken = default,
        string? workingDirectory = null)
    {
        // Try to acquire the operation
        if (!_concurrencyManager.TryAcquireOperation(operationType, target, out var conflictingOperation))
        {
            // Conflict detected - return error
            var errorResponse = ErrorResultFactory.CreateConcurrencyConflict(operationType, target, conflictingOperation!);
            return $"Error: {errorResponse.Errors[0].Message}\nHint: {errorResponse.Errors[0].Hint}";
        }

        try
        {
            // Execute the command
            return await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _logger, unsafeOutput: false, cancellationToken: cancellationToken, workingDirectory: workingDirectory);
        }
        finally
        {
            // Always release the operation lock
            _concurrencyManager.ReleaseOperation(operationType, target);
        }
    }

    /// <summary>
    /// Gets the operation target for concurrency control. Returns the project path if specified, 
    /// otherwise returns the working directory if provided, or the current directory as fallback.
    /// </summary>
    private static string GetOperationTarget(string? project, string? workingDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(project))
            return project;

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            try
            {
                // Normalize to absolute path for consistent concurrency target
                return Path.GetFullPath(workingDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // If normalization fails, fall back to current directory
                // This shouldn't happen as workingDirectory is validated before execution
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static bool IsValidAdditionalOptions(string options)
    {
        // Validation rationale (see PR #42 and follow-up refinement in PR #60):
        // We intentionally use a simple foreach + pattern match instead of LINQ (All) or a HashSet/FrozenSet.
        // Reasons:
        //1. Readability: The allowlist is tiny (5 chars); the loop is explicit and easy to audit for security.
        //2. Performance: Differences among foreach, LINQ, HashSet, or FrozenSet for short CLI option strings are negligible.
        // Avoiding LINQ prevents enumerator/delegate allocations; HashSet/FrozenSet adds unnecessary static initialization.
        //3. Security clarity: A positive allowlist (alphanumeric + specific safe punctuation) makes the policy obvious.
        //4. Modern C# pattern matching (c is '-' or '_' ...) is concise and self-documenting.
        // If additional safe characters are ever required, extend the pattern below and update the comment.
        // Rejected shell/metacharacters: &, |, ;, <, >, `, $, (, ), {, }, [, ], \, ", '
        foreach (var c in options)
        {
            if (!(char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or ' ' or '='))
                return false;
        }
        return true;
    }

    private static async Task<string> WithWorkingDirectoryAsync(string? workingDirectory, Func<Task<string>> action)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return await action();
        }

        var prior = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        DotNetCommandExecutor.WorkingDirectoryOverride.Value = workingDirectory;
        try
        {
            return await action();
        }
        finally
        {
            DotNetCommandExecutor.WorkingDirectoryOverride.Value = prior;
        }
    }

    /// <summary>
    /// Parse the output of 'dotnet --list-sdks' to extract SDK versions.
    /// Expected format: "9.0.306 [/usr/share/dotnet/sdk]"
    /// </summary>
    private static string[] ParseInstalledSdks(string sdksOutput)
    {
        if (string.IsNullOrWhiteSpace(sdksOutput))
            return Array.Empty<string>();

        var sdks = new List<string>();
        var lines = sdksOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Each line format: "version [path]"
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var version = parts[0].Trim();
                // Skip empty lines, error messages, and exit code lines - only keep lines starting with a digit (SDK versions)
                if (!string.IsNullOrEmpty(version) &&
                    !version.StartsWith("Exit", StringComparison.OrdinalIgnoreCase) &&
                    !version.StartsWith("Error", StringComparison.OrdinalIgnoreCase) &&
                    char.IsDigit(version[0]))
                {
                    sdks.Add(version);
                }
            }
        }

        return sdks.ToArray();
    }

    /// <summary>
    /// Sends an MCP log notification to the client if a server connection is available.
    /// Failures are silently swallowed so logging never breaks tool execution.
    /// </summary>
    private static async Task SendMcpLogAsync(McpServer? server, string message, LoggingLevel level = LoggingLevel.Info)
    {
        if (server is null) return;
        try
        {
            await server.SendNotificationAsync(
                NotificationMethods.LoggingMessageNotification,
                new LoggingMessageNotificationParams
                {
                    Level = level,
                    Logger = "dotnet-mcp",
                    Data = JsonSerializer.SerializeToElement(message)
                },
                McpJsonUtilities.DefaultOptions,
                CancellationToken.None);
        }
        catch (Exception)
        {
            // Don't let logging failures break tool execution
        }
    }

    /// <summary>
    /// Executes a long-running operation with progress notifications sent before and after execution.
    /// The completion notification is always sent, even if the operation throws.
    /// </summary>
    private static async Task<string> ExecuteWithProgress(
        IProgress<ProgressNotificationValue>? progress,
        string startMessage,
        string completeMessage,
        Func<Task<string>> execute)
    {
        ReportProgressSafe(progress, 0, startMessage);
        string result;
        try
        {
            result = await execute();
        }
        finally
        {
            ReportProgressSafe(progress, 1, completeMessage);
        }
        return result;
    }

    /// <summary>
    /// Sends a progress notification, swallowing any exceptions so that a broken or
    /// unsupported progress channel never causes a tool invocation (or MCP task) to fail.
    /// </summary>
    private static void ReportProgressSafe(IProgress<ProgressNotificationValue>? progress, int value, string message)
    {
        try
        {
            progress?.Report(new ProgressNotificationValue { Progress = value, Total = 1, Message = message });
        }
        catch (Exception)
        {
            // Progress is best-effort; never let it break tool execution.
        }
    }
}
