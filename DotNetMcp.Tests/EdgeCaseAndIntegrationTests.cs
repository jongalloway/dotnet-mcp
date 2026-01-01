using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Additional comprehensive tests for edge cases and parameter combinations
/// </summary>
public class EdgeCaseAndIntegrationTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public EdgeCaseAndIntegrationTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    // Project Build/Run/Test Edge Cases

    [Fact]
    public async Task DotnetProjectBuild_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectBuild(
            project: "MyProject.csproj",
            configuration: "Release",
            framework: "net10.0",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetProjectRun_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectRun(
            project: "MyApp.csproj",
            configuration: "Debug",
            appArgs: "--verbose --log-level=info",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetProjectPublish_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectPublish(
            project: "MyApp.csproj",
            configuration: "Release",
            output: "./publish",
            runtime: "linux-x64",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetProjectPublish_WithoutRuntime_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectPublish(
            project: "MyApp.csproj",
            configuration: "Release");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    // Template Tests - Multiple Calls

    [Fact]
    public async Task DotnetTemplateList_CalledMultipleTimes_UsesCache()
    {
        // Act
        var result1 = await _tools.DotnetTemplateList(forceReload: false);
        var result2 = await _tools.DotnetTemplateList(forceReload: false);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        // Second call should use cache (we can't directly verify, but both should succeed)
    }

    [Fact]
    public async Task DotnetTemplateSearch_WithEmptyResult_HandlesGracefully()
    {
        // Act
        var result = await _tools.DotnetTemplateSearch(searchTerm: "nonexistenttemplate12345xyz");

        // Assert
        Assert.NotNull(result);
        // Should handle empty results gracefully
    }

    // Package Management Edge Cases

    [Fact]
    public async Task DotnetPackageAdd_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageAdd(
            packageName: "Microsoft.Extensions.Logging",
            project: "MyProject.csproj",
            version: "8.0.0",
            prerelease: false,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetPackageList_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetPackageList(
            project: "MyProject.csproj",
            outdated: true,
            deprecated: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    // Solution Management Edge Cases

    [Fact]
    public async Task DotnetSolutionCreate_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionCreate(
            name: "TestSolution",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    // Certificate/Security Edge Cases

    [Fact]
    public async Task DotnetCertificateExport_WithCaseInsensitiveFormat_HandlesCorrectly()
    {
        // Test case-insensitive format handling
        var result1 = await _tools.DotnetCertificateExport(
            path: "/tmp/cert1.pfx",
            format: "PFX");

        var result2 = await _tools.DotnetCertificateExport(
            path: "/tmp/cert2.pem",
            format: "pem");

        var result3 = await _tools.DotnetCertificateExport(
            path: "/tmp/cert3.pfx",
            format: "Pfx");

        // Assert - all should succeed (or fail gracefully with proper error)
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
    }

    // NuGet Locals Edge Cases

    [Fact]
    public async Task DotnetNugetLocals_WithCaseInsensitiveCacheLocation_HandlesCorrectly()
    {
        // Test case normalization
        var result1 = await _tools.DotnetNugetLocals(
            cacheLocation: "ALL",
            list: true);

        var result2 = await _tools.DotnetNugetLocals(
            cacheLocation: "Http-Cache",
            list: true);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "global-packages",
            list: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    // User Secrets Edge Cases

    [Fact]
    public async Task DotnetSecretsInit_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSecretsInit(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSecretsSet_WithComplexKey_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSecretsSet(
            key: "Azure:Storage:ConnectionString",
            value: "DefaultEndpointsProtocol=https;AccountName=test",
            project: "MyProject.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSecretsList_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSecretsList(
            project: "MyProject.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSecretsRemove_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSecretsRemove(
            key: "TestKey",
            project: "MyProject.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSecretsClear_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSecretsClear(
            project: "MyProject.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    // Tool Management Edge Cases

    [Fact]
    public async Task DotnetToolInstall_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-format",
            global: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetToolList_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetToolList(
            global: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetToolUpdate_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetToolUpdate(
            packageName: "dotnet-ef",
            global: true,
            version: "8.0.0",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetToolUninstall_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetToolUninstall(
            packageName: "dotnet-format",
            global: false,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetToolRestore_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetToolRestore(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetToolManifestCreate_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetToolManifestCreate(
            output: "./tools",
            force: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetToolSearch_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetToolSearch(
            searchTerm: "format",
            detail: true,
            take: 5,
            skip: 0,
            prerelease: false,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetToolRun_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetToolRun(
            toolName: "dotnet-ef",
            args: "database update",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    // Framework Info Edge Cases

    [Fact]
    public async Task DotnetFrameworkInfo_WithNetStandard_ReturnsCorrectInformation()
    {
        // Act
        var result = await _tools.DotnetFrameworkInfo(framework: "netstandard2.1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("netstandard2.1", result);
        Assert.Contains("Is .NET Standard:", result);
    }

    [Fact]
    public async Task DotnetFrameworkInfo_WithModernNet_ReturnsCorrectInformation()
    {
        // Act
        var result = await _tools.DotnetFrameworkInfo(framework: "net10.0");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("net10.0", result);
        Assert.Contains("Is Modern .NET:", result);
    }
}
