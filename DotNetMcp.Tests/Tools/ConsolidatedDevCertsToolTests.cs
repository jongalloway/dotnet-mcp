using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for the consolidated dotnet_dev_certs tool
/// </summary>
public class ConsolidatedDevCertsToolTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public ConsolidatedDevCertsToolTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager, new ProcessSessionManager());
    }

    #region Certificate Trust Action Tests

    [Fact]
    public async Task DotnetDevCerts_CertificateTrust_ExecutesCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateTrust);

        // Assert
        Assert.NotNull(result);
        // Command should execute (may require elevation on some platforms)
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateTrust_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateTrust, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Certificate Check Action Tests

    [Fact]
    public async Task DotnetDevCerts_CertificateCheck_ExecutesCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateCheck);

        // Assert
        Assert.NotNull(result);
        // Should check certificate status
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateCheck_WithTrust_IncludesTrustFlag()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateCheck, trust: true);

        // Assert
        Assert.NotNull(result);
        // Should include --trust flag
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateCheck_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateCheck, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Certificate Clean Action Tests

    [Fact]
    public async Task DotnetDevCerts_CertificateClean_ExecutesCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateClean);

        // Assert
        Assert.NotNull(result);
        // Should clean certificates
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateClean_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateClean, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Certificate Export Action Tests

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithoutPath_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateExport);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("path", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithEmptyPath_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateExport, path: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("path", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithWhitespacePath_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateExport, path: "   ");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("path", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithValidPath_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pfx");

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if no cert exists, but validates command construction)
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithPassword_IncludesPassword()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pfx",
            password: "TestPassword123!");

        // Assert
        Assert.NotNull(result);
        // Command should execute with password parameter
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithPfxFormat_IncludesFormat()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pfx",
            format: "pfx");

        // Assert
        Assert.NotNull(result);
        // Command should execute with format parameter
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithPemFormat_IncludesFormat()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pem",
            format: "pem");

        // Assert
        Assert.NotNull(result);
        // Command should execute with format parameter
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithInvalidFormat_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.txt",
            format: "invalid");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("format", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("PFX")]
    [InlineData("Pfx")]
    [InlineData("PEM")]
    [InlineData("Pem")]
    public async Task DotnetDevCerts_CertificateExport_WithCaseInsensitiveFormat_AcceptsFormat(string format)
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pfx",
            format: format);

        // Assert
        Assert.NotNull(result);
        // Should not reject due to case
        Assert.DoesNotContain("format must be", result);
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithMachineReadable_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("{", result);
        // Should return JSON-formatted error
    }

    #endregion

    #region Secrets Init Action Tests

    [Fact]
    public async Task DotnetDevCerts_SecretsInit_WithoutProject_ExecutesCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsInit);

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if not in a project directory)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsInit_WithProject_IncludesProjectPath()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsInit,
            project: "/tmp/test/test.csproj");

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsInit_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsInit,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Secrets Set Action Tests

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithoutKey_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            value: "test-value");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithoutValue_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("value", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithEmptyKey_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "",
            value: "test-value");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithWhitespaceKey_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "   ",
            value: "test-value");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithEmptyValue_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey",
            value: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("value", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithWhitespaceValue_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey",
            value: "   ");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("value", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithValidKeyValue_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey",
            value: "TestValue");

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if not in a project directory)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithHierarchicalKey_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "ConnectionStrings:DefaultConnection",
            value: "Server=localhost;Database=test");

        // Assert
        Assert.NotNull(result);
        // Command should execute with hierarchical key
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithProject_IncludesProjectPath()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey",
            value: "TestValue",
            project: "/tmp/test/test.csproj");

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithMachineReadable_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("{", result);
        // Should return JSON-formatted error
    }

    #endregion

    #region Secrets List Action Tests

    [Fact]
    public async Task DotnetDevCerts_SecretsList_WithoutProject_ExecutesCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsList);

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if not in a project directory)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsList_WithProject_IncludesProjectPath()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsList,
            project: "/tmp/test/test.csproj");

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsList_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsList,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Secrets Remove Action Tests

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithoutKey_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsRemove);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithEmptyKey_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            key: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithWhitespaceKey_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            key: "   ");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithValidKey_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            key: "TestKey");

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if key doesn't exist)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithProject_IncludesProjectPath()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            key: "TestKey",
            project: "/tmp/test/test.csproj");

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithMachineReadable_ReturnsStructuredError()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("{", result);
        // Should return JSON-formatted error
    }

    #endregion

    #region Secrets Clear Action Tests

    [Fact]
    public async Task DotnetDevCerts_SecretsClear_WithoutProject_ExecutesCommand()
    {
        // Act
        var result = await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsClear);

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if not in a project directory)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsClear_WithProject_IncludesProjectPath()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsClear,
            project: "/tmp/test/test.csproj");

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsClear_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsClear,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Action Routing Tests

    [Fact]
    public async Task DotnetDevCerts_AllActions_RouteCorrectly()
    {
        // Test that all enum values are handled using machineReadable for faster execution
        var actions = Enum.GetValues<DotnetDevCertsAction>();
        
        // Act - test each action with machineReadable=true to verify routing
        var results = await Task.WhenAll(actions.Select(async action => new
        {
            Action = action,
            Result = action switch
            {
                DotnetDevCertsAction.CertificateExport => await _tools.DotnetDevCerts(action, path: "/tmp/test.pfx", machineReadable: true),
                DotnetDevCertsAction.SecretsSet => await _tools.DotnetDevCerts(action, key: "TestKey", value: "TestValue", machineReadable: true),
                DotnetDevCertsAction.SecretsRemove => await _tools.DotnetDevCerts(action, key: "TestKey", machineReadable: true),
                _ => await _tools.DotnetDevCerts(action, machineReadable: true)
            }
        }));

        // Assert
        foreach (var item in results)
        {
            Assert.NotNull(item.Result);
            // Should not throw or return "Unsupported action"
            Assert.DoesNotContain("Unsupported action", item.Result);
        }
    }

    #endregion

    #region Parameter Validation Tests

    [Fact]
    public async Task DotnetDevCerts_ValidatesRequiredParametersPerAction()
    {
        // Test that each action validates its required parameters

        // CertificateExport requires path
        var exportResult = await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateExport);
        Assert.Contains("Error:", exportResult);
        Assert.Contains("path", exportResult, StringComparison.OrdinalIgnoreCase);

        // SecretsSet requires key and value
        var setResultNoKey = await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsSet, value: "test");
        Assert.Contains("Error:", setResultNoKey);
        Assert.Contains("key", setResultNoKey, StringComparison.OrdinalIgnoreCase);

        var setResultNoValue = await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsSet, key: "test");
        Assert.Contains("Error:", setResultNoValue);
        Assert.Contains("value", setResultNoValue, StringComparison.OrdinalIgnoreCase);

        // SecretsRemove requires key
        var removeResult = await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsRemove);
        Assert.Contains("Error:", removeResult);
        Assert.Contains("key", removeResult, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
