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
            var errors = errorsElement.EnumerateArray()
                .Where(error => error.TryGetProperty("data", out var dataElement)
                    && dataElement.ValueKind == JsonValueKind.Object
                    && dataElement.TryGetProperty("command", out var errorCommandElement)
                    && errorCommandElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(errorCommandElement.GetString()));

            foreach (var error in errors)
            {
                if (error.TryGetProperty("data", out var dataElement)
                    && dataElement.TryGetProperty("command", out var errorCommandElement))
                {
                    return errorCommandElement.GetString()!;
                }
            }
        }

        Assert.Fail("Could not find executed command in machine-readable result JSON (expected either root.command or errors[*].data.command).");
        return string.Empty;
    }
}

