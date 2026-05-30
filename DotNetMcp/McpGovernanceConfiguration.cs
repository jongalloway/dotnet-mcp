using Microsoft.Extensions.Configuration;

namespace DotNetMcp;

/// <summary>
/// Provides opt-in settings for MCP governance integration.
/// </summary>
public sealed class McpGovernanceConfiguration
{
    public const string SectionName = "McpGovernance";

    public bool Enabled { get; init; }

    public string DefaultAgentId { get; init; } = "did:mcp:anonymous";

    public string ServerName { get; init; } = "dotnet-mcp";

    public IReadOnlyList<string> PolicyPaths { get; init; } = [];

    public static McpGovernanceConfiguration Load(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var enabled = section.GetValue<bool>("Enabled");

        var configuredPaths = section
            .GetSection("PolicyPaths")
            .Get<string[]>()?
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var policyPaths = configuredPaths is { Length: > 0 }
            ? configuredPaths
            : [];

        var defaultAgentId = section.GetValue<string>("DefaultAgentId");
        var serverName = section.GetValue<string>("ServerName");

        return new McpGovernanceConfiguration
        {
            Enabled = enabled,
            DefaultAgentId = string.IsNullOrWhiteSpace(defaultAgentId) ? "did:mcp:anonymous" : defaultAgentId,
            ServerName = string.IsNullOrWhiteSpace(serverName) ? "dotnet-mcp" : serverName,
            PolicyPaths = policyPaths,
        };
    }
}
