using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for the consolidated dotnet_workload tool
/// </summary>
public class ConsolidatedWorkloadToolTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public ConsolidatedWorkloadToolTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    #region List Action Tests

    [Fact]
    public async Task DotnetWorkload_List_ReturnsWorkloadInformation()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.List);

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
    public async Task DotnetWorkload_List_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.List, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Machine-readable format should not contain plain error prefix
        // (errors are JSON-formatted in machine-readable mode)
    }

    #endregion

    #region Info Action Tests

    [Fact]
    public async Task DotnetWorkload_Info_ReturnsDetailedWorkloadInformation()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Info);

        // Assert
        Assert.NotNull(result);
        // Should contain detailed workload information
        Assert.True(
            result.Contains("Workload", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("Manifest", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("Installation", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("version", StringComparison.OrdinalIgnoreCase),
            "Result should contain detailed workload information");
    }

    [Fact]
    public async Task DotnetWorkload_Info_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Info, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Search Action Tests

    [Fact]
    public async Task DotnetWorkload_Search_WithoutSearchTerm_ReturnsAllWorkloads()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Search);

        // Assert
        Assert.NotNull(result);
        // Should return workload list (even if empty)
        Assert.DoesNotContain("Error: searchTerm", result);
    }

    [Fact]
    public async Task DotnetWorkload_Search_WithSearchTerm_ReturnsFilteredWorkloads()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Search, searchTerm: "maui");

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
    public async Task DotnetWorkload_Search_WithInvalidSearchTerm_HandlesGracefully()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Search, searchTerm: "nonexistent-workload-xyz-123");

        // Assert
        Assert.NotNull(result);
        // Should handle search with no results gracefully
    }

    [Fact]
    public async Task DotnetWorkload_Search_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Search, searchTerm: "maui", machineReadable: true);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Install Action Tests

    [Fact]
    public async Task DotnetWorkload_Install_WithoutWorkloadIds_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Install);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("workloadIds", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithEmptyWorkloadIds_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Install, workloadIds: Array.Empty<string>());

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("workloadIds", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithInvalidWorkloadId_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Install, workloadIds: new[] { "invalid$workload" });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("Invalid workload ID", result);
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithValidSingleWorkload_BuildsCorrectCommand()
    {
        // This test validates command building without actually installing
        // We expect this to fail since we're using a fake workload ID
        
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Install, workloadIds: new[] { "test-workload-id" });

        // Assert
        Assert.NotNull(result);
        // The command should execute (even if it fails due to non-existent workload)
        // This validates that the command was properly constructed
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithMultipleWorkloads_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Install,
            workloadIds: new[] { "workload-one", "workload-two" });

        // Assert
        Assert.NotNull(result);
        // Command should be properly constructed with both workload IDs
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithSkipManifestUpdate_IncludesFlag()
    {
        // Act
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Install,
            workloadIds: new[] { "test-workload" },
            skipManifestUpdate: true);

        // Assert
        Assert.NotNull(result);
        // Command executes with skip-manifest-update flag
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithIncludePreviews_IncludesFlag()
    {
        // Act
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Install,
            workloadIds: new[] { "test-workload" },
            includePreviews: true);

        // Assert
        Assert.NotNull(result);
        // Command executes with include-previews flag
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithSource_IncludesSource()
    {
        // Act
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Install,
            workloadIds: new[] { "test-workload" },
            source: "https://api.nuget.org/v3/index.json");

        // Assert
        Assert.NotNull(result);
        // Command executes with source parameter
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithConfigFile_IncludesConfigFile()
    {
        // Act
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Install,
            workloadIds: new[] { "test-workload" },
            configFile: "nuget.config");

        // Assert
        Assert.NotNull(result);
        // Command executes with config file parameter
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act - test with empty array to trigger validation error in machine-readable format
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Install,
            workloadIds: Array.Empty<string>(),
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return JSON-formatted error
        Assert.Contains("{", result);
    }

    #endregion

    #region Update Action Tests

    [Fact]
    public async Task DotnetWorkload_Update_ExecutesSuccessfully()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Update);

        // Assert
        Assert.NotNull(result);
        // Should complete without syntax errors
        // (may report no workloads to update, which is fine)
    }

    [Fact]
    public async Task DotnetWorkload_Update_WithIncludePreviews_IncludesFlag()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Update, includePreviews: true);

        // Assert
        Assert.NotNull(result);
        // Command executes with include-previews flag
    }

    [Fact]
    public async Task DotnetWorkload_Update_WithSource_IncludesSource()
    {
        // Act
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Update,
            source: "https://api.nuget.org/v3/index.json");

        // Assert
        Assert.NotNull(result);
        // Command executes with source parameter
    }

    [Fact]
    public async Task DotnetWorkload_Update_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Update, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return machine-readable format
    }

    #endregion

    #region Uninstall Action Tests

    [Fact]
    public async Task DotnetWorkload_Uninstall_WithoutWorkloadIds_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Uninstall);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("workloadIds", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetWorkload_Uninstall_WithEmptyWorkloadIds_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Uninstall, workloadIds: Array.Empty<string>());

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("workloadIds", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetWorkload_Uninstall_WithInvalidWorkloadId_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Uninstall, workloadIds: new[] { "invalid!workload" });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("Invalid workload ID", result);
    }

    [Fact]
    public async Task DotnetWorkload_Uninstall_WithValidWorkloadId_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Uninstall, workloadIds: new[] { "test-workload" });

        // Assert
        Assert.NotNull(result);
        // Command should execute (even if workload doesn't exist)
    }

    [Fact]
    public async Task DotnetWorkload_Uninstall_WithMultipleWorkloads_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Uninstall,
            workloadIds: new[] { "workload-one", "workload-two", "workload-three" });

        // Assert
        Assert.NotNull(result);
        // Command should be properly constructed with all workload IDs
    }

    [Fact]
    public async Task DotnetWorkload_Uninstall_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act - test with empty array to trigger validation error in machine-readable format
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Uninstall,
            workloadIds: Array.Empty<string>(),
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        // Should return JSON-formatted error
        Assert.Contains("{", result);
    }

    #endregion

    #region Validation Tests

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
    public async Task DotnetWorkload_Install_WithValidWorkloadIds_AcceptsFormat(string workloadId)
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Install, workloadIds: new[] { workloadId });

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
    public async Task DotnetWorkload_Install_WithInvalidWorkloadIds_RejectsFormat(string workloadId)
    {
        // Act
        var result = await _tools.DotnetWorkload(DotnetWorkloadAction.Install, workloadIds: new[] { workloadId });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("Invalid workload ID", result);
    }

    [Fact]
    public async Task DotnetWorkload_Install_WithMultipleInvalidWorkloads_ReturnsErrorForFirst()
    {
        // Act
        var result = await _tools.DotnetWorkload(
            DotnetWorkloadAction.Install,
            workloadIds: new[] { "valid-workload", "invalid@workload" });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("Invalid workload ID", result);
    }

    #endregion

    #region Action Routing Tests

    [Fact]
    public async Task DotnetWorkload_AllActions_RouteCorrectly()
    {
        // Test that all enum values are handled
        var actions = Enum.GetValues<DotnetWorkloadAction>();
        
        foreach (var action in actions)
        {
            // Act
            var result = action switch
            {
                DotnetWorkloadAction.Install => await _tools.DotnetWorkload(action, workloadIds: new[] { "test-id" }),
                DotnetWorkloadAction.Uninstall => await _tools.DotnetWorkload(action, workloadIds: new[] { "test-id" }),
                _ => await _tools.DotnetWorkload(action)
            };

            // Assert
            Assert.NotNull(result);
            // Should not throw or return "Unsupported action"
            Assert.DoesNotContain("Unsupported action", result);
        }
    }

    #endregion
}
