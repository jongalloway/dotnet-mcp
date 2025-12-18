using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace DotNetMcp.Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class InteractiveFactAttribute : FactAttribute
{
    public const string EnableEnvironmentVariableName = "DOTNET_MCP_INTERACTIVE_TESTS";

    public InteractiveFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0) 
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!IsEnabled())
            Skip = $"Interactive test is opt-in. Set {EnableEnvironmentVariableName}=1 to enable.";
    }

    private static bool IsEnabled()
    {
        var value = Environment.GetEnvironmentVariable(EnableEnvironmentVariableName);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
}