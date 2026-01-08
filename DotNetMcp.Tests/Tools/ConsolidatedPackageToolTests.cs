using System;
using System.IO;
using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Tools;

/// <summary>
/// Tests for the consolidated dotnet_package command.
/// </summary>
public class ConsolidatedPackageToolTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public ConsolidatedPackageToolTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
    }

    #region Add Action Tests

    [Fact]
    public async Task DotnetPackage_Add_WithPackageId_ExecutesCommand()
    {
        // Test basic add action
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: "Newtonsoft.Json",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackage_Add_WithVersion_ExecutesCommand()
    {
        // Test add with specific version
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: "Newtonsoft.Json",
            version: "13.0.3",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json --version 13.0.3");
    }

    [Fact]
    public async Task DotnetPackage_Add_WithProject_ExecutesCommand()
    {
        // Test add with project path
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: "Serilog",
            project: "MyProject.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add \"MyProject.csproj\" package Serilog");
    }

    [Fact]
    public async Task DotnetPackage_Add_WithPrerelease_ExecutesCommand()
    {
        // Test add with prerelease flag
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: "Microsoft.AspNetCore.App",
            prerelease: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Microsoft.AspNetCore.App --prerelease");
    }

    [Fact]
    public async Task DotnetPackage_Add_WithoutPackageId_ReturnsError()
    {
        // Test that missing packageId returns error
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: null);

        Assert.Contains("Error", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetPackage_Add_WithoutPackageId_MachineReadable_ReturnsError()
    {
        // Test that missing packageId returns error in machine-readable format
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Remove Action Tests

    [Fact]
    public async Task DotnetPackage_Remove_WithPackageId_ExecutesCommand()
    {
        // Test basic remove action
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Remove,
            packageId: "Newtonsoft.Json",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackage_Remove_WithProject_ExecutesCommand()
    {
        // Test remove with project path
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Remove,
            packageId: "Serilog",
            project: "MyProject.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove \"MyProject.csproj\" package Serilog");
    }

    [Fact]
    public async Task DotnetPackage_Remove_WithoutPackageId_ReturnsError()
    {
        // Test that missing packageId returns error
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Remove,
            packageId: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Search Action Tests

    [Fact]
    public async Task DotnetPackage_Search_WithSearchTerm_ExecutesCommand()
    {
        // Test basic search action
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Search,
            searchTerm: "Serilog",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search Serilog");
    }

    [Fact]
    public async Task DotnetPackage_Search_WithTake_ExecutesCommand()
    {
        // Test search with take parameter
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Search,
            searchTerm: "logging",
            take: 10,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search logging --take 10");
    }

    [Fact]
    public async Task DotnetPackage_Search_WithPrerelease_ExecutesCommand()
    {
        // Test search with prerelease flag
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Search,
            searchTerm: "AspNetCore",
            prerelease: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search AspNetCore --prerelease");
    }

    [Fact]
    public async Task DotnetPackage_Search_WithExactMatch_ExecutesCommand()
    {
        // Test search with exact match flag
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Search,
            searchTerm: "Newtonsoft.Json",
            exactMatch: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search Newtonsoft.Json --exact-match");
    }

    [Fact]
    public async Task DotnetPackage_Search_WithoutSearchTerm_ReturnsError()
    {
        // Test that missing searchTerm returns error
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Search,
            searchTerm: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("searchTerm", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Update Action Tests

    [Fact]
    public async Task DotnetPackage_Update_WithPackageId_ExecutesCommand()
    {
        // Test basic update action
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Update,
            packageId: "Newtonsoft.Json",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackage_Update_WithVersion_ExecutesCommand()
    {
        // Test update with specific version
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Update,
            packageId: "Serilog",
            version: "3.0.0",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Serilog --version 3.0.0");
    }

    [Fact]
    public async Task DotnetPackage_Update_WithoutPackageId_ReturnsError()
    {
        // Test that missing packageId returns error
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.Update,
            packageId: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("packageId", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region List Action Tests

    [Fact]
    public async Task DotnetPackage_List_WithoutParameters_ExecutesCommand()
    {
        // Test basic list action
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.List,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list package");
    }

    [Fact]
    public async Task DotnetPackage_List_WithProject_ExecutesCommand()
    {
        // Test list with project path
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.List,
            project: "MyProject.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list \"MyProject.csproj\" package");
    }

    [Fact]
    public async Task DotnetPackage_List_WithOutdated_ExecutesCommand()
    {
        // Test list with outdated flag
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.List,
            outdated: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list package --outdated");
    }

    [Fact]
    public async Task DotnetPackage_List_WithDeprecated_ExecutesCommand()
    {
        // Test list with deprecated flag
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.List,
            deprecated: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list package --deprecated");
    }

    #endregion

    #region AddReference Action Tests

    [Fact]
    public async Task DotnetPackage_AddReference_WithRequiredParameters_ExecutesCommand()
    {
        // Test add reference action
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.AddReference,
            project: "MyProject.csproj",
            referencePath: "MyLibrary.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add \"MyProject.csproj\" reference \"MyLibrary.csproj\"");
    }

    [Fact]
    public async Task DotnetPackage_AddReference_WithoutProject_ReturnsError()
    {
        // Test that missing project returns error
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.AddReference,
            project: null,
            referencePath: "MyLibrary.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("project", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetPackage_AddReference_WithoutReferencePath_ReturnsError()
    {
        // Test that missing referencePath returns error
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.AddReference,
            project: "MyProject.csproj",
            referencePath: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("referencePath", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region RemoveReference Action Tests

    [Fact]
    public async Task DotnetPackage_RemoveReference_WithRequiredParameters_ExecutesCommand()
    {
        // Test remove reference action
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.RemoveReference,
            project: "MyProject.csproj",
            referencePath: "MyLibrary.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove \"MyProject.csproj\" reference \"MyLibrary.csproj\"");
    }

    [Fact]
    public async Task DotnetPackage_RemoveReference_WithoutProject_ReturnsError()
    {
        // Test that missing project returns error
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.RemoveReference,
            project: null,
            referencePath: "MyLibrary.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("project", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetPackage_RemoveReference_WithoutReferencePath_ReturnsError()
    {
        // Test that missing referencePath returns error
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.RemoveReference,
            project: "MyProject.csproj",
            referencePath: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("referencePath", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ListReferences Action Tests

    [Fact]
    public async Task DotnetPackage_ListReferences_WithoutProject_ExecutesCommand()
    {
        // Test list references without project
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.ListReferences,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list reference");
    }

    [Fact]
    public async Task DotnetPackage_ListReferences_WithProject_ExecutesCommand()
    {
        // Test list references with project path
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.ListReferences,
            project: "MyProject.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list \"MyProject.csproj\" reference");
    }

    #endregion

    #region ClearCache Action Tests

    [Fact]
    public async Task DotnetPackage_ClearCache_WithoutCacheType_ExecutesCommand()
    {
        // Test clear cache with default (all)
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.ClearCache,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals all --clear");
    }

    [Fact]
    public async Task DotnetPackage_ClearCache_WithHttpCache_ExecutesCommand()
    {
        // Test clear cache with http-cache
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.ClearCache,
            cacheType: "http-cache",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals http-cache --clear");
    }

    [Fact]
    public async Task DotnetPackage_ClearCache_WithGlobalPackages_ExecutesCommand()
    {
        // Test clear cache with global-packages
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.ClearCache,
            cacheType: "global-packages",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals global-packages --clear");
    }

    [Fact]
    public async Task DotnetPackage_ClearCache_WithTemp_ExecutesCommand()
    {
        // Test clear cache with temp
        var result = await _tools.DotnetPackage(
            action: DotnetPackageAction.ClearCache,
            cacheType: "temp",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals temp --clear");
    }

    #endregion
}
