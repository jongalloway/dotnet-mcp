using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class ErrorCodeDictionaryTests
{
    [Fact]
    public void GetErrorInfo_WithValidCode_ReturnsInfo()
    {
        // Arrange
        var errorCode = "CS0103";

        // Act
        var info = ErrorCodeDictionary.GetErrorInfo(errorCode);

        // Assert
        Assert.NotNull(info);
        Assert.Equal("The name does not exist in the current context", info.Title);
        Assert.NotEmpty(info.Explanation);
        Assert.NotEmpty(info.Category);
        Assert.NotEmpty(info.CommonCauses);
        Assert.NotEmpty(info.SuggestedFixes);
        Assert.NotNull(info.DocumentationUrl);
        Assert.Contains("learn.microsoft.com", info.DocumentationUrl);
    }

    [Fact]
    public void GetErrorInfo_WithLowercaseCode_ReturnsInfo()
    {
        // Arrange
        var errorCode = "cs0103"; // lowercase

        // Act
        var info = ErrorCodeDictionary.GetErrorInfo(errorCode);

        // Assert
        Assert.NotNull(info);
        Assert.Equal("The name does not exist in the current context", info.Title);
    }

    [Fact]
    public void GetErrorInfo_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        var errorCode = "INVALID999";

        // Act
        var info = ErrorCodeDictionary.GetErrorInfo(errorCode);

        // Assert
        Assert.Null(info);
    }

    [Fact]
    public void GetErrorInfo_WithNullOrEmpty_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(ErrorCodeDictionary.GetErrorInfo(null!));
        Assert.Null(ErrorCodeDictionary.GetErrorInfo(string.Empty));
        Assert.Null(ErrorCodeDictionary.GetErrorInfo("   "));
    }

    [Fact]
    public void HasErrorInfo_WithValidCode_ReturnsTrue()
    {
        // Arrange
        var errorCode = "CS0103";

        // Act
        var hasInfo = ErrorCodeDictionary.HasErrorInfo(errorCode);

        // Assert
        Assert.True(hasInfo);
    }

    [Fact]
    public void HasErrorInfo_WithInvalidCode_ReturnsFalse()
    {
        // Arrange
        var errorCode = "INVALID999";

        // Act
        var hasInfo = ErrorCodeDictionary.HasErrorInfo(errorCode);

        // Assert
        Assert.False(hasInfo);
    }

    [Fact]
    public void Count_ReturnsPositiveNumber()
    {
        // Act
        var count = ErrorCodeDictionary.Count;

        // Assert
        Assert.True(count > 0);
        // We should have at least 20 error codes based on the requirements (currently 52 unique error codes)
        Assert.True(count >= 20);
    }

    [Theory]
    [InlineData("CS0103")] // Compiler error
    [InlineData("CS0246")]
    [InlineData("CS1001")]
    [InlineData("MSB3644")] // MSBuild error
    [InlineData("MSB4236")]
    [InlineData("MSB1003")]
    [InlineData("NU1101")] // NuGet error
    [InlineData("NU1102")]
    [InlineData("NU1103")]
    [InlineData("NETSDK1045")] // SDK error
    [InlineData("NETSDK1004")]
    public void GetErrorInfo_CommonErrors_HaveCompleteInfo(string errorCode)
    {
        // Act
        var info = ErrorCodeDictionary.GetErrorInfo(errorCode);

        // Assert
        Assert.NotNull(info);
        Assert.False(string.IsNullOrWhiteSpace(info.Title), $"{errorCode} should have a title");
        Assert.False(string.IsNullOrWhiteSpace(info.Explanation), $"{errorCode} should have an explanation");
        Assert.False(string.IsNullOrWhiteSpace(info.Category), $"{errorCode} should have a category");
        Assert.NotEmpty(info.CommonCauses);
        Assert.NotEmpty(info.SuggestedFixes);
        Assert.False(string.IsNullOrWhiteSpace(info.DocumentationUrl), $"{errorCode} should have a documentation URL");
    }

    [Fact]
    public void GetErrorInfo_CS0103_HasCorrectDetails()
    {
        // Act
        var info = ErrorCodeDictionary.GetErrorInfo("CS0103");

        // Assert
        Assert.NotNull(info);
        Assert.Equal("Compilation", info.Category);
        Assert.Contains("typo", info.SuggestedFixes[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(info.SuggestedFixes, fix => fix.Contains("using", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetErrorInfo_MSB3644_HasInstallationSuggestion()
    {
        // Act
        var info = ErrorCodeDictionary.GetErrorInfo("MSB3644");

        // Assert
        Assert.NotNull(info);
        Assert.Equal("Build", info.Category);
        Assert.Contains(info.SuggestedFixes, fix => fix.Contains("Install", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(info.SuggestedFixes, fix => fix.Contains("SDK", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetErrorInfo_NU1101_HasPackageSearchSuggestion()
    {
        // Act
        var info = ErrorCodeDictionary.GetErrorInfo("NU1101");

        // Assert
        Assert.NotNull(info);
        Assert.Equal("Package", info.Category);
        Assert.Contains(info.SuggestedFixes, fix => fix.Contains("search", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(info.SuggestedFixes, fix => fix.Contains("package", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetErrorInfo_NETSDK1045_HasSDKUpdateSuggestion()
    {
        // Act
        var info = ErrorCodeDictionary.GetErrorInfo("NETSDK1045");

        // Assert
        Assert.NotNull(info);
        Assert.Equal("SDK", info.Category);
        Assert.Contains(info.SuggestedFixes, fix => fix.Contains("SDK", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(info.DocumentationUrl);
        Assert.Contains("microsoft.com", info.DocumentationUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetErrorInfo_AllEntries_HaveValidUrls()
    {
        // Arrange
        var errorCodes = new[] 
        { 
            "CS0103", "CS0246", "CS1001", "CS1002", "CS1513",
            "MSB3644", "MSB4236", "MSB1003",
            "NU1101", "NU1102", "NU1103", "NU1605",
            "NETSDK1045", "NETSDK1004", "NETSDK1005"
        };

        foreach (var code in errorCodes)
        {
            // Act
            var info = ErrorCodeDictionary.GetErrorInfo(code);

            // Assert
            Assert.NotNull(info);
            Assert.NotNull(info.DocumentationUrl);
            Assert.StartsWith("https://", info.DocumentationUrl);
            Assert.Contains("microsoft.com", info.DocumentationUrl, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GetErrorInfo_AllEntries_HaveMultipleSuggestedFixes()
    {
        // Arrange - Test a sample of different error types
        var errorCodes = new[] 
        { 
            "CS0103", "MSB3644", "NU1101", "NETSDK1045"
        };

        foreach (var code in errorCodes)
        {
            // Act
            var info = ErrorCodeDictionary.GetErrorInfo(code);

            // Assert
            Assert.NotNull(info);
            Assert.True(info.SuggestedFixes.Count >= 3, 
                $"{code} should have at least 3 suggested fixes, but has {info.SuggestedFixes.Count}");
        }
    }

    [Fact]
    public void GetErrorInfo_CS0246_HasNuGetSuggestion()
    {
        // Act
        var info = ErrorCodeDictionary.GetErrorInfo("CS0246");

        // Assert
        Assert.NotNull(info);
        Assert.Contains(info.SuggestedFixes, fix => 
            fix.Contains("dotnet add package", StringComparison.OrdinalIgnoreCase) ||
            fix.Contains("NuGet", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetErrorInfo_NETSDK1004_HasRestoreSuggestion()
    {
        // Act
        var info = ErrorCodeDictionary.GetErrorInfo("NETSDK1004");

        // Assert
        Assert.NotNull(info);
        Assert.Contains(info.SuggestedFixes, fix => 
            fix.Contains("dotnet restore", StringComparison.OrdinalIgnoreCase));
    }
}
