using DotNetMcp;
using System.Diagnostics;
using System.Text;
using System.ComponentModel;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

/// <summary>
/// Helper utilities for scenario tests, including temporary directory management
/// and common assertion methods.
/// </summary>
internal static class ScenarioHelpers
{
    /// <summary>
    /// Represents a temporary directory created for scenario tests that automatically cleans up
    /// when disposed.
    /// </summary>
    public sealed class TempScenarioDirectory : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempScenarioDirectory"/> class.
        /// </summary>
        /// <param name="path">The path to the temporary directory.</param>
        public TempScenarioDirectory(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets the path to the temporary directory.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Implicitly converts a <see cref="TempScenarioDirectory"/> to a string path.
        /// </summary>
        /// <param name="temp">The temporary directory.</param>
        public static implicit operator string(TempScenarioDirectory temp)
            => temp?.Path ?? throw new ArgumentNullException(nameof(temp));

        /// <summary>
        /// Disposes the temporary directory by recursively deleting it.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (!string.IsNullOrEmpty(Path) && Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup for test scenarios; ignore failures.
            }
        }
    }

    /// <summary>
    /// Creates a temporary directory for scenario tests that will be automatically cleaned up
    /// when disposed.
    /// </summary>
    /// <param name="testName">The name of the test creating the directory.</param>
    /// <returns>A disposable temporary directory.</returns>
    public static TempScenarioDirectory CreateTempDirectory(string testName)
    {
        var root = Path.Join(Path.GetTempPath(), "dotnet-mcp-scenarios", DateTime.UtcNow.ToString("yyyyMMdd"), testName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new TempScenarioDirectory(root);
    }

    /// <summary>
    /// Executes a dotnet command and returns the exit code, stdout, and stderr.
    /// </summary>
    /// <param name="args">The arguments to pass to the dotnet command.</param>
    /// <param name="workingDirectory">The working directory for the command.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A tuple containing the exit code, stdout, and stderr.</returns>
    public static async Task<(int exitCode, string stdout, string stderr)> RunDotNetAsync(string args, string workingDirectory, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start dotnet {args}");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        string stdout;
        string stderr;

        try
        {
            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync(cancellationToken);

            stdout = stdoutTask.Result;
            stderr = stderrTask.Result;
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception || ex is ObjectDisposedException)
            {
                // Best-effort cleanup; ignore expected failures when killing the process on cancellation.
            }

            throw;
        }

        return (process.ExitCode, stdout.TrimEnd(), stderr.TrimEnd());
    }

    /// <summary>
    /// Asserts that the given text does not contain the specified secret.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <param name="secret">The secret that should not be present.</param>
    public static void AssertDoesNotContainSecret(string text, string secret)
    {
        Assert.DoesNotContain(secret, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Asserts that a tool response text indicates success (Exit Code: 0).
    /// </summary>
    /// <param name="text">The response text to check.</param>
    /// <param name="stepDescription">Optional step description shown in failure messages.</param>
    public static void AssertSuccess(string text, string? stepDescription = null)
    {
        var message = stepDescription != null
            ? $"Expected success (Exit Code: 0) for step '{stepDescription}'.\nResponse:\n{text}"
            : $"Expected success (Exit Code: 0).\nResponse:\n{text}";
        Assert.True(text.Contains("Exit Code: 0", StringComparison.Ordinal), message);
    }
}
