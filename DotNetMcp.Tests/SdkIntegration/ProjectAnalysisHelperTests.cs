using System.Text.Json;
using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

public class ProjectAnalysisHelperTests
{
    private readonly ILogger<DotNetCliTools> _logger;

    public ProjectAnalysisHelperTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        // Arrange
        var nonExistentPath = "/tmp/nonexistent.csproj";

        // Act
        var result = await ProjectAnalysisHelper.AnalyzeProjectAsync(nonExistentPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        // Arrange
        var nonExistentPath = "/tmp/nonexistent.csproj";

        // Act
        var result = await ProjectAnalysisHelper.AnalyzeDependenciesAsync(nonExistentPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task ValidateProjectAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        // Arrange
        var nonExistentPath = "/tmp/nonexistent.csproj";

        // Act
        var result = await ProjectAnalysisHelper.ValidateProjectAsync(nonExistentPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithMinimalProject_ReturnsSuccessJson()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <OutputType>Exe</OutputType>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.AnalyzeProjectAsync(projectFile, _logger);

            Assert.NotNull(result);
            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(projectFile, json.RootElement.GetProperty("projectPath").GetString());
            Assert.Equal("Test", json.RootElement.GetProperty("projectName").GetString());
            Assert.True(json.RootElement.TryGetProperty("targetFrameworks", out _));
            Assert.Equal("Exe", json.RootElement.GetProperty("outputType").GetString());
            Assert.True(json.RootElement.TryGetProperty("packageReferences", out _));
            Assert.Equal("enable", json.RootElement.GetProperty("nullable").GetString());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithPackageReferences_ReturnsPackageInfo()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="Serilog" Version="3.1.1" />
              </ItemGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.AnalyzeProjectAsync(projectFile, _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());

            var packages = json.RootElement.GetProperty("packageReferences").EnumerateArray().ToList();
            Assert.Equal(2, packages.Count);

            var names = packages.Select(p => p.GetProperty("name").GetString()).ToHashSet();
            Assert.Contains("Newtonsoft.Json", names);
            Assert.Contains("Serilog", names);

            var newtonsoftPkg = packages.First(p => p.GetProperty("name").GetString() == "Newtonsoft.Json");
            Assert.Equal("13.0.3", newtonsoftPkg.GetProperty("version").GetString());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithMultipleTargetFrameworks_ReturnsAllFrameworks()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.AnalyzeProjectAsync(projectFile, _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());

            var frameworks = json.RootElement.GetProperty("targetFrameworks")
                .EnumerateArray()
                .Select(f => f.GetString())
                .ToList();

            Assert.Contains("net8.0", frameworks);
            Assert.Contains("net9.0", frameworks);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithMinimalProject_ReturnsSuccessJson()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.AnalyzeDependenciesAsync(projectFile, _logger);

            Assert.NotNull(result);
            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.True(json.RootElement.TryGetProperty("directPackageDependencies", out _));
            Assert.True(json.RootElement.TryGetProperty("directProjectDependencies", out _));
            Assert.Equal(0, json.RootElement.GetProperty("totalDirectDependencies").GetInt32());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithPackageReferences_ReturnsDependencies()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.AnalyzeDependenciesAsync(projectFile, _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());

            var packages = json.RootElement.GetProperty("directPackageDependencies").EnumerateArray().ToList();
            Assert.Single(packages);
            Assert.Equal("Newtonsoft.Json", packages[0].GetProperty("name").GetString());
            Assert.Equal("13.0.3", packages[0].GetProperty("version").GetString());
            Assert.Equal(1, json.RootElement.GetProperty("totalDirectDependencies").GetInt32());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task ValidateProjectAsync_WithHealthyProject_ReturnsSuccessAndIsValid()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <OutputType>Exe</OutputType>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.ValidateProjectAsync(projectFile, _logger);

            Assert.NotNull(result);
            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.True(json.RootElement.GetProperty("isValid").GetBoolean());
            Assert.True(json.RootElement.TryGetProperty("errors", out _));
            Assert.True(json.RootElement.TryGetProperty("warnings", out _));
            Assert.True(json.RootElement.TryGetProperty("recommendations", out _));
            Assert.Equal(0, json.RootElement.GetProperty("errors").GetArrayLength());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task ValidateProjectAsync_WithMissingTargetFramework_ReturnsErrors()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.ValidateProjectAsync(projectFile, _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.False(json.RootElement.GetProperty("isValid").GetBoolean());

            var errors = json.RootElement.GetProperty("errors")
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToList();

            Assert.Contains(errors, e => e != null && e.Contains("No target framework"));
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task ValidateProjectAsync_WithNullableEnabled_DoesNotRecommendNullable()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.ValidateProjectAsync(projectFile, _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());

            var recommendations = json.RootElement.GetProperty("recommendations")
                .EnumerateArray()
                .Select(r => r.GetString())
                .ToList();

            Assert.DoesNotContain(recommendations, r => r != null && r.Contains("nullable reference types"));
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static string CreateTempProject(string content)
    {
        var tempDir = Path.GetFullPath(Path.Join(Path.GetTempPath(), "dotnet-mcp-pah-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(tempDir);
        var projectFile = Path.Join(tempDir, "Test.csproj");
        File.WriteAllText(projectFile, content);
        return projectFile;
    }

    [Fact]
    public async Task SetPropertyAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.SetPropertyAsync("/tmp/nonexistent.csproj", "OutputType", "Exe", _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task SetPropertyAsync_AddsNewProperty()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.SetPropertyAsync(projectFile, "OutputType", "Exe", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("OutputType", json.RootElement.GetProperty("propertyName").GetString());
            Assert.Equal("Exe", json.RootElement.GetProperty("propertyValue").GetString());

            // Verify the file was actually modified
            var content = File.ReadAllText(projectFile);
            Assert.Contains("OutputType", content);
            Assert.Contains("Exe", content);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task SetPropertyAsync_UpdatesExistingProperty()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>disable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.SetPropertyAsync(projectFile, "Nullable", "enable", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Nullable", json.RootElement.GetProperty("propertyName").GetString());
            Assert.Equal("enable", json.RootElement.GetProperty("propertyValue").GetString());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task GetPropertyAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.GetPropertyAsync("/tmp/nonexistent.csproj", "OutputType", _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task GetPropertyAsync_ReturnsPropertyValue()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.GetPropertyAsync(projectFile, "Nullable", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Nullable", json.RootElement.GetProperty("propertyName").GetString());
            Assert.Equal("enable", json.RootElement.GetProperty("propertyValue").GetString());
            Assert.True(json.RootElement.GetProperty("isSet").GetBoolean());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task GetPropertyAsync_UnsetProperty_ReturnsEmptyValueAndIsSetFalse()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.GetPropertyAsync(projectFile, "OutputType", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("OutputType", json.RootElement.GetProperty("propertyName").GetString());
            Assert.False(json.RootElement.GetProperty("isSet").GetBoolean());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RemovePropertyAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.RemovePropertyAsync("/tmp/nonexistent.csproj", "Nullable", _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task RemovePropertyAsync_RemovesExistingProperty()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.RemovePropertyAsync(projectFile, "Nullable", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Nullable", json.RootElement.GetProperty("propertyName").GetString());
            Assert.True(json.RootElement.GetProperty("removed").GetBoolean());

            // Verify the property was actually removed from the file
            var content = File.ReadAllText(projectFile);
            Assert.DoesNotContain("<Nullable>", content);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RemovePropertyAsync_NonExistentProperty_ReturnsSuccessWithRemovedFalse()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.RemovePropertyAsync(projectFile, "OutputType", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("OutputType", json.RootElement.GetProperty("propertyName").GetString());
            Assert.False(json.RootElement.GetProperty("removed").GetBoolean());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task AddItemAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.AddItemAsync("/tmp/nonexistent.csproj", "Using", "Xunit", logger: _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task AddItemAsync_AddsNewItem()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.AddItemAsync(projectFile, "Using", "Xunit", logger: _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Using", json.RootElement.GetProperty("itemType").GetString());
            Assert.Equal("Xunit", json.RootElement.GetProperty("include").GetString());

            // Verify the file was actually modified
            var content = File.ReadAllText(projectFile);
            Assert.Contains("Using", content);
            Assert.Contains("Xunit", content);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch (IOException) { /* best-effort */ } catch (UnauthorizedAccessException) { /* best-effort */ }
        }
    }

    [Fact]
    public async Task AddItemAsync_WithMetadata_AddsItemWithMetadata()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var metadata = new[] { new KeyValuePair<string, string>("CopyToOutputDirectory", "PreserveNewest") };
            var result = await ProjectAnalysisHelper.AddItemAsync(projectFile, "Content", "appsettings.json", metadata, _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Content", json.RootElement.GetProperty("itemType").GetString());
            Assert.Equal("appsettings.json", json.RootElement.GetProperty("include").GetString());

            var content = File.ReadAllText(projectFile);
            Assert.Contains("Content", content);
            Assert.Contains("appsettings.json", content);
            Assert.Contains("CopyToOutputDirectory", content);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch (IOException) { /* best-effort */ } catch (UnauthorizedAccessException) { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RemoveItemAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.RemoveItemAsync("/tmp/nonexistent.csproj", "Using", "Xunit", _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesExistingItem()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <Using Include="Xunit" />
              </ItemGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.RemoveItemAsync(projectFile, "Using", "Xunit", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Using", json.RootElement.GetProperty("itemType").GetString());
            Assert.Equal("Xunit", json.RootElement.GetProperty("include").GetString());
            Assert.True(json.RootElement.GetProperty("removed").GetBoolean());

            // Verify the item was actually removed
            var content = File.ReadAllText(projectFile);
            Assert.DoesNotContain("<Using Include=\"Xunit\"", content);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch (IOException) { /* best-effort */ } catch (UnauthorizedAccessException) { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RemoveItemAsync_NonExistentItem_ReturnsSuccessWithRemovedFalse()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.RemoveItemAsync(projectFile, "Using", "Xunit", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Using", json.RootElement.GetProperty("itemType").GetString());
            Assert.Equal("Xunit", json.RootElement.GetProperty("include").GetString());
            Assert.False(json.RootElement.GetProperty("removed").GetBoolean());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch (IOException) { /* best-effort */ } catch (UnauthorizedAccessException) { /* best-effort */ }
        }
    }

    [Fact]
    public async Task ListItemsAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.ListItemsAsync("/tmp/nonexistent.csproj", "Using", _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task ListItemsAsync_WithItemType_ReturnsFilteredItems()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <Using Include="Xunit" />
                <Using Include="System.Linq" />
              </ItemGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.ListItemsAsync(projectFile, "Using", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Using", json.RootElement.GetProperty("itemType").GetString());
            Assert.Equal(2, json.RootElement.GetProperty("count").GetInt32());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch (IOException) { /* best-effort */ } catch (UnauthorizedAccessException) { /* best-effort */ }
        }
    }

    [Fact]
    public async Task ListItemsAsync_WithNoItemType_ReturnsAllItems()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <Using Include="Xunit" />
                <Content Include="appsettings.json" />
              </ItemGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.ListItemsAsync(projectFile, null, _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("(all)", json.RootElement.GetProperty("itemType").GetString());
            // Should have at least the 2 explicitly declared items
            Assert.True(json.RootElement.GetProperty("count").GetInt32() >= 2);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch (IOException) { /* best-effort */ } catch (UnauthorizedAccessException) { /* best-effort */ }
        }
    }
}
