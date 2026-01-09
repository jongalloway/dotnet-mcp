using System;
using System.IO;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Backward compatibility smoke tests for legacy package-related MCP tools.
/// 
/// NOTE: Comprehensive parameter-matrix and command-building tests are in ConsolidatedPackageToolTests.cs.
/// These tests ensure legacy tools still work for backwards compatibility.
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
    public async Task DotnetPackageAdd_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetPackageAdd(
            packageName: "Newtonsoft.Json",
            machineReadable: true));

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageList_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetPackageList(machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list package");
    }

    [Fact]
    public async Task DotnetPackageRemove_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetPackageRemove(
            packageName: "Newtonsoft.Json",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageUpdate_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetPackageUpdate(
            packageName: "Newtonsoft.Json",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add package Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackageSearch_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetPackageSearch(
            searchTerm: "Newtonsoft",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search Newtonsoft");
    }
}
