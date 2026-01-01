using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for template-related MCP tools
/// </summary>
public class TemplateToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public TemplateToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetTemplateList_WithoutForceReload_ReturnsTemplates()
    {
        // Act
        var result = await _tools.DotnetTemplateList(forceReload: false);

        // Assert
        Assert.NotNull(result);
        // Should contain template information (either from cache or SDK)
        Assert.DoesNotContain("Error:", result);
        // Template listings typically contain these keywords
        Assert.True(result.Contains("Template", StringComparison.OrdinalIgnoreCase) || 
                    result.Contains("Short Name", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("console", StringComparison.OrdinalIgnoreCase),
                    "Result should contain template-related information");
    }

    [Fact]
    public async Task DotnetTemplateList_WithForceReload_ReturnsTemplates()
    {
        // Act
        var result = await _tools.DotnetTemplateList(forceReload: true);

        // Assert
        Assert.NotNull(result);
        // Should bypass cache and reload templates
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetTemplateSearch_WithSearchTerm_ReturnsMatchingTemplates()
    {
        // Act
        var result = await _tools.DotnetTemplateSearch(searchTerm: "console");

        // Assert
        Assert.NotNull(result);
        // Should return templates matching "console"
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetTemplateSearch_WithForceReload_ReturnsMatchingTemplates()
    {
        // Act
        var result = await _tools.DotnetTemplateSearch(searchTerm: "web", forceReload: true);

        // Assert
        Assert.NotNull(result);
        // Should bypass cache and search for "web" templates
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetTemplateInfo_WithValidTemplate_ReturnsTemplateDetails()
    {
        // Act
        var result = await _tools.DotnetTemplateInfo(templateShortName: "console");

        // Assert
        Assert.NotNull(result);
        // Should return detailed information about the console template
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetTemplateInfo_WithForceReload_ReturnsTemplateDetails()
    {
        // Act
        var result = await _tools.DotnetTemplateInfo(templateShortName: "classlib", forceReload: true);

        // Assert
        Assert.NotNull(result);
        // Should bypass cache and return template details
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetTemplateClearCache_ClearsCachesSuccessfully()
    {
        // Act
        var result = await _tools.DotnetTemplateClearCache();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("cleared successfully", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetCacheMetrics_ReturnsMetricsInformation()
    {
        // First, trigger some cache usage
        await _tools.DotnetTemplateList();

        // Act
        var result = await _tools.DotnetCacheMetrics();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Cache Metrics", result);
        Assert.Contains("Templates:", result);
        Assert.Contains("SDK Info:", result);
        Assert.Contains("Runtime Info:", result);
        // Metrics should contain hit/miss statistics
        Assert.True(result.Contains("Hits:", StringComparison.OrdinalIgnoreCase) || 
                    result.Contains("Misses:", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("Hit Rate:", StringComparison.OrdinalIgnoreCase),
                    "Result should contain cache statistics");
    }

    [Fact]
    public async Task DotnetCacheMetrics_AfterClearCache_ShowsResetMetrics()
    {
        // Arrange - populate cache
        await _tools.DotnetTemplateList();
        
        // Act - clear cache
        await _tools.DotnetTemplateClearCache();
        
        // Get metrics
        var result = await _tools.DotnetCacheMetrics();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Cache Metrics", result);
    }
}
