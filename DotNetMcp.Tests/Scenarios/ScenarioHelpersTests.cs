using System.Text.Json;
using ModelContextProtocol.Protocol;
using Xunit.Sdk;

namespace DotNetMcp.Tests.Scenarios;

public class ScenarioHelpersTests
{
    [Theory]
    [InlineData(null, "https://api.nuget.org/v3/index.json")]
    [InlineData("https://packages.example.test/v3/index.json", "https://packages.example.test/v3/index.json")]
    public void PublicPackageSource_PreservesDefaultAndExplicitFeed(string? source, string expected) =>
        Assert.Equal(expected, ScenarioHelpers.ResolvePublicPackageSource(source));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-feed")]
    [InlineData("file:///C:/feed")]
    public void PublicPackageSource_RejectsInvalidOverrideWithoutFallback(string source) =>
        Assert.Throws<InvalidOperationException>(() => ScenarioHelpers.ResolvePublicPackageSource(source));

    [Fact]
    public void AssertBuildSuccess_AcceptsStructuredSuccessWithoutExitCodeText()
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "Build succeeded. 0 error(s), 0 warning(s)." }],
            StructuredContent = JsonSerializer.SerializeToElement(new { success = true, errorCount = 0 })
        };

        ScenarioHelpers.AssertBuildSuccess(result);
    }

    [Theory]
    [InlineData(true, """{"success":true,"errorCount":0}""")]
    [InlineData(false, """{"success":false,"errorCount":1}""")]
    [InlineData(false, """{"success":true,"errorCount":1}""")]
    [InlineData(false, """{"success":"true","errorCount":0}""")]
    [InlineData(false, """{"errorCount":0}""")]
    [InlineData(false, null)]
    public void AssertBuildSuccess_RejectsErrorsOrMissingSuccess(bool isError, string? json)
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "Build succeeded. 0 error(s), 0 warning(s)." }],
            IsError = isError,
            StructuredContent = json is null ? null : JsonSerializer.Deserialize<JsonElement>(json)
        };

        Assert.ThrowsAny<XunitException>(() => ScenarioHelpers.AssertBuildSuccess(result));
    }
}
