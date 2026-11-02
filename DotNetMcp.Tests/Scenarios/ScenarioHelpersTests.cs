using System.Text.Json;
using ModelContextProtocol.Protocol;
using Xunit.Sdk;

namespace DotNetMcp.Tests.Scenarios;

public class ScenarioHelpersTests
{
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
