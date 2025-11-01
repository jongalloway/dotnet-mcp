using DotNetMcp;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests;

public class MachineReadableOutputTests
{
    private readonly DotNetCliTools _tools;
    private readonly Mock<ILogger<DotNetCliTools>> _loggerMock;
    private readonly ConcurrencyManager _concurrencyManager;

    public MachineReadableOutputTests()
    {
        _loggerMock = new Mock<ILogger<DotNetCliTools>>();
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_loggerMock.Object, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetSdkVersion_WithMachineReadableFalse_ReturnsPlainText()
    {
        // Act
        var result = await _tools.DotnetSdkVersion(machineReadable: false);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Exit Code:");
        
        // Should not be valid JSON
        var isJson = TryParseJson(result, out _);
        isJson.Should().BeFalse("plain text output should not be JSON");
    }

    [Fact]
    public async Task DotnetSdkVersion_WithMachineReadableTrue_ReturnsValidJson()
    {
        // Act
        var result = await _tools.DotnetSdkVersion(machineReadable: true);

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        // Should be valid JSON
        var isJson = TryParseJson(result, out var jsonDoc);
        isJson.Should().BeTrue("machine-readable output should be valid JSON");
        
        // Verify JSON structure
        var root = jsonDoc!.RootElement;
        root.TryGetProperty("success", out var successProp).Should().BeTrue();
        root.TryGetProperty("exitCode", out var exitCodeProp).Should().BeTrue();
        
        // If successful, should have output property
        if (successProp.GetBoolean())
        {
            root.TryGetProperty("output", out _).Should().BeTrue();
        }
        else
        {
            // If failed, should have errors array
            root.TryGetProperty("errors", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task DotnetProjectBuild_WithInvalidProject_MachineReadableTrue_ReturnsStructuredError()
    {
        // Arrange
        var nonExistentProject = "/tmp/NonExistent_Project_12345.csproj";

        // Act
        var result = await _tools.DotnetProjectBuild(
            project: nonExistentProject,
            machineReadable: true);

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        var isJson = TryParseJson(result, out var jsonDoc);
        isJson.Should().BeTrue("machine-readable error output should be valid JSON");
        
        var root = jsonDoc!.RootElement;
        root.TryGetProperty("success", out var successProp).Should().BeTrue();
        successProp.GetBoolean().Should().BeFalse("build should fail for non-existent project");
        
        root.TryGetProperty("exitCode", out var exitCodeProp).Should().BeTrue();
        exitCodeProp.GetInt32().Should().NotBe(0, "failed builds should have non-zero exit code");
        
        root.TryGetProperty("errors", out var errorsProp).Should().BeTrue();
        errorsProp.ValueKind.Should().Be(JsonValueKind.Array);
        errorsProp.GetArrayLength().Should().BeGreaterThan(0, "should have at least one error");
    }

    [Fact]
    public async Task DotnetPackageSearch_WithMachineReadableTrue_ReturnsValidJson()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "Newtonsoft.Json",
            take: 1,
            machineReadable: true);

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        var isJson = TryParseJson(result, out _);
        isJson.Should().BeTrue("machine-readable output should be valid JSON");
    }

    [Fact]
    public async Task DotnetProjectNew_WithMachineReadableFalse_ReturnsPlainTextByDefault()
    {
        // Act - using default machineReadable parameter (should be false)
        var result = await _tools.DotnetProjectNew(
            template: "console",
            name: "TestApp",
            output: "/tmp/test-output-" + Guid.NewGuid());

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        // Default behavior should be plain text (not JSON)
        var isJson = TryParseJson(result, out _);
        isJson.Should().BeFalse("default output should be plain text for backwards compatibility");
    }

    [Fact]
    public async Task MachineReadableParameter_DefaultsToFalse_ForBackwardsCompatibility()
    {
        // This test verifies that existing code calling methods without machineReadable
        // parameter continues to work and returns plain text

        // Act - call without machineReadable parameter
        var sdkInfoResult = await _tools.DotnetSdkInfo();
        var sdkListResult = await _tools.DotnetSdkList();
        var runtimeListResult = await _tools.DotnetRuntimeList();

        // Assert - all should return plain text (not JSON)
        var results = new[] { sdkInfoResult, sdkListResult, runtimeListResult };
        
        foreach (var result in results)
        {
            result.Should().NotBeNullOrEmpty();
            var isJson = TryParseJson(result, out _);
            isJson.Should().BeFalse("backwards compatibility requires plain text by default");
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
