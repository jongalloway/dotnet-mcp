using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

internal static class ScenarioHelpers
{
    public static string CreateTempDirectory(string testName)
    {
        var root = Path.Combine(Path.GetTempPath(), "dotnet-mcp-scenarios", DateTime.UtcNow.ToString("yyyyMMdd"), testName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

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

    public static JsonDocument ParseJson(string json)
        => JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });

    public static void AssertDoesNotContainSecret(string text, string secret)
    {
        Assert.DoesNotContain(secret, text, StringComparison.Ordinal);
    }

    public static void AssertMachineReadableSuccess(JsonElement root)
    {
        Assert.True(root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True,
            "Expected machineReadable result with success=true");
    }

    public static void AssertMachineReadableFailure(JsonElement root)
    {
        Assert.True(root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False,
            "Expected machineReadable result with success=false");
    }
}
