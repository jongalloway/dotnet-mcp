using DotNetMcp;
using FluentAssertions;
using Xunit;

namespace DotNetMcp.Tests;

public class TemplateEngineHelperTests
{
    [Fact]
    public async Task GetInstalledTemplatesAsync_ReturnsTemplates()
    {
        // Act
        var result = await TemplateEngineHelper.GetInstalledTemplatesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("No templates found");
        result.Should().Contain("Installed .NET Templates:");
        result.Should().Contain("Total templates:");
        
        // Verify that at least common templates are present
        // These are part of the default .NET SDK installation
        result.Should().Contain("console");
        result.Should().Contain("classlib");
    }

    [Fact]
    public async Task SearchTemplatesAsync_FindsConsoleTemplate()
    {
        // Act
        var result = await TemplateEngineHelper.SearchTemplatesAsync("console");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("No templates found");
        result.Should().Contain("console");
    }

    [Fact]
    public async Task GetTemplateDetailsAsync_ReturnsDetailsForConsoleTemplate()
    {
        // Act
        var result = await TemplateEngineHelper.GetTemplateDetailsAsync("console");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("not found");
        result.Should().Contain("console");
        result.Should().Contain("Short Name");
    }

    [Fact]
    public async Task ValidateTemplateExistsAsync_ReturnsTrueForConsoleTemplate()
    {
        // Act
        var result = await TemplateEngineHelper.ValidateTemplateExistsAsync("console");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTemplateExistsAsync_ReturnsFalseForNonExistentTemplate()
    {
        // Act
        var result = await TemplateEngineHelper.ValidateTemplateExistsAsync("non-existent-template-xyz123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ClearCacheAsync_ClearsTheCache()
    {
        // Arrange - Load templates into cache
        await TemplateEngineHelper.GetInstalledTemplatesAsync();

        // Act - Clear the cache
        await TemplateEngineHelper.ClearCacheAsync();

        // Assert - Should still be able to reload templates after clearing
        var result = await TemplateEngineHelper.GetInstalledTemplatesAsync();
        result.Should().NotContain("No templates found");
        result.Should().Contain("Total templates:");
    }
}
