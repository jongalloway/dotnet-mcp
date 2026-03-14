using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Unit tests for <see cref="CompletionProvider"/> — validate that completion suggestions are
/// correctly generated and filtered for each supported argument name.
/// These tests exercise the provider directly without starting the MCP server.
/// </summary>
/// <remarks>
/// Uses the CachingIntegrationTests collection to ensure sequential execution when
/// the <see cref="CompletionProvider.GetTemplateShortNamesOverride"/> test seam is set,
/// following the same convention as <see cref="TemplateEngineHelperTests"/>.
/// </remarks>
[Collection("CachingIntegrationTests")]
public class CompletionProviderTests
{
    #region Framework Completions

    [Fact]
    public async Task GetCompletionsAsync_Framework_EmptyPrefix_ReturnsAllModernFrameworks()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("framework", "", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("net10.0", result);
        Assert.Contains("net8.0", result);
        Assert.Contains("net6.0", result);
    }

    [Fact]
    public async Task GetCompletionsAsync_Framework_PrefixNet10_ReturnsOnlyMatchingFrameworks()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("framework", "net10", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, v => Assert.StartsWith("net10", v, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCompletionsAsync_Framework_PrefixThatMatchesNothing_ReturnsEmpty()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("framework", "xyz", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCompletionsAsync_Framework_PrefixIsCaseInsensitive()
    {
        // Act
        var lower = (await CompletionProvider.GetCompletionsAsync("framework", "NET10", TestContext.Current.CancellationToken)).ToList();
        var upper = (await CompletionProvider.GetCompletionsAsync("framework", "net10", TestContext.Current.CancellationToken)).ToList();

        // Assert - both should return the same results
        Assert.Equal(upper, lower);
    }

    #endregion

    #region Configuration Completions

    [Fact]
    public async Task GetCompletionsAsync_Configuration_EmptyPrefix_ReturnsBothValues()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("configuration", "", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Debug", result);
        Assert.Contains("Release", result);
    }

    [Fact]
    public async Task GetCompletionsAsync_Configuration_PrefixD_ReturnsOnlyDebug()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("configuration", "D", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Debug", result[0]);
    }

    [Fact]
    public async Task GetCompletionsAsync_Configuration_PrefixR_ReturnsOnlyRelease()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("configuration", "R", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Release", result[0]);
    }

    #endregion

    #region Runtime Completions

    [Fact]
    public async Task GetCompletionsAsync_Runtime_EmptyPrefix_ReturnsAllRuntimes()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("runtime", "", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("win-x64", result);
        Assert.Contains("linux-x64", result);
        Assert.Contains("osx-arm64", result);
    }

    [Fact]
    public async Task GetCompletionsAsync_Runtime_PrefixWin_ReturnsOnlyWindowsRuntimes()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("runtime", "win", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, v => Assert.StartsWith("win", v, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("linux-x64", result);
        Assert.DoesNotContain("osx-x64", result);
    }

    [Fact]
    public async Task GetCompletionsAsync_Runtime_PrefixLinux_ReturnsOnlyLinuxRuntimes()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("runtime", "linux", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, v => Assert.StartsWith("linux", v, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Template Completions (Fallback)

    [Fact]
    public void GetFallbackTemplateCompletions_ContainsCommonTemplates()
    {
        // Act
        var result = CompletionProvider.GetFallbackTemplateCompletions().ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("console", result);
        Assert.Contains("webapi", result);
        Assert.Contains("classlib", result);
        Assert.Contains("worker", result);
        Assert.Contains("xunit", result);
    }

    [Fact]
    public void GetFallbackTemplateCompletions_AllValuesAreNonEmpty()
    {
        // Act
        var result = CompletionProvider.GetFallbackTemplateCompletions().ToList();

        // Assert
        Assert.All(result, v => Assert.NotEmpty(v));
    }

    #endregion

    #region Unknown Argument

    [Fact]
    public async Task GetCompletionsAsync_UnknownArgument_ReturnsEmpty()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("unknownArgument", "", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCompletionsAsync_UnknownArgument_WithPrefix_ReturnsEmpty()
    {
        // Act
        var result = (await CompletionProvider.GetCompletionsAsync("projectPath", "src/", TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region MaxResults

    [Fact]
    public async Task GetCompletionsAsync_Template_ResultCountCappedAtMaxResults()
    {
        // Arrange – inject more short names than MaxResults so the Take() cap is actually exercised.
        var manyNames = Enumerable.Range(1, CompletionProvider.MaxResults + 5)
            .Select(i => $"fake-template-{i:D3}")
            .ToList();
        CompletionProvider.GetTemplateShortNamesOverride = _ => Task.FromResult<IEnumerable<string>>(manyNames);

        try
        {
            // Act – empty prefix so all injected names are candidates
            var result = (await CompletionProvider.GetCompletionsAsync("template", "", TestContext.Current.CancellationToken)).ToList();

            // Assert – result is capped, not the full injected list
            Assert.Equal(CompletionProvider.MaxResults, result.Count);
        }
        finally
        {
            CompletionProvider.GetTemplateShortNamesOverride = null;
        }
    }

    #endregion

    #region RuntimeCompletions Helper

    [Fact]
    public void GetRuntimeCompletions_ContainsExpectedPlatforms()
    {
        // Act
        var result = CompletionProvider.GetRuntimeCompletions().ToList();

        // Assert
        Assert.Contains("win-x64", result);
        Assert.Contains("win-arm64", result);
        Assert.Contains("linux-x64", result);
        Assert.Contains("linux-arm64", result);
        Assert.Contains("osx-x64", result);
        Assert.Contains("osx-arm64", result);
    }

    #endregion
}
