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
    public async Task ValidateTemplateExistsAsync_WhenApiReturnsEmpty_AndCliFallbackSucceeds_ReturnsTrue()
    {
        // This test verifies the fix for the classlib template issue
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            // Arrange: Simulate Template Engine API returning empty (the problem scenario)
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<ITemplateInfo>>(Array.Empty<ITemplateInfo>());
            
            // Arrange: Simulate CLI fallback succeeding (exit code 0 = template found)
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                // Simulate successful dotnet new list output (exit code 0)
                return Task.FromResult("These templates matched your input: 'classlib'\n\nTemplate Name  Short Name  ...");
            };

            // Act
            var exists = await TemplateEngineHelper.ValidateTemplateExistsAsync("classlib", forceReload: true);

            // Assert
            Assert.True(exists, "Template should be found via CLI fallback when API returns empty");
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    [Fact]
    public async Task ValidateTemplateExistsAsync_WhenApiReturnsEmpty_AndCliFallbackFailsWithExit103_ReturnsFalse()
    {
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            // Arrange: Simulate Template Engine API returning empty
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<ITemplateInfo>>(Array.Empty<ITemplateInfo>());
            
            // Arrange: Simulate CLI fallback failing (exit code 103 = template not found)
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                // ExecuteCommandForResourceAsync throws InvalidOperationException on non-zero exit code
                throw new InvalidOperationException("dotnet command failed: No templates found matching: 'nonexistent'");
            };

            // Act
            var exists = await TemplateEngineHelper.ValidateTemplateExistsAsync("nonexistent", forceReload: true);

            // Assert
            Assert.False(exists, "Template should not be found when CLI returns exit code 103");
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
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

    [Fact]
    public async Task ValidateTemplateExistsAsync_WhenCliReturnsErrorPrefixedOutput_ReturnsFalse()
    {
        // This test verifies that when the CLI returns exit code 0 but emits "Error:" to stdout,
        // the validation treats it as a failure to avoid false positives
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            // Arrange: Simulate Template Engine API returning empty
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<ITemplateInfo>>(Array.Empty<ITemplateInfo>());
            
            // Arrange: Simulate CLI returning error-prefixed output (exit code 0 but contains error)
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                // In some CI environments, dotnet new can emit errors to stdout while returning exit code 0
                return Task.FromResult("Error: Something went wrong but exit code was 0\nTemplate Name  Short Name  ...");
            };

            // Act
            var exists = await TemplateEngineHelper.ValidateTemplateExistsAsync("badtemplate", forceReload: true);

            // Assert
            Assert.False(exists, "Template validation should fail when CLI output starts with 'Error:' even if exit code is 0");
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    #region SanitizeDotnetNewOutput Tests

    [Fact]
    public void SanitizeDotnetNewOutput_WhenOutputIsNull_ReturnsNull()
    {
        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WhenOutputIsWhitespace_ReturnsWhitespace()
    {
        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput("   \t  ");

        // Assert
        Assert.Equal("   \t  ", result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WhenOutputHasNoErrorLines_ReturnsOriginal()
    {
        // Arrange
        var output = "Template Name    Short Name\nConsole App      console\nClass Library    classlib";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal(output, result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WhenOutputHasLeadingErrorsFollowedByContent_StripsErrors()
    {
        // Arrange
        var output = "Error: Something went wrong\nError: Another error\nTemplate Name    Short Name\nConsole App      console";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal("Template Name    Short Name\nConsole App      console", result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WhenOutputIsEntirelyErrors_ReturnsOriginal()
    {
        // Arrange
        var output = "Error: Something went wrong\nError: Another error\nError: Yet another error";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal(output, result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WithCrlfLineEndings_HandlesCorrectly()
    {
        // Arrange
        var output = "Error: Something went wrong\r\nError: Another error\r\nTemplate Name    Short Name\r\nConsole App      console";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal("Template Name    Short Name\nConsole App      console", result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WithMixedLineEndings_HandlesCorrectly()
    {
        // Arrange
        var output = "Error: Something went wrong\r\nError: Another error\nTemplate Name    Short Name\r\nConsole App      console";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal("Template Name    Short Name\nConsole App      console", result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WhenErrorsFollowedByBlankLines_StripsErrorsAndBlankLines()
    {
        // Arrange
        var output = "Error: Something went wrong\nError: Another error\n\n\nTemplate Name    Short Name";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal("Template Name    Short Name", result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WhenErrorsFollowedByOnlyWhitespace_ReturnsOriginal()
    {
        // Arrange
        var output = "Error: Something went wrong\nError: Another error\n   \n\t";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal(output, result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_CaseInsensitiveErrorDetection_StripsErrors()
    {
        // Arrange
        var output = "error: lowercase error\nERROR: uppercase error\nTemplate Name    Short Name";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal("Template Name    Short Name", result);
    }

    [Fact]
    public void SanitizeDotnetNewOutput_WhenErrorInMiddleOfOutput_DoesNotStrip()
    {
        // Only leading errors should be stripped
        // Arrange
        var output = "Template Name    Short Name\nError: Something in the middle\nConsole App      console";

        // Act
        var result = TemplateEngineHelper.SanitizeDotnetNewOutput(output);

        // Assert
        Assert.Equal(output, result);
    }

    #endregion
}
