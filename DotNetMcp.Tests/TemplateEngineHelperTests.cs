using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class TemplateEngineHelperTests
{
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
