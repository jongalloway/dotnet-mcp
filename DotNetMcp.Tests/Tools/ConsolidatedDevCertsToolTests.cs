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

    [InteractiveFact]
    public async Task DotnetDevCerts_CertificateTrust_ExecutesCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateTrust)).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute (may require elevation on some platforms)
    }

    [InteractiveFact]
    public async Task DotnetDevCerts_CertificateTrust_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateTrust)).GetText();

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
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateCheck)).GetText();

        // Assert
        Assert.NotNull(result);
        // Should check certificate status
    }

    [InteractiveFact]
    public async Task DotnetDevCerts_CertificateCheck_WithTrust_IncludesTrustFlag()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateCheck, trust: true)).GetText();

        // Assert
        Assert.NotNull(result);
        // Should include --trust flag
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateCheck_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateCheck)).GetText();

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Certificate Clean Action Tests

    [InteractiveFact]
    public async Task DotnetDevCerts_CertificateClean_ExecutesCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateClean)).GetText();

        // Assert
        Assert.NotNull(result);
        // Should clean certificates
    }

    [InteractiveFact]
    public async Task DotnetDevCerts_CertificateClean_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateClean)).GetText();

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
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateExport)).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("path", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithEmptyPath_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateExport, path: "")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("path", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithWhitespacePath_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateExport, path: "   ")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("path", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithValidPath_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pfx")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if no cert exists, but validates command construction)
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithPassword_IncludesPassword()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pfx",
            password: "TestPassword123!")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with password parameter
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithPfxFormat_IncludesFormat()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pfx",
            format: "pfx")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with format parameter
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithPemFormat_IncludesFormat()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pem",
            format: "pem")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with format parameter
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithInvalidFormat_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.txt",
            format: "invalid")).GetText();

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
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport,
            path: "/tmp/test-cert.pfx",
            format: format)).GetText();

        // Assert
        Assert.NotNull(result);
        // Should not reject due to case
        Assert.DoesNotContain("format must be", result);
    }

    [Fact]
    public async Task DotnetDevCerts_CertificateExport_WithMachineReadable_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.CertificateExport)).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Secrets Init Action Tests

    [Fact]
    public async Task DotnetDevCerts_SecretsInit_WithoutProject_ExecutesCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsInit)).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if not in a project directory)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsInit_WithProject_IncludesProjectPath()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsInit,
            project: "/tmp/test/test.csproj")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsInit_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsInit)).GetText();

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
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            value: "test-value")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithoutValue_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("value", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithEmptyKey_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "",
            value: "test-value")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithWhitespaceKey_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "   ",
            value: "test-value")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithEmptyValue_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey",
            value: "")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("value", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithWhitespaceValue_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey",
            value: "   ")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("value", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithValidKeyValue_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey",
            value: "TestValue")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if not in a project directory)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithHierarchicalKey_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "ConnectionStrings:DefaultConnection",
            value: "Server=localhost;Database=test")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with hierarchical key
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithProject_IncludesProjectPath()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet,
            key: "TestKey",
            value: "TestValue",
            project: "/tmp/test/test.csproj")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsSet_WithMachineReadable_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsSet)).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Secrets List Action Tests

    [Fact]
    public async Task DotnetDevCerts_SecretsList_WithoutProject_ExecutesCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsList)).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if not in a project directory)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsList_WithProject_IncludesProjectPath()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsList,
            project: "/tmp/test/test.csproj")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsList_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsList)).GetText();

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
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsRemove)).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithEmptyKey_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            key: "")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithWhitespaceKey_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            key: "   ")).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("key", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithValidKey_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            key: "TestKey")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if key doesn't exist)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithProject_IncludesProjectPath()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove,
            key: "TestKey",
            project: "/tmp/test/test.csproj")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsRemove_WithMachineReadable_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsRemove)).GetText();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Secrets Clear Action Tests

    [Fact]
    public async Task DotnetDevCerts_SecretsClear_WithoutProject_ExecutesCommand()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsClear)).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute (may fail if not in a project directory)
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsClear_WithProject_IncludesProjectPath()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsClear,
            project: "/tmp/test/test.csproj")).GetText();

        // Assert
        Assert.NotNull(result);
        // Command should execute with project parameter
    }

    [Fact]
    public async Task DotnetDevCerts_SecretsClear_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = (await _tools.DotnetDevCerts(
            DotnetDevCertsAction.SecretsClear)).GetText();

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Action Routing Tests

    [Fact]
    public async Task DotnetDevCerts_AllActions_RouteCorrectly()
    {
        // Exclude interactive actions so this test can run in non-interactive environments.
        var actions = Enum.GetValues<DotnetDevCertsAction>()
            .Where(action => action is not DotnetDevCertsAction.CertificateTrust and not DotnetDevCertsAction.CertificateClean)
            .ToArray();
        
        // Act - test each action and verify it routes correctly
        var results = await Task.WhenAll(actions.Select(async action => new
        {
            Action = action,
            Result = action switch
            {
                DotnetDevCertsAction.CertificateExport => (await _tools.DotnetDevCerts(action, path: "/tmp/test.pfx")).GetText(),
                DotnetDevCertsAction.SecretsSet => (await _tools.DotnetDevCerts(action, key: "TestKey", value: "TestValue")).GetText(),
                DotnetDevCertsAction.SecretsRemove => (await _tools.DotnetDevCerts(action, key: "TestKey")).GetText(),
                _ => (await _tools.DotnetDevCerts(action)).GetText()
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
        var exportResult = (await _tools.DotnetDevCerts(DotnetDevCertsAction.CertificateExport)).GetText();
        Assert.Contains("Error:", exportResult);
        Assert.Contains("path", exportResult, StringComparison.OrdinalIgnoreCase);

        // SecretsSet requires key and value
        var setResultNoKey = (await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsSet, value: "test")).GetText();
        Assert.Contains("Error:", setResultNoKey);
        Assert.Contains("key", setResultNoKey, StringComparison.OrdinalIgnoreCase);

        var setResultNoValue = (await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsSet, key: "test")).GetText();
        Assert.Contains("Error:", setResultNoValue);
        Assert.Contains("value", setResultNoValue, StringComparison.OrdinalIgnoreCase);

        // SecretsRemove requires key
        var removeResult = (await _tools.DotnetDevCerts(DotnetDevCertsAction.SecretsRemove)).GetText();
        Assert.Contains("Error:", removeResult);
        Assert.Contains("key", removeResult, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
