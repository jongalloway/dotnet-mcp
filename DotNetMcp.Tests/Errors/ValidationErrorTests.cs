using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for validation errors with machine-readable output.
/// Ensures that validation failures return structured JSON when machineReadable=true.
/// </summary>
public class ValidationErrorTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public ValidationErrorTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
    }

    #region Tool Management Tests

    [Fact]
    public async Task DotnetToolInstall_WithEmptyPackageName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetToolInstall(
            packageName: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "packageName", "required");
    }

    [Fact]
    public async Task DotnetToolInstall_WithEmptyPackageName_MachineReadableFalse_ReturnsPlainText()
    {
        // Act
        var result = await _tools.DotnetToolInstall(
            packageName: "",
            machineReadable: false);

        // Assert
        Assert.StartsWith("Error:", result);
        Assert.Contains("packageName", result);
        Assert.False(TryParseJson(result, out _));
    }

    [Fact]
    public async Task DotnetToolUpdate_WithEmptyPackageName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetToolUpdate(
            packageName: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "packageName", "required");
    }

    [Fact]
    public async Task DotnetToolUninstall_WithEmptyPackageName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetToolUninstall(
            packageName: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "packageName", "required");
    }

    [Fact]
    public async Task DotnetToolSearch_WithEmptySearchTerm_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetToolSearch(
            searchTerm: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "searchTerm", "required");
    }

    [Fact]
    public async Task DotnetToolRun_WithEmptyToolName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetToolRun(
            toolName: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "toolName", "required");
    }

    [Fact]
    public async Task DotnetToolRun_WithInvalidArgs_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetToolRun(
            toolName: "test-tool",
            args: "invalid<>chars",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "args", "invalid characters");
    }

    #endregion

    #region Project Tests

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "invalid<>chars",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "additionalOptions", "invalid characters");
    }

    [Fact(Skip = "Template validation requires actual dotnet templates to be installed")]
    public async Task DotnetProjectNew_WithInvalidFramework_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetProjectNew(
            template: "console",
            framework: "invalidframework",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "framework", "invalid format");
    }

    [Fact]
    public async Task DotnetProjectRestore_WithInvalidProjectPath_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetProjectRestore(
            project: "test.txt",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "project", "invalid extension");
    }

    [Fact]
    public async Task DotnetProjectBuild_WithInvalidProjectPath_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetProjectBuild(
            project: "test.txt",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "project", "invalid extension");
    }

    #endregion

    #region Entity Framework Tests

    [Fact]
    public async Task DotnetEfMigrationsAdd_WithEmptyName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "name", "required");
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithEmptyConnection_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "",
            provider: "Microsoft.EntityFrameworkCore.SqlServer",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "connection", "required");
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithEmptyProvider_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "Server=localhost;Database=test;",
            provider: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "provider", "required");
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task DotnetCertificateExport_WithEmptyPath_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetCertificateExport(
            path: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "path", "required");
    }

    [Fact]
    public async Task DotnetCertificateExport_WithInvalidFormat_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            format: "invalid",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "format", "invalid value");
    }

    [Fact]
    public async Task DotnetSecretsSet_WithEmptyKey_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetSecretsSet(
            key: "",
            value: "test",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "key", "required");
    }

    [Fact]
    public async Task DotnetSecretsSet_WithEmptyValue_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetSecretsSet(
            key: "TestKey",
            value: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "value", "required");
    }

    [Fact]
    public async Task DotnetSecretsRemove_WithEmptyKey_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetSecretsRemove(
            key: "",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "key", "required");
    }

    #endregion

    #region Solution Tests

    [Fact]
    public async Task DotnetSolutionCreate_WithInvalidFormat_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetSolutionCreate(
            name: "TestSolution",
            format: "invalid",
            machineReadable: true);

        // Assert
        AssertValidationError(result, "format", "invalid value");
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithEmptyProjects_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetSolutionAdd(
            solution: "TestSolution.sln",
            projects: Array.Empty<string>(),
            machineReadable: true);

        // Assert
        AssertValidationError(result, "projects", "required");
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithEmptyProjects_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetSolutionRemove(
            solution: "TestSolution.sln",
            projects: Array.Empty<string>(),
            machineReadable: true);

        // Assert
        AssertValidationError(result, "projects", "required");
    }

    #endregion

    #region Package Tests

    [Fact]
    public async Task DotnetNugetLocals_WithNeitherListNorClear_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            list: false,
            clear: false,
            machineReadable: true);

        // Assert
        AssertValidationError(result, "list/clear", "at least one required");
    }

    [Fact]
    public async Task DotnetNugetLocals_WithBothListAndClear_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            list: true,
            clear: true,
            machineReadable: true);

        // Assert
        AssertValidationError(result, "list/clear", "mutually exclusive");
    }

    [Fact]
    public async Task DotnetNugetLocals_WithInvalidCacheLocation_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "invalid-location",
            list: true,
            machineReadable: true);

        // Assert
        AssertValidationError(result, "cacheLocation", "invalid value");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Assert that the result is a valid validation error JSON with expected structure.
    /// </summary>
    private static void AssertValidationError(string result, string expectedParameter, string expectedReason)
    {
        // Should be valid JSON
        Assert.True(TryParseJson(result, out var jsonDoc), "Result should be valid JSON");

        var root = jsonDoc!.RootElement;

        // Verify success is false
        Assert.True(root.TryGetProperty("success", out var successProp));
        Assert.False(successProp.GetBoolean());

        // Verify exit code is -1
        Assert.True(root.TryGetProperty("exitCode", out var exitCodeProp));
        Assert.Equal(-1, exitCodeProp.GetInt32());

        // Verify errors array exists and has one error
        Assert.True(root.TryGetProperty("errors", out var errorsProp));
        Assert.Equal(JsonValueKind.Array, errorsProp.ValueKind);
        Assert.Equal(1, errorsProp.GetArrayLength());

        var error = errorsProp[0];

        // Verify error code is INVALID_PARAMS
        Assert.True(error.TryGetProperty("code", out var codeProp));
        Assert.Equal("INVALID_PARAMS", codeProp.GetString());

        // Verify category is Validation
        Assert.True(error.TryGetProperty("category", out var categoryProp));
        Assert.Equal("Validation", categoryProp.GetString());

        // Verify message exists and is not empty
        Assert.True(error.TryGetProperty("message", out var messageProp));
        Assert.False(string.IsNullOrWhiteSpace(messageProp.GetString()));

        // Verify MCP error code is InvalidParams
        Assert.True(error.TryGetProperty("mcpErrorCode", out var mcpErrorCodeProp));
        Assert.Equal(McpErrorCodes.InvalidParams, mcpErrorCodeProp.GetInt32());

        // Verify data exists
        Assert.True(error.TryGetProperty("data", out var dataProp));
        Assert.Equal(JsonValueKind.Object, dataProp.ValueKind);

        // Verify command is null or not present (no command executed)
        // Note: When using JsonIgnoreCondition.WhenWritingNull, null values are omitted from JSON
        if (dataProp.TryGetProperty("command", out var commandProp))
        {
            Assert.Equal(JsonValueKind.Null, commandProp.ValueKind);
        }

        // Verify exit code is -1
        Assert.True(dataProp.TryGetProperty("exitCode", out var dataExitCodeProp));
        Assert.Equal(-1, dataExitCodeProp.GetInt32());

        // Verify additional data contains parameter and reason
        Assert.True(dataProp.TryGetProperty("additionalData", out var additionalDataProp), 
            $"additionalData property not found. Data JSON: {dataProp.GetRawText()}");
        Assert.Equal(JsonValueKind.Object, additionalDataProp.ValueKind);

        Assert.True(additionalDataProp.TryGetProperty("parameter", out var parameterProp),
            $"parameter property not found in additionalData. AdditionalData JSON: {additionalDataProp.GetRawText()}");
        Assert.Equal(expectedParameter, parameterProp.GetString());

        Assert.True(additionalDataProp.TryGetProperty("reason", out var reasonProp),
            $"reason property not found in additionalData. AdditionalData JSON: {additionalDataProp.GetRawText()}");
        Assert.Equal(expectedReason, reasonProp.GetString());
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

    #endregion
}
