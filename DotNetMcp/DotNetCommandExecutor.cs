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

    /// <summary>
    /// Execute a dotnet command with full output handling, logging, and truncation support.
    /// </summary>
    /// <param name="arguments">The command-line arguments to pass to dotnet.exe</param>
    /// <param name="logger">Optional logger for debug/warning messages</param>
    /// <param name="machineReadable">When true, returns JSON format with structured errors; when false, returns plain text</param>
    /// <param name="unsafeOutput">When true, disables security redaction of sensitive information (default: false). Use with caution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Combined output, error, and exit code information (plain text or JSON based on machineReadable)</returns>
    public static async Task<string> ExecuteCommandAsync(string arguments, ILogger? logger = null, bool machineReadable = false, bool unsafeOutput = false, CancellationToken cancellationToken = default)
    {
        logger?.LogDebug("Executing: dotnet {Arguments}", arguments);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

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
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to start dotnet process");

            if (machineReadable)
            {
                var alternatives = new[]
                {
                    "Install the .NET SDK from https://dotnet.microsoft.com/download",
                    "Verify 'dotnet' is on PATH (try: dotnet --info)",
                    "If using global.json, ensure the requested SDK is installed"
                };

                var result = ErrorResultFactory.ReturnCapabilityNotAvailable(
                    feature: "dotnet CLI",
                    alternatives: alternatives,
                    command: $"dotnet {arguments}",
                    details: ex.Message);

                return ErrorResultFactory.ToJson(result);
            }

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
            
            if (machineReadable)
            {
                var code = "OPERATION_CANCELLED";
                var category = "Cancellation";
                var mcpErrorCode = McpErrorCodes.GetMcpErrorCode(code, category, -1);
                
                var cancelResult = new ErrorResponse
                {
                    Success = false,
                    Errors = new List<ErrorResult>
                    {
                        new ErrorResult
                        {
                            Code = code,
                            Message = "The operation was cancelled by the user",
                            Category = category,
                            Hint = "The command was terminated before completion",
                            RawOutput = partialOutput,
                            McpErrorCode = mcpErrorCode,
                            Data = new ErrorData
                            {
                                Command = $"dotnet {arguments}",
                                ExitCode = -1
                            }
                        }
                    },
                    ExitCode = -1
                };
                return ErrorResultFactory.ToJson(cancelResult);
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

        // Apply security redaction unless unsafeOutput is explicitly enabled
        var outputStr = output.ToString().TrimEnd();
        var errorStr = error.ToString().TrimEnd();
        
        if (!unsafeOutput)
        {
            outputStr = SecretRedactor.Redact(outputStr);
            errorStr = SecretRedactor.Redact(errorStr);
        }

        // If machine-readable format is requested, return structured JSON
        if (machineReadable)
        {
            var result = ErrorResultFactory.CreateResult(outputStr, errorStr, process.ExitCode, $"dotnet {arguments}");
            return ErrorResultFactory.ToJson(result);
        }

        // Otherwise, return plain text format (backwards compatible)
        var textResult = new StringBuilder();
        if (outputStr.Length > 0) textResult.AppendLine(outputStr);
        if (errorStr.Length > 0)
        {
            textResult.AppendLine("Errors:");
            textResult.AppendLine(errorStr);
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
    /// <returns>Standard output only (no error or exit code information), with security redaction applied</returns>
    /// <exception cref="InvalidOperationException">Thrown if the command fails</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled</exception>
    public static async Task<string> ExecuteCommandForResourceAsync(string arguments, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger?.LogDebug("Executing: dotnet {Arguments}", arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

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
}
