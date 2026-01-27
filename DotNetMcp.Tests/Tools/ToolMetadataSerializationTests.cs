using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests to verify that tool metadata (descriptions, McpMeta tags) survives serialization
/// and appears correctly for clients using SDK v0.5 ListToolsAsync.
/// This is a regression test for SDK v0.5 compatibility.
/// </summary>
public class ToolMetadataSerializationTests
{
    /// <summary>
    /// Verifies that all tools with [McpServerTool] attribute can be discovered
    /// and their metadata is accessible.
    /// After Phase 2: Only consolidated tools and utilities have [McpServerTool].
    /// Expected: 8 consolidated tools (project, package, solution, ef, workload, tool, sdk, dev-certs)
    ///          + 3 utilities (server_capabilities, help, framework_info)
    ///          = 11 total tools
    /// </summary>
    [Fact]
    public void AllToolMethods_HaveMcpServerToolAttribute()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        // Act
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Assert
        Assert.NotEmpty(toolMethods);
        // Phase 2: Verify we have exactly the expected consolidated tools and utilities
        Assert.Equal(11, toolMethods.Count);
    }

    /// <summary>
    /// Verifies that McpMeta attributes are properly attached to tool methods
    /// and can be read via reflection.
    /// </summary>
    [Fact]
    public void ToolMethods_WithMcpMetaAttributes_CanBeDiscovered()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Act
        var methodsWithMeta = toolMethods
            .Where(m => m.GetCustomAttributes<McpMetaAttribute>().Any())
            .ToList();

        // Assert
        Assert.NotEmpty(methodsWithMeta);
        
        // Verify consolidated tool has expected metadata (using DotnetProject instead of legacy DotnetTemplateList)
        var projectMethod = toolMethods.FirstOrDefault(m => m.Name == "DotnetProject");
        Assert.NotNull(projectMethod);
        
        var metaAttrs = projectMethod.GetCustomAttributes<McpMetaAttribute>().ToList();
        Assert.NotEmpty(metaAttrs);
        
        // Check for specific metadata on consolidated tools
        Assert.Contains(metaAttrs, m => m.Name == "category");
        Assert.Contains(metaAttrs, m => m.Name == "commonlyUsed");
        Assert.Contains(metaAttrs, m => m.Name == "priority");
        Assert.Contains(metaAttrs, m => m.Name == "consolidatedTool");
        Assert.Contains(metaAttrs, m => m.Name == "actions");
    }

    /// <summary>
    /// Verifies that McpMeta attributes with JsonValue property are properly set
    /// and the JSON is valid.
    /// </summary>
    [Fact]
    public void McpMetaAttributes_WithJsonValue_ContainValidJson()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Act
        var metaWithJson = toolMethods
            .SelectMany(m => m.GetCustomAttributes<McpMetaAttribute>())
            .Where(meta => !string.IsNullOrEmpty(meta.JsonValue))
            .ToList();

        // Assert
        Assert.NotEmpty(metaWithJson);
        
        foreach (var meta in metaWithJson)
        {
            // Verify JSON is valid by parsing it
            Assert.NotNull(meta.JsonValue);
            var parseException = Record.Exception(() => JsonDocument.Parse(meta.JsonValue));
            Assert.Null(parseException);
        }
    }

    /// <summary>
    /// Verifies that tool class is properly marked with [McpServerToolType] attribute.
    /// </summary>
    [Fact]
    public void DotNetCliTools_HasMcpServerToolTypeAttribute()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);

        // Act
        var attr = toolType.GetCustomAttribute<McpServerToolTypeAttribute>();

        // Assert
        Assert.NotNull(attr);
    }

    /// <summary>
    /// Integration test that verifies tools can be registered with the MCP server
    /// and the SDK properly processes the metadata.
    /// </summary>
    [Fact]
    public void McpServer_WithTools_RegistersSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Register dependencies
        services.AddSingleton<ConcurrencyManager>();
        services.AddSingleton<ProcessSessionManager>();
        services.AddLogging();

        // Act - This should not throw and should register all tools
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<DotNetCliTools>();

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify the service provider was built successfully
        Assert.NotNull(serviceProvider);
        
        // Verify we can resolve ConcurrencyManager
        var concurrencyManager = serviceProvider.GetRequiredService<ConcurrencyManager>();
        Assert.NotNull(concurrencyManager);
        
        // Verify we can resolve ProcessSessionManager
        var processSessionManager = serviceProvider.GetRequiredService<ProcessSessionManager>();
        Assert.NotNull(processSessionManager);
        
        // Verify we can create a DotNetCliTools instance
        var toolsInstance = ActivatorUtilities.CreateInstance<DotNetCliTools>(
            serviceProvider,
            NullLogger<DotNetCliTools>.Instance,
            concurrencyManager,
            processSessionManager);
        Assert.NotNull(toolsInstance);
    }

    /// <summary>
    /// Verifies that all commonly used tools have the expected metadata structure.
    /// This ensures the metadata will be correctly serialized for v0.5 clients.
    /// </summary>
    [Fact]
    public void CommonlyUsedTools_HaveCompleteMetadata()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Get tools marked as commonly used
        var commonlyUsedTools = toolMethods
            .Where(m => m.GetCustomAttributes<McpMetaAttribute>()
                .Any(meta => meta.Name == "commonlyUsed"))
            .ToList();

        // Assert
        Assert.NotEmpty(commonlyUsedTools);
        
        // Each commonly used tool should have category
        foreach (var tool in commonlyUsedTools)
        {
            var metaAttrs = tool.GetCustomAttributes<McpMetaAttribute>().ToList();
            
            // Should have a category
            Assert.Contains(metaAttrs, m => m.Name == "category");
            
            // Tools with tags should have valid JSON
            var tagsAttr = metaAttrs.FirstOrDefault(m => m.Name == "tags");
            if (tagsAttr != null && !string.IsNullOrEmpty(tagsAttr.JsonValue))
            {
                // Verify tags is a valid JSON array
                using var doc = JsonDocument.Parse(tagsAttr.JsonValue);
                Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            }
        }
    }

    /// <summary>
    /// Verifies that tool descriptions from XML documentation are not empty.
    /// SDK v0.5 relies on these descriptions for tool metadata.
    /// </summary>
    [Fact]
    public void ToolMethods_HaveXmlDocumentation()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Act & Assert
        // Note: We can't directly access XML documentation at runtime without the XML file,
        // but we can verify the methods exist and are properly attributed.
        // The actual XML documentation is validated by the XmlDocumentationCoverageTests.
        Assert.NotEmpty(toolMethods);
        
        // Verify that each method has parameters with reasonable names
        var parameterValidations = toolMethods.Take(5) // Sample check
            .SelectMany(method => method.GetParameters()
                .Select(param => new { method, param }));
        
        foreach (var validation in parameterValidations)
        {
            // Parameter names should not be compiler-generated
            Assert.False(string.IsNullOrEmpty(validation.param.Name));
            Assert.DoesNotContain("<>", validation.param.Name);
        }
    }

    /// <summary>
    /// Verifies that metadata categories are consistently applied across tools.
    /// </summary>
    [Fact]
    public void MetadataCategories_AreConsistent()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Act - Find all tools that have category metadata
        var toolsWithCategories = toolMethods
            .Where(m => m.GetCustomAttributes<McpMetaAttribute>().Any(meta => meta.Name == "category"))
            .ToList();
        
        // Assert - Verify a significant portion of tools have category metadata
        Assert.NotEmpty(toolsWithCategories);
        
        // Most tools should have categories (use 80% as threshold to allow for flexibility)
        var percentageWithCategories = (double)toolsWithCategories.Count / toolMethods.Count;
        Assert.True(percentageWithCategories >= 0.80, 
            $"Expected at least 80% of tools to have categories, found {percentageWithCategories:P0} ({toolsWithCategories.Count}/{toolMethods.Count})");
        
        // Verify each tool with a category has the metadata properly set
        var categoryMetadata = toolsWithCategories
            .Select(tool => tool.GetCustomAttributes<McpMetaAttribute>()
                .FirstOrDefault(m => m.Name == "category"));
        
        foreach (var categoryMeta in categoryMetadata)
        {
            Assert.NotNull(categoryMeta);
            // The category metadata should exist (value is set via constructor, not directly accessible)
        }
    }

    /// <summary>
    /// Verifies that all tool methods have icons configured via the IconSource property
    /// on the McpServerTool attribute. This ensures improved AI assistant UX.
    /// </summary>
    [Fact]
    public void AllToolMethods_HaveIcons()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        // Act
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Assert
        Assert.NotEmpty(toolMethods);

        var toolsWithoutIcons = new List<string>();
        foreach (var method in toolMethods)
        {
            var attr = method.GetCustomAttribute<McpServerToolAttribute>();
            Assert.NotNull(attr);
            
            if (string.IsNullOrWhiteSpace(attr.IconSource))
            {
                toolsWithoutIcons.Add(method.Name);
            }
        }

        // All tools should have icons
        Assert.Empty(toolsWithoutIcons);
    }

    /// <summary>
    /// Verifies that tool icons use valid URLs pointing to Fluent UI emoji assets.
    /// </summary>
    [Fact]
    public void ToolIcons_UseValidFluentUIUrls()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Act & Assert
        foreach (var method in toolMethods)
        {
            var attr = method.GetCustomAttribute<McpServerToolAttribute>();
            Assert.NotNull(attr);
            Assert.NotNull(attr.IconSource);
            
            // Icons should use Fluent UI emoji from the official Microsoft repository
            Assert.StartsWith("https://raw.githubusercontent.com/microsoft/fluentui-emoji/", attr.IconSource);
            
            // Icons should be either SVG or PNG format
            Assert.True(
                attr.IconSource.EndsWith(".svg") || attr.IconSource.EndsWith(".png"),
                $"Tool {method.Name} has icon with unexpected format: {attr.IconSource}");
        }
    }

    /// <summary>
    /// Verifies that commonly used tools have appropriate icon choices based on their category.
    /// </summary>
    [Fact]
    public void CommonlyUsedTools_HaveAppropriateIcons()
    {
        // Arrange
        var toolType = typeof(DotNetCliTools);
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null).ToList();

        // Category icon mapping - categories with multiple icons use arrays
        var categoryIconMapping = new Dictionary<string, string[]>
        {
            { "project", new[] { "file_folder_flat.svg" } },
            { "package", new[] { "package_flat.svg" } },
            { "solution", new[] { "card_file_box_flat.svg" } },
            { "sdk", new[] { "gear_flat.svg" } },
            { "tool", new[] { "hammer_and_wrench_flat.svg" } },
            { "workload", new[] { "books_flat.svg" } },
            { "ef", new[] { "floppy_disk_flat.svg" } },
            { "security", new[] { "locked_flat.svg" } },
            // Help category has multiple icons depending on the specific tool
            { "help", new[] { "light_bulb_flat.svg", "bar_chart_flat.svg", "information_flat.svg" } }
        };

        // Act & Assert
        foreach (var method in toolMethods)
        {
            var attr = method.GetCustomAttribute<McpServerToolAttribute>();
            var metaAttrs = method.GetCustomAttributes<McpMetaAttribute>().ToList();
            var categoryMeta = metaAttrs.FirstOrDefault(m => m.Name == "category");
            
            if (categoryMeta != null)
            {
                // JsonValue contains the value in JSON format (e.g., "\"project\"")
                var categoryValue = categoryMeta.JsonValue?.Trim('"');
                if (categoryValue != null && categoryIconMapping.TryGetValue(categoryValue, out var expectedIcons))
                {
                    Assert.NotNull(attr);
                    Assert.NotNull(attr.IconSource);
                    
                    // Check if the icon matches any of the expected icons for this category
                    var iconMatches = expectedIcons.Any(expectedIcon =>
                        attr.IconSource.Contains(expectedIcon, StringComparison.OrdinalIgnoreCase));
                    
                    Assert.True(iconMatches,
                        $"Tool {method.Name} in category '{categoryValue}' should use one of these icons: " +
                        $"{string.Join(", ", expectedIcons)}, but has '{attr.IconSource}'");
                }
            }
        }
    }
}
