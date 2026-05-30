using DotNetMcp;
using Microsoft.Extensions.Configuration;

namespace DotNetMcp.Tests.Server;

public class McpGovernanceConfigurationTests
{
    [Fact]
    public void Load_UsesSafeDefaults_WhenSectionMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = McpGovernanceConfiguration.Load(configuration);

        Assert.False(result.Enabled);
        Assert.Equal("did:mcp:anonymous", result.DefaultAgentId);
        Assert.Equal("dotnet-mcp", result.ServerName);
        Assert.Empty(result.PolicyPaths);
    }

    [Fact]
    public void Load_NormalizesPolicyPaths_WhenConfigured()
    {
        var values = new Dictionary<string, string?>
        {
            ["McpGovernance:Enabled"] = "true",
            ["McpGovernance:DefaultAgentId"] = "did:mcp:server",
            ["McpGovernance:ServerName"] = "contoso-support",
            ["McpGovernance:PolicyPaths:0"] = " policies/mcp.yaml ",
            ["McpGovernance:PolicyPaths:1"] = "",
            ["McpGovernance:PolicyPaths:2"] = "policies/mcp.yaml",
            ["McpGovernance:PolicyPaths:3"] = "policies/extra.yaml",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var result = McpGovernanceConfiguration.Load(configuration);

        Assert.True(result.Enabled);
        Assert.Equal("did:mcp:server", result.DefaultAgentId);
        Assert.Equal("contoso-support", result.ServerName);
        Assert.Equal(2, result.PolicyPaths.Count);
        Assert.Equal("policies/mcp.yaml", result.PolicyPaths[0]);
        Assert.Equal("policies/extra.yaml", result.PolicyPaths[1]);
    }
}
