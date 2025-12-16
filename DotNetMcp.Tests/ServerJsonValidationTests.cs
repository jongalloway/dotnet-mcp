using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests;

[Collection("Sequential")]
public class ServerJsonValidationTests
{
    private const string ServerJsonPath = "DotNetMcp/.mcp/server.json";

    [Fact]
    public void ServerJson_ShouldExist()
    {
        // Arrange & Act
        var serverJsonFullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            ServerJsonPath);

        // Assert
        Assert.True(File.Exists(serverJsonFullPath),
            $"server.json not found at {serverJsonFullPath}");
    }

    [Fact]
    public void ServerJson_ShouldBeValidJson()
    {
        // Arrange
        var serverJsonFullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            ServerJsonPath);

        var jsonContent = File.ReadAllText(serverJsonFullPath);

        // Act & Assert
        var exception = Record.Exception(() => JsonDocument.Parse(jsonContent));
        Assert.Null(exception);
    }

    [Fact]
    public void ServerJson_ShouldHaveRequiredProperties()
    {
        // Arrange
        var serverJsonFullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            ServerJsonPath);

        var jsonContent = File.ReadAllText(serverJsonFullPath);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // Act & Assert
        Assert.True(root.TryGetProperty("name", out _), "Missing 'name' property");
        Assert.True(root.TryGetProperty("description", out _), "Missing 'description' property");
        Assert.True(root.TryGetProperty("version", out _), "Missing 'version' property");
    }

    [Fact]
    public void ServerJson_NameShouldBeInReverseDnsFormat()
    {
        // Arrange
        var serverJsonFullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            ServerJsonPath);

        var jsonContent = File.ReadAllText(serverJsonFullPath);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // Act
        var name = root.GetProperty("name").GetString();

        // Assert
        Assert.NotNull(name);
        Assert.Contains("/", name);
        Assert.Matches(@"^[a-zA-Z0-9.-]+/[a-zA-Z0-9._-]+$", name);
    }

    [Fact]
    public void ServerJson_ShouldNotContainInvalidProperties()
    {
        // Arrange
        var serverJsonFullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            ServerJsonPath);

        var jsonContent = File.ReadAllText(serverJsonFullPath);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        var validProperties = new HashSet<string>
        {
            "$schema", "_meta", "description", "icons", "name",
            "packages", "remotes", "repository", "title", "version", "websiteUrl"
        };

        var invalidProperties = new List<string>();

        // Act
        foreach (var property in root.EnumerateObject())
        {
            if (!validProperties.Contains(property.Name))
            {
                invalidProperties.Add(property.Name);
            }
        }

        // Assert
        Assert.Empty(invalidProperties);
    }

    [Fact]
    public void ServerJson_ShouldNotContainToolsOrResources()
    {
        // Arrange
        var serverJsonFullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            ServerJsonPath);

        var jsonContent = File.ReadAllText(serverJsonFullPath);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // Act & Assert
        Assert.False(root.TryGetProperty("tools", out _),
            "'tools' property should not be in server.json - these are dynamically exposed by the server");
        Assert.False(root.TryGetProperty("resources", out _),
            "'resources' property should not be in server.json - these are dynamically exposed by the server");
        Assert.False(root.TryGetProperty("status", out _),
            "'status' property is not part of the MCP server.json schema");
    }

    [Fact]
    public void ServerJson_PackagesShouldHaveRequiredFields()
    {
        // Arrange
        var serverJsonFullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            ServerJsonPath);

        var jsonContent = File.ReadAllText(serverJsonFullPath);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // Act
        Assert.True(root.TryGetProperty("packages", out var packages), "Missing 'packages' property");
        Assert.True(packages.GetArrayLength() > 0, "packages array should not be empty");

        foreach (var package in packages.EnumerateArray())
        {
            // Assert
            Assert.True(package.TryGetProperty("registryType", out _),
                "Package missing 'registryType'");
            Assert.True(package.TryGetProperty("identifier", out _),
                "Package missing 'identifier'");
            Assert.True(package.TryGetProperty("transport", out _),
                "Package missing 'transport'");
        }
    }
}
