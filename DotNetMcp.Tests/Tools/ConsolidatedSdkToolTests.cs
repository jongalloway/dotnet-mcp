using System;
using System.Threading.Tasks;
using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for the consolidated dotnet_sdk command.
/// </summary>
public class ConsolidatedSdkToolTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public ConsolidatedSdkToolTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetSdk_WithMissingWorkingDirectory_MachineReadable_ReturnsValidationError()
    {
        var missingDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dotnet-mcp-missing-" + Guid.NewGuid().ToString("N")));

        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.Version,
            workingDirectory: missingDir,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("INVALID_PARAMS", result);
        Assert.Contains("workingDirectory", result, StringComparison.OrdinalIgnoreCase);
    }

    #region Version Action Tests

    [Fact]
    public async Task DotnetSdk_Version_ExecutesCommand()
    {
        // Test basic version action
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.Version);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_Version_WithMachineReadable_ExecutesCommand()
    {
        // Test version with machine-readable output
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.Version,
            machineReadable: true);

        Assert.NotNull(result);
    }

    #endregion

    #region Info Action Tests

    [Fact]
    public async Task DotnetSdk_Info_ExecutesCommand()
    {
        // Test basic info action
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.Info);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_Info_WithMachineReadable_ExecutesCommand()
    {
        // Test info with machine-readable output
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.Info,
            machineReadable: true);

        Assert.NotNull(result);
    }

    #endregion

    #region ListSdks Action Tests

    [Fact]
    public async Task DotnetSdk_ListSdks_ExecutesCommand()
    {
        // Test list SDKs action
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.ListSdks);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_ListSdks_WithMachineReadable_ExecutesCommand()
    {
        // Test list SDKs with machine-readable output
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.ListSdks,
            machineReadable: true);

        Assert.NotNull(result);
    }

    #endregion

    #region ListRuntimes Action Tests

    [Fact]
    public async Task DotnetSdk_ListRuntimes_ExecutesCommand()
    {
        // Test list runtimes action
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.ListRuntimes);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_ListRuntimes_WithMachineReadable_ExecutesCommand()
    {
        // Test list runtimes with machine-readable output
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.ListRuntimes,
            machineReadable: true);

        Assert.NotNull(result);
    }

    #endregion

    #region ListTemplates Action Tests

    [Fact]
    public async Task DotnetSdk_ListTemplates_ExecutesCommand()
    {
        // Test list templates action
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.ListTemplates);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_ListTemplates_WithForceReload_ExecutesCommand()
    {
        // Test list templates with force reload
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.ListTemplates,
            forceReload: true);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    #endregion

    #region SearchTemplates Action Tests

    [Fact]
    public async Task DotnetSdk_SearchTemplates_WithSearchTerm_ExecutesCommand()
    {
        // Test search templates with search term
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates,
            searchTerm: "console");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_SearchTemplates_WithForceReload_ExecutesCommand()
    {
        // Test search templates with force reload
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates,
            searchTerm: "web",
            forceReload: true);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_SearchTemplates_WithoutSearchTerm_ReturnsError()
    {
        // Test search templates without search term
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates);

        Assert.NotNull(result);
        Assert.Contains("Error", result);
        Assert.Contains("searchTerm", result);
    }

    [Fact]
    public async Task DotnetSdk_SearchTemplates_WithoutSearchTerm_MachineReadable_ReturnsError()
    {
        // Test search templates without search term in machine-readable format
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("searchTerm", result);
        Assert.Contains("required", result);
    }

    #endregion

    #region TemplateInfo Action Tests

    [Fact]
    public async Task DotnetSdk_TemplateInfo_WithTemplateShortName_ExecutesCommand()
    {
        // Test template info with template short name
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo,
            templateShortName: "console");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_TemplateInfo_WithForceReload_ExecutesCommand()
    {
        // Test template info with force reload
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo,
            templateShortName: "console",
            forceReload: true);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_TemplateInfo_WithoutTemplateShortName_ReturnsError()
    {
        // Test template info without template short name
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo);

        Assert.NotNull(result);
        Assert.Contains("Error", result);
        Assert.Contains("templateShortName", result);
    }

    [Fact]
    public async Task DotnetSdk_TemplateInfo_WithoutTemplateShortName_MachineReadable_ReturnsError()
    {
        // Test template info without template short name in machine-readable format
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("templateShortName", result);
        Assert.Contains("required", result);
    }

    #endregion

    #region ClearTemplateCache Action Tests

    [Fact]
    public async Task DotnetSdk_ClearTemplateCache_ExecutesCommand()
    {
        // Test clear template cache action
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.ClearTemplateCache);

        Assert.NotNull(result);
        Assert.Contains("cleared", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region FrameworkInfo Action Tests

    [Fact]
    public async Task DotnetSdk_FrameworkInfo_WithoutFramework_ExecutesCommand()
    {
        // Test framework info without specific framework (lists all)
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.FrameworkInfo);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DotnetSdk_FrameworkInfo_WithFramework_ExecutesCommand()
    {
        // Test framework info with specific framework
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.FrameworkInfo,
            framework: "net8.0");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("net8.0", result);
    }

    #endregion

    #region CacheMetrics Action Tests

    [Fact]
    public async Task DotnetSdk_CacheMetrics_ExecutesCommand()
    {
        // Test cache metrics action
        var result = await _tools.DotnetSdk(
            action: DotnetSdkAction.CacheMetrics);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Cache Metrics", result);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task DotnetSdk_InvalidAction_ReturnsError()
    {
        // Test with an invalid action (cast from invalid int)
        var invalidAction = (DotnetSdkAction)9999;
        var result = await _tools.DotnetSdk(action: invalidAction);

        Assert.NotNull(result);
        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task DotnetSdk_InvalidAction_MachineReadable_ReturnsJsonError()
    {
        // Test with an invalid action in machine-readable format
        var invalidAction = (DotnetSdkAction)9999;
        var result = await _tools.DotnetSdk(
            action: invalidAction,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("error", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task DotnetSdk_MultipleActions_ExecuteSuccessfully()
    {
        // Test multiple actions in sequence to ensure no state issues
        var versionResult = await _tools.DotnetSdk(action: DotnetSdkAction.Version);
        Assert.NotNull(versionResult);
        Assert.NotEmpty(versionResult);

        var infoResult = await _tools.DotnetSdk(action: DotnetSdkAction.Info);
        Assert.NotNull(infoResult);
        Assert.NotEmpty(infoResult);

        var listSdksResult = await _tools.DotnetSdk(action: DotnetSdkAction.ListSdks);
        Assert.NotNull(listSdksResult);
        Assert.NotEmpty(listSdksResult);

        var listRuntimesResult = await _tools.DotnetSdk(action: DotnetSdkAction.ListRuntimes);
        Assert.NotNull(listRuntimesResult);
        Assert.NotEmpty(listRuntimesResult);
    }

    [Fact]
    public async Task DotnetSdk_TemplateWorkflow_ExecutesSuccessfully()
    {
        // Test complete template workflow
        var listResult = await _tools.DotnetSdk(action: DotnetSdkAction.ListTemplates);
        Assert.NotNull(listResult);
        Assert.NotEmpty(listResult);

        var searchResult = await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates,
            searchTerm: "console");
        Assert.NotNull(searchResult);
        Assert.NotEmpty(searchResult);

        var infoResult = await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo,
            templateShortName: "console");
        Assert.NotNull(infoResult);
        Assert.NotEmpty(infoResult);
    }

    #endregion
}
