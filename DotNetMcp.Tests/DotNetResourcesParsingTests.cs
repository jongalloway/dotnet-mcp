using System.IO;
using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class DotNetResourcesParsingTests
{
    [Fact]
    public void ParseSdkListOutput_Empty_ReturnsEmpty()
    {
        var result = DotNetResources.ParseSdkListOutput("\n\n");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseSdkListOutput_ParsesAndSorts_WithStableAfterPrereleaseForSameBaseVersion()
    {
        var output = string.Join("\n", new[]
        {
            "11.0.100-preview.1 [C:\\Program Files\\dotnet\\sdk]",
            "10.0.101 [C:\\Program Files\\dotnet\\sdk]",
            "11.0.100 [C:\\Program Files\\dotnet\\sdk]",
        });

        var sdks = DotNetResources.ParseSdkListOutput(output);

        Assert.Equal(3, sdks.Count);
        Assert.Equal("10.0.101", sdks[0].Version);
        Assert.Equal("11.0.100-preview.1", sdks[1].Version);
        Assert.Equal("11.0.100", sdks[2].Version);

        Assert.Equal(Path.Combine("C:\\Program Files\\dotnet\\sdk", "10.0.101"), sdks[0].Path);
        Assert.Equal(Path.Combine("C:\\Program Files\\dotnet\\sdk", "11.0.100-preview.1"), sdks[1].Path);
        Assert.Equal(Path.Combine("C:\\Program Files\\dotnet\\sdk", "11.0.100"), sdks[2].Path);
    }

    [Fact]
    public void ParseRuntimeListOutput_ParsesValidLines_IgnoresInvalid()
    {
        var output = string.Join("\n", new[]
        {
            "Microsoft.NETCore.App 10.0.1 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]",
            "This is not a runtime line",
            "Microsoft.AspNetCore.App 10.0.1 [C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App]",
        });

        var runtimes = DotNetResources.ParseRuntimeListOutput(output);

        Assert.Equal(2, runtimes.Count);
        Assert.Equal("Microsoft.NETCore.App", runtimes[0].Name);
        Assert.Equal("10.0.1", runtimes[0].Version);
        Assert.Equal(Path.Combine("C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App", "10.0.1"), runtimes[0].Path);

        Assert.Equal("Microsoft.AspNetCore.App", runtimes[1].Name);
        Assert.Equal("10.0.1", runtimes[1].Version);
        Assert.Equal(Path.Combine("C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App", "10.0.1"), runtimes[1].Path);
    }

    [Fact]
    public void GetSupportedModernFrameworksForResources_AddsNet110OnlyWhenSdk11Installed()
    {
        var without11 = DotNetResources.GetSupportedModernFrameworksForResources(new[]
        {
            new SdkInfo("10.0.101", "C:\\Program Files\\dotnet\\sdk\\10.0.101"),
        });
        Assert.DoesNotContain(DotNetSdkConstants.TargetFrameworks.Net110, without11);

        var with11 = DotNetResources.GetSupportedModernFrameworksForResources(new[]
        {
            new SdkInfo("11.0.100-preview.1", "C:\\Program Files\\dotnet\\sdk\\11.0.100-preview.1"),
        });
        Assert.Contains(DotNetSdkConstants.TargetFrameworks.Net110, with11);

        // It should be inserted at the beginning when present
        Assert.Equal(DotNetSdkConstants.TargetFrameworks.Net110, with11[0]);
    }
}
