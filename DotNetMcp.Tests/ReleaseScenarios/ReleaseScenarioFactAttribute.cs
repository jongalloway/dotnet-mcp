using System.Runtime.CompilerServices;
using Xunit;

namespace DotNetMcp.Tests.ReleaseScenarios;

/// <summary>
/// Marks a test method as a long-running release-gate scenario test.
/// These tests are disabled by default and intended to run only in manual workflows
/// (e.g., workflow_dispatch) before shipping a release.
/// </summary>
/// <remarks>
/// To enable these tests locally or in CI, set <see cref="EnableEnvironmentVariableName"/>
/// to <c>1</c>, <c>true</c>, or <c>yes</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ReleaseScenarioFactAttribute : FactAttribute
{
    /// <summary>
    /// The environment variable name that controls whether release scenario tests are enabled.
    /// </summary>
    public const string EnableEnvironmentVariableName = "DOTNET_MCP_RELEASE_SCENARIO_TESTS";

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseScenarioFactAttribute"/> class and
    /// configures the test to be skipped unless release scenarios are explicitly enabled.
    /// </summary>
    /// <param name="sourceFilePath">
    /// The source file path of the test method, supplied automatically by the compiler.
    /// </param>
    /// <param name="sourceLineNumber">
    /// The line number of the test method, supplied automatically by the compiler.
    /// </param>
    public ReleaseScenarioFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!IsEnabled())
        {
            Skip = $"Release scenario tests are opt-in. Set {EnableEnvironmentVariableName}=1 to enable.";
        }
    }

    /// <summary>
    /// Determines whether release scenario tests are enabled based on the environment variable.
    /// </summary>
    /// <returns><c>true</c> if enabled; otherwise, <c>false</c>.</returns>
    public static bool IsEnabled()
    {
        var value = Environment.GetEnvironmentVariable(EnableEnvironmentVariableName);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
