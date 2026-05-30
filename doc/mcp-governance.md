# MCP Governance Configuration

`dotnet-mcp` supports opt-in governance via `Microsoft.AgentGovernance.Extensions.ModelContextProtocol`.

Governance is disabled by default. When enabled, you can provide one or more policy files through `McpGovernance:PolicyPaths`.

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
- The sample above is a starting point; tune actions and conditions to your environment and threat model.
- If no policy paths are provided, governance behavior depends on the toolkit defaults and your runtime options.

## References

- Agent Governance Toolkit repository: https://github.com/microsoft/agent-governance-toolkit
- Agent Governance Toolkit for .NET: https://github.com/microsoft/agent-governance-toolkit/tree/main/agent-governance-dotnet
- Announcement: https://devblogs.microsoft.com/dotnet/announcing-agent-governance-toolkit-mcp-extensions-for-dotnet/
