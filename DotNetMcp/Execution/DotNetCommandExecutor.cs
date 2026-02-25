using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

/// <summary>
/// Helper class for executing dotnet CLI commands.
/// Provides a centralized implementation to avoid code duplication between tools and resources.
/// </summary>
public static class DotNetCommandExecutor
{
    private const int MaxOutputCharacters = 1_000_000;
    private static readonly int NewLineLength = Environment.NewLine.Length;

    internal static readonly AsyncLocal<string?> WorkingDirectoryOverride = new();

    /// <summary>
    /// Execute a dotnet command with full output handling, logging, and truncation support.
    /// </summary>
    /// <param name="arguments">The command-line arguments to pass to dotnet.exe</param>
    /// <param name="logger">Optional logger for debug/warning messages</param>
    /// <param name="unsafeOutput">When true, disables security redaction of sensitive information (default: false). Use with caution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <param name="workingDirectory">Optional working directory for command execution</param>
    /// <returns>Combined output, error, and exit code information</returns>
    public static async Task<string> ExecuteCommandAsync(string arguments, ILogger? logger = null, bool unsafeOutput = false, CancellationToken cancellationToken = default, string? workingDirectory = null)
    {
        logger?.LogDebug("Executing: dotnet {Arguments}", arguments);

        workingDirectory ??= WorkingDirectoryOverride.Value;

        string? normalizedWorkingDirectory = null;
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            try
            {
                normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                var message = $"Invalid workingDirectory path: {workingDirectory}. {ex.Message}";
                return $"Error: {message}";
            }

            if (!Directory.Exists(normalizedWorkingDirectory))
            {
                var message = $"Directory not found: {normalizedWorkingDirectory}. The workingDirectory parameter must point to an existing directory.";
                return $"Error: {message}";
            }
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(normalizedWorkingDirectory))
        {
            psi.WorkingDirectory = normalizedWorkingDirectory;
        }

        using var process = new Process { StartInfo = psi };
        var output = new StringBuilder();
        var error = new StringBuilder();
        var outputTruncated = false;
        var errorTruncated = false;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                // Check if adding this line would exceed the limit
                int projectedLength = output.Length + e.Data.Length + NewLineLength;
                if (projectedLength < MaxOutputCharacters)
                {
                    output.AppendLine(e.Data);
                }
                else if (!outputTruncated)
                {
                    output.AppendLine("[Output truncated - exceeded maximum character limit]");
                    outputTruncated = true;
                }
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                // Check if adding this line would exceed the limit
                int projectedLength = error.Length + e.Data.Length + NewLineLength;
                if (projectedLength < MaxOutputCharacters)
                {
                    error.AppendLine(e.Data);
                }
                else if (!errorTruncated)
                {
                    error.AppendLine("[Error output truncated - exceeded maximum character limit]");
                    errorTruncated = true;
                }
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            logger?.LogError(ex, "Failed to start dotnet process");
            return $"dotnet command could not be started: {ex.Message}";
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Register cancellation callback to kill the process
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    logger?.LogWarning("Cancellation requested - terminating process");
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited - expected race condition
                logger?.LogDebug("Process already exited during cancellation");
            }
        });

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger?.LogWarning("Command was cancelled");

            // Apply security redaction to partial output unless unsafeOutput is enabled
            var partialOutput = output.ToString().TrimEnd();
            if (!unsafeOutput)
            {
                partialOutput = SecretRedactor.Redact(partialOutput);
            }

            return $"Operation cancelled\nPartial output:\n{partialOutput}\nExit Code: -1";
        }

        logger?.LogDebug("Command completed with exit code: {ExitCode}", process.ExitCode);
        if (outputTruncated)
        {
            logger?.LogWarning("Output was truncated due to size limit");
        }
        if (errorTruncated)
        {
            logger?.LogWarning("Error output was truncated due to size limit");
        }

        var outputStr = output.ToString().TrimEnd();
        var errorStr = error.ToString().TrimEnd();

        // Apply security redaction unless unsafeOutput is explicitly enabled
        if (!unsafeOutput)
        {
            outputStr = SecretRedactor.Redact(outputStr);
            errorStr = SecretRedactor.Redact(errorStr);
        }

        var textResult = new StringBuilder();
        var displayedArguments = unsafeOutput ? arguments : SecretRedactor.Redact(arguments);
        textResult.AppendLine($"Command: dotnet {displayedArguments}");
        if (outputStr.Length > 0) textResult.AppendLine(outputStr);
        if (errorStr.Length > 0)
        {
            textResult.AppendLine("Errors:");
            textResult.AppendLine(errorStr);
        }

        // Special handling for exit code 106 (template pack already installed)
        if (process.ExitCode == ErrorResultFactory.TemplatePackAlreadyInstalledExitCode && arguments.Contains("new install", StringComparison.OrdinalIgnoreCase))
        {
            textResult.AppendLine($"Template pack already installed (exit code {ErrorResultFactory.TemplatePackAlreadyInstalledExitCode} treated as success)");
        }

        textResult.AppendLine($"Exit Code: {process.ExitCode}");
        return textResult.ToString();
    }

    /// <summary>
    /// Execute a dotnet command and return only the standard output.
    /// Throws an exception if the command fails with a non-zero exit code.
    /// NOTE: This method always applies security redaction and does not support unsafeOutput parameter.
    /// It is used for resource operations where security is critical.
    /// </summary>
    /// <param name="arguments">The command-line arguments to pass to dotnet.exe</param>
    /// <param name="logger">Optional logger for debug messages</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <param name="workingDirectory">Optional working directory for command execution</param>
    /// <returns>Standard output only (no error or exit code information), with security redaction applied</returns>
    /// <exception cref="InvalidOperationException">Thrown if the command fails</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled</exception>
    public static async Task<string> ExecuteCommandForResourceAsync(string arguments, ILogger? logger = null, CancellationToken cancellationToken = default, string? workingDirectory = null)
    {
        logger?.LogDebug("Executing: dotnet {Arguments}", arguments);

        workingDirectory ??= WorkingDirectoryOverride.Value;

        string? normalizedWorkingDirectory = null;
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            try
            {
                normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException($"Invalid workingDirectory path: {workingDirectory}. {ex.Message}", ex);
            }

            if (!Directory.Exists(normalizedWorkingDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Directory not found: {normalizedWorkingDirectory}. The workingDirectory parameter must point to an existing directory.");
            }
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(normalizedWorkingDirectory))
        {
            startInfo.WorkingDirectory = normalizedWorkingDirectory;
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start dotnet process with arguments: {arguments}");
        }

        // Register cancellation callback
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    logger?.LogWarning("Cancellation requested - terminating process");
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited - expected race condition
                logger?.LogDebug("Process already exited during cancellation");
            }
        });

        // Read both streams concurrently to avoid deadlock
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        string output;
        string error;

        try
        {
            await Task.WhenAll(outputTask, errorTask);
            output = await outputTask;
            error = await errorTask;
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            throw new OperationCanceledException("Command execution was cancelled", ex, cancellationToken);
        }

        if (process.ExitCode != 0)
        {
            // Apply redaction to error output before logging or throwing
            var redactedError = SecretRedactor.Redact(error);
            logger?.LogError("Command failed with exit code {ExitCode}: {Error}", process.ExitCode, redactedError);
            var errorMessage = !string.IsNullOrEmpty(redactedError)
                ? $"dotnet command failed: {redactedError}"
                : $"dotnet command failed with exit code {process.ExitCode} and no error output.";
            throw new InvalidOperationException(errorMessage);
        }

        logger?.LogDebug("Command completed successfully");
        // Apply redaction to output before returning
        return SecretRedactor.Redact(output);
    }

    /// <summary>
    /// Start a dotnet command process and return it without waiting for completion.
    /// Used for background execution scenarios like 'dotnet run' in background mode.
    /// The caller is responsible for managing the process lifecycle (monitoring, disposal).
    /// </summary>
    /// <param name="arguments">The command-line arguments to pass to dotnet.exe</param>
    /// <param name="logger">Optional logger for debug messages</param>
    /// <param name="workingDirectory">Optional working directory for command execution</param>
    /// <returns>Started Process object</returns>
    /// <exception cref="InvalidOperationException">Thrown if the process cannot be started</exception>
    public static Process StartProcess(string arguments, ILogger? logger = null, string? workingDirectory = null)
    {
        logger?.LogDebug("Starting process: dotnet {Arguments}", arguments);

        workingDirectory ??= WorkingDirectoryOverride.Value;

        string? normalizedWorkingDirectory = null;
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            try
            {
                normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException($"Invalid workingDirectory path: {workingDirectory}. {ex.Message}", ex);
            }

            if (!Directory.Exists(normalizedWorkingDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Directory not found: {normalizedWorkingDirectory}. The workingDirectory parameter must point to an existing directory.");
            }
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(normalizedWorkingDirectory))
        {
            psi.WorkingDirectory = normalizedWorkingDirectory;
        }

        var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
            logger?.LogDebug("Process started with PID: {ProcessId}", process.Id);
            return process;
        }
        catch (Exception ex)
        {
            // Catch all exceptions during process start to ensure proper cleanup
            // Common exceptions: Win32Exception, InvalidOperationException, FileNotFoundException, UnauthorizedAccessException
            logger?.LogError(ex, "Failed to start dotnet process");
            process.Dispose();
            throw new InvalidOperationException($"dotnet command could not be started: {ex.Message}", ex);
        }
    }
}
