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
        _tools = new DotNetCliTools(_logger, _concurrencyManager, new ProcessSessionManager());
    }

    [Fact]
    public async Task DotnetSdk_WithMissingWorkingDirectory_MachineReadable_ReturnsValidationError()
    {
        var missingDir = Path.GetFullPath(Path.Join(Path.GetTempPath(), "dotnet-mcp-missing-" + Guid.NewGuid().ToString("N")));

        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.Version,
            workingDirectory: missingDir));

        Assert.NotNull(result);
        Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workingDirectory", result.GetText(), StringComparison.OrdinalIgnoreCase);
    }

    #region Version Action Tests

    [Fact]
    public async Task DotnetSdk_Version_ExecutesCommand()
    {
        // Test basic version action
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.Version));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_Version_WithMachineReadable_ExecutesCommand()
    {
        // Test version with machine-readable output
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.Version));

        Assert.NotNull(result);
    }

    #endregion

    #region Info Action Tests

    [Fact]
    public async Task DotnetSdk_Info_ExecutesCommand()
    {
        // Test basic info action
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.Info));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_Info_WithMachineReadable_ExecutesCommand()
    {
        // Test info with machine-readable output
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.Info));

        Assert.NotNull(result);
    }

    #endregion

    #region ListSdks Action Tests

    [Fact]
    public async Task DotnetSdk_ListSdks_ExecutesCommand()
    {
        // Test list SDKs action
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.ListSdks));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_ListSdks_WithMachineReadable_ExecutesCommand()
    {
        // Test list SDKs with machine-readable output
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.ListSdks));

        Assert.NotNull(result);
    }

    #endregion

    #region ListRuntimes Action Tests

    [Fact]
    public async Task DotnetSdk_ListRuntimes_ExecutesCommand()
    {
        // Test list runtimes action
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.ListRuntimes));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_ListRuntimes_WithMachineReadable_ExecutesCommand()
    {
        // Test list runtimes with machine-readable output
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.ListRuntimes));

        Assert.NotNull(result);
    }

    #endregion

    #region ListTemplates Action Tests

    [Fact]
    public async Task DotnetSdk_ListTemplates_ExecutesCommand()
    {
        // Test list templates action
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.ListTemplates));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_ListTemplates_WithForceReload_ExecutesCommand()
    {
        // Test list templates with force reload
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.ListTemplates,
            forceReload: true));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    #endregion

    #region SearchTemplates Action Tests

    [Fact]
    public async Task DotnetSdk_SearchTemplates_WithSearchTerm_ExecutesCommand()
    {
        // Test search templates with search term
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates,
            searchTerm: "console"));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_SearchTemplates_WithForceReload_ExecutesCommand()
    {
        // Test search templates with force reload
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates,
            searchTerm: "web",
            forceReload: true));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_SearchTemplates_WithoutSearchTerm_ReturnsError()
    {
        // Test search templates without search term
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates));

        Assert.NotNull(result);
        Assert.Contains("Error", result.GetText());
        Assert.Contains("searchTerm", result.GetText());
    }

    [Fact]
    public async Task DotnetSdk_SearchTemplates_WithoutSearchTerm_MachineReadable_ReturnsError()
    {
        // Test search templates without search term in machine-readable format
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates));

        Assert.NotNull(result);
        Assert.Contains("searchTerm", result.GetText());
        Assert.Contains("required", result.GetText());
    }

    #endregion

    #region TemplateInfo Action Tests

    [Fact]
    public async Task DotnetSdk_TemplateInfo_WithTemplateShortName_ExecutesCommand()
    {
        // Test template info with template short name
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo,
            templateShortName: "console"));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_TemplateInfo_WithForceReload_ExecutesCommand()
    {
        // Test template info with force reload
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo,
            templateShortName: "console",
            forceReload: true));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_TemplateInfo_WithoutTemplateShortName_ReturnsError()
    {
        // Test template info without template short name
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo));

        Assert.NotNull(result);
        Assert.Contains("Error", result.GetText());
        Assert.Contains("templateShortName", result.GetText());
    }

    [Fact]
    public async Task DotnetSdk_TemplateInfo_WithoutTemplateShortName_MachineReadable_ReturnsError()
    {
        // Test template info without template short name in machine-readable format
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo));

        Assert.NotNull(result);
        Assert.Contains("templateShortName", result.GetText());
        Assert.Contains("required", result.GetText());
    }

    #endregion

    #region ClearTemplateCache Action Tests

    [Fact]
    public async Task DotnetSdk_ClearTemplateCache_ExecutesCommand()
    {
        // Test clear template cache action
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.ClearTemplateCache));

        Assert.NotNull(result);
        Assert.Contains("cleared", result.GetText(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region FrameworkInfo Action Tests

    [Fact]
    public async Task DotnetSdk_FrameworkInfo_WithoutFramework_ExecutesCommand()
    {
        // Test framework info without specific framework (lists all)
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.FrameworkInfo));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DotnetSdk_FrameworkInfo_WithFramework_ExecutesCommand()
    {
        // Test framework info with specific framework
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.FrameworkInfo,
            framework: "net8.0"));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.Contains("net8.0", result.GetText());
    }

    #endregion

    #region CacheMetrics Action Tests

    [Fact]
    public async Task DotnetSdk_CacheMetrics_ExecutesCommand()
    {
        // Test cache metrics action
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.CacheMetrics));

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.Contains("Cache Metrics", result.GetText());
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task DotnetSdk_InvalidAction_ReturnsError()
    {
        // Test with an invalid action (cast from invalid int)
        var invalidAction = (DotnetSdkAction)9999;
        var result = (await _tools.DotnetSdk(action: invalidAction));

        Assert.NotNull(result);
        Assert.Contains("Error", result.GetText());
    }

    [Fact]
    public async Task DotnetSdk_InvalidAction_MachineReadable_ReturnsJsonError()
    {
        // Test with an invalid action in machine-readable format
        var invalidAction = (DotnetSdkAction)9999;
        var result = (await _tools.DotnetSdk(
            action: invalidAction));

        Assert.NotNull(result);
        Assert.Contains("error", result.GetText(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task DotnetSdk_MultipleActions_ExecuteSuccessfully()
    {
        // Test multiple actions in sequence to ensure no state issues
        var versionResult = (await _tools.DotnetSdk(action: DotnetSdkAction.Version));
        Assert.NotNull(versionResult);
        Assert.NotEmpty(versionResult.Content);

        var infoResult = (await _tools.DotnetSdk(action: DotnetSdkAction.Info));
        Assert.NotNull(infoResult);
        Assert.NotEmpty(infoResult.Content);

        var listSdksResult = (await _tools.DotnetSdk(action: DotnetSdkAction.ListSdks));
        Assert.NotNull(listSdksResult);
        Assert.NotEmpty(listSdksResult.Content);

        var listRuntimesResult = (await _tools.DotnetSdk(action: DotnetSdkAction.ListRuntimes));
        Assert.NotNull(listRuntimesResult);
        Assert.NotEmpty(listRuntimesResult.Content);
    }

    [Fact]
    public async Task DotnetSdk_TemplateWorkflow_ExecutesSuccessfully()
    {
        // Test complete template workflow
        var listResult = (await _tools.DotnetSdk(action: DotnetSdkAction.ListTemplates));
        Assert.NotNull(listResult);
        Assert.NotEmpty(listResult.Content);

        var searchResult = (await _tools.DotnetSdk(
            action: DotnetSdkAction.SearchTemplates,
            searchTerm: "console"));
        Assert.NotNull(searchResult);
        Assert.NotEmpty(searchResult.Content);

        var infoResult = (await _tools.DotnetSdk(
            action: DotnetSdkAction.TemplateInfo,
            templateShortName: "console"));
        Assert.NotNull(infoResult);
        Assert.NotEmpty(infoResult.Content);
    }

    [Fact]
    public async Task DotnetSdk_ListTemplates_WhenTemplateEngineReturnsEmpty_ButCliFallbackSucceeds_ReturnsSuccess()
    {
        // This test verifies the fix for the template listing issue when Template Engine API returns empty
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            // Arrange: Simulate Template Engine API returning empty (the problem scenario)
            TemplateEngineHelper.LoadTemplatesOverride = () => 
                Task.FromResult(Enumerable.Empty<Microsoft.TemplateEngine.Abstractions.ITemplateInfo>());
            
            // Arrange: Simulate CLI fallback succeeding
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                if (args.Contains("new list"))
                {
                    // Simulate successful dotnet new list output
                    return Task.FromResult("These templates are available:\n\nTemplate Name     Short Name    Language\nConsole App       console       [C#],F#,VB\nClass Library     classlib      [C#],F#,VB");
                }
                throw new InvalidOperationException("Unexpected command");
            };

            // Act
            var result = (await _tools.DotnetSdk(
                action: DotnetSdkAction.ListTemplates));

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dotnet new list", result.GetText(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    #endregion

    #region Template Pack Install Tests

    [Fact]
    public async Task DotnetSdk_InstallTemplatePack_WithVersion_UsesAtSymbol()
    {
        // Arrange
        await using var temp = TempTemplatePackDirectory.Create(_tools, "dotnet-mcp-template-pack-test");

        // Act
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.InstallTemplatePack,
            templatePackage: temp.Path,
            templateVersion: "1.0.0"));

        // Assert
        Assert.NotNull(result);
        // Verify the @ symbol is used for version specification
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result.GetText(), $"dotnet new install \"{temp.Path}@1.0.0\"");
    }

    [Fact]
    public async Task DotnetSdk_InstallTemplatePack_WithVersionAndAtSymbolInPackage_ReturnsError()
    {
        // Arrange - templatePackage already contains @
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.InstallTemplatePack,
            templatePackage: "MyPackage@1.0.0",
            templateVersion: "2.0.0"));

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("templatePackage", result.GetText());
        Assert.Contains("already contains", result.GetText());
    }

    [Fact]
    public async Task DotnetSdk_InstallTemplatePack_WithVersionAndDoubleColonInPackage_ReturnsError()
    {
        // Arrange - templatePackage already contains :: (legacy syntax)
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.InstallTemplatePack,
            templatePackage: "MyPackage::1.0.0",
            templateVersion: "2.0.0"));

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("templatePackage", result.GetText());
        Assert.Contains("already contains", result.GetText());
    }

    [Fact]
    public async Task DotnetSdk_InstallTemplatePack_WithInvalidCharactersInPackageId_ReturnsError()
    {
        // Arrange - invalid character : in package ID (not as separator)
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.InstallTemplatePack,
            templatePackage: "My:Package"));

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("package ID", result.GetText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSdk_InstallTemplatePack_WithMultipleAtSeparators_ReturnsError()
    {
        // Arrange - multiple @ separators
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.InstallTemplatePack,
            templatePackage: "MyPackage@1.0.0@extra"));

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("templatePackage", result.GetText());
    }

    [Fact]
    public async Task DotnetSdk_InstallTemplatePack_WithBothSeparators_ReturnsError()
    {
        // Arrange - both @ and :: separators (ambiguous)
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.InstallTemplatePack,
            templatePackage: "MyPackage@1.0.0::extra"));

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("templatePackage", result.GetText());
    }

    [Fact]
    public async Task DotnetSdk_InstallTemplatePack_WithEmptyVersionAfterSeparator_ReturnsError()
    {
        // Arrange - @ separator with empty version
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.InstallTemplatePack,
            templatePackage: "MyPackage@"));

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("version", result.GetText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSdk_InstallTemplatePack_WithValidPackageIdInline_Succeeds()
    {
        // Arrange - valid package with inline version using @
        await using var temp = TempTemplatePackDirectory.Create(_tools, "dotnet-mcp-valid-test");

        // Create a minimal valid package structure to avoid actual NuGet call
        var result = (await _tools.DotnetSdk(
            action: DotnetSdkAction.InstallTemplatePack,
            templatePackage: temp.Path));

        // Should execute without validation error (may fail at install if not a valid template)
        Assert.NotNull(result);
    }

    #endregion

    #region ConfigureGlobalJson Action Tests

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_NoParameters_ReturnsError()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                workingDirectory: tempDir);

            Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sdkVersion", result.GetText(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_CreatesFileWithTestRunner()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                testRunner: "Microsoft.Testing.Platform",
                workingDirectory: tempDir);

            Assert.Contains("Created", result.GetText(), StringComparison.OrdinalIgnoreCase);
            var filePath = Path.Join(tempDir, "global.json");
            Assert.True(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.Contains("Microsoft.Testing.Platform", content);
            Assert.Contains("runner", content);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_CreatesFileWithSdkVersion()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                sdkVersion: "10.0.100",
                workingDirectory: tempDir);

            Assert.Contains("Created", result.GetText(), StringComparison.OrdinalIgnoreCase);
            var filePath = Path.Join(tempDir, "global.json");
            Assert.True(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.Contains("10.0.100", content);
            Assert.Contains("version", content);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_CreatesFileWithSdkVersionAndRollForward()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                sdkVersion: "10.0.100",
                rollForward: "latestMinor",
                workingDirectory: tempDir);

            Assert.Contains("Created", result.GetText(), StringComparison.OrdinalIgnoreCase);
            var filePath = Path.Join(tempDir, "global.json");
            Assert.True(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.Contains("10.0.100", content);
            Assert.Contains("latestMinor", content);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_UpdatesExistingFilePreservingFields()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // Arrange - write an existing global.json with a custom field and sdk section
            var filePath = Path.Join(tempDir, "global.json");
            File.WriteAllText(filePath, """
                {
                  "sdk": {
                    "version": "9.0.100"
                  }
                }
                """);

            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                testRunner: "Microsoft.Testing.Platform",
                workingDirectory: tempDir);

            Assert.Contains("Updated", result.GetText(), StringComparison.OrdinalIgnoreCase);
            var content = File.ReadAllText(filePath);
            // Existing sdk.version should be preserved
            Assert.Contains("9.0.100", content);
            // New test.runner should be added
            Assert.Contains("Microsoft.Testing.Platform", content);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_UpdatesTestRunnerInExistingFile()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // Arrange - write an existing global.json that already has a test section
            var filePath = Path.Join(tempDir, "global.json");
            File.WriteAllText(filePath, """
                {
                  "test": {
                    "runner": "VSTest"
                  }
                }
                """);

            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                testRunner: "Microsoft.Testing.Platform",
                workingDirectory: tempDir);

            Assert.Contains("Updated", result.GetText(), StringComparison.OrdinalIgnoreCase);
            var content = File.ReadAllText(filePath);
            Assert.Contains("Microsoft.Testing.Platform", content);
            Assert.DoesNotContain("VSTest", content);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_WithExplicitPath_CreatesFileAtPath()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var explicitPath = Path.Join(tempDir, "subdir", "global.json");

            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                testRunner: "Microsoft.Testing.Platform",
                globalJsonPath: explicitPath);

            Assert.Contains("Created", result.GetText(), StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(explicitPath));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_InvalidJson_ReturnsError()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // Arrange - write an invalid global.json
            var filePath = Path.Join(tempDir, "global.json");
            File.WriteAllText(filePath, "{ this is not valid json }");

            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                testRunner: "Microsoft.Testing.Platform",
                workingDirectory: tempDir);

            Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("parse", result.GetText(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_NonObjectJsonRoot_ReturnsError()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // Arrange - write a valid JSON file whose root is not an object (array)
            var filePath = Path.Join(tempDir, "global.json");
            File.WriteAllText(filePath, """["not", "an", "object"]""");

            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                testRunner: "Microsoft.Testing.Platform",
                workingDirectory: tempDir);

            Assert.Contains("Error:", result.GetText(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("JSON object", result.GetText(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_ConfigureGlobalJson_RelativeGlobalJsonPath_ResolvesAgainstWorkingDirectory()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-globaljson-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // Pass a relative path — should resolve against workingDirectory, not process CWD
            var result = await _tools.DotnetSdk(
                action: DotnetSdkAction.ConfigureGlobalJson,
                testRunner: "Microsoft.Testing.Platform",
                globalJsonPath: "global.json",
                workingDirectory: tempDir);

            Assert.Contains("Created", result.GetText(), StringComparison.OrdinalIgnoreCase);
            var expectedFile = Path.Join(tempDir, "global.json");
            Assert.True(File.Exists(expectedFile));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
            catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        }
    }

    #endregion
}
