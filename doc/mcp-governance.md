# MCP Governance Configuration

`dotnet-mcp` supports opt-in governance via `Microsoft.AgentGovernance.Extensions.ModelContextProtocol`.

Governance is disabled by default. When enabled, you can provide one or more policy files through `McpGovernance:PolicyPaths`.

> **Current limitation:** Enabling governance with the full tool catalog fails startup. The configuration and policy examples below describe configuration syntax, not a working full-catalog workaround.

## SDK compatibility and startup restriction

The current governance extension is **5.0.0**, used with MCP SDK **2.3.0**. The SDK 2.0 migration initially used governance 4.0.0; those package versions are no longer the current dependency pins. Compatibility tests exercise allow/deny policies with production tools for inline and Tasks execution, and confirmation through MRTR and task input updates.

**Full-catalog governance startup has a pre-existing toolkit scanner limitation.** This is reproducible with MCP SDK **1.4.0**, not introduced by SDK 2.0. The unchanged baseline first rejects an innocuous template description containing "override"; after rewording only that description and the equivalent EF description, SDK 1.4 also rejects the existing `dotnet_dev_certs` input property `password` as an unsafe sensitive field. The v4 and v5 scanners both contain this rule. The parameter is part of the certificate-export tool contract; renaming it or disabling the scanner is not an SDK compatibility fix.

The server keeps `ScanToolsOnStartup`, `FailOnUnsafeTools`, policy enforcement, authentication requirements, and response sanitization unchanged. Enabling governance with the full catalog therefore fails startup rather than silently accepting the flagged schema, preserving the baseline fail-closed behavior. The tests separately verify that rejection and policy/MRTR/Tasks compatibility on a safe subset of production tools with scanning still enabled. The package upgrade does not resolve the independent full-catalog startup limitation; that still requires an upstream scanner fix or an explicitly approved governance-policy change.

There is currently no supported full-catalog workaround documented here. An allow policy does not bypass startup scanning, and the safe subset used by integration tests is not a supported runtime catalog-selection option. Do not disable scanning, relax fail-closed behavior, or rename the certificate password parameter to evade the finding.

Leave `McpGovernance:Enabled` set to `false` (the default) for ordinary server use without governance. This supplies **no governance enforcement** and is not an equivalent security workaround. If your deployment requires governance, the full-catalog startup restriction remains a blocker.

## Destructive-operation confirmation depends on client elicitation

**MCP confirmation prompts require client elicitation support**, independently of whether governance is enabled. Project Clean, solution Remove, actual EF DatabaseDrop, CertificateClean, SecretsRemove, and SecretsClear request confirmation when the client advertises elicitation. Native ordinary calls use multi-round-trip requests (MRTR), legacy clients use `elicitation/create`, and opted-in Tasks suspend for input until `tasks/update` supplies the answer. MRTR and Tasks do not remove the elicitation requirement.

For these prompted operations, only an accepted response with `confirmed: true` proceeds. Declined, cancelled, false, missing, or mistyped confirmation does not authorize execution; failed input exchanges are errors, not implicit approval. Clients without elicitation retain the existing **unprompted behavior**, as do direct calls without an MCP server. Actual EF DatabaseDrop still independently requires `force=true`; `dryRun=true` does not require force or request confirmation.

Confirmation is a selective human-in-the-loop safeguard, **not an authorization boundary** or a blanket prompt for every mutation. Continue using client permissions and independent backups; do not rely on confirmation as a substitute for governance enforcement. See the [destructive-operation confirmation policy](sdk-v1.1-compatibility.md#destructive-operation-confirmation) for action-level coverage, consequences, and protocol details.

## Example appsettings configuration

```json
"McpGovernance": {
  "Enabled": true,
  "DefaultAgentId": "did:mcp:anonymous",
  "ServerName": "dotnet-mcp",
  "PolicyPaths": [
    "/path/to/your/policies/mcp.yaml"
  ]
}
```

## Example policy file (`mcp.yaml`)

```yaml
apiVersion: governance.toolkit/v1
version: "1.0"
name: dotnet-mcp-sample-policy
default_action: deny
rules:
  - name: allow-dotnet-tools
    condition: "tool_name =~ '^dotnet_'"
    action: allow
    priority: 100
```

## Notes

- This repository intentionally does not bundle a default policy file for runtime enforcement.
- The sample above illustrates policy syntax; it does not resolve the full-catalog startup restriction. Tune actions and conditions to your environment and threat model.
- If no policy paths are provided, governance behavior depends on the toolkit defaults and your runtime options.

## References

- Agent Governance Toolkit repository: https://github.com/microsoft/agent-governance-toolkit
- Agent Governance Toolkit for .NET: https://github.com/microsoft/agent-governance-toolkit/tree/main/agent-governance-dotnet
- Announcement: https://devblogs.microsoft.com/dotnet/announcing-agent-governance-toolkit-mcp-extensions-for-dotnet/
