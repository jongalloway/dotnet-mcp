using System;
using System.IO;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for package-related MCP tools
/// </summary>
[Collection("ProcessWideStateTests")]
public class PackageToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public PackageToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    private static async Task<string> ExecuteInTempDirectoryAsync(Func<Task<string>> action)
    {
        var originalDirectory = Environment.CurrentDirectory;
        var tempDirectory = Path.Combine(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            Environment.CurrentDirectory = tempDirectory;
            return await action();
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetPackageAdd_WithPackageName_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageAdd(
            packageName: "Newtonsoft.Json",
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageAdd_WithVersion_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageAdd(
            packageName: "Newtonsoft.Json",
            version: "13.0.3",
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json --version 13.0.3");
    }

    [Fact]
    public async Task DotnetPackageAdd_WithPrerelease_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageAdd(
            packageName: "Microsoft.AspNetCore.App",
            prerelease: true,
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Microsoft.AspNetCore.App --prerelease");
    }

    [Fact]
    public async Task DotnetPackageAdd_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageAdd(
            packageName: "Newtonsoft.Json",
            project: "MyProject.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add \"MyProject.csproj\" package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageAdd_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageAdd(
            packageName: "Newtonsoft.Json",
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageList_WithoutParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list package");
    }

    [Fact]
    public async Task DotnetPackageList_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(project: "MyProject.csproj", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list \"MyProject.csproj\" package");
    }

    [Fact]
    public async Task DotnetPackageList_WithOutdated_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(outdated: true, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list package --outdated");
    }

    [Fact]
    public async Task DotnetPackageList_WithDeprecated_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(deprecated: true, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list package --deprecated");
    }

    [Fact]
    public async Task DotnetPackageList_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list package");
    }

    [Fact]
    public async Task DotnetPackageRemove_WithPackageName_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageRemove(
            packageName: "Newtonsoft.Json",
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageRemove_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageRemove(
            packageName: "Newtonsoft.Json",
            project: "MyProject.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove \"MyProject.csproj\" package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageRemove_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageRemove(
            packageName: "Newtonsoft.Json",
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageUpdate_WithPackageName_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageUpdate(
            packageName: "Newtonsoft.Json",
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageUpdate_WithVersion_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageUpdate(
            packageName: "Newtonsoft.Json",
            version: "13.0.3",
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json --version 13.0.3");
    }

    [Fact]
    public async Task DotnetPackageUpdate_WithPrerelease_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageUpdate(
            packageName: "Microsoft.AspNetCore.App",
            prerelease: true,
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Microsoft.AspNetCore.App --prerelease");
    }

    [Fact]
    public async Task DotnetPackageUpdate_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageUpdate(
            packageName: "Newtonsoft.Json",
            project: "MyProject.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add \"MyProject.csproj\" package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageSearch_WithSearchTerm_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(searchTerm: "json", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search json");
    }

    [Fact]
    public async Task DotnetPackageSearch_WithTake_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "json",
            take: 10,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search json --take 10");
    }

    [Fact]
    public async Task DotnetPackageSearch_WithSkip_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "json",
            skip: 5,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search json --skip 5");
    }

    [Fact]
    public async Task DotnetPackageSearch_WithPrerelease_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "aspnetcore",
            prerelease: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search aspnetcore --prerelease");
    }

    [Fact]
    public async Task DotnetPackageSearch_WithExactMatch_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "Newtonsoft.Json",
            exactMatch: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search Newtonsoft.Json --exact-match");
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
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search aspnetcore --take 10 --skip 5 --prerelease");
    }

    [Fact]
    public async Task DotnetPackCreate_WithoutParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackCreate(machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack");
    }

    [Fact]
    public async Task DotnetPackCreate_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(project: "MyLibrary.csproj", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack \"MyLibrary.csproj\"");
    }

    [Fact]
    public async Task DotnetPackCreate_WithConfiguration_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(configuration: "Release", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack -c Release");
    }

    [Fact]
    public async Task DotnetPackCreate_WithOutput_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(output: "./packages", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack -o \"./packages\"");
    }

    [Fact]
    public async Task DotnetPackCreate_WithIncludeSymbols_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(includeSymbols: true, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack --include-symbols");
    }

    [Fact]
    public async Task DotnetPackCreate_WithIncludeSource_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackCreate(includeSource: true, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack --include-source");
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
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack \"MyLibrary.csproj\" -c Release -o \"./packages\" --include-symbols --include-source");
    }
}
