namespace DotNetMcp.Tests.Scenarios;

internal static class McpScenarioGate
{
    private const string EnvVar = "DOTNET_MCP_SCENARIO_TESTS";

    // Kept to avoid breaking references in downstream branches.
    // New scenario tests should use [ScenarioFact] which sets Skip on the test.
    public static bool IsEnabled()
    {
        var enabled = Environment.GetEnvironmentVariable(EnvVar);
        return string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(enabled, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
