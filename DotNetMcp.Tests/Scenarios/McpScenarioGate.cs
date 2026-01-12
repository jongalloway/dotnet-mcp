namespace DotNetMcp.Tests.Scenarios;

/// <summary>
/// Provides a simple environment-variable-based gate for enabling or disabling
/// scenario tests in this test suite.
/// </summary>
internal static class McpScenarioGate
{
    private const string EnvVar = "DOTNET_MCP_SCENARIO_TESTS";

    /// <summary>
    /// Determines whether scenario tests are enabled based on the
    /// <c>DOTNET_MCP_SCENARIO_TESTS</c> environment variable.
    /// </summary>
    /// <remarks>
    /// This method is kept to avoid breaking references in downstream branches.
    /// New scenario tests should prefer using <c>[ScenarioFact]</c>, which sets
    /// <see cref="Xunit.FactAttribute.Skip"/> on the test when scenarios are disabled.
    /// </remarks>
    /// <returns><c>true</c> if scenario tests are enabled; otherwise, <c>false</c>.</returns>
    public static bool IsEnabled()
    {
        return ScenarioFactAttribute.IsEnabled();
    }
}
