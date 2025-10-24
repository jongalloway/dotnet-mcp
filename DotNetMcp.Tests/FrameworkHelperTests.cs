using DotNetMcp;
using FluentAssertions;
using Xunit;

namespace DotNetMcp.Tests;

public class FrameworkHelperTests
{
    [Theory]
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
        result.Should().Be(expected);
    }

    [Theory]
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
        result.Should().Be(expected);
    }

    [Theory]
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
        result.Should().Be(expected);
    }

    [Fact]
    public void GetLatestRecommendedFramework_ReturnsNet90()
    {
        // Act
        var result = FrameworkHelper.GetLatestRecommendedFramework();

        // Assert
        result.Should().Be("net9.0");
    }

    [Fact]
    public void GetLatestLtsFramework_ReturnsNet80()
    {
        // Act
        var result = FrameworkHelper.GetLatestLtsFramework();

        // Assert
        result.Should().Be("net8.0");
    }

    [Theory]
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
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("netcoreapp3.1", true)]
    [InlineData("netcoreapp2.1", true)]
    [InlineData("net9.0", false)]
    [InlineData("netstandard2.0", false)]
    public void IsNetCore_ClassifiesCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsNetCore(tfm);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("net48", true)]
    [InlineData("net472", true)]
    [InlineData("net9.0", false)]
    [InlineData("netcoreapp3.1", false)]
    public void IsNetFramework_ClassifiesCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsNetFramework(tfm);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("netstandard2.1", true)]
    [InlineData("netstandard2.0", true)]
    [InlineData("net9.0", false)]
    [InlineData("netcoreapp3.1", false)]
    public void IsNetStandard_ClassifiesCorrectly(string tfm, bool expected)
    {
        // Act
        var result = FrameworkHelper.IsNetStandard(tfm);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetSupportedModernFrameworks_ReturnsExpectedCount()
    {
        // Act
        var result = FrameworkHelper.GetSupportedModernFrameworks();

        // Assert
        result.Count.Should().BeGreaterThanOrEqualTo(5);
        result.Should().Contain("net9.0");
        result.Should().Contain("net8.0");
    }

    [Fact]
    public void GetSupportedNetCoreFrameworks_ReturnsExpectedFrameworks()
    {
        // Act
        var result = FrameworkHelper.GetSupportedNetCoreFrameworks();

        // Assert
        result.Should().Contain("netcoreapp3.1");
        result.Should().Contain("netcoreapp2.1");
    }

    [Fact]
    public void GetSupportedNetStandardFrameworks_ReturnsExpectedFrameworks()
    {
        // Act
        var result = FrameworkHelper.GetSupportedNetStandardFrameworks();

        // Assert
        result.Should().Contain("netstandard2.1");
        result.Should().Contain("netstandard2.0");
    }
}
