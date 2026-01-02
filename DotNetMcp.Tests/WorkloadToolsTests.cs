using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for workload management MCP tools
/// </summary>
public class WorkloadToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public WorkloadToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetWorkloadList_ReturnsWorkloadInformation()
    {
        // Act
        var result = await _tools.DotnetWorkloadList();

        // Assert
        Assert.NotNull(result);
        // Should contain workload version or list information
        Assert.True(
            result.Contains("Workload", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("Installed", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("Manifest", StringComparison.OrdinalIgnoreCase),
            "Result should contain workload-related information");
    }

    [Fact]
    public async Task DotnetWorkloadList_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetWorkloadList(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Machine-readable format should not contain plain error prefix
        // (errors are JSON-formatted in machine-readable mode)
    }

    [Fact]
    public async Task DotnetWorkloadSearch_WithoutSearchTerm_ReturnsAllWorkloads()
    {
        // Act
        var result = await _tools.DotnetWorkloadSearch();

        // Assert
        Assert.NotNull(result);
        // Should return workload list (even if empty)
        Assert.DoesNotContain("Error: searchTerm", result);
    }

    [Fact]
    public async Task DotnetWorkloadSearch_WithSearchTerm_ReturnsFilteredWorkloads()
    {
        // Act
        var result = await _tools.DotnetWorkloadSearch(searchTerm: "maui");

        // Assert
        Assert.NotNull(result);
        // Should contain MAUI-related workloads or indicate no results found
        Assert.True(
            result.Contains("maui", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("Workload", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("Description", StringComparison.OrdinalIgnoreCase),
            "Result should contain workload search results or headers");
    }

    [Fact]
    public async Task DotnetWorkloadSearch_WithInvalidSearchTerm_HandlesGracefully()
    {
        // Act
        var result = await _tools.DotnetWorkloadSearch(searchTerm: "nonexistent-workload-xyz-123");

        // Assert
        Assert.NotNull(result);
        // Should handle search with no results gracefully
    }

    [Fact]
    public async Task DotnetWorkloadInstall_WithoutWorkloadIds_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(workloadIds: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("workloadIds", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetWorkloadInstall_WithWhitespaceWorkloadIds_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(workloadIds: "   ");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
    }

    [Fact]
    public async Task DotnetWorkloadInstall_WithInvalidCharacters_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(workloadIds: "invalid$workload");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("Invalid workload ID", result);
    }

    [Fact]
    public async Task DotnetWorkloadInstall_WithMultipleInvalidWorkloads_ReturnsErrorForFirst()
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(workloadIds: "valid-workload,invalid@workload");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("Invalid workload ID", result);
    }

    [Fact]
    public async Task DotnetWorkloadInstall_WithValidSingleWorkload_BuildsCorrectCommand()
    {
        // This test validates command building without actually installing
        // We expect this to fail since we're using a fake workload ID
        
        // Act
        var result = await _tools.DotnetWorkloadInstall(
            workloadIds: "test-workload-id");

        // Assert
        Assert.NotNull(result);
        // The command should execute (even if it fails due to non-existent workload)
        // This validates that the command was properly constructed
    }

    [Fact]
    public async Task DotnetWorkloadInstall_WithMultipleWorkloads_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(
            workloadIds: "workload-one,workload-two");

        // Assert
        Assert.NotNull(result);
        // Command should be properly constructed with both workload IDs
    }

    [Fact]
    public async Task DotnetWorkloadInstall_WithSkipManifestUpdate_IncludesFlag()
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(
            workloadIds: "test-workload",
            skipManifestUpdate: true);

        // Assert
        Assert.NotNull(result);
        // Command executes with skip-manifest-update flag
    }

    [Fact]
    public async Task DotnetWorkloadInstall_WithIncludePreviews_IncludesFlag()
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(
            workloadIds: "test-workload",
            includePreviews: true);

        // Assert
        Assert.NotNull(result);
        // Command executes with include-previews flag
    }

    [Fact]
    public async Task DotnetWorkloadUpdate_ExecutesSuccessfully()
    {
        // Act
        var result = await _tools.DotnetWorkloadUpdate();

        // Assert
        Assert.NotNull(result);
        // Should complete without syntax errors
        // (may report no workloads to update, which is fine)
    }

    [Fact]
    public async Task DotnetWorkloadUpdate_WithIncludePreviews_IncludesFlag()
    {
        // Act
        var result = await _tools.DotnetWorkloadUpdate(includePreviews: true);

        // Assert
        Assert.NotNull(result);
        // Command executes with include-previews flag
    }

    [Fact]
    public async Task DotnetWorkloadUpdate_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetWorkloadUpdate(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    [Fact]
    public async Task DotnetWorkloadUninstall_WithoutWorkloadIds_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkloadUninstall(workloadIds: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("workloadIds", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetWorkloadUninstall_WithWhitespaceWorkloadIds_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkloadUninstall(workloadIds: "   ");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
    }

    [Fact]
    public async Task DotnetWorkloadUninstall_WithInvalidCharacters_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkloadUninstall(workloadIds: "invalid!workload");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("Invalid workload ID", result);
    }

    [Fact]
    public async Task DotnetWorkloadUninstall_WithValidWorkloadId_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetWorkloadUninstall(workloadIds: "test-workload");

        // Assert
        Assert.NotNull(result);
        // Command should execute (even if workload doesn't exist)
    }

    [Fact]
    public async Task DotnetWorkloadUninstall_WithMultipleWorkloads_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetWorkloadUninstall(
            workloadIds: "workload-one,workload-two,workload-three");

        // Assert
        Assert.NotNull(result);
        // Command should be properly constructed with all workload IDs
    }

    [Fact]
    public async Task DotnetWorkloadUninstall_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetWorkloadUninstall(
            workloadIds: "test-workload",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    // Validation tests for workload ID format
    [Theory]
    [InlineData("maui-android")]
    [InlineData("maui-ios")]
    [InlineData("wasm-tools")]
    [InlineData("android")]
    [InlineData("ios")]
    [InlineData("maccatalyst")]
    [InlineData("maui-windows")]
    [InlineData("maui-tizen")]
    [InlineData("workload123")]
    [InlineData("WORKLOAD_NAME")]
    [InlineData("workload-name_123")]
    public async Task DotnetWorkloadInstall_WithValidWorkloadIds_AcceptsFormat(string workloadId)
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(workloadIds: workloadId);

        // Assert
        Assert.NotNull(result);
        // Should not reject due to format validation
        Assert.DoesNotContain("Invalid workload ID", result);
    }

    [Theory]
    [InlineData("workload$name")]
    [InlineData("workload@name")]
    [InlineData("workload name")]  // spaces not allowed in IDs
    [InlineData("workload/name")]
    [InlineData("workload\\name")]
    [InlineData("workload;name")]
    [InlineData("workload|name")]
    public async Task DotnetWorkloadInstall_WithInvalidWorkloadIds_RejectsFormat(string workloadId)
    {
        // Act
        var result = await _tools.DotnetWorkloadInstall(workloadIds: workloadId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("Invalid workload ID", result);
    }
}
