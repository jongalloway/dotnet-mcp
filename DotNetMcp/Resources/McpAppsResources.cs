using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// MCP Apps UI resources (SEP-1865).
/// Provides interactive HTML views rendered inline in hosts that support
/// the <c>io.modelcontextprotocol/ui</c> extension.
/// </summary>
[McpServerResourceType]
public sealed class McpAppsResources
{
    [McpServerResource(
        UriTemplate = "ui://dotnet-mcp/sdk-dashboard",
        Name = "sdk_dashboard_ui",
        MimeType = "text/html;profile=mcp-app")]
    [McpMeta("ui", JsonValue = """{"prefersBorder": true}""")]
    public static ResourceContents GetSdkDashboardUI() => new TextResourceContents
    {
        Uri = "ui://dotnet-mcp/sdk-dashboard",
        MimeType = "text/html;profile=mcp-app",
        Text = SdkDashboardHtml,
        Meta = new JsonObject
        {
            ["ui"] = new JsonObject { ["prefersBorder"] = true }
        }
    };

    /// <summary>
    /// Interactive HTML dashboard for .NET SDK information.
    /// Renders installed SDKs, runtimes, and framework info in a styled table
    /// with host-aware theming via CSS variables from SEP-1865.
    /// </summary>
    private const string SdkDashboardHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>.NET SDK Dashboard</title>
<style>
  :root {
    /* Fallback values when host doesn't provide theme variables */
    --color-background-primary: light-dark(#ffffff, #1e1e1e);
    --color-background-secondary: light-dark(#f9fafb, #2d2d2d);
    --color-text-primary: light-dark(#1a1a1a, #e0e0e0);
    --color-text-secondary: light-dark(#6b7280, #9ca3af);
    --color-border-primary: light-dark(#e5e7eb, #404040);
    --color-background-info: light-dark(#eff6ff, #1e3a5f);
    --color-text-info: light-dark(#1d4ed8, #60a5fa);
    --color-background-success: light-dark(#f0fdf4, #14532d);
    --color-text-success: light-dark(#15803d, #4ade80);
    --color-background-warning: light-dark(#fffbeb, #78350f);
    --color-text-warning: light-dark(#b45309, #fbbf24);
    --border-radius-md: 8px;
    --border-radius-sm: 4px;
    --font-sans: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    --font-mono: 'Cascadia Code', 'Fira Code', Consolas, monospace;
    --font-text-sm-size: 13px;
    --font-text-md-size: 14px;
  }
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body {
    font-family: var(--font-sans);
    font-size: var(--font-text-md-size);
    color: var(--color-text-primary);
    background: var(--color-background-primary);
    padding: 16px;
    line-height: 1.5;
    color-scheme: light dark;
  }
  h2 {
    font-size: 16px;
    font-weight: 600;
    margin-bottom: 8px;
    display: flex;
    align-items: center;
    gap: 6px;
  }
  .section { margin-bottom: 16px; }
  .badge {
    display: inline-block;
    font-size: 11px;
    font-weight: 500;
    padding: 2px 8px;
    border-radius: 9999px;
  }
  .badge-lts {
    background: var(--color-background-success);
    color: var(--color-text-success);
  }
  .badge-preview {
    background: var(--color-background-warning);
    color: var(--color-text-warning);
  }
  .badge-current {
    background: var(--color-background-info);
    color: var(--color-text-info);
  }
  table {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--font-text-sm-size);
  }
  th {
    text-align: left;
    padding: 6px 10px;
    background: var(--color-background-secondary);
    color: var(--color-text-secondary);
    font-weight: 500;
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    border-bottom: 1px solid var(--color-border-primary);
  }
  td {
    padding: 6px 10px;
    border-bottom: 1px solid var(--color-border-primary);
  }
  .version-cell {
    font-family: var(--font-mono);
    font-weight: 500;
  }
  .loading, .error {
    text-align: center;
    padding: 24px;
    color: var(--color-text-secondary);
  }
  .error { color: var(--color-text-warning); }
  .action-bar {
    display: flex;
    gap: 8px;
    margin-bottom: 12px;
  }
  button {
    border: 1px solid var(--color-border-primary);
    border-radius: var(--border-radius-sm);
    background: var(--color-background-secondary);
    color: var(--color-text-primary);
    padding: 4px 12px;
    font-size: var(--font-text-sm-size);
    cursor: pointer;
    font-family: var(--font-sans);
  }
  button:hover { opacity: 0.8; }
  button:disabled { opacity: 0.5; cursor: default; }
  .refresh-spin { animation: spin 1s linear infinite; display: inline-block; }
  @keyframes spin { to { transform: rotate(360deg); } }
</style>
</head>
<body>
  <div class="action-bar">
    <button id="btnRefresh" onclick="refreshData()">&#x21bb; Refresh</button>
    <button onclick="sendToChat()">&#x1f4cb; Send to Chat</button>
  </div>

  <div class="section">
    <h2>&#x2699;&#xfe0f; Installed SDKs</h2>
    <div id="sdks"><div class="loading">Loading SDK data&hellip;</div></div>
  </div>

  <div class="section">
    <h2>&#x1f4e6; Installed Runtimes</h2>
    <div id="runtimes"><div class="loading">Loading runtime data&hellip;</div></div>
  </div>

  <div id="status" style="font-size:11px;color:var(--color-text-secondary);margin-top:8px;"></div>

<script>
// === MCP Apps JSON-RPC transport (SEP-1865) ===
let nextId = 1;
const pending = {};

function sendRequest(method, params) {
  const id = nextId++;
  window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
  return new Promise((resolve, reject) => {
    pending[id] = { resolve, reject };
    setTimeout(() => {
      if (pending[id]) {
        delete pending[id];
        reject(new Error('Request timed out: ' + method));
      }
    }, 30000);
  });
}

function sendNotification(method, params) {
  window.parent.postMessage({ jsonrpc: '2.0', method, params }, '*');
}

// Track data received from host
let toolInputData = null;
let toolResultData = null;
let hostContext = null;

window.addEventListener('message', (e) => {
  const msg = e.data;
  if (!msg || !msg.jsonrpc) return;

  // Handle responses to our requests
  if (msg.id && pending[msg.id]) {
    const { resolve, reject } = pending[msg.id];
    delete pending[msg.id];
    if (msg.error) reject(new Error(msg.error.message || 'Unknown error'));
    else resolve(msg.result);
    return;
  }

  // Handle notifications from host
  if (msg.method === 'ui/notifications/tool-input') {
    toolInputData = msg.params?.arguments;
    setStatus('Received tool input');
  }
  if (msg.method === 'ui/notifications/tool-result') {
    toolResultData = msg.params;
    renderFromToolResult(msg.params);
  }
  if (msg.method === 'ui/notifications/host-context-changed') {
    Object.assign(hostContext || {}, msg.params);
    applyTheme();
  }
});

// === Initialization ===
async function init() {
  try {
    const result = await sendRequest('ui/initialize', {
      protocolVersion: '2026-01-26',
      clientInfo: { name: 'dotnet-mcp-sdk-dashboard', version: '1.0.0' },
      appCapabilities: { availableDisplayModes: ['inline'] }
    });
    hostContext = result?.hostContext;
    applyTheme();
    sendNotification('ui/notifications/initialized', {});
    setStatus('Connected to host');

    // If we don't get tool data within 2s, fetch it ourselves
    setTimeout(() => {
      if (!toolResultData) fetchSdkData();
    }, 2000);
  } catch (err) {
    setStatus('Init failed: ' + err.message + ' — fetching data directly');
    fetchSdkData();
  }
}

function applyTheme() {
  if (!hostContext?.styles?.variables) return;
  const root = document.documentElement;
  for (const [key, value] of Object.entries(hostContext.styles.variables)) {
    if (value) root.style.setProperty(key, value);
  }
  if (hostContext.theme) {
    root.style.colorScheme = hostContext.theme;
  }
}

// === Data fetching via tools/call ===
async function fetchSdkData() {
  setStatus('Fetching SDK data...');
  const btn = document.getElementById('btnRefresh');
  btn.disabled = true;
  btn.innerHTML = '<span class="refresh-spin">&#x21bb;</span> Refreshing';
  try {
    // Call the DotnetSdk tool with Version action
    const versionResult = await sendRequest('tools/call', {
      name: 'DotnetSdk', arguments: { action: 'Version' }
    });
    // Call ListSdks
    const sdksResult = await sendRequest('tools/call', {
      name: 'DotnetSdk', arguments: { action: 'ListSdks' }
    });
    // Call ListRuntimes
    const runtimesResult = await sendRequest('tools/call', {
      name: 'DotnetSdk', arguments: { action: 'ListRuntimes' }
    });
    renderSdks(sdksResult, versionResult);
    renderRuntimes(runtimesResult);
    setStatus('Data loaded');
  } catch (err) {
    setStatus('Error: ' + err.message);
    document.getElementById('sdks').innerHTML = '<div class="error">Failed to load: ' + escapeHtml(err.message) + '</div>';
  } finally {
    btn.disabled = false;
    btn.innerHTML = '&#x21bb; Refresh';
  }
}

function refreshData() { fetchSdkData(); }

// === Render from tool-result notification ===
function renderFromToolResult(params) {
  const structured = params?.structuredContent;
  if (!structured) return;

  // DotnetSdk Version/ListSdks/ListRuntimes structured content
  if (structured.sdks) renderSdksFromStructured(structured);
  if (structured.runtimes) renderRuntimesFromStructured(structured);
  if (structured.version) {
    setStatus('SDK version: ' + structured.version);
  }
}

// === Render helpers ===
function renderSdks(result, versionResult) {
  const text = extractText(result);
  const versionText = extractText(versionResult)?.trim();
  if (!text) {
    document.getElementById('sdks').innerHTML = '<div class="error">No SDK data</div>';
    return;
  }
  const rows = text.split('\n').filter(l => l.trim()).map(line => {
    const m = line.match(/^(.+?)\s+\[(.+)\]$/);
    if (!m) return null;
    return { version: m[1].trim(), path: m[2].trim() };
  }).filter(Boolean);

  renderSdkTable(rows, versionText);
}

function renderSdksFromStructured(data) {
  if (!data.sdks) return;
  renderSdkTable(data.sdks, data.latestSdk || data.version);
}

function renderSdkTable(sdks, currentVersion) {
  if (!sdks.length) {
    document.getElementById('sdks').innerHTML = '<div class="error">No SDKs found</div>';
    return;
  }
  const reversed = [...sdks].reverse(); // latest first
  let html = '<table><thead><tr><th>Version</th><th>Status</th><th>Path</th></tr></thead><tbody>';
  for (const sdk of reversed) {
    const v = sdk.version || sdk.Version;
    const p = sdk.path || sdk.Path || '';
    const isPreview = v.includes('-');
    const isCurrent = currentVersion && v === currentVersion;
    let badge = '';
    if (isCurrent) badge = '<span class="badge badge-current">current</span>';
    else if (isPreview) badge = '<span class="badge badge-preview">preview</span>';
    else badge = '<span class="badge badge-lts">stable</span>';
    html += '<tr><td class="version-cell">' + escapeHtml(v) + '</td><td>' + badge + '</td><td>' + escapeHtml(p) + '</td></tr>';
  }
  html += '</tbody></table>';
  document.getElementById('sdks').innerHTML = html;
}

function renderRuntimes(result) {
  const text = extractText(result);
  if (!text) {
    document.getElementById('runtimes').innerHTML = '<div class="error">No runtime data</div>';
    return;
  }
  const rows = text.split('\n').filter(l => l.trim()).map(line => {
    const m = line.match(/^(.+?)\s+(\S+)\s+\[(.+)\]$/);
    if (!m) return null;
    return { name: m[1].trim(), version: m[2].trim(), path: m[3].trim() };
  }).filter(Boolean);

  renderRuntimeTable(rows);
}

function renderRuntimesFromStructured(data) {
  if (!data.runtimes) return;
  renderRuntimeTable(data.runtimes);
}

function renderRuntimeTable(runtimes) {
  if (!runtimes.length) {
    document.getElementById('runtimes').innerHTML = '<div class="error">No runtimes found</div>';
    return;
  }
  const reversed = [...runtimes].reverse();
  let html = '<table><thead><tr><th>Runtime</th><th>Version</th><th>Path</th></tr></thead><tbody>';
  for (const rt of reversed) {
    const n = rt.name || rt.Name;
    const v = rt.version || rt.Version;
    const p = rt.path || rt.Path || '';
    html += '<tr><td>' + escapeHtml(n) + '</td><td class="version-cell">' + escapeHtml(v) + '</td><td>' + escapeHtml(p) + '</td></tr>';
  }
  html += '</tbody></table>';
  document.getElementById('runtimes').innerHTML = html;
}

// === Chat integration ===
async function sendToChat() {
  const sdkTable = document.getElementById('sdks').innerText;
  const runtimeTable = document.getElementById('runtimes').innerText;
  try {
    await sendRequest('ui/message', {
      role: 'user',
      content: { type: 'text', text: 'Here is my .NET SDK environment:\n\nSDKs:\n' + sdkTable + '\n\nRuntimes:\n' + runtimeTable }
    });
    setStatus('Sent to chat');
  } catch (err) {
    setStatus('Could not send: ' + err.message);
  }
}

// === Utilities ===
function extractText(result) {
  if (!result) return '';
  if (result.content) {
    for (const c of result.content) {
      if (c.type === 'text' && c.text) return c.text;
    }
  }
  if (typeof result === 'string') return result;
  return '';
}

function escapeHtml(str) {
  const d = document.createElement('div');
  d.textContent = str || '';
  return d.innerHTML;
}

function setStatus(msg) {
  document.getElementById('status').textContent = msg;
}

// Auto-resize support
const ro = new ResizeObserver(() => {
  sendNotification('ui/notifications/size-changed', {
    width: document.documentElement.scrollWidth,
    height: document.documentElement.scrollHeight
  });
});
ro.observe(document.documentElement);

// Start
init();
</script>
</body>
</html>
""";
}
