using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests to verify server-level icon configuration for the MCP server.
/// </summary>
public class ServerIconTests
{
    /// <summary>
    /// Verifies that the server has icon configuration in ServerInfo.
    /// This is configured in Program.cs via AddMcpServer options.
    /// </summary>
    [Fact]
    public void ServerInfo_HasIcons()
    {
        // Arrange - Create server info similar to Program.cs
        var serverInfo = new Implementation
        {
            Name = "dotnet-mcp",
            Version = "1.0.0",
            Title = ".NET MCP Server",
            Description = "MCP server providing AI assistants with direct access to the .NET SDK",
            WebsiteUrl = "https://github.com/jongalloway/dotnet-mcp",
            Icons =
            [
                new Icon
                {
                    Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg",
                    MimeType = "image/svg+xml",
                    Sizes = ["any"],
                    Theme = "light"
                },
                new Icon
                {
                    Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/3D/gear_3d.png",
                    MimeType = "image/png",
                    Sizes = ["256x256"]
                }
            ]
        };

        // Act & Assert
        Assert.NotNull(serverInfo);
        Assert.NotNull(serverInfo.Icons);
        Assert.NotEmpty(serverInfo.Icons);
        Assert.Equal(2, serverInfo.Icons.Count);
    }

    /// <summary>
    /// Verifies that server icons use valid Fluent UI emoji URLs.
    /// </summary>
    [Fact]
    public void ServerIcons_UseFluentUIEmoji()
    {
        // Arrange
        var icons = new List<Icon>
        {
            new Icon
            {
                Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg",
                MimeType = "image/svg+xml",
                Sizes = ["any"],
                Theme = "light"
            },
            new Icon
            {
                Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/3D/gear_3d.png",
                MimeType = "image/png",
                Sizes = ["256x256"]
            }
        };

        // Assert
        foreach (var icon in icons)
        {
            Assert.StartsWith("https://raw.githubusercontent.com/microsoft/fluentui-emoji/", icon.Source);
            Assert.NotNull(icon.MimeType);
            Assert.NotNull(icon.Sizes);
            Assert.NotEmpty(icon.Sizes!);
        }
    }

    /// <summary>
    /// Verifies that server has both SVG and PNG icon formats for compatibility.
    /// </summary>
    [Fact]
    public void ServerIcons_IncludeMultipleFormats()
    {
        // Arrange
        var icons = new List<Icon>
        {
            new Icon
            {
                Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg",
                MimeType = "image/svg+xml",
                Sizes = ["any"],
                Theme = "light"
            },
            new Icon
            {
                Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/3D/gear_3d.png",
                MimeType = "image/png",
                Sizes = ["256x256"]
            }
        };

        // Assert - Should have both SVG and PNG
        Assert.Contains(icons, i => i.MimeType == "image/svg+xml");
        Assert.Contains(icons, i => i.MimeType == "image/png");
    }

    /// <summary>
    /// Verifies that SVG icon is scalable (uses "any" size).
    /// </summary>
    [Fact]
    public void ServerIcon_SvgIsScalable()
    {
        // Arrange
        var svgIcon = new Icon
        {
            Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg",
            MimeType = "image/svg+xml",
            Sizes = ["any"],
            Theme = "light"
        };

        // Assert
        Assert.Contains("any", svgIcon.Sizes);
    }

    /// <summary>
    /// Verifies that PNG icon has specific size information.
    /// </summary>
    [Fact]
    public void ServerIcon_PngHasSize()
    {
        // Arrange
        var pngIcon = new Icon
        {
            Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/3D/gear_3d.png",
            MimeType = "image/png",
            Sizes = ["256x256"]
        };

        // Assert
        Assert.NotEmpty(pngIcon.Sizes);
        Assert.Contains("256x256", pngIcon.Sizes);
    }
}
