using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class FrameworkHelperTests
{
    [Theory]
    [InlineData("net11.0", true)]
    [InlineData("net10.0", true)]
    [InlineData("net9.0", true)]
    [InlineData("net8.0", true)]
    [InlineData("net7.0", true)]
    [InlineData("netcoreapp3.1", true)]
    [InlineData("netstandard2.0", true)]
    [InlineData("netstandard2.1", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void IsValidFramework_ValidatesCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsValidFramework(tfm);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("net11.0", false)]
    [InlineData("net10.0", true)]
    [InlineData("net8.0", true)]
    [InlineData("net6.0", true)]
    [InlineData("netcoreapp3.1", true)]
    [InlineData("netcoreapp2.1", true)]
    [InlineData("net9.0", false)]
    [InlineData("net7.0", false)]
    [InlineData("net5.0", false)]
    public void IsLtsFramework_IdentifiesLtsCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsLtsFramework(tfm);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("net11.0", ".NET 11.0 (Preview)")]
    [InlineData("net10.0", ".NET 10.0 (LTS)")]
    [InlineData("net9.0", ".NET 9.0")]
    [InlineData("net8.0", ".NET 8.0 (LTS)")]
    [InlineData("net6.0", ".NET 6.0 (LTS)")]
    [InlineData("netcoreapp3.1", ".NET Core 3.1 (LTS)")]
    [InlineData("netstandard2.0", ".NET Standard 2.0")]
    public void GetFrameworkDescription_ReturnsCorrectDescription(string tfm, string expected)
    {
        // Act
        var result = FrameworkHelper.GetFrameworkDescription(tfm);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetLatestRecommendedFramework_ReturnsNet100()
    {
        // Act
        var result = FrameworkHelper.GetLatestRecommendedFramework();

        // Assert
        Assert.Equal("net10.0", result);
    }

    [Fact]
    public void GetLatestLtsFramework_ReturnsNet100()
    {
        // Act
        var result = FrameworkHelper.GetLatestLtsFramework();

        // Assert
        Assert.Equal("net10.0", result);
    }

    [Theory]
    [InlineData("net11.0", true)]
    [InlineData("net10.0", true)]
    [InlineData("net9.0", true)]
    [InlineData("net8.0", true)]
    [InlineData("net5.0", true)]
    [InlineData("netcoreapp3.1", false)]
    [InlineData("netstandard2.0", false)]
    [InlineData("net48", false)]
    public void IsModernNet_ClassifiesCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsModernNet(tfm);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("netcoreapp3.1", true)]
    [InlineData("netcoreapp2.1", true)]
    [InlineData("net11.0", false)]
    [InlineData("net10.0", false)]
    [InlineData("net9.0", false)]
    [InlineData("netstandard2.0", false)]
    public void IsNetCore_ClassifiesCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsNetCore(tfm);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("net48", true)]
    [InlineData("net472", true)]
    [InlineData("net11.0", false)]
    [InlineData("net10.0", false)]
    [InlineData("net9.0", false)]
    [InlineData("netcoreapp3.1", false)]
    public void IsNetFramework_ClassifiesCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsNetFramework(tfm);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("netstandard2.1", true)]
    [InlineData("netstandard2.0", true)]
    [InlineData("net11.0", false)]
    [InlineData("net10.0", false)]
    [InlineData("net9.0", false)]
    [InlineData("netcoreapp3.1", false)]
    public void IsNetStandard_ClassifiesCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsNetStandard(tfm);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSupportedModernFrameworks_ReturnsExpectedCount()
    {
        // Act
        var result = FrameworkHelper.GetSupportedModernFrameworks();

        // Assert
        Assert.True(result.Count >= 5);
        Assert.Contains("net10.0", result);
        Assert.Contains("net9.0", result);
        Assert.Contains("net8.0", result);
    }

    [Fact]
    public void GetSupportedNetCoreFrameworks_ReturnsExpectedFrameworks()
    {
        // Act
        var result = FrameworkHelper.GetSupportedNetCoreFrameworks();

        // Assert
        Assert.Contains("netcoreapp3.1", result);
        Assert.Contains("netcoreapp2.1", result);
    }

    [Fact]
    public void GetSupportedNetStandardFrameworks_ReturnsExpectedFrameworks()
    {
        // Act
        var result = FrameworkHelper.GetSupportedNetStandardFrameworks();

        // Assert
        Assert.Contains("netstandard2.1", result);
        Assert.Contains("netstandard2.0", result);
    }
}
