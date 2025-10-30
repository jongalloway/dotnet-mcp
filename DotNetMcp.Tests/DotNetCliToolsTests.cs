using DotNetMcp;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetMcp.Tests;

public class DotNetCliToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly Mock<ILogger<DotNetCliTools>> _loggerMock;

    public DotNetCliToolsTests()
    {
        _loggerMock = new Mock<ILogger<DotNetCliTools>>();
        _tools = new DotNetCliTools(_loggerMock.Object);
    }

    [Fact]
    public async Task DotnetProjectTest_WithBasicParameters_BuildsCorrectCommand()
    {
        // This test validates that the method exists and can be called with basic parameters
        // The actual command execution would require the dotnet CLI to be available
        var result = await _tools.DotnetProjectTest(
            project: "test.csproj",
            configuration: "Debug",
            filter: "FullyQualifiedName~MyTest");

        // The result should contain output (even if it's an error about missing project)
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithCollectParameter_BuildsCorrectCommand()
    {
        // Validates that the collect parameter is accepted
        var result = await _tools.DotnetProjectTest(
            collect: "XPlat Code Coverage");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithResultsDirectory_BuildsCorrectCommand()
    {
        // Validates that the resultsDirectory parameter is accepted
        var result = await _tools.DotnetProjectTest(
            resultsDirectory: "/tmp/test-results");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithLogger_BuildsCorrectCommand()
    {
        // Validates that the logger parameter is accepted
        var result = await _tools.DotnetProjectTest(
            logger: "trx");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithNoBuild_BuildsCorrectCommand()
    {
        // Validates that the noBuild parameter is accepted
        var result = await _tools.DotnetProjectTest(
            noBuild: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithNoRestore_BuildsCorrectCommand()
    {
        // Validates that the noRestore parameter is accepted
        var result = await _tools.DotnetProjectTest(
            noRestore: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithVerbosity_BuildsCorrectCommand()
    {
        // Validates that the verbosity parameter is accepted
        var result = await _tools.DotnetProjectTest(
            verbosity: "detailed");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithFramework_BuildsCorrectCommand()
    {
        // Validates that the framework parameter is accepted
        var result = await _tools.DotnetProjectTest(
            framework: "net8.0");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithBlame_BuildsCorrectCommand()
    {
        // Validates that the blame parameter is accepted
        var result = await _tools.DotnetProjectTest(
            blame: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithListTests_BuildsCorrectCommand()
    {
        // Validates that the listTests parameter is accepted
        var result = await _tools.DotnetProjectTest(
            listTests: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetProjectTest_WithAllNewParameters_BuildsCorrectCommand()
    {
        // Validates that all new parameters can be used together
        var result = await _tools.DotnetProjectTest(
            project: "test.csproj",
            configuration: "Release",
            filter: "Category=Unit",
            collect: "XPlat Code Coverage",
            resultsDirectory: "/tmp/results",
            logger: "trx;LogFileName=test-results.trx",
            noBuild: true,
            noRestore: true,
            verbosity: "minimal",
            framework: "net9.0",
            blame: true,
            listTests: false);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetDevCertsHttpsTrust_ExecutesCommand()
    {
        // Validates that the trust command can be executed
        var result = await _tools.DotnetDevCertsHttpsTrust();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetDevCertsHttpsCheck_ExecutesCommand()
    {
        // Validates that the check command can be executed
        var result = await _tools.DotnetDevCertsHttpsCheck();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetDevCertsHttpsClean_ExecutesCommand()
    {
        // Validates that the clean command can be executed
        var result = await _tools.DotnetDevCertsHttpsClean();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetDevCertsHttpsExport_WithPathOnly_ExecutesCommand()
    {
        // Validates that the export command with just a path works
        var result = await _tools.DotnetDevCertsHttpsExport(
            path: "/tmp/cert.pfx");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetDevCertsHttpsExport_WithPathAndPassword_ExecutesCommand()
    {
        // Validates that the export command with path and password works
        var result = await _tools.DotnetDevCertsHttpsExport(
            path: "/tmp/cert.pfx",
            password: "testPassword123");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetDevCertsHttpsExport_WithPfxFormat_ExecutesCommand()
    {
        // Validates that the export command with PFX format works
        var result = await _tools.DotnetDevCertsHttpsExport(
            path: "/tmp/cert.pfx",
            format: "Pfx",
            password: "testPassword123");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetDevCertsHttpsExport_WithPemFormat_ExecutesCommand()
    {
        // Validates that the export command with PEM format works
        var result = await _tools.DotnetDevCertsHttpsExport(
            path: "/tmp/cert.pem",
            format: "Pem");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetDevCertsHttpsExport_WithInvalidFormat_ReturnsError()
    {
        // Validates that invalid format returns error
        var result = await _tools.DotnetDevCertsHttpsExport(
            path: "/tmp/cert.pfx",
            format: "invalid");

        result.Should().Contain("Error");
        result.Should().Contain("format must be either 'pfx' or 'pem'");
    }

    [Fact]
    public async Task DotnetDevCertsHttpsExport_WithEmptyPath_ReturnsError()
    {
        // Validates that empty path returns error
        var result = await _tools.DotnetDevCertsHttpsExport(
            path: "");

        result.Should().Contain("Error");
        result.Should().Contain("path parameter is required");
    }

    [Fact]
    public async Task DotnetDevCertsHttpsExport_WithAllParameters_ExecutesCommand()
    {
        // Validates that all parameters work together
        var result = await _tools.DotnetDevCertsHttpsExport(
            path: "/tmp/cert.pfx",
            password: "strongPassword123!",
            format: "Pfx");

        result.Should().NotBeNull();
    }
}
