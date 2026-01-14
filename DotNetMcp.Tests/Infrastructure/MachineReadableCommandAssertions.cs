using System.Linq;
using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests;

internal static class MachineReadableCommandAssertions
{
    public static void AssertExecutedDotnetCommand(string resultJson, string expectedCommand)
    {
        var actual = ExtractExecutedDotnetCommand(resultJson);
        Assert.Equal(expectedCommand, actual);
    }

    public static string ExtractExecutedDotnetCommand(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        // Preferred: SuccessResult now includes the executed command.
        if (root.TryGetProperty("command", out var commandElement)
            && commandElement.ValueKind == JsonValueKind.String)
        {
            var command = commandElement.GetString();
            if (!string.IsNullOrWhiteSpace(command))
            {
                return command!;
            }
        }

        // Fallback: ErrorResponse includes the command on errors[*].data.command.
        if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
        {
            var command = errorsElement.EnumerateArray()
                .Where(e => e.TryGetProperty("data", out var d)
                    && d.ValueKind == JsonValueKind.Object
                    && d.TryGetProperty("command", out var c)
                    && c.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(c.GetString()))
                .Select(e => e.GetProperty("data").GetProperty("command").GetString()!)
                .FirstOrDefault();

            if (command != null)
            {
                return command;
            }
        }

        Assert.Fail("Could not find executed command in machine-readable result JSON (expected either root.command or errors[*].data.command).");
        return string.Empty;
    }

    /// <summary>
    /// Gets the executed command from machine-readable output. Alias for ExtractExecutedDotnetCommand.
    /// </summary>
    public static string GetExecutedCommand(string resultJson) => ExtractExecutedDotnetCommand(resultJson);
}

