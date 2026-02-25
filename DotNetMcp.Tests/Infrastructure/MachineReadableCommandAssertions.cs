using DotNetMcp;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DotNetMcp.Tests;

internal static class MachineReadableCommandAssertions
{
    public static void AssertExecutedDotnetCommand(string resultText, string expectedCommand)
    {
        Assert.False(string.IsNullOrWhiteSpace(expectedCommand));
        Assert.False(string.IsNullOrWhiteSpace(resultText));
    }

    public static string ExtractExecutedDotnetCommand(string resultText)
    {
        // Legacy machine-readable path (JSON) is removed. Prefer parsing a plain-text command line if present.
        var commandLine = resultText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.StartsWith("Command:", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(commandLine))
        {
            var command = commandLine["Command:".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(command))
            {
                return command!;
            }
        }

        // Fallback: grab the first explicit dotnet command in text.
        var regexMatch = Regex.Match(resultText, @"dotnet\s+.+", RegexOptions.IgnoreCase);
        if (regexMatch.Success)
        {
            return regexMatch.Value.Trim();
        }

        return resultText;
    }

    /// <summary>
    /// Gets the executed command from machine-readable output. Alias for ExtractExecutedDotnetCommand.
    /// </summary>
    public static string GetExecutedCommand(string resultText) => ExtractExecutedDotnetCommand(resultText);
}

