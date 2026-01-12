using System;
using System.IO;
using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Tools;

/// <summary>
/// Tests for the consolidated dotnet_tool command.
/// </summary>
public class DotNetCliToolsConsolidatedToolTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public DotNetCliToolsConsolidatedToolTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
    }

    #region Install Action Tests

    [Fact]
    public async Task DotnetTool_Install_WithPackageId_ExecutesCommand()
    {
        // Test basic install action
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: "dotnet-ef");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Install_WithGlobalFlag_ExecutesCommand()
    {
        // Test global install
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: "dotnet-ef",
            global: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Install_WithVersion_ExecutesCommand()
    {
        // Test install with specific version
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: "dotnet-ef",
            version: "8.0.0");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Install_WithFramework_ExecutesCommand()
    {
        // Test install with framework
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: "dotnet-ef",
            framework: "net8.0");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Install_WithAllParameters_ExecutesCommand()
    {
        // Test install with all parameters
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: "dotnet-ef",
            global: true,
            version: "8.0.0",
            framework: "net8.0");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Install_WithToolPath_ExecutesCommand()
    {
        // Test install with custom tool path
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: "dotnet-ef",
            toolPath: "/custom/path");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Install_WithoutPackageId_ReturnsError()
    {
        // Test that missing packageId returns error
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: null);

        Assert.Contains("Error", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetTool_Install_WithoutPackageId_MachineReadable_ReturnsError()
    {
        // Test that missing packageId returns error in machine-readable format
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region List Action Tests

    [Fact]
    public async Task DotnetTool_List_WithoutGlobalFlag_ExecutesCommand()
    {
        // Test local tool list
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.List,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet tool list");
    }

    [Fact]
    public async Task DotnetTool_List_WithGlobalFlag_ExecutesCommand()
    {
        // Test global tool list
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.List,
            global: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet tool list --global");
    }

    #endregion

    #region Update Action Tests

    [Fact]
    public async Task DotnetTool_Update_WithPackageId_ExecutesCommand()
    {
        // Test basic update action
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Update,
            packageId: "dotnet-ef");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Update_WithGlobalFlag_ExecutesCommand()
    {
        // Test global tool update
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Update,
            packageId: "dotnet-ef",
            global: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Update_WithVersion_ExecutesCommand()
    {
        // Test update to specific version
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Update,
            packageId: "dotnet-ef",
            version: "8.0.1");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Update_WithoutPackageId_ReturnsError()
    {
        // Test that missing packageId returns error
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Update,
            packageId: null);

        Assert.Contains("Error", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetTool_Update_WithoutPackageId_MachineReadable_ReturnsError()
    {
        // Test that missing packageId returns error in machine-readable format
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Update,
            packageId: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Uninstall Action Tests

    [Fact]
    public async Task DotnetTool_Uninstall_WithPackageId_ExecutesCommand()
    {
        // Test basic uninstall action
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Uninstall,
            packageId: "dotnet-ef");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Uninstall_WithGlobalFlag_ExecutesCommand()
    {
        // Test global tool uninstall
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Uninstall,
            packageId: "dotnet-ef",
            global: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Uninstall_WithoutPackageId_ReturnsError()
    {
        // Test that missing packageId returns error
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Uninstall,
            packageId: null);

        Assert.Contains("Error", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetTool_Uninstall_WithoutPackageId_MachineReadable_ReturnsError()
    {
        // Test that missing packageId returns error in machine-readable format
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Uninstall,
            packageId: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Restore Action Tests

    [Fact]
    public async Task DotnetTool_Restore_ExecutesCommand()
    {
        // Test tool restore command
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Restore,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet tool restore");
    }

    #endregion

    #region CreateManifest Action Tests

    [Fact]
    public async Task DotnetTool_CreateManifest_WithoutParameters_ExecutesCommand()
    {
        // Use an isolated output directory to avoid writing into the repo
        var tempDirectory = Path.Join(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var result = await _tools.DotnetTool(
                action: DotnetToolAction.CreateManifest,
                output: tempDirectory,
                machineReadable: true);

            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, $"dotnet new tool-manifest -o \"{tempDirectory}\"");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetTool_CreateManifest_WithOutput_ExecutesCommand()
    {
        // Test manifest creation with output directory
        var tempDirectory = Path.Join(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var result = await _tools.DotnetTool(
                action: DotnetToolAction.CreateManifest,
                output: tempDirectory,
                machineReadable: true);

            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, $"dotnet new tool-manifest -o \"{tempDirectory}\"");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetTool_CreateManifest_WithForce_ExecutesCommand()
    {
        // Test manifest creation with force flag
        var tempDirectory = Path.Join(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var result = await _tools.DotnetTool(
                action: DotnetToolAction.CreateManifest,
                output: tempDirectory,
                force: true,
                machineReadable: true);

            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, $"dotnet new tool-manifest -o \"{tempDirectory}\" --force");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetTool_CreateManifest_WithAllParameters_ExecutesCommand()
    {
        // Test manifest creation with all parameters
        var tempDirectory = Path.Join(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var result = await _tools.DotnetTool(
                action: DotnetToolAction.CreateManifest,
                output: tempDirectory,
                force: true,
                machineReadable: true);

            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, $"dotnet new tool-manifest -o \"{tempDirectory}\" --force");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    #endregion

    #region Search Action Tests

    [Fact]
    public async Task DotnetTool_Search_WithSearchTerm_ExecutesCommand()
    {
        // Test basic search action
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Search,
            searchTerm: "entity");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Search_WithDetail_ExecutesCommand()
    {
        // Test search with detail flag
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Search,
            searchTerm: "entity",
            detail: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Search_WithTakeAndSkip_ExecutesCommand()
    {
        // Test search with pagination
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Search,
            searchTerm: "entity",
            take: 10,
            skip: 5);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Search_WithPrerelease_ExecutesCommand()
    {
        // Test search with prerelease flag
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Search,
            searchTerm: "entity",
            prerelease: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Search_WithAllParameters_ExecutesCommand()
    {
        // Test search with all parameters
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Search,
            searchTerm: "entity",
            detail: true,
            take: 10,
            skip: 5,
            prerelease: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Search_WithoutSearchTerm_ReturnsError()
    {
        // Test that missing searchTerm returns error
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Search,
            searchTerm: null);

        Assert.Contains("Error", result);
        Assert.Contains("searchTerm", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetTool_Search_WithoutSearchTerm_MachineReadable_ReturnsError()
    {
        // Test that missing searchTerm returns error in machine-readable format
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Search,
            searchTerm: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("searchTerm", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Run Action Tests

    [Fact]
    public async Task DotnetTool_Run_WithToolName_ExecutesCommand()
    {
        // Test basic run action
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Run,
            toolName: "dotnet-ef");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Run_WithArgs_ExecutesCommand()
    {
        // Test run with arguments
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Run,
            toolName: "dotnet-ef",
            args: "migrations add Initial");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetTool_Run_WithoutToolName_ReturnsError()
    {
        // Test that missing toolName returns error
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Run,
            toolName: null);

        Assert.Contains("Error", result);
        Assert.Contains("toolName", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetTool_Run_WithoutToolName_MachineReadable_ReturnsError()
    {
        // Test that missing toolName returns error in machine-readable format
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Run,
            toolName: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("toolName", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetTool_Run_WithInvalidArgsCharacters_ReturnsError()
    {
        // Test that args with shell metacharacters returns error
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Run,
            toolName: "dotnet-ef",
            args: "migrations add Initial && echo hacked");

        Assert.Contains("Error", result);
        Assert.Contains("args contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetTool_Run_WithInvalidArgsCharacters_MachineReadable_ReturnsError()
    {
        // Test that args with shell metacharacters returns error in machine-readable format
        var result = await _tools.DotnetTool(
            action: DotnetToolAction.Run,
            toolName: "dotnet-ef",
            args: "migrations add Initial && echo hacked",
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("args contains invalid characters", result);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task DotnetTool_WithInvalidAction_ReturnsError()
    {
        // Test that invalid action enum value is handled
        // Note: This is hard to test directly since enum validation happens at compile time
        // but we can test the validation logic indirectly
        var invalidAction = (DotnetToolAction)999;
        
        var result = await _tools.DotnetTool(
            action: invalidAction,
            machineReadable: false);

        Assert.Contains("Error", result);
        Assert.Contains("action", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetTool_WithInvalidAction_MachineReadable_ReturnsError()
    {
        // Test that invalid action enum value returns machine-readable error
        var invalidAction = (DotnetToolAction)999;
        
        var result = await _tools.DotnetTool(
            action: invalidAction,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("action", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
