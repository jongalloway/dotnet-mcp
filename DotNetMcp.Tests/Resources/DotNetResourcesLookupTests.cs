using System.Text.Json;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

public class DotNetResourcesLookupTests
{
    [Fact]
    public void ParseSdkListOutput_IgnoresLinesWithoutBracketDelimiter()
    {
        var output = string.Join("\n", new[]
        {
            "garbage line without path",
            "10.0.101 [C:\\Program Files\\dotnet\\sdk]",
            "also invalid"
        });

        var sdks = DotNetResources.ParseSdkListOutput(output);

        Assert.Single(sdks);
        Assert.Equal("10.0.101", sdks[0].Version);
    }

    [Fact]
    public void ParseSdkListOutput_ParsesPreviewBuildMetadataPathCorrectly()
    {
        var output = "11.0.100-alpha.1+abcdef [C:\\Program Files\\dotnet\\sdk]";

        var sdks = DotNetResources.ParseSdkListOutput(output);

        Assert.Single(sdks);
        Assert.Equal("11.0.100-alpha.1+abcdef", sdks[0].Version);
        Assert.Equal(Path.Join("C:\\Program Files\\dotnet\\sdk", "11.0.100-alpha.1+abcdef"), sdks[0].Path);
    }

    [Fact]
    public void ParseRuntimeListOutput_Empty_ReturnsEmpty()
    {
        var runtimes = DotNetResources.ParseRuntimeListOutput(string.Empty);

        Assert.Empty(runtimes);
    }

    [Fact]
    public void ParseRuntimeListOutput_IgnoresLinesMissingVersionSegment()
    {
        var output = string.Join("\n", new[]
        {
            "Microsoft.NETCore.App [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]",
            "Microsoft.AspNetCore.App 10.0.1 [C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App]"
        });

        var runtimes = DotNetResources.ParseRuntimeListOutput(output);

        Assert.Single(runtimes);
        Assert.Equal("Microsoft.AspNetCore.App", runtimes[0].Name);
        Assert.Equal("10.0.1", runtimes[0].Version);
    }

    [Fact]
    public void GetSupportedModernFrameworksForResources_WithMultipleSdk11s_AddsNet110Once()
    {
        var frameworks = DotNetResources.GetSupportedModernFrameworksForResources(new[]
        {
            new SdkInfo("11.0.100-preview.1", "C:\\dotnet\\sdk\\11.0.100-preview.1"),
            new SdkInfo("11.0.100", "C:\\dotnet\\sdk\\11.0.100"),
            new SdkInfo("10.0.101", "C:\\dotnet\\sdk\\10.0.101")
        });

        Assert.Equal(1, frameworks.Count(fw => fw == DotNetSdkConstants.TargetFrameworks.Net110));
        Assert.Equal(DotNetSdkConstants.TargetFrameworks.Net110, frameworks[0]);
    }

    [Fact]
    public async Task GetTelemetryData_ReturnsCapabilityNotAvailablePayloadWithAlternatives()
    {
        var resources = new DotNetResources(NullLogger<DotNetResources>.Instance);

        var json = await resources.GetTelemetryData();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());

        var error = root.GetProperty("errors")[0];
        Assert.Equal("CAPABILITY_NOT_AVAILABLE", error.GetProperty("code").GetString());
        Assert.Contains("telemetry data resource", error.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);

        var alternatives = error.GetProperty("alternatives").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains(alternatives, alt => alt != null && alt.Contains("dotnet://sdk-info", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(alternatives, alt => alt != null && alt.Contains("dotnet://runtime-info", StringComparison.OrdinalIgnoreCase));

        var data = error.GetProperty("data");
        var additionalData = data.GetProperty("additionalData");
        Assert.Equal("telemetry data resource", additionalData.GetProperty("feature").GetString());
    }
}