# SDK v1.1 Compatibility Audit

## Summary

This document summarizes the compatibility audit for the ModelContextProtocol C# SDK v1.1.0 integration in dotnet-mcp.

**Conclusion**: dotnet-mcp is compatible with SDK v1.1.0. The core server setup from the v1.0 line remains valid, and the project now also adopts task-related SDK features for long-running operations.

## Changes Adopted in This Upgrade

### Package Update

- Updated the `ModelContextProtocol` package reference to `1.1.0`.

### Task Support Adoption

dotnet-mcp now uses task-related SDK APIs for long-running tool execution:

- Registers `IMcpTaskStore` with `InMemoryMcpTaskStore` in `Program.cs`
- Declares `TaskSupport = ToolTaskSupport.Optional` on `dotnet_project`
- Continues to pair task-aware flows with MCP progress notifications for build, test, publish, and related long-running operations

This enables capable clients to run long-running requests using the MCP task lifecycle instead of forcing all such operations to block a single foreground request.

### Existing v1.0-era Features Preserved

The upgrade preserves the MCP features already in use across the server:

- Tools with rich metadata and icons
- Resources for SDK, runtime, template, and framework discovery
- Prompts for guided .NET workflows
- Roots-based workspace discovery
- Sampling for AI-assisted failure interpretation
- Elicitation for destructive-operation confirmation
- Progress notifications for long-running operations
- Completion handlers for prompt and parameter entry

## dotnet-mcp SDK Usage in v1.1

### Server-Side APIs Used

dotnet-mcp uses these MCP C# SDK registration and hosting features:

1. `AddMcpServer()` for DI-based server registration
2. `WithStdioServerTransport()` for stdio transport
3. `WithTools<DotNetCliTools>()` for the consolidated tool surface
4. `WithResources<DotNetResources>()` for read-only SDK metadata resources
5. `WithPrompts<DotNetPrompts>()` for reusable workflow prompts
6. `WithSubscribeToResourcesHandler(...)` and `WithUnsubscribeFromResourcesHandler(...)` for resource subscription support
7. `WithCompleteHandler(...)` for completion/autocomplete support

### SDK Features Actively Used

The project exercises the following MCP capabilities through the C# SDK:

- **Tools**: consolidated tool methods with XML-doc-generated descriptions and rich metadata
- **Resources**: cached JSON resources for installed SDKs, runtimes, templates, and frameworks
- **Prompts**: reusable prompt templates for common .NET workflows
- **Roots**: automatic project/solution discovery from client workspace roots
- **Sampling**: client-mediated LLM calls for concise error interpretation
- **Elicitation**: explicit confirmation for destructive operations
- **Progress**: progress notifications for long-running operations
- **Tasks**: optional task support for long-running project operations
- **Completion**: prompt argument completion support
- **Icons and metadata**: tool and server presentation data for MCP-aware clients

## Protocol Version

The server continues to advertise the MCP protocol version string `2025-11-25`, which remains appropriate for the current SDK line.

## Compatibility Notes

### No Breaking Server Registration Changes Required

The existing DI-based host setup remains valid under v1.1. The upgrade did not require architectural changes to:

- server registration
- stdio transport configuration
- source-generator-based tool/resource/prompt discovery
- structured `CallToolResult` handling

### User-Facing Impact

The most meaningful user-facing outcome of the v1.1 adoption is improved support for long-running MCP-native workflows:

- clients can treat `dotnet_project` operations as optional MCP tasks
- progress reporting continues to work for long-running operations
- destructive actions still use elicitation for safety
- resource, prompt, and roots-based discovery remain first-class MCP flows

## Documentation Updates

As part of the v1.1 audit, the following documentation was refreshed:

- `README.md`
- `doc/telemetry.md`
- `doc/icons.md`
- this compatibility audit

## References

- [MCP C# SDK documentation](https://csharp.sdk.modelcontextprotocol.io/)
- [MCP C# SDK v1.1.0 release](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v1.1.0)
- [Model Context Protocol specification](https://modelcontextprotocol.io/specification/latest)

## Audit Date

**Date**: 2026-03-14  
**SDK Version**: ModelContextProtocol 1.1.0  
**Result**: Compatible - existing integration preserved, task support adoption documented
