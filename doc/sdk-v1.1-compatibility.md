# MCP SDK Compatibility

This page retains its historical filename for existing links. The current server uses **ModelContextProtocol 2.0.0** and **ModelContextProtocol.Extensions.Tasks 2.0.0**, targeting .NET 10 and stdio transport.

## Protocol revisions

The maximum supported revision is **2026-07-28**. The SDK also accepts legacy initialization, including **2025-11-25**; no protocol pin removes older clients. Tools, resources, prompts, completions, structured results, icons, and existing MCP Apps resource metadata are preserved.

On the current revision, clients use discovery and per-request metadata. The removed `ping`, `resources/subscribe`, and `resources/unsubscribe` methods are not available. Resource subscriptions use `subscriptions/listen` with `resourceSubscriptions`; legacy clients retain the original methods.

Resource updates are routed to each active native listener with its subscription ID in `_meta`, and cancelled listeners are removed. Legacy subscribers continue to receive session notifications without a subscription ID.

`dotnet_server_capabilities` reports the connection's negotiated revision and request-dependent feature availability. A direct, no-connection snapshot reports maximum implementation support rather than promising every client can use each feature.

## Tasks extension

`WithTasks` uses a shared `InMemoryMcpTaskStore` from the separate Tasks package. There are no `ToolTaskSupport` attributes or legacy experimental task capability objects.

Async execution requires protocol 2026-07-28 and per-request opt-in to `io.modelcontextprotocol/tasks`. The extension supplies task creation, `tasks/get`, `tasks/update`, and `tasks/cancel`. Ordinary calls, including legacy-client calls, remain inline. The new extension is not wire-compatible with the old experimental Tasks implementation.

An application error (`CallToolResult.IsError = true`) is retained inside a completed task. Protocol failures produce failed tasks. Cancellation reaches pending elicitation and CLI process execution, while progress reporting, concurrency locks, and tool filters remain active.

## Destructive-operation confirmation

Clean and solution Remove request a required boolean confirmation before side effects. On native ordinary calls, MRTR returns an input request and validates the continuation against the operation and arguments. Only an accepted response with `confirmed: true` proceeds; decline, cancel, and false confirmation do not.

Legacy clients retain elicitation, and the SDK's legacy MRTR resolver remains available. Tasks use task-aware elicitation and `input_required`/`tasks/update`, because SDK 2.0 does not compose explicit `InputRequiredException` with task handlers. Clients without elicitation retain the existing unprompted behavior; confirmation is not an authorization boundary.

## Retained deprecated stdio features

Roots, Sampling, and Logging have MCP9005 deprecations. Their stateful stdio paths remain in place with narrow, explained diagnostic suppressions. Roots-based workspace discovery and Sampling-assisted failure interpretation remain conditional on client support.

Current-protocol logging requires `_meta["io.modelcontextprotocol/logLevel"]` on each request; omitting it emits no MCP log messages. Legacy session log levels remain supported, and ordinary host logging continues to stderr. SDK 2.0's client currently strips the log-level metadata during request injection; the logging tests supply it at the transport boundary to verify the server's actual wire behavior.

## Governance

Governance **4.0.0** remains a required dependency. Policy enforcement, authentication requirements, schema scanning, fail-closed startup, and response sanitization are not disabled for SDK compatibility.

The full catalog still encounters an upstream scanner restriction on the certificate tool's `password` schema field, also reproduced with SDK 1.4.0. Safe-subset integration tests establish allow/deny and MRTR/Tasks compatibility; a separate test verifies the pre-existing full-catalog fail-closed behavior is preserved. This is **not** a successful full-catalog governance startup. See [governance configuration](mcp-governance.md).

## Error semantics

Application error annotations such as `mcpErrorCode` remain stable inside tool results. The SDK selects actual JSON-RPC wire errors according to the active revision: for example, missing resource reads use `-32602` on 2026-07-28 and `-32002` on 2025-11-25. See the [machine-readable contract](machine-readable-contract.md#mcp-error-code-mapping).

## References

- [MCP C# SDK 2.0.0 release](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.0.0)
- [Tasks extension](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.0.0/docs/concepts/tasks/tasks.md)
- [MRTR](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.0.0/docs/concepts/mrtr/mrtr.md)
- [Testing](testing.md#mcp-sdk-compatibility-checks)
