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
    /// <returns>Combined output, error, and exit code information</returns>
    public static async Task<string> ExecuteCommandAsync(string arguments, ILogger? logger = null)
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

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        logger?.LogDebug("Command completed with exit code: {ExitCode}", process.ExitCode);
        if (outputTruncated)
        {
            logger?.LogWarning("Output was truncated due to size limit");
        }
        if (errorTruncated)
        {
            logger?.LogWarning("Error output was truncated due to size limit");
        }

        var result = new StringBuilder();
        if (output.Length > 0) result.AppendLine(output.ToString());
        if (error.Length > 0)
        {
            result.AppendLine("Errors:");
            result.AppendLine(error.ToString());
        }
        result.AppendLine($"Exit Code: {process.ExitCode}");
        return result.ToString();
    }

    /// <summary>
    /// Execute a dotnet command and return only the standard output.
    /// Throws an exception if the command fails with a non-zero exit code.
    /// </summary>
    /// <param name="arguments">The command-line arguments to pass to dotnet.exe</param>
    /// <param name="logger">Optional logger for debug messages</param>
    /// <returns>Standard output only (no error or exit code information)</returns>
    /// <exception cref="InvalidOperationException">Thrown if the command fails</exception>
    public static async Task<string> ExecuteCommandForResourceAsync(string arguments, ILogger? logger = null)
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
            throw new InvalidOperationException("Failed to start dotnet process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            logger?.LogError("Command failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            var errorMessage = !string.IsNullOrEmpty(error)
                ? $"dotnet command failed: {error}"
                : $"dotnet command failed with exit code {process.ExitCode} and no error output.";
            throw new InvalidOperationException(errorMessage);
        }

        logger?.LogDebug("Command completed successfully");
        return output;
    }
}
