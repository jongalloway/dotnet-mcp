namespace DotNetMcp;

/// <summary>
/// MCP (Model Context Protocol) error codes following JSON-RPC 2.0 specification.
/// These codes align with MCP v0.5.0 protocol error semantics.
/// </summary>
public static class McpErrorCodes
{
    /// <summary>
    /// Parse error - Invalid JSON was received by the server (-32700)
    /// </summary>
    public const int ParseError = -32700;

    /// <summary>
    /// Invalid Request - The JSON sent is not a valid Request object (-32600)
    /// </summary>
    public const int InvalidRequest = -32600;

    /// <summary>
    /// Method not found - The method does not exist / is not available (-32601)
    /// </summary>
    public const int MethodNotFound = -32601;

    /// <summary>
    /// Invalid params - Invalid method parameter(s) (-32602)
    /// </summary>
    public const int InvalidParams = -32602;

    /// <summary>
    /// Internal error - Internal JSON-RPC error (-32603)
    /// </summary>
    public const int InternalError = -32603;

    /// <summary>
    /// Resource not found - The requested resource does not exist (MCP-specific: -32002)
    /// Used when a resource URI cannot be resolved or a file/project/package is not found.
    /// </summary>
    public const int ResourceNotFound = -32002;

    /// <summary>
    /// Capability not available - The tool/feature exists but is unavailable in the current environment (MCP-specific: -32001)
    /// Used when a feature is not yet implemented, disabled, or unsupported due to environment limitations.
    /// </summary>
    public const int CapabilityNotAvailable = -32001;

    /// <summary>
    /// Server error range start - Reserved for implementation-defined server errors (-32000)
    /// </summary>
    public const int ServerErrorRangeStart = -32000;

    /// <summary>
    /// Server error range end - Reserved for implementation-defined server errors (-32099)
    /// </summary>
    public const int ServerErrorRangeEnd = -32099;

    /// <summary>
    /// Determines if an MCP error code should be assigned based on the error context.
    /// </summary>
    /// <param name="errorCode">The dotnet error code (e.g., "CS0103", "NU1101", "EXIT_1")</param>
    /// <param name="category">The error category (e.g., "Compilation", "Package", "Unknown")</param>
    /// <param name="exitCode">The process exit code</param>
    /// <returns>The appropriate MCP error code, or null if no specific MCP code applies</returns>
    public static int? GetMcpErrorCode(string errorCode, string category, int exitCode)
    {
        // Resource not found scenarios
        // Use Equals for exact matching to avoid false positives like "NU1101ABC"
        if (errorCode.Equals("NU1101", StringComparison.OrdinalIgnoreCase) || // Package not found
            errorCode.Equals("NU1102", StringComparison.OrdinalIgnoreCase) || // Package version not found
            errorCode.Equals("MSB1003", StringComparison.OrdinalIgnoreCase) || // Project/solution file not found
            errorCode.Equals("NETSDK1004", StringComparison.OrdinalIgnoreCase) || // Assets file not found
            errorCode.Equals("MSB4236", StringComparison.OrdinalIgnoreCase)) // SDK not found
        {
            return ResourceNotFound;
        }

        // Invalid params scenarios
        if (errorCode.Equals("NETSDK1045", StringComparison.OrdinalIgnoreCase) || // Framework not supported
            errorCode.Equals("CS1001", StringComparison.OrdinalIgnoreCase) || // Identifier expected
            errorCode.Equals("CS1513", StringComparison.OrdinalIgnoreCase)) // Closing brace expected
        {
            return InvalidParams;
        }

        // Internal errors for unexpected failures
        if (errorCode == "OPERATION_CANCELLED" || 
            errorCode == "CONCURRENCY_CONFLICT" ||
            errorCode == "CAPABILITY_NOT_AVAILABLE")
        {
            return InternalError;
        }

        // For generic failures, use InternalError
        if (exitCode != 0 && category == "Unknown")
        {
            return InternalError;
        }

        // No specific MCP error code for this scenario
        return null;
    }
}
