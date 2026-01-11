using DotNetMcp;
using Microsoft.TemplateEngine.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for TemplateEngineHelper functionality.
/// Uses the same collection as CachingIntegrationTests to ensure sequential execution
/// since both test classes share the static TemplateEngineHelper cache state.
/// </summary>
[Collection("CachingIntegrationTests")]
public class TemplateEngineHelperTests
{
    [Fact]
    public async Task GetInstalledTemplatesAsync_Fallback_MachineReadable_ReturnsSuccessResultJson()
    {
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<ITemplateInfo>>(Array.Empty<ITemplateInfo>());
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (_, _) => Task.FromResult("FAKE TEMPLATE LIST OUTPUT");

            var json = await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: true, machineReadable: true);

            Assert.Contains("\"success\": true", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dotnet new list", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FAKE TEMPLATE LIST OUTPUT", json);
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    [Fact]
    public async Task GetInstalledTemplatesAsync_WhenTemplateApiReturnsEmpty_UsesDotnetNewListFallback()
    {
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<ITemplateInfo>>(Array.Empty<ITemplateInfo>());

            string? executedArgs = null;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                executedArgs = args;
                return Task.FromResult("FAKE TEMPLATE LIST OUTPUT");
            };

            var result = await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: true);

            Assert.Contains("dotnet new list", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FAKE TEMPLATE LIST OUTPUT", result);
            Assert.Equal("new list --columns author --columns language --columns type --columns tags", executedArgs);
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    [Fact]
    public async Task SearchTemplatesAsync_WhenTemplateApiReturnsEmpty_UsesDotnetNewListFallback()
    {
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<ITemplateInfo>>(Array.Empty<ITemplateInfo>());

            string? executedArgs = null;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                executedArgs = args;
                return Task.FromResult("FAKE SEARCH OUTPUT");
            };

            var result = await TemplateEngineHelper.SearchTemplatesAsync("console", forceReload: true);

            Assert.Contains("Templates matching 'console'", result);
            Assert.Contains("FAKE SEARCH OUTPUT", result);
            Assert.Equal("new list \"console\" --columns author --columns language --columns type --columns tags", executedArgs);
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    [Fact]
    public async Task GetTemplateDetailsAsync_WhenTemplateApiReturnsEmpty_UsesDotnetNewHelpFallback()
    {
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<ITemplateInfo>>(Array.Empty<ITemplateInfo>());

            string? executedArgs = null;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                executedArgs = args;
                return Task.FromResult("USAGE: dotnet new console [options]");
            };

            var result = await TemplateEngineHelper.GetTemplateDetailsAsync("console", forceReload: true);

            Assert.Contains("Template help", result);
            Assert.Contains("USAGE:", result);
            Assert.Equal("new console --help", executedArgs);
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    [Fact]
    public async Task ValidateTemplateExistsAsync_WhenTemplateApiReturnsEmpty_UsesDotnetNewListFallback()
    {
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<ITemplateInfo>>(Array.Empty<ITemplateInfo>());

            string? executedArgs = null;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                executedArgs = args;
                return Task.FromResult("These templates matched your input: 'console'\n\n...");
            };

            var exists = await TemplateEngineHelper.ValidateTemplateExistsAsync("console", forceReload: true);

            Assert.True(exists);
            Assert.Equal("new list \"console\" --columns author --columns language --columns type --columns tags", executedArgs);
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    [Fact]
    public async Task GetInstalledTemplatesAsync_ReturnsSuccessfully()
    {
        // Act
        var result = await TemplateEngineHelper.GetInstalledTemplatesAsync();

        // Assert
        Assert.NotNull(result);
        // Note: The result may indicate no templates found if the template cache hasn't been
        // populated yet. This is expected behavior - templates are discovered when dotnet new
        // commands are first run in the environment. The important thing is that the method
        // executes without throwing exceptions.
    }

    [Fact]
    public async Task SearchTemplatesAsync_ReturnsSuccessfully()
    {
        // Act
        var result = await TemplateEngineHelper.SearchTemplatesAsync("console");

        // Assert
        Assert.NotNull(result);
        // The search should return successfully, even if no templates are found
    }

    [Fact]
    public async Task GetTemplateDetailsAsync_ReturnsSuccessfully()
    {
        // Act
        var result = await TemplateEngineHelper.GetTemplateDetailsAsync("console");

        // Assert
        Assert.NotNull(result);
        // The method should return successfully with an appropriate message
    }

    [Fact]
    public async Task ValidateTemplateExistsAsync_ReturnsBoolean()
    {
        // Act
        var result = await TemplateEngineHelper.ValidateTemplateExistsAsync("console");

        // Assert
        // Should return a boolean value without throwing - no specific assertion needed
        // The test passes if no exception is thrown
    }

    [Fact]
    public async Task ValidateTemplateExistsAsync_HandlesNonExistentTemplate()
    {
        // Act
        var result = await TemplateEngineHelper.ValidateTemplateExistsAsync("non-existent-template-xyz123");

        // Assert
        // Should return false for non-existent templates
        Assert.False(result);
    }

    [Fact]
    public async Task ClearCacheAsync_ExecutesSuccessfully()
    {
        // Arrange - Load templates into cache (or try to)
        await TemplateEngineHelper.GetInstalledTemplatesAsync();

        // Act - Clear the cache
        await TemplateEngineHelper.ClearCacheAsync();

        // Assert - Should be able to query again after clearing
        var result = await TemplateEngineHelper.GetInstalledTemplatesAsync();
        Assert.NotNull(result);
    }
}
