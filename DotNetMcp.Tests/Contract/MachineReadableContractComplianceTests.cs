using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Contract;

/// <summary>
/// Tests to verify that all tools comply with the v1.0 machine-readable JSON contract.
/// These tests ensure consistency and stability of the machine-readable output format.
/// </summary>
public class MachineReadableContractComplianceTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public MachineReadableContractComplianceTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
    }

    #region Success Envelope Compliance Tests

    [Fact]
    public async Task DotnetSdkVersion_MachineReadable_ReturnsValidSuccessEnvelope()
    {
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Version, machineReadable: true);
        AssertSuccessEnvelope(result);
    }

    [Fact]
    public async Task DotnetSdkInfo_MachineReadable_ReturnsValidSuccessEnvelope()
    {
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Info, machineReadable: true);
        AssertSuccessEnvelope(result);
    }

    [Fact]
    public async Task DotnetSdkList_MachineReadable_ReturnsValidSuccessEnvelope()
    {
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.ListSdks, machineReadable: true);
        AssertSuccessEnvelope(result);
    }

    [Fact]
    public async Task DotnetRuntimeList_MachineReadable_ReturnsValidSuccessEnvelope()
    {
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.ListRuntimes, machineReadable: true);
        AssertSuccessEnvelope(result);
    }

    #endregion

    #region Error Envelope Compliance Tests - Validation Failures

    [Fact]
    public async Task DotnetToolInstall_EmptyPackageName_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Install,
            packageId: "",
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetToolUpdate_EmptyPackageName_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Update,
            packageId: "",
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetToolUninstall_EmptyPackageName_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Uninstall,
            packageId: "",
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetSecretsSet_EmptyKey_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetDevCerts(
            action: DotNetMcp.Actions.DotnetDevCertsAction.SecretsSet,
            key: "",
            value: "test",
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetSecretsSet_EmptyValue_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetDevCerts(
            action: DotNetMcp.Actions.DotnetDevCertsAction.SecretsSet,
            key: "test",
            value: "",
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetCertificateExport_EmptyPath_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetDevCerts(
            action: DotNetMcp.Actions.DotnetDevCertsAction.CertificateExport,
            path: "",
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetCertificateExport_InvalidFormat_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetDevCerts(
            action: DotNetMcp.Actions.DotnetDevCertsAction.CertificateExport,
            path: "/tmp/cert.pfx",
            format: "invalid",
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetSolutionAdd_EmptyProjects_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Add,
            solution: "Test.sln",
            projects: Array.Empty<string>(),
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetNugetLocals_NeitherListNorClear_ReturnsValidValidationError()
    {
        var result = await _tools.DotnetPackage(
            action: DotNetMcp.Actions.DotnetPackageAction.ClearCache,
            cacheType: "invalid",
            machineReadable: true);
        AssertValidationErrorEnvelope(result);
    }

    #endregion

    #region Error Envelope Compliance Tests - Command Execution Failures

    [Fact]
    public async Task DotnetProjectBuild_NonExistentProject_ReturnsValidErrorEnvelope()
    {
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Build,
            project: "/tmp/NonExistent_Project_12345.csproj",
            machineReadable: true);
        AssertErrorEnvelope(result);
    }

    [Fact]
    public async Task DotnetProjectRun_NonExistentProject_ReturnsValidErrorEnvelope()
    {
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Run,
            project: "/tmp/NonExistent_Project_12345.csproj",
            machineReadable: true);
        AssertErrorEnvelope(result);
    }

    #endregion

    #region Error Envelope Compliance Tests - Capability Not Available

    [Fact]
    public async Task DotnetTelemetry_AlwaysReturnsCapabilityNotAvailable()
    {
        var result = await _tools.DotnetTelemetry(enable: true, machineReadable: true);
        AssertCapabilityNotAvailableEnvelope(result);
    }

    #endregion

    #region Assertion Helpers

    /// <summary>
    /// Assert that the result is a valid success envelope (SuccessResult).
    /// </summary>
    private static void AssertSuccessEnvelope(string result)
    {
        // Must be valid JSON
        Assert.True(TryParseJson(result, out var jsonDoc), "Result must be valid JSON");
        var root = jsonDoc!.RootElement;

        // Required fields
        Assert.True(root.TryGetProperty("success", out var successProp), "Missing 'success' field");
        Assert.True(successProp.GetBoolean(), "'success' must be true for success envelope");

        Assert.True(root.TryGetProperty("output", out var outputProp), "Missing 'output' field");
        Assert.Equal(JsonValueKind.String, outputProp.ValueKind);

        Assert.True(root.TryGetProperty("exitCode", out var exitCodeProp), "Missing 'exitCode' field");
        Assert.Equal(0, exitCodeProp.GetInt32());

        // Optional fields (command)
        if (root.TryGetProperty("command", out var commandProp))
        {
            Assert.Equal(JsonValueKind.String, commandProp.ValueKind);
        }

        // Should NOT have error-related fields
        Assert.False(root.TryGetProperty("errors", out _), "Success envelope should not have 'errors' field");
    }

    /// <summary>
    /// Assert that the result is a valid error envelope (ErrorResponse).
    /// </summary>
    private static void AssertErrorEnvelope(string result)
    {
        // Must be valid JSON
        Assert.True(TryParseJson(result, out var jsonDoc), "Result must be valid JSON");
        var root = jsonDoc!.RootElement;

        // Required fields at root level
        Assert.True(root.TryGetProperty("success", out var successProp), "Missing 'success' field");
        Assert.False(successProp.GetBoolean(), "'success' must be false for error envelope");

        Assert.True(root.TryGetProperty("errors", out var errorsProp), "Missing 'errors' field");
        Assert.Equal(JsonValueKind.Array, errorsProp.ValueKind);
        Assert.True(errorsProp.GetArrayLength() > 0, "'errors' array must contain at least one error");

        Assert.True(root.TryGetProperty("exitCode", out var exitCodeProp), "Missing 'exitCode' field");
        Assert.NotEqual(0, exitCodeProp.GetInt32());

        // Validate each error in the array
        foreach (var error in errorsProp.EnumerateArray())
        {
            AssertErrorResultStructure(error);
        }
    }

    /// <summary>
    /// Assert that the result is a valid validation error envelope.
    /// </summary>
    private static void AssertValidationErrorEnvelope(string result)
    {
        AssertErrorEnvelope(result);

        var jsonDoc = JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;

        // Validation errors should have exit code -1
        Assert.True(root.TryGetProperty("exitCode", out var exitCodeProp));
        Assert.Equal(-1, exitCodeProp.GetInt32());

        // First error should be INVALID_PARAMS
        var firstError = root.GetProperty("errors")[0];
        Assert.True(firstError.TryGetProperty("code", out var codeProp));
        Assert.Equal("INVALID_PARAMS", codeProp.GetString());

        Assert.True(firstError.TryGetProperty("category", out var categoryProp));
        Assert.Equal("Validation", categoryProp.GetString());

        Assert.True(firstError.TryGetProperty("mcpErrorCode", out var mcpErrorCodeProp));
        Assert.Equal(McpErrorCodes.InvalidParams, mcpErrorCodeProp.GetInt32());
    }

    /// <summary>
    /// Assert that the result is a valid capability not available error envelope.
    /// </summary>
    private static void AssertCapabilityNotAvailableEnvelope(string result)
    {
        AssertErrorEnvelope(result);

        var jsonDoc = JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;

        // Capability errors should have exit code -1
        Assert.True(root.TryGetProperty("exitCode", out var exitCodeProp));
        Assert.Equal(-1, exitCodeProp.GetInt32());

        // First error should be CAPABILITY_NOT_AVAILABLE
        var firstError = root.GetProperty("errors")[0];
        Assert.True(firstError.TryGetProperty("code", out var codeProp));
        Assert.Equal("CAPABILITY_NOT_AVAILABLE", codeProp.GetString());

        Assert.True(firstError.TryGetProperty("category", out var categoryProp));
        Assert.Equal("Capability", categoryProp.GetString());

        Assert.True(firstError.TryGetProperty("mcpErrorCode", out var mcpErrorCodeProp));
        // Note: CAPABILITY_NOT_AVAILABLE returns -32603 (InternalError) when using the (feature, reason, alternatives) overload
        // and -32001 (CapabilityNotAvailable) when using the (feature, alternatives, command, details) overload.
        // This test uses DotnetTelemetry which uses the first overload, so expects -32603.
        Assert.Equal(McpErrorCodes.InternalError, mcpErrorCodeProp.GetInt32());

        // Should have alternatives or explanation
        Assert.True(
            firstError.TryGetProperty("alternatives", out _) ||
            firstError.TryGetProperty("explanation", out _),
            "Capability error should have 'alternatives' or 'explanation'");
    }

    /// <summary>
    /// Assert that an ErrorResult has all required fields and correct structure.
    /// </summary>
    private static void AssertErrorResultStructure(JsonElement error)
    {
        // Required fields
        Assert.True(error.TryGetProperty("code", out var codeProp), "Missing 'code' field");
        Assert.Equal(JsonValueKind.String, codeProp.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(codeProp.GetString()), "'code' cannot be empty");

        Assert.True(error.TryGetProperty("message", out var messageProp), "Missing 'message' field");
        Assert.Equal(JsonValueKind.String, messageProp.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(messageProp.GetString()), "'message' cannot be empty");

        Assert.True(error.TryGetProperty("category", out var categoryProp), "Missing 'category' field");
        Assert.Equal(JsonValueKind.String, categoryProp.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(categoryProp.GetString()), "'category' cannot be empty");

        Assert.True(error.TryGetProperty("rawOutput", out var rawOutputProp), "Missing 'rawOutput' field");
        Assert.Equal(JsonValueKind.String, rawOutputProp.ValueKind);

        // Optional fields - validate structure if present
        if (error.TryGetProperty("hint", out var hintProp))
        {
            Assert.Equal(JsonValueKind.String, hintProp.ValueKind);
        }

        if (error.TryGetProperty("explanation", out var explanationProp))
        {
            Assert.Equal(JsonValueKind.String, explanationProp.ValueKind);
        }

        if (error.TryGetProperty("documentationUrl", out var docUrlProp))
        {
            Assert.Equal(JsonValueKind.String, docUrlProp.ValueKind);
        }

        if (error.TryGetProperty("suggestedFixes", out var fixesProp))
        {
            Assert.Equal(JsonValueKind.Array, fixesProp.ValueKind);
        }

        if (error.TryGetProperty("alternatives", out var altProp))
        {
            Assert.Equal(JsonValueKind.Array, altProp.ValueKind);
        }

        if (error.TryGetProperty("mcpErrorCode", out var mcpCodeProp))
        {
            Assert.Equal(JsonValueKind.Number, mcpCodeProp.ValueKind);
        }

        if (error.TryGetProperty("data", out var dataProp))
        {
            Assert.Equal(JsonValueKind.Object, dataProp.ValueKind);
            AssertErrorDataStructure(dataProp);
        }
    }

    /// <summary>
    /// Assert that ErrorData has correct structure.
    /// </summary>
    private static void AssertErrorDataStructure(JsonElement data)
    {
        // All fields are optional, but if present must have correct type
        if (data.TryGetProperty("command", out var commandProp))
        {
            Assert.Equal(JsonValueKind.String, commandProp.ValueKind);
        }

        if (data.TryGetProperty("exitCode", out var exitCodeProp))
        {
            Assert.Equal(JsonValueKind.Number, exitCodeProp.ValueKind);
        }

        if (data.TryGetProperty("stderr", out var stderrProp))
        {
            Assert.Equal(JsonValueKind.String, stderrProp.ValueKind);
        }

        if (data.TryGetProperty("additionalData", out var additionalDataProp))
        {
            Assert.Equal(JsonValueKind.Object, additionalDataProp.ValueKind);
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

    #endregion
}
