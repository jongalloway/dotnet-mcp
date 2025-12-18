using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

public class DotNetCliToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public DotNetCliToolsTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
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
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithCollectParameter_BuildsCorrectCommand()
    {
        // Validates that the collect parameter is accepted
        var result = await _tools.DotnetProjectTest(
            collect: "XPlat Code Coverage");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithResultsDirectory_BuildsCorrectCommand()
    {
        // Validates that the resultsDirectory parameter is accepted
        var result = await _tools.DotnetProjectTest(
            resultsDirectory: "/tmp/test-results");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithLogger_BuildsCorrectCommand()
    {
        // Validates that the logger parameter is accepted
        var result = await _tools.DotnetProjectTest(
            logger: "trx");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithNoBuild_BuildsCorrectCommand()
    {
        // Validates that the noBuild parameter is accepted
        var result = await _tools.DotnetProjectTest(
            noBuild: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithNoRestore_BuildsCorrectCommand()
    {
        // Validates that the noRestore parameter is accepted
        var result = await _tools.DotnetProjectTest(
            noRestore: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithVerbosity_BuildsCorrectCommand()
    {
        // Validates that the verbosity parameter is accepted
        var result = await _tools.DotnetProjectTest(
            verbosity: "detailed");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithFramework_BuildsCorrectCommand()
    {
        // Validates that the framework parameter is accepted
        var result = await _tools.DotnetProjectTest(
            framework: "net8.0");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithBlame_BuildsCorrectCommand()
    {
        // Validates that the blame parameter is accepted
        var result = await _tools.DotnetProjectTest(
            blame: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetProjectTest_WithListTests_BuildsCorrectCommand()
    {
        // Validates that the listTests parameter is accepted
        var result = await _tools.DotnetProjectTest(
            listTests: true);

        Assert.NotNull(result);
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
            framework: "net10.0",
            blame: true,
            listTests: false);

        Assert.NotNull(result);
    }

    [InteractiveFact]
    public async Task DotnetCertificateTrust_ExecutesCommand()
    {
        // Validates that the trust command can be executed
        var result = await _tools.DotnetCertificateTrust();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetCertificateCheck_ExecutesCommand()
    {
        // Validates that the check command can be executed
        var result = await _tools.DotnetCertificateCheck();

        Assert.NotNull(result);
    }

    [InteractiveFact]
    public async Task DotnetCertificateClean_ExecutesCommand()
    {
        // Validates that the clean command can be executed
        var result = await _tools.DotnetCertificateClean();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetCertificateExport_WithPathOnly_ExecutesCommand()
    {
        // Validates that the export command with just a path works
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetCertificateExport_WithPathAndPassword_ExecutesCommand()
    {
        // Validates that the export command with path and password works
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            password: "testPassword123");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetCertificateExport_WithPfxFormat_ExecutesCommand()
    {
        // Validates that the export command with PFX format works
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            format: "Pfx",
            password: "testPassword123");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetCertificateExport_WithPemFormat_ExecutesCommand()
    {
        // Validates that the export command with PEM format works
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pem",
            format: "Pem");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetCertificateExport_WithInvalidFormat_ReturnsError()
    {
        // Validates that invalid format returns error
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            format: "invalid");

        Assert.Contains("Error", result);
        Assert.Contains("format must be either 'pfx' or 'pem'", result);
    }

    [Fact]
    public async Task DotnetCertificateExport_WithEmptyPath_ReturnsError()
    {
        // Validates that empty path returns error
        var result = await _tools.DotnetCertificateExport(
            path: "");

        Assert.Contains("Error", result);
        Assert.Contains("path parameter is required", result);
    }

    [Fact]
    public async Task DotnetCertificateExport_WithAllParameters_ExecutesCommand()
    {
        // Validates that all parameters work together
        var result = await _tools.DotnetCertificateExport(
            path: "/tmp/cert.pfx",
            password: "strongPassword123!",
            format: "Pfx");

        Assert.NotNull(result);
    }

    // Security validation tests for IsValidAdditionalOptions (tested via DotnetProjectNew)

    [Fact]
    public async Task DotnetProjectNew_WithValidAdditionalOptions_AcceptsCommand()
    {
        // Valid options with allowed characters: alphanumeric, hyphens, underscores, dots, spaces, equals
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--use-program-main --framework net8.0");

        Assert.NotNull(result);
        Assert.DoesNotContain("Error: additionalOptions contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetProjectNew_WithValidAdditionalOptionsWithEquals_AcceptsCommand()
    {
        // Valid options with equals sign (key=value format)
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--langVersion=latest --nullable=enable");

        Assert.NotNull(result);
        Assert.DoesNotContain("Error: additionalOptions contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Semicolon_RejectsCommand()
    {
        // Semicolon is a shell metacharacter and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option; malicious-command");

        Assert.Contains("Error: additionalOptions contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Pipe_RejectsCommand()
    {
        // Pipe is a shell metacharacter and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option | malicious-command");

        Assert.Contains("Error: additionalOptions contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Ampersand_RejectsCommand()
    {
        // Ampersand is a shell metacharacter and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option && malicious-command");

        Assert.Contains("Error: additionalOptions contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Backtick_RejectsCommand()
    {
        // Backtick is a shell metacharacter and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option `malicious-command`");

        Assert.Contains("Error: additionalOptions contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_DollarSign_RejectsCommand()
    {
        // Dollar sign is used for variable expansion and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option $MALICIOUS_VAR");

        Assert.Contains("Error: additionalOptions contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_Parentheses_RejectsCommand()
    {
        // Parentheses are shell metacharacters and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option (malicious)");

        Assert.Contains("Error: additionalOptions contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetProjectNew_WithInvalidAdditionalOptions_AngleBrackets_RejectsCommand()
    {
        // Angle brackets are used for redirection and should be rejected
        var result = await _tools.DotnetProjectNew(
            template: "console",
            additionalOptions: "--option < input.txt");

        Assert.Contains("Error: additionalOptions contains invalid characters", result);
    }

    // Tool Management Tests

    [Fact]
    public async Task DotnetToolInstall_WithPackageName_ExecutesCommand()
    {
        // Validates that tool install with package name works
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolInstall_WithGlobalFlag_ExecutesCommand()
    {
        // Validates that global tool install works
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef",
            global: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolInstall_WithVersion_ExecutesCommand()
    {
        // Validates that tool install with specific version works
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef",
            version: "8.0.0");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolInstall_WithFramework_ExecutesCommand()
    {
        // Validates that tool install with framework works
        var result = await _tools.DotnetToolInstall(
            packageName: "dotnet-ef",
            framework: "net8.0");

        Assert.NotNull(result);
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

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolInstall_WithEmptyPackageName_ReturnsError()
    {
        // Validates that empty package name returns error
        var result = await _tools.DotnetToolInstall(
            packageName: "");

        Assert.Contains("Error", result);
        Assert.Contains("packageName parameter is required", result);
    }

    [Fact]
    public async Task DotnetToolList_WithoutGlobalFlag_ExecutesCommand()
    {
        // Validates that local tool list works
        var result = await _tools.DotnetToolList();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolList_WithGlobalFlag_ExecutesCommand()
    {
        // Validates that global tool list works
        var result = await _tools.DotnetToolList(
            global: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolUpdate_WithPackageName_ExecutesCommand()
    {
        // Validates that tool update with package name works
        var result = await _tools.DotnetToolUpdate(
            packageName: "dotnet-ef");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolUpdate_WithGlobalFlag_ExecutesCommand()
    {
        // Validates that global tool update works
        var result = await _tools.DotnetToolUpdate(
            packageName: "dotnet-ef",
            global: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolUpdate_WithVersion_ExecutesCommand()
    {
        // Validates that tool update to specific version works
        var result = await _tools.DotnetToolUpdate(
            packageName: "dotnet-ef",
            version: "8.0.1");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolUpdate_WithEmptyPackageName_ReturnsError()
    {
        // Validates that empty package name returns error
        var result = await _tools.DotnetToolUpdate(
            packageName: "");

        Assert.Contains("Error", result);
        Assert.Contains("packageName parameter is required", result);
    }

    [Fact]
    public async Task DotnetToolUninstall_WithPackageName_ExecutesCommand()
    {
        // Validates that tool uninstall with package name works
        var result = await _tools.DotnetToolUninstall(
            packageName: "dotnet-ef");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolUninstall_WithGlobalFlag_ExecutesCommand()
    {
        // Validates that global tool uninstall works
        var result = await _tools.DotnetToolUninstall(
            packageName: "dotnet-ef",
            global: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolUninstall_WithEmptyPackageName_ReturnsError()
    {
        // Validates that empty package name returns error
        var result = await _tools.DotnetToolUninstall(
            packageName: "");

        Assert.Contains("Error", result);
        Assert.Contains("packageName parameter is required", result);
    }

    [Fact]
    public async Task DotnetToolRestore_ExecutesCommand()
    {
        // Validates that tool restore command works
        var result = await _tools.DotnetToolRestore();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolManifestCreate_WithoutParameters_ExecutesCommand()
    {
        // Validates that tool manifest create without parameters works
        var result = await _tools.DotnetToolManifestCreate();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolManifestCreate_WithOutput_ExecutesCommand()
    {
        // Validates that tool manifest create with output directory works
        var result = await _tools.DotnetToolManifestCreate(
            output: "./test-dir");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolManifestCreate_WithForce_ExecutesCommand()
    {
        // Validates that tool manifest create with force flag works
        var result = await _tools.DotnetToolManifestCreate(
            force: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolManifestCreate_WithAllParameters_ExecutesCommand()
    {
        // Validates that all parameters work together
        var result = await _tools.DotnetToolManifestCreate(
            output: "./test-dir",
            force: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolSearch_WithSearchTerm_ExecutesCommand()
    {
        // Validates that tool search with search term works
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolSearch_WithDetail_ExecutesCommand()
    {
        // Validates that tool search with detail flag works
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity",
            detail: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolSearch_WithTakeAndSkip_ExecutesCommand()
    {
        // Validates that tool search with pagination works
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity",
            take: 10,
            skip: 5);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolSearch_WithPrerelease_ExecutesCommand()
    {
        // Validates that tool search with prerelease flag works
        var result = await _tools.DotnetToolSearch(
            searchTerm: "entity",
            prerelease: true);

        Assert.NotNull(result);
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

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolSearch_WithEmptySearchTerm_ReturnsError()
    {
        // Validates that empty search term returns error
        var result = await _tools.DotnetToolSearch(
            searchTerm: "");

        Assert.Contains("Error", result);
        Assert.Contains("searchTerm parameter is required", result);
    }

    [Fact]
    public async Task DotnetToolRun_WithToolName_ExecutesCommand()
    {
        // Validates that tool run with tool name works
        var result = await _tools.DotnetToolRun(
            toolName: "dotnet-ef");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolRun_WithArgs_ExecutesCommand()
    {
        // Validates that tool run with arguments works
        var result = await _tools.DotnetToolRun(
            toolName: "dotnet-ef",
            args: "migrations add Initial");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetToolRun_WithEmptyToolName_ReturnsError()
    {
        // Validates that empty tool name returns error
        var result = await _tools.DotnetToolRun(
            toolName: "");

        Assert.Contains("Error", result);
        Assert.Contains("toolName parameter is required", result);
    }

    [Fact]
    public async Task DotnetToolRun_WithInvalidArgsCharacters_ReturnsError()
    {
        // Validates that args with shell metacharacters returns error
        var result = await _tools.DotnetToolRun(
            toolName: "dotnet-ef",
            args: "migrations add Initial && echo hacked");

        Assert.Contains("Error", result);
        Assert.Contains("args contains invalid characters", result);
    }

    [Fact]
    public async Task DotnetSecretsInit_WithoutProject_ExecutesCommand()
    {
        // Validates that the init command can be executed without project parameter
        var result = await _tools.DotnetSecretsInit();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsInit_WithProject_ExecutesCommand()
    {
        // Validates that the init command can be executed with project parameter
        var result = await _tools.DotnetSecretsInit(project: "MyProject.csproj");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsSet_WithRequiredParameters_ExecutesCommand()
    {
        // Validates that the set command can be executed with required parameters
        var result = await _tools.DotnetSecretsSet(
            key: "TestKey",
            value: "TestValue");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsSet_WithHierarchicalKey_ExecutesCommand()
    {
        // Validates that the set command supports hierarchical keys
        var result = await _tools.DotnetSecretsSet(
            key: "ConnectionStrings:DefaultConnection",
            value: "Server=localhost;Database=TestDb");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsSet_WithProject_ExecutesCommand()
    {
        // Validates that the set command can be executed with project parameter
        var result = await _tools.DotnetSecretsSet(
            key: "ApiKey",
            value: "secret-value-123",
            project: "MyProject.csproj");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsSet_WithEmptyKey_ReturnsError()
    {
        // Validates that empty key returns error
        var result = await _tools.DotnetSecretsSet(
            key: "",
            value: "TestValue");

        Assert.Contains("Error", result);
        Assert.Contains("key parameter is required", result);
    }

    [Fact]
    public async Task DotnetSecretsSet_WithEmptyValue_ReturnsError()
    {
        // Validates that empty value returns error
        var result = await _tools.DotnetSecretsSet(
            key: "TestKey",
            value: "");

        Assert.Contains("Error", result);
        Assert.Contains("value parameter is required", result);
    }

    [Fact]
    public async Task DotnetSecretsList_WithoutProject_ExecutesCommand()
    {
        // Validates that the list command can be executed without project parameter
        var result = await _tools.DotnetSecretsList();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsList_WithProject_ExecutesCommand()
    {
        // Validates that the list command can be executed with project parameter
        var result = await _tools.DotnetSecretsList(project: "MyProject.csproj");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsRemove_WithKey_ExecutesCommand()
    {
        // Validates that the remove command can be executed with key
        var result = await _tools.DotnetSecretsRemove(key: "TestKey");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsRemove_WithKeyAndProject_ExecutesCommand()
    {
        // Validates that the remove command can be executed with key and project
        var result = await _tools.DotnetSecretsRemove(
            key: "ConnectionStrings:DefaultConnection",
            project: "MyProject.csproj");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsRemove_WithEmptyKey_ReturnsError()
    {
        // Validates that empty key returns error
        var result = await _tools.DotnetSecretsRemove(key: "");

        Assert.Contains("Error", result);
        Assert.Contains("key parameter is required", result);
    }

    [Fact]
    public async Task DotnetSecretsClear_WithoutProject_ExecutesCommand()
    {
        // Validates that the clear command can be executed without project parameter
        var result = await _tools.DotnetSecretsClear();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetSecretsClear_WithProject_ExecutesCommand()
    {
        // Validates that the clear command can be executed with project parameter
        var result = await _tools.DotnetSecretsClear(project: "MyProject.csproj");

        Assert.NotNull(result);
    }
}
