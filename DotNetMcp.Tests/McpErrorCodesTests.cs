using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class McpErrorCodesTests
{
    [Fact]
    public void Constants_HaveCorrectValues()
    {
        // Assert - Verify JSON-RPC 2.0 standard error codes
        Assert.Equal(-32700, McpErrorCodes.ParseError);
        Assert.Equal(-32600, McpErrorCodes.InvalidRequest);
        Assert.Equal(-32601, McpErrorCodes.MethodNotFound);
        Assert.Equal(-32602, McpErrorCodes.InvalidParams);
        Assert.Equal(-32603, McpErrorCodes.InternalError);
        
        // MCP-specific error code
        Assert.Equal(-32002, McpErrorCodes.ResourceNotFound);
        
        // Server error range
        Assert.Equal(-32000, McpErrorCodes.ServerErrorRangeStart);
        Assert.Equal(-32099, McpErrorCodes.ServerErrorRangeEnd);
    }

    [Theory]
    [InlineData("NU1101", "Package", 1, -32002)] // Package not found
    [InlineData("NU1102", "Package", 1, -32002)] // Package version not found
    [InlineData("MSB1003", "Build", 1, -32002)] // Project file not found
    [InlineData("NETSDK1004", "SDK", 1, -32002)] // Assets file not found
    public void GetMcpErrorCode_WithResourceNotFoundScenarios_ReturnsResourceNotFound(
        string errorCode, string category, int exitCode, int expectedMcpCode)
    {
        // Act
        var mcpCode = McpErrorCodes.GetMcpErrorCode(errorCode, category, exitCode);

        // Assert
        Assert.NotNull(mcpCode);
        Assert.Equal(expectedMcpCode, mcpCode.Value);
    }

    [Theory]
    [InlineData("MSB4236", "Build", 1, -32602)] // SDK not found
    [InlineData("NETSDK1045", "SDK", 1, -32602)] // Framework not supported
    [InlineData("CS1001", "Compilation", 1, -32602)] // Identifier expected
    [InlineData("CS1513", "Compilation", 1, -32602)] // Closing brace expected
    public void GetMcpErrorCode_WithInvalidParamsScenarios_ReturnsInvalidParams(
        string errorCode, string category, int exitCode, int expectedMcpCode)
    {
        // Act
        var mcpCode = McpErrorCodes.GetMcpErrorCode(errorCode, category, exitCode);

        // Assert
        Assert.NotNull(mcpCode);
        Assert.Equal(expectedMcpCode, mcpCode.Value);
    }

    [Theory]
    [InlineData("OPERATION_CANCELLED", "Cancellation", -1, -32603)]
    [InlineData("CONCURRENCY_CONFLICT", "Concurrency", -1, -32603)]
    [InlineData("EXIT_1", "Unknown", 1, -32603)]
    public void GetMcpErrorCode_WithInternalErrorScenarios_ReturnsInternalError(
        string errorCode, string category, int exitCode, int expectedMcpCode)
    {
        // Act
        var mcpCode = McpErrorCodes.GetMcpErrorCode(errorCode, category, exitCode);

        // Assert
        Assert.NotNull(mcpCode);
        Assert.Equal(expectedMcpCode, mcpCode.Value);
    }

    [Theory]
    [InlineData("CS0103", "Compilation", 1)] // Regular compiler error
    [InlineData("MSB3644", "Build", 1)] // Reference assemblies not found (not a resource lookup failure)
    public void GetMcpErrorCode_WithNoApplicableMcpCode_ReturnsNull(
        string errorCode, string category, int exitCode)
    {
        // Act
        var mcpCode = McpErrorCodes.GetMcpErrorCode(errorCode, category, exitCode);

        // Assert
        Assert.Null(mcpCode);
    }

    [Fact]
    public void GetMcpErrorCode_WithSuccessExitCode_ReturnsNull()
    {
        // Act
        var mcpCode = McpErrorCodes.GetMcpErrorCode("CS0103", "Compilation", 0);

        // Assert
        Assert.Null(mcpCode);
    }

    [Fact]
    public void GetMcpErrorCode_CaseInsensitive()
    {
        // Act - Test with different casing
        var mcpCode1 = McpErrorCodes.GetMcpErrorCode("nu1101", "Package", 1);
        var mcpCode2 = McpErrorCodes.GetMcpErrorCode("NU1101", "Package", 1);
        var mcpCode3 = McpErrorCodes.GetMcpErrorCode("Nu1101", "Package", 1);

        // Assert
        Assert.NotNull(mcpCode1);
        Assert.NotNull(mcpCode2);
        Assert.NotNull(mcpCode3);
        Assert.Equal(mcpCode1, mcpCode2);
        Assert.Equal(mcpCode2, mcpCode3);
        Assert.Equal(-32002, mcpCode1.Value);
    }
}
