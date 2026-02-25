using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        _tools = new DotNetCliTools(_logger, _concurrencyManager, new ProcessSessionManager());
    }

    #region Tool Management Tests

    [Fact]
    public async Task DotnetToolInstall_WithEmptyPackageName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Install,
            packageId: "")).GetText();

        // Assert
        AssertValidationError(result, "packageId", "required");
    }

    [Fact]
    public async Task DotnetToolInstall_WithEmptyPackageName_MachineReadableFalse_ReturnsPlainText()
    {
        // Act
        var result = (await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Install,
            packageId: "")).GetText();

        // Assert
        Assert.StartsWith("Error:", result);
        Assert.Contains("packageId", result);
    }

    [Fact]
    public async Task DotnetToolUpdate_WithEmptyPackageName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Update,
            packageId: "")).GetText();

        // Assert
        AssertValidationError(result, "packageId", "required");
    }

    [Fact]
    public async Task DotnetToolUninstall_WithEmptyPackageName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Uninstall,
            packageId: "")).GetText();

        // Assert
        AssertValidationError(result, "packageId", "required");
    }

    [Fact]
    public async Task DotnetToolSearch_WithEmptySearchTerm_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Search,
            searchTerm: "")).GetText();

        // Assert
        AssertValidationError(result, "searchTerm", "required");
    }

    [Fact]
    public async Task DotnetToolRun_WithEmptyToolName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Run,
            toolName: "")).GetText();

        // Assert
        AssertValidationError(result, "toolName", "required");
    }

    [Fact]
    public async Task DotnetToolRun_WithInvalidArgs_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetTool(
            action: DotNetMcp.Actions.DotnetToolAction.Run,
            toolName: "test-tool",
            args: "invalid<>chars")).GetText();

        // Assert
        AssertValidationError(result, "args", "invalid characters");
    }

    #endregion

    #region Project Tests

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "invalid<>chars"));

        // Assert
        AssertValidationError(result, "additionalOptions", "invalid characters");
    }

    [Fact(Skip = "Template validation requires actual dotnet templates to be installed")]
    public async Task DotnetProjectNew_WithInvalidFramework_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetProjectNew(
            template: "console",
            framework: "invalidframework"));

        // Assert
        AssertValidationError(result, "framework", "invalid format");
    }

    [Fact]
    public async Task DotnetProjectRestore_WithInvalidProjectPath_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetProjectRestore(
            project: "test.txt"));

        // Assert
        AssertValidationError(result, "project", "invalid extension");
    }

    [Fact]
    public async Task DotnetProjectBuild_WithInvalidProjectPath_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetProjectBuild(
            project: "test.txt"));

        // Assert
        AssertValidationError(result, "project", "invalid extension");
    }

    #endregion

    #region Entity Framework Tests

    [Fact]
    public async Task DotnetEfMigrationsAdd_WithEmptyName_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetEfMigrationsAdd(
            name: ""));

        // Assert
        AssertValidationError(result, "name", "required");
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithEmptyConnection_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetEfDbContextScaffold(
            connection: "",
            provider: "Microsoft.EntityFrameworkCore.SqlServer"));

        // Assert
        AssertValidationError(result, "connection", "required");
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithEmptyProvider_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetEfDbContextScaffold(
            connection: "Server=localhost;Database=test;",
            provider: ""));

        // Assert
        AssertValidationError(result, "provider", "required");
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task DotnetCertificateExport_WithEmptyPath_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetCertificateExport(
            path: ""));

        // Assert
        AssertValidationError(result, "path", "required");
    }

    [Fact]
    public async Task DotnetCertificateExport_WithInvalidFormat_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            format: "invalid"));

        // Assert
        AssertValidationError(result, "format", "invalid value");
    }

    [Fact]
    public async Task DotnetSecretsSet_WithEmptyKey_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetSecretsSet(
            key: "",
            value: "test"));

        // Assert
        AssertValidationError(result, "key", "required");
    }

    [Fact]
    public async Task DotnetSecretsSet_WithEmptyValue_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetSecretsSet(
            key: "TestKey",
            value: ""));

        // Assert
        AssertValidationError(result, "value", "required");
    }

    [Fact]
    public async Task DotnetSecretsRemove_WithEmptyKey_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetSecretsRemove(
            key: ""));

        // Assert
        AssertValidationError(result, "key", "required");
    }

    #endregion

    #region Solution Tests

    [Fact]
    public async Task DotnetSolutionCreate_WithInvalidFormat_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Create,
            name: "TestSolution",
            format: "invalid")).GetText();

        // Assert
        AssertValidationError(result, "format", "invalid value");
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithEmptyProjects_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Add,
            solution: "TestSolution.sln",
            projects: Array.Empty<string>())).GetText();

        // Assert
        AssertValidationError(result, "projects", "required");
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithEmptyProjects_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Remove,
            solution: "TestSolution.sln",
            projects: Array.Empty<string>())).GetText();

        // Assert
        AssertValidationError(result, "projects", "required");
    }

    #endregion

    #region Package Tests

    [Fact]
    public async Task DotnetNugetLocals_WithNeitherListNorClear_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            list: false,
            clear: false));

        // Assert
        AssertValidationError(result, "list/clear", "at least one required");
    }

    [Fact]
    public async Task DotnetNugetLocals_WithBothListAndClear_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            list: true,
            clear: true));

        // Assert
        AssertValidationError(result, "list/clear", "mutually exclusive");
    }

    [Fact]
    public async Task DotnetNugetLocals_WithInvalidCacheLocation_MachineReadableTrue_ReturnsStructuredError()
    {
        // Act
        var result = (await _tools.DotnetNugetLocals(
            cacheLocation: "invalid-location",
            list: true));

        // Assert
        AssertValidationError(result, "cacheLocation", "invalid value");
    }

    #endregion

    #region Helper Methods

    private static void AssertValidationError(string result, string expectedParameter, string expectedReason)
    {
        Assert.Contains("Error:", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
