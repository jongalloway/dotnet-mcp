using DotNetMcp;
using ModelContextProtocol;

namespace DotNetMcp.Tests;

public class ResourceSubscriptionManagerTests
{
    [Fact]
    public void Subscribe_AddsUri()
    {
        var manager = new ResourceSubscriptionManager();

        manager.Subscribe("dotnet://templates");

        Assert.True(manager.IsSubscribed("dotnet://templates"));
    }

    [Fact]
    public void Subscribe_MultipleUris_TracksEachIndependently()
    {
        var manager = new ResourceSubscriptionManager();

        manager.Subscribe("dotnet://templates");
        manager.Subscribe("dotnet://sdk-info");

        Assert.True(manager.IsSubscribed("dotnet://templates"));
        Assert.True(manager.IsSubscribed("dotnet://sdk-info"));
        Assert.False(manager.IsSubscribed("dotnet://runtime-info"));
    }

    [Fact]
    public void Subscribe_UnknownUri_ThrowsMcpException()
    {
        var manager = new ResourceSubscriptionManager();

        Assert.Throws<McpException>(() => manager.Subscribe("dotnet://unknown-resource"));
    }

    [Fact]
    public void Unsubscribe_RemovesUri()
    {
        var manager = new ResourceSubscriptionManager();
        manager.Subscribe("dotnet://templates");

        manager.Unsubscribe("dotnet://templates");

        Assert.False(manager.IsSubscribed("dotnet://templates"));
    }

    [Fact]
    public void Unsubscribe_KnownUriThatWasNeverSubscribed_IsIdempotent()
    {
        var manager = new ResourceSubscriptionManager();

        // Known URI that was never subscribed — should succeed (idempotent unsubscribe per MCP spec).
        manager.Unsubscribe("dotnet://templates");

        Assert.False(manager.IsSubscribed("dotnet://templates"));
    }

    [Fact]
    public void Unsubscribe_UnknownUri_ThrowsMcpException()
    {
        var manager = new ResourceSubscriptionManager();

        Assert.Throws<McpException>(() => manager.Unsubscribe("dotnet://unknown-resource"));
    }

    [Fact]
    public void Subscribe_IsCaseSensitive()
    {
        var manager = new ResourceSubscriptionManager();

        manager.Subscribe("dotnet://templates");

        Assert.True(manager.IsSubscribed("dotnet://templates"));
        Assert.False(manager.IsSubscribed("dotnet://TEMPLATES"));
    }

    [Fact]
    public void Subscribe_Duplicate_DoesNotThrow()
    {
        var manager = new ResourceSubscriptionManager();

        manager.Subscribe("dotnet://templates");
        manager.Subscribe("dotnet://templates"); // second call is a no-op

        Assert.True(manager.IsSubscribed("dotnet://templates"));
        Assert.Single(manager.SubscribedUris, u => u == "dotnet://templates");
    }

    [Fact]
    public void SubscribedUris_ReflectsCurrentSubscriptions()
    {
        var manager = new ResourceSubscriptionManager();

        manager.Subscribe("dotnet://templates");
        manager.Subscribe("dotnet://sdk-info");
        manager.Unsubscribe("dotnet://templates");

        var uris = manager.SubscribedUris.ToList();
        Assert.Single(uris);
        Assert.Contains("dotnet://sdk-info", uris);
        Assert.DoesNotContain("dotnet://templates", uris);
    }

    [Fact]
    public void KnownResourceUris_ContainsExpectedResources()
    {
        Assert.Contains("dotnet://sdk-info", ResourceSubscriptionManager.KnownResourceUris);
        Assert.Contains("dotnet://runtime-info", ResourceSubscriptionManager.KnownResourceUris);
        Assert.Contains("dotnet://templates", ResourceSubscriptionManager.KnownResourceUris);
        Assert.Contains("dotnet://frameworks", ResourceSubscriptionManager.KnownResourceUris);
        Assert.Contains("dotnet://telemetry-data", ResourceSubscriptionManager.KnownResourceUris);
    }

    [Fact]
    public async Task SendResourceUpdatedAsync_NullServer_DoesNotThrow()
    {
        var manager = new ResourceSubscriptionManager();
        manager.Subscribe("dotnet://templates");

        // No exception when server is null
        await manager.SendResourceUpdatedAsync(null, "dotnet://templates", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendResourceUpdatedAsync_NotSubscribed_DoesNotThrow()
    {
        var manager = new ResourceSubscriptionManager();

        // Not subscribed — should be a no-op
        await manager.SendResourceUpdatedAsync(null, "dotnet://templates", TestContext.Current.CancellationToken);
    }
}
