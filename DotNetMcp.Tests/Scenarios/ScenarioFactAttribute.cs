using System.Runtime.CompilerServices;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

/// <summary>
/// Marks a test method as a scenario test that exercises the full dotnet-mcp server end-to-end.
/// Scenario tests spin up the real MCP server and communicate with it over stdio, so they behave
/// like integration tests rather than fast, isolated unit tests.
/// </summary>
/// <remarks>
/// Scenario tests are slower and more resource intensive than regular unit tests and are disabled
/// by default. To run them locally or in CI, set the <see cref="EnableEnvironmentVariableName"/>
/// environment variable to <c>1</c>, <c>true</c>, or <c>yes</c>. When the variable is not set to
/// an accepted value, tests annotated with this attribute are automatically skipped.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ScenarioFactAttribute : FactAttribute
{
    /// <summary>
    /// The environment variable name that controls whether scenario tests are enabled.
    /// </summary>
    public const string EnableEnvironmentVariableName = "DOTNET_MCP_SCENARIO_TESTS";

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioFactAttribute"/> class and
    /// configures the test to be skipped unless scenario tests are explicitly enabled.
    /// </summary>
    /// <param name="sourceFilePath">
    /// The source file path of the test method, supplied automatically by the compiler and
    /// passed through to <see cref="FactAttribute"/> for display and diagnostics.
    /// </param>
    /// <param name="sourceLineNumber">
    /// The line number of the test method, supplied automatically by the compiler and
    /// passed through to <see cref="FactAttribute"/> for display and diagnostics.
    /// </param>
    public ScenarioFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!IsEnabled())
        {
            Skip = $"Scenario tests are opt-in. Set {EnableEnvironmentVariableName}=1 to enable.";
        }
    }

    private static bool IsEnabled()
    {
        var value = Environment.GetEnvironmentVariable(EnableEnvironmentVariableName);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
