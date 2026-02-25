using System.Text.Json;
using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests verifying that migrated tools return proper StructuredContent in CallToolResult.
/// </summary>
public class StructuredContentTests
{
    private readonly DotNetCliTools _tools;

    public StructuredContentTests()
    {
        _tools = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager());
    }

    [Fact]
    public async Task DotnetSdk_Version_ReturnsStructuredContent()
    {
        var result = await _tools.DotnetSdk(action: DotnetSdkAction.Version);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.True(result.StructuredContent.HasValue);
        var structured = result.StructuredContent!.Value;
        Assert.True(structured.TryGetProperty("version", out var versionProp));
        Assert.Equal(JsonValueKind.String, versionProp.ValueKind);
    }

    [Fact]
    public async Task DotnetSdk_ListSdks_ReturnsStructuredContent()
    {
        var result = await _tools.DotnetSdk(action: DotnetSdkAction.ListSdks);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.True(result.StructuredContent.HasValue);
        var structured = result.StructuredContent!.Value;
        Assert.True(structured.TryGetProperty("sdks", out var sdksProp));
        Assert.Equal(JsonValueKind.Array, sdksProp.ValueKind);
    }

    [Fact]
    public async Task DotnetSdk_ListRuntimes_ReturnsStructuredContent()
    {
        var result = await _tools.DotnetSdk(action: DotnetSdkAction.ListRuntimes);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.True(result.StructuredContent.HasValue);
        var structured = result.StructuredContent!.Value;
        Assert.True(structured.TryGetProperty("runtimes", out var runtimesProp));
        Assert.Equal(JsonValueKind.Array, runtimesProp.ValueKind);
    }

    [Fact]
    public async Task DotnetSdk_OtherActions_ReturnNoStructuredContent()
    {
        var result = await _tools.DotnetSdk(action: DotnetSdkAction.Info);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        // Non-structured actions should not have structured content
        Assert.False(result.StructuredContent.HasValue);
    }

    [Fact]
    public async Task DotnetSdk_TextContent_IsNotEmpty()
    {
        var result = (await _tools.DotnetSdk(action: DotnetSdkAction.Version)).GetText();
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetServerCapabilities_ReturnsStructuredContent()
    {
        var result = await _tools.DotnetServerCapabilities();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.True(result.StructuredContent.HasValue);
        var structured = result.StructuredContent!.Value;
        Assert.True(structured.TryGetProperty("serverVersion", out _));
    }

    [Fact]
    public async Task DotnetSolution_List_ReturnsCallToolResult()
    {
        // The list action should return a CallToolResult even on error
        var result = await _tools.DotnetSolution(
            action: DotnetSolutionAction.List,
            solution: "nonexistent.sln");
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetPackage_List_ReturnsCallToolResult()
    {
        // The list action should return a CallToolResult
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.List);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task GetText_ExtensionMethod_ReturnsTextFromCallToolResult()
    {
        var result = (await _tools.DotnetSdk(action: DotnetSdkAction.Version)).GetText();
        var text = result;
        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task DotnetSdk_Version_StructuredContent_HasValidVersionString()
    {
        var result = await _tools.DotnetSdk(action: DotnetSdkAction.Version);
        Assert.True(result.StructuredContent.HasValue);
        var version = result.StructuredContent!.Value.GetProperty("version").GetString();
        Assert.NotNull(version);
        Assert.Matches(@"\d+\.\d+", version!);
    }

    [Fact]
    public async Task DotnetSdk_ListSdks_StructuredContent_SdksIsNonEmpty()
    {
        var result = await _tools.DotnetSdk(action: DotnetSdkAction.ListSdks);
        Assert.True(result.StructuredContent.HasValue);
        var sdks = result.StructuredContent!.Value.GetProperty("sdks");
        Assert.True(sdks.GetArrayLength() > 0);
    }

    [Fact]
    public async Task DotnetSdk_ListRuntimes_StructuredContent_RuntimesIsNonEmpty()
    {
        var result = await _tools.DotnetSdk(action: DotnetSdkAction.ListRuntimes);
        Assert.True(result.StructuredContent.HasValue);
        var runtimes = result.StructuredContent!.Value.GetProperty("runtimes");
        Assert.True(runtimes.GetArrayLength() > 0);
    }

    [Fact]
    public async Task DotnetSolution_List_StructuredContent_HasProjectsProperty()
    {
        // Use a real solution file - find it relative to the test assembly location
        var slnPath = Path.GetFullPath(
            Path.Join(AppContext.BaseDirectory, "..", "..", "..", "..", "DotNetMcp.slnx"));
        Assert.True(File.Exists(slnPath), $"Expected solution file to exist at: {slnPath}");

        var result = await _tools.DotnetSolution(
            action: DotnetSolutionAction.List,
            solution: slnPath);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.True(result.StructuredContent.HasValue);
        var structured = result.StructuredContent!.Value;
        Assert.True(structured.TryGetProperty("projects", out var projects));
        Assert.Equal(JsonValueKind.Array, projects.ValueKind);
    }

    [Fact]
    public void StructuredContentHelper_ToCallToolResult_WithoutStructured_HasNoStructuredContent()
    {
        var callResult = StructuredContentHelper.ToCallToolResult("hello", null);
        Assert.False(callResult.StructuredContent.HasValue);
        Assert.Equal("hello", callResult.GetText());
    }

    [Fact]
    public void StructuredContentHelper_ToCallToolResult_WithStructured_HasStructuredContent()
    {
        var callResult = StructuredContentHelper.ToCallToolResult("hello", new { key = "value" });
        Assert.True(callResult.StructuredContent.HasValue);
        Assert.Equal("hello", callResult.GetText());
        Assert.True(callResult.StructuredContent!.Value.TryGetProperty("key", out var keyProp));
        Assert.Equal("value", keyProp.GetString());
    }
}
