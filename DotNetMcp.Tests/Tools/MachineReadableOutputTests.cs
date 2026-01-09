using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests;

public class MachineReadableOutputTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public MachineReadableOutputTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetSdkVersion_WithMachineReadableFalse_ReturnsPlainText()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Version, machineReadable: false);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Exit Code:", result);
        
        // Should not be valid JSON
        var isJson = TryParseJson(result, out _);
        Assert.False(isJson);
    }

    [Fact]
    public async Task DotnetSdkVersion_WithMachineReadableTrue_ReturnsValidJson()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Version, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Should be valid JSON
        var isJson = TryParseJson(result, out var jsonDoc);
        Assert.True(isJson);
        
        // Verify JSON structure
        var root = jsonDoc!.RootElement;
        Assert.True(root.TryGetProperty("success", out var successProp));
        Assert.True(root.TryGetProperty("exitCode", out var exitCodeProp));
        
        // If successful, should have output property
        if (successProp.GetBoolean())
        {
            Assert.True(root.TryGetProperty("output", out _));
        }
        else
        {
            // If failed, should have errors array
            Assert.True(root.TryGetProperty("errors", out _));
        }
    }

    [Fact]
    public async Task DotnetProjectBuild_WithInvalidProject_MachineReadableTrue_ReturnsStructuredError()
    {
        // Arrange
        var nonExistentProject = "/tmp/NonExistent_Project_12345.csproj";

        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Build,
            project: nonExistentProject,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var isJson = TryParseJson(result, out var jsonDoc);
        Assert.True(isJson);
        
        var root = jsonDoc!.RootElement;
        Assert.True(root.TryGetProperty("success", out var successProp));
        Assert.False(successProp.GetBoolean());
        
        Assert.True(root.TryGetProperty("exitCode", out var exitCodeProp));
        Assert.NotEqual(0, exitCodeProp.GetInt32());
        
        Assert.True(root.TryGetProperty("errors", out var errorsProp));
        Assert.Equal(JsonValueKind.Array, errorsProp.ValueKind);
        Assert.True(errorsProp.GetArrayLength() > 0);
    }

    [Fact]
    public async Task DotnetPackageSearch_WithMachineReadableTrue_ReturnsValidJson()
    {
        // Act
        var result = await _tools.DotnetPackage(
            action: DotNetMcp.Actions.DotnetPackageAction.Search,
            searchTerm: "Newtonsoft.Json",
            take: 1,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var isJson = TryParseJson(result, out _);
        Assert.True(isJson);
    }

    [Fact]
    public async Task DotnetProjectNew_WithMachineReadableFalse_ReturnsPlainTextByDefault()
    {
        // Act - using default machineReadable parameter (should be false)
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.New,
            template: "console",
            name: "TestApp",
            output: "/tmp/test-output-" + Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Default behavior should be plain text (not JSON)
        var isJson = TryParseJson(result, out _);
        Assert.False(isJson);
    }

    [Fact]
    public async Task MachineReadableParameter_DefaultsToFalse_ForBackwardsCompatibility()
    {
        // This test verifies that calling consolidated tools without machineReadable
        // continues to return plain text

        // Act - call without machineReadable parameter
        var sdkInfoResult = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Info);
        var sdkListResult = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.ListSdks);
        var runtimeListResult = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.ListRuntimes);

        // Assert - all should return plain text (not JSON)
        var results = new[] { sdkInfoResult, sdkListResult, runtimeListResult };
        
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            var isJson = TryParseJson(result, out _);
            Assert.False(isJson);
        }
    }

    private static bool TryParseJson(string text, out JsonDocument? document)
    {
        document = null;
        try
        {
            document = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
