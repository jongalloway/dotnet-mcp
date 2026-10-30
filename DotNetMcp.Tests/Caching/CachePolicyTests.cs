using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp.Tests;

public class CachePolicyTests : IAsyncDisposable
{
    private readonly ServiceProvider _services;

    public CachePolicyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMcpServer().WithStreamServerTransport(Stream.Null, Stream.Null);
        _services = services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("2026-07-28", true)]
    [InlineData("2025-11-25", false)]
    [InlineData("2025-06-18", false)]
    [InlineData("2025-03-26", false)]
    [InlineData("2024-11-05", false)]
    [InlineData(null, false)]
    public async Task ListFilter_PreservesHandlerResultAndUsesContentCacheDefault(string? protocol, bool cacheable)
    {
        using var cache = new CachedResourceManager<string>("policy-test");
        var entry = await cache.GetOrLoadAsync(() => Task.FromResult("data"),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(TimeSpan.FromSeconds(300), entry.CacheDuration);

        var result = new ListToolsResult
        {
            Tools = [new Tool { Name = "example", InputSchema = JsonSerializer.SerializeToElement(new { type = "object" }) }],
            NextCursor = "next-page"
        };
        var context = CreateContext(protocol);
        var calls = 0;
        var handler = CachePolicy.CreateListFilter<ListToolsRequestParams, ListToolsResult>()((request, ct) =>
        {
            Assert.Same(context, request);
            Assert.Equal(TestContext.Current.CancellationToken, ct);
            calls++;
            return ValueTask.FromResult(result);
        });

        var response = await handler(context, TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        Assert.Same(result, response);
        Assert.Equal("next-page", response.NextCursor);
        Assert.Equal("example", Assert.Single(response.Tools).Name);
        Assert.Equal(cacheable ? entry.CacheDuration : (TimeSpan?)null, response.TimeToLive);
        Assert.Equal(cacheable ? CacheScope.Private : (CacheScope?)null, response.CacheScope);
        Assert.Equal(1, cache.Metrics.Misses);
        Assert.Equal(0, cache.Metrics.Hits);
    }

    [Fact]
    public async Task ListFilter_PropagatesHandlerError()
    {
        var error = new McpException("list failed");
        var handler = CachePolicy.CreateListFilter<ListToolsRequestParams, ListToolsResult>()((_, _) => throw error);

        var thrown = await Assert.ThrowsAsync<McpException>(
            async () => await handler(CreateContext("2026-07-28"), TestContext.Current.CancellationToken));

        Assert.Same(error, thrown);
    }

    [Fact]
    public async Task ListFilter_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var handler = CachePolicy.CreateListFilter<ListToolsRequestParams, ListToolsResult>()(
            (_, ct) => ValueTask.FromCanceled<ListToolsResult>(ct));

        var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await handler(CreateContext("2026-07-28"), cancellation.Token));

        Assert.Equal(cancellation.Token, thrown.CancellationToken);
    }

    private RequestContext<ListToolsRequestParams> CreateContext(string? protocol) => new(
        _services.GetRequiredService<McpServer>(),
        new JsonRpcRequest
        {
            Method = RequestMethods.ToolsList,
            Context = new() { ProtocolVersion = protocol }
        },
        new ListToolsRequestParams());

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
