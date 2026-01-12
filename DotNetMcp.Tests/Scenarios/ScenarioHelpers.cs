using System.Diagnostics;
using System.Text;
using System.Text.Json;
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
        var root = Path.Combine(Path.GetTempPath(), "dotnet-mcp-scenarios", DateTime.UtcNow.ToString("yyyyMMdd"), testName, Guid.NewGuid().ToString("N"));
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

        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, (await stdoutTask).TrimEnd(), (await stderrTask).TrimEnd());
    }

    /// <summary>
    /// Parses a JSON string into a JsonDocument.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A JsonDocument representing the parsed JSON.</returns>
    public static JsonDocument ParseJson(string json)
        => JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });

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
    /// Asserts that a machine-readable JSON result indicates success.
    /// </summary>
    /// <param name="root">The root JSON element to check.</param>
    public static void AssertMachineReadableSuccess(JsonElement root)
    {
        Assert.True(root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True,
            "Expected machineReadable result with success=true");
    }

    /// <summary>
    /// Asserts that a machine-readable JSON result indicates failure.
    /// </summary>
    /// <param name="root">The root JSON element to check.</param>
    public static void AssertMachineReadableFailure(JsonElement root)
    {
        Assert.True(root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False,
            "Expected machineReadable result with success=false");
    }
}
