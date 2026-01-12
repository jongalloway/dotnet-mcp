using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// MCP Server Conformance Tests
/// 
/// These tests validate that the dotnet-mcp server conforms to the Model Context Protocol (MCP)
/// specification. They test basic protocol behaviors including:
/// - Server initialization and handshake
/// - Tool listing and discovery
/// - Tool invocation and responses
/// - Error handling and semantics
/// - Resource listing
/// 
/// These tests run against the actual dotnet-mcp server binary in an in-process stdio configuration,
/// ensuring deterministic behavior without requiring external services.
/// </summary>
public class McpConformanceTests : IAsyncLifetime
{
    private McpClient? _client;
    private readonly ILoggerFactory _loggerFactory;

    public McpConformanceTests()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            builder.SetMinimumLevel(LogLevel.Warning); // Only show warnings and errors in test output
        });
    }

    public async ValueTask InitializeAsync()
    {
        // Start the dotnet-mcp server using stdio transport
        var serverPath = GetServerExecutablePath();
        
        var transportOptions = new StdioClientTransportOptions
        {
            Command = serverPath.command,
            Arguments = serverPath.arguments,
            Name = "dotnet-mcp-conformance-test"
        };

        var transport = new StdioClientTransport(transportOptions);
        
        // Connect to the server and perform handshake
        _client = await McpClient.CreateAsync(
            transport,
            loggerFactory: _loggerFactory,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        _loggerFactory.Dispose();
    }

    #region Handshake and Initialization Tests

    [Fact]
    public void Server_ShouldBeConnectedAfterHandshake()
    {
        // Assert
        Assert.NotNull(_client);
    }

    [Fact]
    public void Server_ShouldProvideServerInfo()
    {
        // Assert
        Assert.NotNull(_client);
        Assert.NotNull(_client.ServerInfo);
        Assert.NotEmpty(_client.ServerInfo.Name);
        Assert.NotEmpty(_client.ServerInfo.Version);
    }

    [Fact]
    public void Server_ShouldProvideProtocolVersion()
    {
        // Assert
        Assert.NotNull(_client);
        Assert.NotNull(_client.NegotiatedProtocolVersion);
        
        // Should be using MCP protocol version from the SDK (0.5.0-preview.1 or compatible)
        Assert.NotEmpty(_client.NegotiatedProtocolVersion);
    }

    [Fact]
    public void Server_ShouldProvideCapabilities()
    {
        // Assert
        Assert.NotNull(_client);
        Assert.NotNull(_client.ServerCapabilities);
    }

    [Fact]
    public async Task Server_ShouldRespondToPing()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        await _client.PingAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert - if no exception, ping succeeded
    }

    #endregion

    #region Tool Listing Tests

    [Fact]
    public async Task Server_ShouldListTools()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var tools = await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(tools);
        Assert.NotEmpty(tools);
    }

    [Fact]
    public async Task Server_ToolList_ShouldHaveRequiredFields()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var tools = await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        foreach (var tool in tools)
        {
            Assert.NotNull(tool.Name);
            Assert.NotEmpty(tool.Name);
            
            // Description is recommended but not strictly required by MCP spec
            // However, for good UX, all tools should have descriptions
            Assert.NotNull(tool.Description);
            Assert.NotEmpty(tool.Description);
            
            // InputSchema should be present - it's a JsonElement value type so just access it
            _ = tool.ProtocolTool.InputSchema;
        }
    }

    [Fact]
    public async Task Server_ToolList_ShouldIncludeExpectedTools()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var tools = await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var toolNames = tools.Select(t => t.Name).ToHashSet();

        // Assert - Check for consolidated and utility tools that should always be present
        // After Phase 2: only consolidated tools and utilities are exposed
        Assert.Contains("dotnet_project", toolNames);
        Assert.Contains("dotnet_sdk", toolNames);
        Assert.Contains("dotnet_server_capabilities", toolNames);
        Assert.Contains("dotnet_help", toolNames);
    }

    [Fact]
    public async Task Server_ToolList_ShouldIncludeConsolidatedTools()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var tools = await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var toolNames = tools.Select(t => t.Name).ToHashSet();

        // Assert - Check that all consolidated tools are exposed
        Assert.Contains("dotnet_project", toolNames);
        Assert.Contains("dotnet_package", toolNames);
        Assert.Contains("dotnet_solution", toolNames);
        Assert.Contains("dotnet_ef", toolNames);
        Assert.Contains("dotnet_workload", toolNames);
        Assert.Contains("dotnet_tool", toolNames);
        Assert.Contains("dotnet_sdk", toolNames);
        Assert.Contains("dotnet_dev_certs", toolNames);
    }

    [Fact]
    public async Task Server_ToolList_ShouldNotIncludeLegacyTools()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var tools = await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var toolNames = tools.Select(t => t.Name).ToHashSet();

        // Assert - Phase 2: Verify legacy tools are removed from tool listing
        // These legacy per-command tools should no longer be exposed
        Assert.DoesNotContain("dotnet_project_new", toolNames);
        Assert.DoesNotContain("dotnet_project_build", toolNames);
        Assert.DoesNotContain("dotnet_template_list", toolNames);
        Assert.DoesNotContain("dotnet_package_add", toolNames);
        Assert.DoesNotContain("dotnet_ef_migrations_add", toolNames);
        Assert.DoesNotContain("dotnet_watch_run", toolNames);
        Assert.DoesNotContain("dotnet_format", toolNames);
    }


    [Fact]
    public async Task Server_ConsolidatedTool_ShouldHaveActionEnumInSchema()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var tools = await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var projectTool = tools.FirstOrDefault(t => t.Name == "dotnet_project");

        // Assert
        Assert.NotNull(projectTool);
        
        // The input schema should contain an 'action' parameter with enum values
        var schemaJson = projectTool.ProtocolTool.InputSchema.ToString();
        Assert.Contains("action", schemaJson);
        
        // Verify some expected action values are in the schema
        Assert.Contains("New", schemaJson);
        Assert.Contains("Build", schemaJson);
        Assert.Contains("Test", schemaJson);
    }

    [Fact]
    public async Task Server_ConsolidatedTools_AllShouldHaveActionParameter()
    {
        // Arrange
        Assert.NotNull(_client);
        var consolidatedTools = new[] 
        { 
            "dotnet_project", "dotnet_package", "dotnet_solution", 
            "dotnet_ef", "dotnet_workload", "dotnet_tool", 
            "dotnet_sdk", "dotnet_dev_certs" 
        };

        // Act
        var tools = await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        foreach (var tool in consolidatedTools.Select(toolName => tools.FirstOrDefault(t => t.Name == toolName)))
        {
            Assert.NotNull(tool);
            
            var schemaJson = tool.ProtocolTool.InputSchema.ToString();
            Assert.Contains("action", schemaJson);
        }
    }

    #endregion

    #region Tool Invocation Tests

    [Fact]
    public async Task Server_ShouldInvokeToolSuccessfully()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act - Call a simple, deterministic tool (server capabilities)
        var result = await _client.CallToolAsync(
            "dotnet_server_capabilities",
            new Dictionary<string, object?>(),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.IsError); // Should not be an error
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task Server_ToolInvocation_ShouldReturnTextContent()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var result = await _client.CallToolAsync(
            "dotnet_server_capabilities",
            new Dictionary<string, object?>(),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var textContent = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textContent);
        Assert.NotEmpty(textContent.Text);
        
        // Should be valid JSON for this specific tool
        using var _ = JsonDocument.Parse(textContent.Text);
        // JsonDocument is disposed to release pooled memory
    }

    [Fact]
    public async Task Server_ToolInvocation_WithInvalidTool_ShouldReturnError()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act & Assert
        await Assert.ThrowsAsync<McpProtocolException>(async () =>
        {
            await _client.CallToolAsync(
                "nonexistent_tool_that_does_not_exist",
                new Dictionary<string, object?>(),
                cancellationToken: TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task Server_ToolInvocation_SdkInfo_ShouldReturnValidData()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act - Call consolidated dotnet_sdk tool with Info action
        var result = await _client.CallToolAsync(
            "dotnet_sdk",
            new Dictionary<string, object?> { { "action", "Info" } },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.IsError);
        
        var textContent = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textContent);
        Assert.NotEmpty(textContent.Text);
        
        // Should contain SDK information
        Assert.Contains(".NET", textContent.Text);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Server_ShouldProvideStructuredErrors()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act & Assert - Try to invoke a tool with invalid parameters
        var exception = await Assert.ThrowsAsync<McpProtocolException>(async () =>
        {
            await _client.CallToolAsync(
                "nonexistent_tool",
                new Dictionary<string, object?>(),
                cancellationToken: TestContext.Current.CancellationToken);
        });

        // The server should provide a proper MCP error
        Assert.NotNull(exception);
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task Server_ErrorResponse_ShouldHaveErrorCode()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<McpProtocolException>(async () =>
        {
            await _client.CallToolAsync(
                "nonexistent_tool",
                new Dictionary<string, object?>(),
                cancellationToken: TestContext.Current.CancellationToken);
        });

        // MCP protocol errors should provide error information
        Assert.NotNull(exception);
        Assert.NotEmpty(exception.Message);
        Assert.NotEqual(0, (int)exception.ErrorCode);
    }

    #endregion

    #region Resource Listing Tests

    [Fact]
    public async Task Server_ShouldListResources()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var resources = await _client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(resources);
        // dotnet-mcp may or may not have resources - just verify the API works
    }

    [Fact]
    public async Task Server_ResourceList_ShouldHaveValidStructure()
    {
        // Arrange
        Assert.NotNull(_client);

        // Act
        var resources = await _client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(resources);
        
        // If there are resources, they should have required fields
        foreach (var resource in resources)
        {
            Assert.NotNull(resource.Uri);
            Assert.NotEmpty(resource.Uri);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the path to the dotnet-mcp server executable for testing.
    /// This finds the compiled DotNetMcp binary in the same configuration as the test project.
    /// </summary>
    private static (string command, string[] arguments) GetServerExecutablePath()
    {
        // The test binary is in: DotNetMcp.Tests/bin/{Configuration}/{Framework}/
        // The server binary is in: DotNetMcp/bin/{Configuration}/{Framework}/
        var testBinaryDir = AppContext.BaseDirectory;
        var configuration = Path.GetFileName(Path.GetDirectoryName(testBinaryDir.TrimEnd(Path.DirectorySeparatorChar))!);
        var targetFramework = Path.GetFileName(testBinaryDir.TrimEnd(Path.DirectorySeparatorChar));
        
        var serverBinaryDir = Path.GetFullPath(
            Path.Join(testBinaryDir, "..", "..", "..", "..", "DotNetMcp", "bin", configuration, targetFramework));

        if (!Directory.Exists(serverBinaryDir))
        {
            throw new DirectoryNotFoundException(
                $"Server binary directory not found at: {serverBinaryDir}. " +
                $"Make sure DotNetMcp project is built before running conformance tests.");
        }

        // Determine the appropriate command and arguments based on platform
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var exePath = Path.Join(serverBinaryDir, "DotNetMcp.exe");
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException($"Server executable not found at: {exePath}");
            }
            return (exePath, Array.Empty<string>());
        }
        else
        {
            // On Unix-like systems, use 'dotnet' to run the DLL
            var dllPath = Path.Join(serverBinaryDir, "DotNetMcp.dll");
            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException($"Server assembly not found at: {dllPath}");
            }
            return ("dotnet", new[] { dllPath });
        }
    }

    #endregion
}
