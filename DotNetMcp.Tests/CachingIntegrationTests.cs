using System.Diagnostics;
using DotNetMcp;
using FluentAssertions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Integration tests for the caching layer across template and resource operations.
/// </summary>
public class CachingIntegrationTests
{
    [Fact]
    public async Task TemplateEngineHelper_UsesCachingEffectively()
    {
        // Arrange - Clear cache and metrics first
        await TemplateEngineHelper.ClearCacheAsync();

        // Act - First call should be a cache miss
        var result1 = await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: false);

        // Act - Second call should be a cache hit
        var result2 = await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: false);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        
        // Cache metrics should show 1 miss and 1 hit
        TemplateEngineHelper.Metrics.Misses.Should().Be(1);
        TemplateEngineHelper.Metrics.Hits.Should().Be(1);
        TemplateEngineHelper.Metrics.HitRatio.Should().BeApproximately(0.5, 0.01);
        
        // Results should be consistent
        result1.Should().Be(result2);
    }

    [Fact]
    public async Task TemplateEngineHelper_ForceReload_BypassesCache()
    {
        // Arrange - Clear cache first
        await TemplateEngineHelper.ClearCacheAsync();

        // Act - Load templates
        await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: false);
        
        // Act - Force reload should bypass cache
        await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: true);

        // Assert - Both calls should be cache misses
        TemplateEngineHelper.Metrics.Misses.Should().Be(2);
        TemplateEngineHelper.Metrics.Hits.Should().Be(0);
    }

    [Fact]
    public async Task ClearCache_ResetsMetrics()
    {
        // Arrange - Generate some cache activity
        await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: false);
        await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: false);
        await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: false);

        // Verify we have metrics
        TemplateEngineHelper.Metrics.Hits.Should().BeGreaterThan(0);

        // Act - Clear cache
        await TemplateEngineHelper.ClearCacheAsync();

        // Assert - Metrics should be reset
        TemplateEngineHelper.Metrics.Hits.Should().Be(0);
        TemplateEngineHelper.Metrics.Misses.Should().Be(0);
        TemplateEngineHelper.Metrics.HitRatio.Should().Be(0.0);
    }

    [Fact]
    public async Task MultipleTemplateOperations_ShareSameCache()
    {
        // Arrange - Clear cache first
        await TemplateEngineHelper.ClearCacheAsync();

        // Act - Different operations should use same underlying cache
        await TemplateEngineHelper.GetInstalledTemplatesAsync(); // Cache miss
        await TemplateEngineHelper.SearchTemplatesAsync("console"); // Cache hit
        await TemplateEngineHelper.GetTemplateDetailsAsync("console"); // Cache hit

        // Assert - Should show 1 miss and 2 hits
        TemplateEngineHelper.Metrics.Misses.Should().Be(1);
        TemplateEngineHelper.Metrics.Hits.Should().Be(2);
    }

    [Fact]
    public async Task DotNetResources_ClearAllCaches_ClearsAllResourceCaches()
    {
        // Arrange - Clear all caches first
        await DotNetResources.ClearAllCachesAsync();

        // Act - Generate activity in template cache
        await TemplateEngineHelper.GetInstalledTemplatesAsync();
        TemplateEngineHelper.Metrics.Misses.Should().Be(1);

        // Act - Clear all caches
        await DotNetResources.ClearAllCachesAsync();

        // Assert - Template metrics should be reset
        TemplateEngineHelper.Metrics.Hits.Should().Be(0);
        TemplateEngineHelper.Metrics.Misses.Should().Be(0);
    }
}
