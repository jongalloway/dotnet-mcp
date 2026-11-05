using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class FrameworkHelperTests
{
    private static readonly DateOnly BeforeGa = new(2026, 11, 9);

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
    [InlineData("net9.0", ".NET 9.0 (STS)")]
    [InlineData("net8.0", ".NET 8.0 (LTS)")]
    [InlineData("net6.0", ".NET 6.0 (LTS, EOL)")]
    [InlineData("netcoreapp3.1", ".NET Core 3.1 (LTS, EOL)")]
    [InlineData("netstandard2.0", ".NET Standard 2.0")]
    public void GetFrameworkDescription_ReturnsCorrectDescription(string tfm, string expected)
    {
        // Act
        var result = FrameworkHelper.GetFrameworkDescription(tfm, BeforeGa);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetLatestRecommendedFramework_ReturnsNet100()
    {
        // Act
        var result = FrameworkHelper.GetLatestRecommendedFramework(BeforeGa);

        // Assert
        Assert.Equal("net10.0", result);
    }

    [Fact]
    public void GetLatestLtsFramework_ReturnsNet100()
    {
        // Act
        var result = FrameworkHelper.GetLatestLtsFramework(BeforeGa);

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

    [Theory]
    [InlineData("net11.0", "2026-11-09", "Preview", "STS", "2026-11-10", "2028-11-14")]
    [InlineData("NET11.0", "2026-11-10", "Supported", "STS", "2026-11-10", "2028-11-14")]
    [InlineData("net11.0", "2026-11-11", "Supported", "STS", "2026-11-10", "2028-11-14")]
    [InlineData("net11.0", "2028-11-14", "Supported", "STS", "2026-11-10", "2028-11-14")]
    [InlineData("net11.0", "2028-11-15", "EOL", "STS", "2026-11-10", "2028-11-14")]
    [InlineData("net10.0", "2028-11-14", "Supported", "LTS", "2025-11-11", "2028-11-14")]
    [InlineData("net10.0", "2028-11-15", "EOL", "LTS", "2025-11-11", "2028-11-14")]
    [InlineData("net9.0", "2026-11-09", "Supported", "STS", "2024-11-12", "2026-11-10")]
    [InlineData("net9.0", "2026-11-10", "Supported", "STS", "2024-11-12", "2026-11-10")]
    [InlineData("net9.0", "2026-11-11", "EOL", "STS", "2024-11-12", "2026-11-10")]
    [InlineData("net8.0", "2026-11-09", "Supported", "LTS", "2023-11-14", "2026-11-10")]
    [InlineData("net8.0", "2026-11-10", "Supported", "LTS", "2023-11-14", "2026-11-10")]
    [InlineData("net8.0", "2026-11-11", "EOL", "LTS", "2023-11-14", "2026-11-10")]
    public void GetFrameworkLifecycle_UsesExplicitInclusiveSupportDates(
        string framework, string date, string status, string type, string releaseDate, string endOfSupport)
    {
        var lifecycle = FrameworkHelper.GetFrameworkLifecycle(framework, DateOnly.Parse(date));

        Assert.NotNull(lifecycle);
        Assert.Equal(status, lifecycle.SupportStatus);
        Assert.Equal(type, lifecycle.ReleaseType);
        Assert.Equal(DateOnly.Parse(releaseDate), lifecycle.ReleaseDate);
        Assert.Equal(DateOnly.Parse(endOfSupport), lifecycle.EndOfSupport);
    }

    [Theory]
    [InlineData("net7.0", "STS", "2024-05-14")]
    [InlineData("net6.0", "LTS", "2024-11-12")]
    [InlineData("net5.0", "STS", "2022-05-10")]
    [InlineData("netcoreapp3.1", "LTS", "2022-12-13")]
    [InlineData("netcoreapp3.0", "STS", "2020-03-03")]
    [InlineData("netcoreapp2.2", "STS", "2019-12-23")]
    [InlineData("netcoreapp2.1", "LTS", "2021-08-21")]
    [InlineData("netcoreapp2.0", "STS", "2018-10-01")]
    public void GetFrameworkLifecycle_HistoricalReleasesRetainTypeButAreEol(string framework, string type, string endOfSupport)
    {
        var lifecycle = FrameworkHelper.GetFrameworkLifecycle(framework, BeforeGa);

        Assert.NotNull(lifecycle);
        Assert.Equal("EOL", lifecycle.SupportStatus);
        Assert.Equal(type, lifecycle.ReleaseType);
        Assert.Equal(DateOnly.Parse(endOfSupport), lifecycle.EndOfSupport);
        Assert.Equal(type == "LTS", FrameworkHelper.IsLtsFramework(framework));
    }

    [Theory]
    [InlineData("netstandard2.0")]
    [InlineData("net48")]
    [InlineData("net99.0")]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetFrameworkLifecycle_UnknownOrNotApplicable_ReturnsNull(string? framework)
    {
        Assert.Null(FrameworkHelper.GetFrameworkLifecycle(framework!, BeforeGa));
    }

    [Theory]
    [InlineData("2026-11-09", "net10.0")]
    [InlineData("2026-11-10", "net11.0")]
    [InlineData("2026-11-11", "net11.0")]
    [InlineData("2028-11-14", "net11.0")]
    public void Recommendations_UseSupportedGaReleases(string date, string recommended)
    {
        var asOf = DateOnly.Parse(date);
        Assert.Equal(recommended, FrameworkHelper.GetLatestRecommendedFramework(asOf));
        Assert.Equal("net10.0", FrameworkHelper.GetLatestLtsFramework(asOf));
    }

    [Fact]
    public void Recommendations_AfterAllRecordedReleasesExpire_FailExplicitly()
    {
        var asOf = new DateOnly(2028, 11, 15);
        Assert.Throws<InvalidOperationException>(() => FrameworkHelper.GetLatestRecommendedFramework(asOf));
        Assert.Throws<InvalidOperationException>(() => FrameworkHelper.GetLatestLtsFramework(asOf));
    }

    [Theory]
    [InlineData("net11.0", "2026-11-09", ".NET 11.0 (Preview)")]
    [InlineData("NET11.0", "2026-11-10", ".NET 11.0 (STS)")]
    [InlineData("net8.0", "2026-11-10", ".NET 8.0 (LTS)")]
    [InlineData("net8.0", "2026-11-11", ".NET 8.0 (LTS, EOL)")]
    [InlineData("net9.0", "2026-11-11", ".NET 9.0 (STS, EOL)")]
    [InlineData("net11.0", "2028-11-15", ".NET 11.0 (STS, EOL)")]
    [InlineData("net48", "2026-11-11", ".NET Framework 4.8")]
    [InlineData("unknown", "2026-11-11", "unknown")]
    public void Descriptions_ReflectLifecycleWithoutChangingLegacyNames(string framework, string date, string expected)
    {
        Assert.Equal(expected, FrameworkHelper.GetFrameworkDescription(framework, DateOnly.Parse(date)));
    }

    [Fact]
    public void ModernCatalog_RetainsEolEntriesAndIncludesNet11OnlyAfterGa()
    {
        Assert.DoesNotContain("net11.0", FrameworkHelper.GetModernFrameworks(BeforeGa));
        var afterGa = FrameworkHelper.GetModernFrameworks(new(2026, 11, 11));
        Assert.Equal("net11.0", afterGa[0]);
        Assert.Equal(7, afterGa.Count);
        Assert.Contains("net8.0", afterGa);
        Assert.Contains("net5.0", afterGa);
    }
}
