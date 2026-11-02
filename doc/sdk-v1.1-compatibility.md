# MCP SDK Compatibility

This page retains its historical filename for existing links. The current server uses **ModelContextProtocol 2.3.0** and **ModelContextProtocol.Extensions.Tasks 2.3.0**, targeting .NET 10 and stdio transport.

The SDK 2.0 migration to MCP 2026-07-28 supersedes the former SDK 1.1 guidance, including its experimental Tasks API and task-store workaround. This page documents the current 2.x implementation; historical changelog entries still describe the behavior of their original releases.

## Protocol revisions

The maximum supported revision is **2026-07-28**. The SDK also accepts legacy initialization, including **2025-11-25**; no protocol pin removes older clients. Tools, resources, prompts, completions, structured results, icons, and existing MCP Apps resource metadata are preserved.

On the current revision, clients use discovery and per-request metadata. The removed `ping`, `resources/subscribe`, and `resources/unsubscribe` methods are not available. Resource subscriptions use `subscriptions/listen` with `resourceSubscriptions`; legacy clients retain the original methods.

Resource updates are routed to each active native listener with its subscription ID in `_meta`, and cancelled listeners are removed. Legacy subscribers continue to receive session notifications without a subscription ID.

`dotnet_server_capabilities` reports the connection's negotiated revision and request-dependent feature availability. A direct, no-connection snapshot reports maximum implementation support rather than promising every client can use each feature.

## Tasks extension

`WithTasks` uses a hosting-owned `InMemoryMcpTaskStore` from the separate Tasks package. The SDK 2.x extension still provides `IMcpTaskStore` and `InMemoryMcpTaskStore`; these are not the removed SDK 1.x experimental API. The tools do not inject the store. Task records are process-local and lost on restart. There are no `ToolTaskSupport` attributes or legacy experimental task capability objects.

Async execution requires protocol 2026-07-28 and per-request opt-in to `io.modelcontextprotocol/tasks`. The extension supplies task creation, `tasks/get`, `tasks/update`, and `tasks/cancel`. Ordinary calls, including legacy-client calls, remain inline. The new extension is not wire-compatible with the old experimental Tasks implementation.

`WithTasks` wraps existing tool handlers automatically, including build, test, publish, and restore; no manual alternate-result handler is needed. Clients receive `CreateTaskResult` for task-backed calls through `ResultOrCreatedTask<CallToolResult>` and poll `tasks/get` for status and the terminal result. There is no separate `tasks/status`, `tasks/list`, or `tasks/result` endpoint. Input requests use `input_required` and are answered through `tasks/update`.

For connected capability snapshots, `asyncTasks` requires both server extension registration and current-request protocol/opt-in support. It is false for ordinary calls, legacy calls, or a server without Tasks registration; opt-in does not carry over to later calls. The no-connection snapshot reports maximum implementation support.

An application error (`CallToolResult.IsError = true`) is retained inside a completed task. Protocol failures produce failed tasks. Cancellation reaches pending elicitation and CLI process execution, while progress reporting, concurrency locks, and tool filters remain active.

## Destructive-operation confirmation

Project Clean, solution Remove, actual EF DatabaseDrop, CertificateClean, SecretsRemove, and SecretsClear request a required boolean confirmation before their selected side effects. On native ordinary calls, MRTR returns an input request and validates the continuation against the operation and arguments. Only an accepted response with `confirmed: true` proceeds; decline, cancel, false, missing, and mistyped confirmation do not. Invalid continuations and failed input exchanges are errors, never implicit approval. Request/task cancellation interrupts pending input without starting the command.

Actual DatabaseDrop still requires `force=true`, even for clients that support elicitation. This flag skips the EF CLI prompt, not MCP confirmation. `dryRun=true` does not request confirmation and does not require force; EF can still build/run design-time application code during a dry run.

### Action-level policy

Confirmation is a selective human-in-the-loop check, not a blanket prompt for every mutation. Existing Clean and solution Remove behavior is retained; new gates focus on database data, user-wide certificates, and user-secret values that the server cannot restore.

| Actions | Safeguards, consequences, and confirmation decision |
| --- | --- |
| Project Clean; solution Remove | Confirm before deleting outputs or changing solution membership. Existing behavior is retained, even though outputs can normally be rebuilt and solution edits can be version-controlled. |
| EF DatabaseDrop | Confirm actual deletion after the independent force check. This permanently deletes the selected database and data. Dry runs remain unprompted. |
| CertificateClean | Confirm removal of all HTTPS development certificates for the current user, which can disrupt other local applications. Platform-specific CLI/OS dialogs are not a portable MCP safeguard. |
| SecretsRemove; SecretsClear | Confirm deletion of a selected key or all secrets for the selected project/current directory. Remove validates the key first. Prompts never include secret values. The store is outside the repository and the server provides no undo. |
| Tool/workload/template uninstall | No new prompt: explicit validated identifiers select normally reinstallable components. Global tools/workloads/templates can affect other projects. Template uninstall without a package only lists installed packs. |
| Package/reference removal; project property/item removal | No new prompt: explicit selection edits project metadata rather than deleting referenced source files. Recovery requires version control or backups; the server does not automatically undo edits. |
| NuGet/template cache clearing; server-metrics resets | No new prompt for regenerable caches/disposable counters. Clearing caches can break offline workflows and repopulation may require access to feeds. |
| EF MigrationsRemove | No new prompt. `force=true` can revert an applied migration as well as remove generated files and can lose data; the existing force behavior is preserved. |
| EF DatabaseUpdate | No new prompt. The selected migration, including `0` rollback, or latest migration runs application-controlled schema/data changes. Even forward migrations can lose data; the server does not infer migration direction or safety from names. |
| Generation/overwrites, configuration edits, process Stop, certificate trust/export, secret Set | No additional MCP prompts. Existing action/parameter intent, overwrite flags, and CLI/platform safeguards remain. Builds, tools, and application code can cause additional effects beyond the action's name. |
| Discovery/check/list actions | No new confirmation. Lack of a prompt does not imply a sandbox or side-effect-free execution: some commands initialize the CLI, build, or run design-time code. |

Legacy clients use the existing one-shot `ElicitAsync` path: the server sends `elicitation/create` and completes the original tool request after the answer. This does not require a native MRTR retry loop in the legacy client. Separately, SDK 2.3.0's backward-compatibility resolver can bridge an explicit MRTR input request to legacy JSON-RPC and retry the handler with its input responses.

The SDK's legacy resolver allows up to **10 retry rounds**. The SDK client's automatic native MRTR retry loop also has a 10-retry limit. A simple confirmation needs only **one input round**: native ordinary calls make an initial `tools/call` and one continuation; explicit MRTR on legacy connections executes its handler twice within one wire-level tool call. The retained production legacy `ElicitAsync` path needs no handler retry.

Tasks deliberately retain task-aware `ElicitAsync`, suspending the existing execution with `input_required` until `tasks/update` supplies the answer, rather than replaying the tool. Clients without elicitation retain the existing unprompted behavior; confirmation is not an authorization boundary.

The unsupported-client rule also applies to the newly gated actions and direct calls without an MCP server; EF's independent force requirement still applies. There is no new bypass parameter or rejection policy for non-interactive clients. Confirmation does not supply backups, rollback, database isolation, or permission to execute arbitrary build/application code. Continue using client permissions, governance, and independent backups for those concerns.

## Retained deprecated stdio features

Roots, Sampling, and Logging have MCP9005 deprecations. Their stateful stdio paths remain in place with narrow, explained diagnostic suppressions. Roots-based workspace discovery and Sampling-assisted failure interpretation remain conditional on client support.

| Capability | Short-term compatibility | Deferred migration |
| --- | --- | --- |
| Roots | Keep SDK-backed `roots/list` discovery when the client advertises Roots. Explicit project/solution paths take precedence; discovery chooses only a single top-level matching file and otherwise retains the existing fallback. | Define an MRTR workspace-input contract and continuation handling before replacing the deprecated Roots API. Preserve explicit-path precedence, ambiguity handling, and cancellation for ordinary and task-backed calls. |
| Sampling | Keep client-mediated analysis of failed build/test commands when the client advertises Sampling. Successful commands and unsupported clients retain their original output without analysis; existing prompt/response bounds and cancellation remain intact. | Define MRTR analysis inputs and continuations, including unsupported or declined input and task-backed execution, before replacing the deprecated Sampling API. Do not introduce a server-side model or credentials as a fallback. |
| Logging | Current-protocol logging already uses the SDK's parsed per-request `MetaKeys.LogLevel`; legacy calls retain session thresholds. Keep the deprecated notification API behind its targeted suppression while it remains supported. | Replace obsolete SDK logging APIs when a supported replacement is available, without regressing per-request opt-in, thresholds, legacy clients, or stderr host logging. This is separate from the deferred Roots/Sampling MRTR work. |

Current-protocol logging requires `_meta["io.modelcontextprotocol/logLevel"]` on each request; omitting it emits no MCP log messages. The server's informational notifications are delivered at Debug/Info thresholds and suppressed at Warning or higher. Legacy session log levels remain supported, defaulting to Info when unset, and ordinary host logging continues to stderr. The logging tests explicitly supply log-level metadata at the transport boundary to verify server wire behavior independently of client metadata injection.

`dotnet_server_capabilities.supports.mcpLogging` indicates whether logging is available to the request, not whether its threshold permits Info messages. `supports.sampling` reflects the client's advertised capability. On `2026-07-28`, these flags are request-scoped and are not inferred from earlier calls; legacy Sampling advertisement is session-scoped. Without a connection, both flags report implementation support rather than a promise of notifications or an available client model. Advertised Roots/Sampling capabilities do not by themselves guarantee that a transport can deliver server-to-client input requests.

The Roots/Sampling MRTR replacement is intentionally deferred; this compatibility work does not add new wire methods or extension IDs. See [the SDK migration tracker](https://github.com/jongalloway/dotnet-mcp/issues/481) and [the separate Elicitation MRTR migration](https://github.com/jongalloway/dotnet-mcp/issues/486) for related context.

## Governance

Governance **5.0.0** is the current dependency, superseding the 4.0.0 package used during the SDK 2.0 migration. Runtime governance is opt-in and disabled by default. When enabled, policy enforcement, authentication requirements, schema scanning, fail-closed startup, and response sanitization are not disabled for SDK compatibility.

The full catalog still encounters an upstream scanner restriction on the certificate tool's `password` schema field, also reproduced with SDK 1.4.0. Safe-subset integration tests establish allow/deny and MRTR/Tasks compatibility; a separate test verifies the pre-existing full-catalog fail-closed behavior is preserved. This is **not** a successful full-catalog governance startup. See [governance configuration](mcp-governance.md).

## Error semantics

Application error annotations such as `mcpErrorCode` remain stable inside tool results. The SDK selects actual JSON-RPC wire errors according to the active revision: for example, missing resource reads use `-32602` on 2026-07-28 and `-32002` on 2025-11-25. Tool-result resource annotations still use `-32002` on both revisions. Application capability annotations (`-32001` or factory-specific `-32603`) are not the SDK's `MissingRequiredClientCapability` wire error (`-32021`). The SDK's renumbered wire codes (`-32020`, `-32021`, `-32022`) do not collide with the custom application annotations. See the [machine-readable contract](machine-readable-contract.md#mcp-error-code-mapping) for the mapping and intentional legacy resource-code overlap.

## References

- [MCP C# SDK 2.3.0 release](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.3.0)
- [Tasks extension](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/docs/concepts/tasks/tasks.md)
- [MRTR](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/docs/concepts/mrtr/mrtr.md)
- [Testing](testing.md#mcp-sdk-compatibility-checks)
