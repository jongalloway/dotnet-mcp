# MCP Apps Integration Guide

## Overview

[MCP Apps](https://github.com/modelcontextprotocol/ext-apps) (SEP-1865) is an extension to the Model Context Protocol that allows MCP servers to surface interactive HTML UIs inline within host applications like VS Code. When a tool with UI metadata is called, the host renders the linked HTML resource in an embedded webview alongside the tool's text output.

This document covers how to implement MCP Apps in a **C# / .NET MCP server** using the `ModelContextProtocol` NuGet SDK, with VS Code as the host.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  VS Code (Host)                                         │
│  ┌──────────────┐     ┌──────────────────────────────┐  │
│  │ MCP Client   │────▶│ MCP Server (stdio)           │  │
│  │              │◀────│  • tools/list                 │  │
│  └──────┬───────┘     │  • tools/call                │  │
│         │             │  • resources/read             │  │
│  ┌──────▼───────┐     └──────────────────────────────┘  │
│  │ Webview      │                                       │
│  │ (iframe)     │  ◀─── postMessage (JSON-RPC) ───▶     │
│  │ HTML App     │       HOST (VS Code)                  │
│  └──────────────┘                                       │
└─────────────────────────────────────────────────────────┘
```

Key insight: the HTML app communicates with the **host** (VS Code) via `postMessage`, not directly with the MCP server. The host proxies `tools/call` and `resources/read` requests to the server on behalf of the app. The MCP server only needs to:

1. Declare a `ui://` resource that returns HTML
2. Add `_meta` linking a tool to that resource
3. Return the HTML via `resources/read`

## Implementation

### 1. Resource Class

Create a resource class that returns HTML with the `text/html;profile=mcp-app` MIME type:

```csharp
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

[McpServerResourceType]
public sealed class McpAppsResources
{
    [McpServerResource(
        UriTemplate = "ui://dotnet-mcp/sdk-dashboard",
        Name = "sdk_dashboard_ui",
        MimeType = "text/html;profile=mcp-app")]
    public static ResourceContents GetSdkDashboardUI() => new TextResourceContents
    {
        Uri = "ui://dotnet-mcp/sdk-dashboard",
        MimeType = "text/html;profile=mcp-app",
        Text = "<html>...</html>",
        Meta = new JsonObject
        {
            ["ui"] = new JsonObject { ["prefersBorder"] = true }
        }
    };
}
```

Requirements:
- **URI scheme**: Must use `ui://`
- **MIME type**: Must be `text/html;profile=mcp-app`
- **Return type**: `TextResourceContents` (not `string`) with `Uri`, `MimeType`, `Text`, and optional `Meta`
- **`Meta.ui`**: Optional rendering hints like `prefersBorder`

### 2. Tool Metadata Linking

Link a tool to the UI resource using `[McpMeta]` attributes:

```csharp
[McpServerTool(Title = ".NET SDK & Templates")]
[McpMeta("ui", JsonValue = """{"resourceUri": "ui://dotnet-mcp/sdk-dashboard"}""")]
[McpMeta("ui/resourceUri", "ui://dotnet-mcp/sdk-dashboard")]
public async partial Task<CallToolResult> DotnetSdk(...)
```

> **Critical**: You must include **both** metadata keys. See [Common Pitfalls](#the-legacy-flat-key-is-required) below.

### 3. Register in Program.cs

```csharp
builder.Services
    .AddMcpServer()
    .WithResources<McpAppsResources>()
    // ... other registrations
```

### 4. HTML App Structure

The HTML returned by the resource must implement the MCP Apps client protocol:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <style>
    :root {
      /* SEP-1865 CSS custom properties — host overrides these */
      --color-background-primary: light-dark(#ffffff, #1e1e1e);
      --color-text-primary: light-dark(#1a1a1a, #e0e0e0);
      /* ... more theme variables */
    }
    body { color-scheme: light dark; }
  </style>
</head>
<body>
  <div id="content">Loading...</div>

  <script>
    // JSON-RPC over postMessage to the host
    let nextId = 1;
    const pending = {};

    function sendRequest(method, params) {
      const id = nextId++;
      window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
      return new Promise((resolve, reject) => {
        pending[id] = { resolve, reject };
        setTimeout(() => { delete pending[id]; reject(new Error('Timeout')); }, 30000);
      });
    }

    function sendNotification(method, params) {
      window.parent.postMessage({ jsonrpc: '2.0', method, params }, '*');
    }

    window.addEventListener('message', (e) => {
      const msg = e.data;
      if (!msg?.jsonrpc) return;
      if (msg.id && pending[msg.id]) {
        const { resolve, reject } = pending[msg.id];
        delete pending[msg.id];
        msg.error ? reject(new Error(msg.error.message)) : resolve(msg.result);
        return;
      }
      // Handle host notifications
      if (msg.method === 'ui/notifications/tool-input') { /* ... */ }
      if (msg.method === 'ui/notifications/tool-result') { /* ... */ }
      if (msg.method === 'ui/notifications/host-context-changed') { /* ... */ }
    });

    // Initialize handshake
    async function init() {
      const result = await sendRequest('ui/initialize', {
        protocolVersion: '2026-01-26',
        clientInfo: { name: 'my-app', version: '1.0.0' },
        appCapabilities: {}
      });
      sendNotification('ui/notifications/initialized', {});
      // App is now connected — fetch data via tools/call
    }

    // Auto-resize so the host adjusts iframe height
    const ro = new ResizeObserver(() => {
      sendNotification('ui/notifications/size-changed', {
        width: document.documentElement.scrollWidth,
        height: document.documentElement.scrollHeight
      });
    });
    ro.observe(document.documentElement);

    init();
  </script>
</body>
</html>
```

Key app behaviors:
- **`ui/initialize`**: Handshake with the host; receives theme context
- **`ui/notifications/initialized`**: Confirms the app is ready
- **`tools/call`**: Call MCP server tools (proxied by the host)
- **`ui/notifications/size-changed`**: Report content size for iframe resizing
- **`ui/message`**: Send messages to the chat conversation
- **Host notifications**: `tool-input`, `tool-result`, `host-context-changed`

## Common Pitfalls

### The Legacy Flat Key Is Required

The ext-apps spec defines two ways to link a tool to a UI resource:

| Format | Key | Example |
|--------|-----|---------|
| **Modern (nested)** | `_meta.ui.resourceUri` | `{ "ui": { "resourceUri": "ui://..." } }` |
| **Legacy (flat)** | `_meta["ui/resourceUri"]` | `{ "ui/resourceUri": "ui://..." }` |

The spec says hosts "must check both formats." In practice, **VS Code currently only checks the legacy flat key**. The TypeScript `registerAppTool` helper from `@modelcontextprotocol/ext-apps` automatically emits both, which is why TypeScript servers work out of the box.

In C#, you must add both explicitly:

```csharp
// Modern nested format (for spec compliance and future hosts)
[McpMeta("ui", JsonValue = """{"resourceUri": "ui://dotnet-mcp/sdk-dashboard"}""")]
// Legacy flat key (required for VS Code to detect the UI)
[McpMeta("ui/resourceUri", "ui://dotnet-mcp/sdk-dashboard")]
```

Without the flat key, VS Code will:
- Successfully fetch the resource via `resources/read`
- Execute the tool normally
- **Never create a webview** (zero MCP App console messages)

### Return TextResourceContents, Not String

The resource handler must return `TextResourceContents` with explicit properties:

```csharp
// CORRECT
public static ResourceContents GetUI() => new TextResourceContents
{
    Uri = "ui://dotnet-mcp/sdk-dashboard",
    MimeType = "text/html;profile=mcp-app",
    Text = htmlString,
    Meta = new JsonObject { ["ui"] = new JsonObject { ["prefersBorder"] = true } }
};
```

Returning a plain `string` from the resource handler will not include the required MIME type in the wire format.

### VS Code Setting Must Be Enabled

MCP Apps requires `chat.mcp.apps.enabled` to be `true` in VS Code settings. This is a preview feature in VS Code Insiders.

### Tool Name Casing in tools/call from the App

When your HTML app calls `tools/call` via the host proxy, use the exact tool name as it appears in `tools/list`. The C# MCP SDK lowercases and snake_cases method names (e.g., `DotnetSdk` → `dotnet_sdk`).

## Wire Format Reference

A working `tools/list` response for a tool with MCP Apps UI looks like:

```json
{
  "name": "dotnet_sdk",
  "title": ".NET SDK & Templates",
  "inputSchema": { ... },
  "_meta": {
    "ui": { "resourceUri": "ui://dotnet-mcp/sdk-dashboard" },
    "ui/resourceUri": "ui://dotnet-mcp/sdk-dashboard",
    "category": "sdk",
    "priority": 9
  }
}
```

The `resources/read` response for the UI resource:

```json
{
  "contents": [{
    "uri": "ui://dotnet-mcp/sdk-dashboard",
    "mimeType": "text/html;profile=mcp-app",
    "text": "<!DOCTYPE html>...",
    "_meta": {
      "ui": { "prefersBorder": true }
    }
  }]
}
```

## Theming

MCP Apps defines CSS custom properties that the host injects. Use these in your HTML for automatic light/dark theme support:

| Variable | Purpose |
|----------|---------|
| `--color-background-primary` | Main background |
| `--color-background-secondary` | Card/table header background |
| `--color-text-primary` | Main text color |
| `--color-text-secondary` | Muted text |
| `--color-border-primary` | Borders |
| `--font-sans` | UI font family |
| `--font-mono` | Code font family |

Use `color-scheme: light dark` and `light-dark()` for CSS fallback values when the host hasn't provided variables yet.

## Testing

### Wire Format Test

Verify the `_meta` serialization is correct with a test that connects to the server and inspects the `tools/list` response:

```csharp
[Fact]
public async Task DotnetSdk_Meta_Ui_ShouldHaveBothFormats()
{
    var tools = await _client.ListToolsAsync();
    var sdkTool = tools.First(t => t.Name == "dotnet_sdk");
    var json = JsonSerializer.Serialize(sdkTool.ProtocolTool, ...);
    using var doc = JsonDocument.Parse(json);
    var meta = doc.RootElement.GetProperty("_meta");

    // Modern nested format
    var ui = meta.GetProperty("ui");
    Assert.Equal(JsonValueKind.Object, ui.ValueKind);
    Assert.Equal("ui://dotnet-mcp/sdk-dashboard",
        ui.GetProperty("resourceUri").GetString());

    // Legacy flat key
    Assert.Equal("ui://dotnet-mcp/sdk-dashboard",
        meta.GetProperty("ui/resourceUri").GetString());
}
```

### Manual Testing

1. Build the project
2. Register in VS Code's `mcp.json`
3. Enable `chat.mcp.apps.enabled` in VS Code Insiders settings
4. Call the tool from Copilot Chat — the UI should render inline

## References

- [MCP Apps (ext-apps) repository](https://github.com/modelcontextprotocol/ext-apps)
- [SEP-1865 specification](https://github.com/anthropics/anthropic-cookbook/blob/main/misc/sep-1865-mcp-apps.md)
- [VS Code MCP Apps docs](https://code.visualstudio.com/docs/copilot/chat/mcp-servers#_mcp-server-apps)
- [basic-vue reference server](https://www.npmjs.com/package/@anthropic-ai/mcp-server-basic-vue) — minimal working TypeScript example
