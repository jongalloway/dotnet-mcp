using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DotNetMcp.Tests;

public class ListResultCachingTests
{
    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    [InlineData("2025-06-18")]
    [InlineData("2025-03-26")]
    [InlineData("2024-11-05")]
    public async Task ListResults_UseProtocolSpecificCacheHints(string protocol)
    {
        await using var client = await CreateClientAsync(protocol);
        Assert.Equal(protocol, client.NegotiatedProtocolVersion);

        foreach (var (method, property) in new[]
        {
            (RequestMethods.ToolsList, "tools"),
            (RequestMethods.PromptsList, "prompts"),
            (RequestMethods.ResourcesList, "resources"),
            (RequestMethods.ResourcesTemplatesList, "resourceTemplates")
        })
        {
            var response = await client.SendRequestAsync(new() { Method = method },
                TestContext.Current.CancellationToken);
            var result = Assert.IsType<JsonObject>(response.Result);
            var entries = Assert.IsType<JsonArray>(result[property]);
            if (method == RequestMethods.ResourcesTemplatesList)
                Assert.Empty(entries);
            else
                Assert.NotEmpty(entries);

            if (protocol == "2026-07-28")
            {
                Assert.Equal(300000, result["ttlMs"]!.GetValue<int>());
                Assert.Equal("private", result["cacheScope"]!.GetValue<string>());
            }
            else
            {
                Assert.False(result.ContainsKey("ttlMs"), $"{method} must omit ttlMs for {protocol}.");
                Assert.False(result.ContainsKey("cacheScope"), $"{method} must omit cacheScope for {protocol}.");
            }

            if (method == RequestMethods.ToolsList)
            {
                var sdk = Assert.Single(entries, tool => tool!["name"]!.GetValue<string>() == "dotnet_sdk");
                Assert.Equal("object", sdk!["inputSchema"]!["type"]!.GetValue<string>());
                Assert.NotNull(sdk["outputSchema"]);
                Assert.Equal("ui://dotnet-mcp/sdk-dashboard",
                    sdk["_meta"]!["ui"]!["resourceUri"]!.GetValue<string>());
            }
            else if (method == RequestMethods.ResourcesList)
            {
                Assert.Contains(entries, resource => resource!["uri"]!.GetValue<string>() == "dotnet://workspace");
                Assert.Contains(entries, resource => resource!["uri"]!.GetValue<string>() == "ui://dotnet-mcp/sdk-dashboard");
            }
            else if (method == RequestMethods.PromptsList)
            {
                Assert.Contains(entries, prompt => prompt!["name"]!.GetValue<string>() == "new_console_app");
            }
        }
    }

    [Fact]
    public async Task Discovery_RetainsSafeSdkDefaults()
    {
        await using var client = await CreateClientAsync("2026-07-28");
        var response = await client.SendRequestAsync(new() { Method = RequestMethods.ServerDiscover },
            TestContext.Current.CancellationToken);
        var result = Assert.IsType<JsonObject>(response.Result);
        Assert.Equal(0, result["ttlMs"]!.GetValue<int>());
        Assert.Equal("private", result["cacheScope"]!.GetValue<string>());
        Assert.Contains("2026-07-28", result["supportedVersions"]!.AsArray().Select(value => value!.GetValue<string>()));
        Assert.NotNull(result["capabilities"]!["tools"]);
        Assert.NotNull(result["capabilities"]!["prompts"]);
        Assert.NotNull(result["capabilities"]!["resources"]);
    }

    private static async Task<McpClient> CreateClientAsync(string protocol)
    {
        var binaryDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var framework = Path.GetFileName(binaryDirectory);
        var configuration = Path.GetFileName(Path.GetDirectoryName(binaryDirectory));
        var serverDll = Path.GetFullPath(Path.Join(binaryDirectory, "..", "..", "..", "..",
            "DotNetMcp", "bin", configuration, framework, "DotNetMcp.dll"));
        Assert.True(File.Exists(serverDll), $"Build the server before running wire-format tests: {serverDll}");

        return await McpClient.CreateAsync(
            new StdioClientTransport(new()
            {
                Command = "dotnet",
                Arguments = [serverDll],
                WorkingDirectory = Path.GetDirectoryName(serverDll),
                Name = $"dotnet-mcp-list-caching-{protocol}"
            }),
            new()
            {
                ProtocolVersion = protocol,
                DiscoverProbeTimeout = Timeout.InfiniteTimeSpan
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
