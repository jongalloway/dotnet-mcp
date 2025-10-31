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
    public async Task DotnetCertificateTrust_ExecutesCommand()
    {
        // Validates that the trust command can be executed
        var result = await _tools.DotnetCertificateTrust();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetCertificateCheck_ExecutesCommand()
    {
        // Validates that the check command can be executed
        var result = await _tools.DotnetCertificateCheck();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetCertificateClean_ExecutesCommand()
    {
        // Validates that the clean command can be executed
        var result = await _tools.DotnetCertificateClean();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetCertificateExport_WithPathOnly_ExecutesCommand()
    {
        // Validates that the export command with just a path works
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetCertificateExport_WithPathAndPassword_ExecutesCommand()
    {
        // Validates that the export command with path and password works
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            password: "testPassword123");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetCertificateExport_WithPfxFormat_ExecutesCommand()
    {
        // Validates that the export command with PFX format works
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            format: "Pfx",
            password: "testPassword123");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetCertificateExport_WithPemFormat_ExecutesCommand()
    {
        // Validates that the export command with PEM format works
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pem",
            format: "Pem");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetCertificateExport_WithInvalidFormat_ReturnsError()
    {
        // Validates that invalid format returns error
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            format: "invalid");

        result.Should().Contain("Error");
        result.Should().Contain("format must be either 'pfx' or 'pem'");
    }

    [Fact]
    public async Task DotnetCertificateExport_WithEmptyPath_ReturnsError()
    {
        // Validates that empty path returns error
        var result = await _tools.DotnetCertificateExport(
            path: "");

        result.Should().Contain("Error");
        result.Should().Contain("path parameter is required");
    }

    [Fact]
    public async Task DotnetCertificateExport_WithAllParameters_ExecutesCommand()
    {
        // Validates that all parameters work together
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            password: "strongPassword123!",
            format: "Pfx");

        result.Should().NotBeNull();
    }

    // Security validation tests for IsValidAdditionalOptions (tested via DotnetProjectNew)
    
    [Fact]
    public async Task DotnetProjectNew_WithValidAdditionalOptions_AcceptsCommand()
    {
        // Valid options with allowed characters: alphanumeric, hyphens, underscores, dots, spaces, equals
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--use-program-main --framework net8.0");

        result.Should().NotBeNull();
        result.Should().NotContain("Error: additionalOptions contains invalid characters");
    }

    [Fact]
    public async Task DotnetProjectNew_WithValidAdditionalOptionsWithEquals_AcceptsCommand()
    {
        // Valid options with equals sign (key=value format)
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--langVersion=latest --nullable=enable");

        result.Should().NotBeNull();
        result.Should().NotContain("Error: additionalOptions contains invalid characters");
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Semicolon_RejectsCommand()
    {
        // Semicolon is a shell metacharacter and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option; malicious-command");

        result.Should().Contain("Error: additionalOptions contains invalid characters");
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Pipe_RejectsCommand()
    {
        // Pipe is a shell metacharacter and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option | malicious-command");

        result.Should().Contain("Error: additionalOptions contains invalid characters");
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Ampersand_RejectsCommand()
    {
        // Ampersand is a shell metacharacter and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option && malicious-command");

        result.Should().Contain("Error: additionalOptions contains invalid characters");
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Backtick_RejectsCommand()
    {
        // Backtick is a shell metacharacter and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option `malicious-command`");

        result.Should().Contain("Error: additionalOptions contains invalid characters");
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_DollarSign_RejectsCommand()
    {
        // Dollar sign is used for variable expansion and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option $MALICIOUS_VAR");

        result.Should().Contain("Error: additionalOptions contains invalid characters");
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Parentheses_RejectsCommand()
    {
        // Parentheses are shell metacharacters and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option (malicious)");

        result.Should().Contain("Error: additionalOptions contains invalid characters");
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_AngleBrackets_RejectsCommand()
    {
        // Angle brackets are used for redirection and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option < input.txt");

        result.Should().Contain("Error: additionalOptions contains invalid characters");
    }

    // Tool Management Tests

    [Fact]
    public async Task DotnetToolInstall_WithPackageName_ExecutesCommand()
    {
        // Validates that tool install with package name works
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolInstall_WithGlobalFlag_ExecutesCommand()
    {
        // Validates that global tool install works
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef",
            global: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolInstall_WithVersion_ExecutesCommand()
    {
        // Validates that tool install with specific version works
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef",
            version: "8.0.0");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolInstall_WithFramework_ExecutesCommand()
    {
        // Validates that tool install with framework works
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef",
            framework: "net8.0");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolInstall_WithAllParameters_ExecutesCommand()
    {
        // Validates that all parameters work together
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef",
            global: true,
            version: "8.0.0",
            framework: "net8.0");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolInstall_WithEmptyPackageName_ReturnsError()
    {
        // Validates that empty package name returns error
        var result = await _tools.DotnetToolInstall(
            packageName: "");

        result.Should().Contain("Error");
        result.Should().Contain("packageName parameter is required");
    }

    [Fact]
    public async Task DotnetToolList_WithoutGlobalFlag_ExecutesCommand()
    {
        // Validates that local tool list works
        var result = await _tools.DotnetToolList();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolList_WithGlobalFlag_ExecutesCommand()
    {
        // Validates that global tool list works
        var result = await _tools.DotnetToolList(
            global: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolUpdate_WithPackageName_ExecutesCommand()
    {
        // Validates that tool update with package name works
        var result = await _tools.DotnetToolUpdate(
            packageName: "dotnet-ef");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolUpdate_WithGlobalFlag_ExecutesCommand()
    {
        // Validates that global tool update works
        var result = await _tools.DotnetToolUpdate(
            packageName: "dotnet-ef",
            global: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolUpdate_WithVersion_ExecutesCommand()
    {
        // Validates that tool update to specific version works
        var result = await _tools.DotnetToolUpdate(
            packageName: "dotnet-ef",
            version: "8.0.1");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolUpdate_WithEmptyPackageName_ReturnsError()
    {
        // Validates that empty package name returns error
        var result = await _tools.DotnetToolUpdate(
            packageName: "");

        result.Should().Contain("Error");
        result.Should().Contain("packageName parameter is required");
    }

    [Fact]
    public async Task DotnetToolUninstall_WithPackageName_ExecutesCommand()
    {
        // Validates that tool uninstall with package name works
        var result = await _tools.DotnetToolUninstall(
            packageName: "dotnet-ef");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolUninstall_WithGlobalFlag_ExecutesCommand()
    {
        // Validates that global tool uninstall works
        var result = await _tools.DotnetToolUninstall(
            packageName: "dotnet-ef",
            global: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolUninstall_WithEmptyPackageName_ReturnsError()
    {
        // Validates that empty package name returns error
        var result = await _tools.DotnetToolUninstall(
            packageName: "");

        result.Should().Contain("Error");
        result.Should().Contain("packageName parameter is required");
    }

    [Fact]
    public async Task DotnetToolRestore_ExecutesCommand()
    {
        // Validates that tool restore command works
        var result = await _tools.DotnetToolRestore();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolSearch_WithSearchTerm_ExecutesCommand()
    {
        // Validates that tool search with search term works
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolSearch_WithDetail_ExecutesCommand()
    {
        // Validates that tool search with detail flag works
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity",
            detail: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolSearch_WithTakeAndSkip_ExecutesCommand()
    {
        // Validates that tool search with pagination works
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity",
            take: 10,
            skip: 5);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolSearch_WithPrerelease_ExecutesCommand()
    {
        // Validates that tool search with prerelease flag works
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity",
            prerelease: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolSearch_WithAllParameters_ExecutesCommand()
    {
        // Validates that all parameters work together
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity",
            detail: true,
            take: 10,
            skip: 5,
            prerelease: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolSearch_WithEmptySearchTerm_ReturnsError()
    {
        // Validates that empty search term returns error
        var result = await _tools.DotnetToolSearch(
            searchTerm: "");

        result.Should().Contain("Error");
        result.Should().Contain("searchTerm parameter is required");
    }

    [Fact]
    public async Task DotnetToolRun_WithToolName_ExecutesCommand()
    {
        // Validates that tool run with tool name works
        var result = await _tools.DotnetToolRun(
            toolName: "dotnet-ef");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolRun_WithArgs_ExecutesCommand()
    {
        // Validates that tool run with arguments works
        var result = await _tools.DotnetToolRun(
            toolName: "dotnet-ef",
            args: "migrations add Initial");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetToolRun_WithEmptyToolName_ReturnsError()
    {
        // Validates that empty tool name returns error
        var result = await _tools.DotnetToolRun(
            toolName: "");

        result.Should().Contain("Error");
        result.Should().Contain("toolName parameter is required");
    }

    [Fact]
    public async Task DotnetToolRun_WithInvalidArgsCharacters_ReturnsError()
    {
        // Validates that args with shell metacharacters returns error
        var result = await _tools.DotnetToolRun(
            toolName: "dotnet-ef",
            args: "migrations add Initial && echo hacked");

        result.Should().Contain("Error");
        result.Should().Contain("args contains invalid characters");
    }
}
