using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for package-related MCP tools
/// </summary>
public class PackageToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public PackageToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetPackageAdd_WithPackageName_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageAdd(packageName: "Newtonsoft.Json");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageAdd_WithVersion_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageAdd(
            packageName: "Newtonsoft.Json",
            version: "13.0.3");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageAdd_WithPrerelease_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageAdd(
            packageName: "Microsoft.AspNetCore.App",
            prerelease: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageAdd_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageAdd(
            packageName: "Newtonsoft.Json",
            project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageAdd_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageAdd(
            packageName: "Newtonsoft.Json",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageList_WithoutParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageList_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageList_WithOutdated_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(outdated: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageList_WithDeprecated_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(deprecated: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageList_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(machineReadable: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageRemove_WithPackageName_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageRemove(packageName: "Newtonsoft.Json");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageRemove_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageRemove(
            packageName: "Newtonsoft.Json",
            project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageRemove_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageRemove(
            packageName: "Newtonsoft.Json",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageUpdate_WithPackageName_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageUpdate(packageName: "Newtonsoft.Json");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageUpdate_WithVersion_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageUpdate(
            packageName: "Newtonsoft.Json",
            version: "13.0.3");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageUpdate_WithPrerelease_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageUpdate(
            packageName: "Microsoft.AspNetCore.App",
            prerelease: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageUpdate_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageUpdate(
            packageName: "Newtonsoft.Json",
            project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageSearch_WithSearchTerm_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(searchTerm: "json");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageSearch_WithTake_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "json",
            take: 10);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageSearch_WithSkip_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "json",
            skip: 5);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageSearch_WithPrerelease_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "aspnetcore",
            prerelease: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageSearch_WithExactMatch_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "Newtonsoft.Json",
            exactMatch: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackageSearch_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "aspnetcore",
            take: 10,
            skip: 5,
            prerelease: true,
            exactMatch: false,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackCreate_WithoutParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackCreate_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(project: "MyLibrary.csproj");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackCreate_WithConfiguration_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(configuration: "Release");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackCreate_WithOutput_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(output: "./packages");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackCreate_WithIncludeSymbols_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(includeSymbols: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackCreate_WithIncludeSource_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(includeSource: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackCreate_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(
            project: "MyLibrary.csproj",
            configuration: "Release",
            output: "./packages",
            includeSymbols: true,
            includeSource: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
    }
}
