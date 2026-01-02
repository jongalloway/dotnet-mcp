# CAPABILITY_NOT_AVAILABLE Error Strategy

## Overview

The `CAPABILITY_NOT_AVAILABLE` error code provides a standardized way to communicate that a feature exists in the API but cannot be executed in the current environment or configuration.

## Purpose

This error strategy disambiguates between:
- **Method doesn't exist** (standard JSON-RPC -32601 "Method not found")
- **Method exists but can't run** (`CAPABILITY_NOT_AVAILABLE`)

## Use Cases

Use `CAPABILITY_NOT_AVAILABLE` when:

1. **Feature not yet implemented** - Planned functionality that's in the API but not ready
2. **Feature flag disabled** - Capability exists but is disabled by configuration
3. **OS/platform limitations** - Feature requires specific OS or platform
4. **Missing dependencies** - Required external tools or libraries not available
5. **Environment constraints** - Cannot run in current deployment environment

## API

### Helper Method

```csharp
public static ErrorResponse ReturnCapabilityNotAvailable(
    string feature, 
    string reason, 
    List<string>? alternatives = null)
```

**Parameters:**
- `feature` - Name of the unavailable capability
- `reason` - Why the capability is not available
- `alternatives` - Optional list of alternative actions or tools

**Returns:**
- `ErrorResponse` with:
  - Error code: `CAPABILITY_NOT_AVAILABLE`
  - Category: `Capability`
  - MCP error code: `-32603` (InternalError)
  - Structured data with feature and reason
  - Optional alternatives array

## Response Structure

```json
{
  "success": false,
  "errors": [
    {
      "code": "CAPABILITY_NOT_AVAILABLE",
      "message": "The 'feature-name' capability is not available: reason-here",
      "category": "Capability",
      "hint": "Consider using one of the suggested alternatives.",
      "explanation": "This tool/feature exists but cannot be executed in the current environment...",
      "alternatives": [
        "Alternative action 1",
        "Alternative action 2"
      ],
      "mcpErrorCode": -32603,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "feature": "feature-name",
          "reason": "reason-here"
        }
      }
    }
  ],
  "exitCode": -1
}
```

## Examples

### Example 1: Not Yet Implemented

```csharp
[McpServerTool]
public Task<string> DotnetTelemetry(bool enable = true)
{
    var alternatives = new List<string>
    {
        "Use dotnet_server_capabilities to check current feature support",
        "Monitor SDK usage manually through build logs",
        "Use external telemetry tools like Application Insights"
    };

    var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
        "telemetry reporting",
        "Not yet implemented - planned for future release",
        alternatives);

    return Task.FromResult(ErrorResultFactory.ToJson(error));
}
```

### Example 2: OS Limitation

```csharp
public string EnableWindowsAuth()
{
    if (!OperatingSystem.IsWindows())
    {
        var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
            "Windows authentication",
            "Requires Windows operating system",
            alternatives: new List<string> 
            { 
                "Use JWT authentication instead",
                "Configure OAuth 2.0 authentication"
            });
        
        return ErrorResultFactory.ToJson(error);
    }
    
    // Windows-specific implementation...
}
```

### Example 3: Feature Flag Disabled

```csharp
public string GetAdvancedMetrics()
{
    if (!_config.EnableAdvancedMetrics)
    {
        var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
            "advanced metrics",
            "Feature flag disabled in configuration",
            alternatives: new List<string>
            {
                "Enable advanced metrics in server configuration",
                "Use basic metrics via dotnet_cache_metrics"
            });
        
        return ErrorResultFactory.ToJson(error);
    }
    
    // Implementation when enabled...
}
```

### Example 4: Resource Not Available

```csharp
[McpServerResource(UriTemplate = "dotnet://telemetry-data")]
public Task<string> GetTelemetryData()
{
    var alternatives = new List<string>
    {
        "Use dotnet://sdk-info to get SDK version information",
        "Use dotnet://runtime-info for runtime details"
    };

    var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
        "telemetry data resource",
        "Telemetry collection not yet implemented",
        alternatives);

    return Task.FromResult(ErrorResultFactory.ToJson(error));
}
```

## Benefits

### For AI Agents
- **Clear signal**: Agent knows the method exists but can't be used
- **Actionable alternatives**: Agent can select a different approach
- **Better planning**: Agent treats this as a constraint rather than an error

### For Developers
- **Consistent pattern**: Standardized way to handle unavailable features
- **Self-documenting**: Response clearly explains what's unavailable and why
- **Graceful degradation**: Alternatives guide users to working solutions

### For Users
- **Clear messages**: Understand what's unavailable and why
- **Next steps**: Get concrete alternatives instead of dead ends
- **Future visibility**: Know what features are planned

## Best Practices

1. **Be specific** - Clearly state what capability is unavailable
2. **Explain why** - Give a concise reason for the limitation
3. **Provide alternatives** - When possible, suggest workarounds
4. **Keep alternatives actionable** - Suggest concrete next steps
5. **Don't overuse** - Reserve for genuine capability gaps, not regular errors

## Related Error Codes

- `EXIT_1` - Generic command failure (different from capability limitation)
- `OPERATION_CANCELLED` - User-initiated cancellation
- `CONCURRENCY_CONFLICT` - Resource temporarily unavailable due to concurrent access

## MCP Error Code Mapping

`CAPABILITY_NOT_AVAILABLE` maps to JSON-RPC error code `-32603` (InternalError), indicating a server-side limitation that prevents execution.
