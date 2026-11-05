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
        }, new DateOnly(2026, 11, 9));

        Assert.Equal(1, frameworks.Count(fw => fw == DotNetSdkConstants.TargetFrameworks.Net110));
        Assert.Equal(DotNetSdkConstants.TargetFrameworks.Net110, frameworks[0]);
    }

    [Theory]
    [InlineData("2026-11-09", false)]
    [InlineData("2026-11-10", true)]
    [InlineData("2026-11-11", true)]
    public void ModernFrameworks_StableNet11DoesNotRequireInstalledSdk(string date, bool includesNet11)
    {
        var frameworks = DotNetResources.GetSupportedModernFrameworksForResources(
            [new SdkInfo("10.0.101", "sdk")], DateOnly.Parse(date));
        Assert.Equal(includesNet11, frameworks.Contains("net11.0"));
        Assert.Equal(frameworks.Count, frameworks.Distinct().Count());
        Assert.Contains("net6.0", frameworks);
    }

    [Theory]
    [InlineData("2026-11-09", "Preview", "Supported", "net10.0")]
    [InlineData("2026-11-10", "Supported", "Supported", "net11.0")]
    [InlineData("2026-11-11", "Supported", "EOL", "net11.0")]
    public void FrameworkResource_SerializesDateAwareLifecycleAndRecommendations(
        string date, string net11Status, string net8Status, string recommended)
    {
        var response = DotNetResources.DescribeFrameworks(
            [new SdkInfo("11.0.100-rc.3", "sdk"), new SdkInfo("11.0.100", "sdk")], DateOnly.Parse(date));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, ServerJsonContext.Default.FrameworkResourceData));
        var root = json.RootElement;
        var frameworks = root.GetProperty("modernFrameworks").EnumerateArray().ToArray();
        var net11 = Assert.Single(frameworks, fw => fw.GetProperty("tfm").GetString() == "net11.0");
        Assert.Equal("STS", net11.GetProperty("releaseType").GetString());
        Assert.False(net11.GetProperty("isLts").GetBoolean());
        Assert.Equal(net11Status, net11.GetProperty("supportStatus").GetString());
        Assert.Equal("2026-11-10", net11.GetProperty("releaseDate").GetString());
        Assert.Equal("2028-11-14", net11.GetProperty("endOfSupport").GetString());
        Assert.Equal(net8Status, frameworks.Single(fw => fw.GetProperty("tfm").GetString() == "net8.0")
            .GetProperty("supportStatus").GetString());
        Assert.Equal(recommended, root.GetProperty("latestRecommended").GetString());
        Assert.Equal("net10.0", root.GetProperty("latestLts").GetString());
        Assert.All(root.GetProperty("netCoreFrameworks").EnumerateArray(),
            fw => Assert.Equal("EOL", fw.GetProperty("supportStatus").GetString()));
        Assert.All(root.GetProperty("netStandardFrameworks").EnumerateArray(),
            fw => Assert.False(fw.TryGetProperty("supportStatus", out _)));
    }

    [Fact]
    public void FrameworkResource_ProductionSerializationPreservesUnknownLifecycleAsNull()
    {
        var response = DotNetResources.DescribeFrameworks([], new DateOnly(2026, 11, 11));
        using var json = JsonDocument.Parse(ServerJson.SerializeResource(response));
        var modern = json.RootElement.GetProperty("modernFrameworks").EnumerateArray().ToArray();
        Assert.Equal("net11.0", modern[0].GetProperty("tfm").GetString());
        Assert.Equal("Supported", modern[0].GetProperty("supportStatus").GetString());
        Assert.Equal("2028-11-14", modern[0].GetProperty("endOfSupport").GetString());
        Assert.All(json.RootElement.GetProperty("netStandardFrameworks").EnumerateArray(), framework =>
        {
            foreach (var field in new[] { "releaseType", "supportStatus", "releaseDate", "endOfSupport" })
                Assert.Equal(JsonValueKind.Null, framework.GetProperty(field).ValueKind);
        });
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