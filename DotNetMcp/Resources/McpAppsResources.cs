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

    [McpServerResource(
        UriTemplate = "ui://dotnet-mcp/server-metrics",
        Name = "server_metrics_ui",
        MimeType = "text/html;profile=mcp-app")]
    [McpMeta("ui", JsonValue = """{"prefersBorder": true}""")]
    public static ResourceContents GetServerMetricsUI() => new TextResourceContents
    {
        Uri = "ui://dotnet-mcp/server-metrics",
        MimeType = "text/html;profile=mcp-app",
        Text = ServerMetricsDashboardHtml,
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
  else if (structured.sdks) {
    // ListSdks was called but runtimes weren't included — show a helpful message
    document.getElementById('runtimes').innerHTML = '<div class="loading" style="font-size:12px;">Runtime data not available from this action. Click Refresh to load.</div>';
  }
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

    /// <summary>
    /// Interactive HTML dashboard for server metrics monitoring.
    /// Renders tool usage charts, performance gauges, cache metrics,
    /// error rates, and token savings with auto-refresh capability.
    /// Uses host-aware theming via CSS variables from SEP-1865.
    /// </summary>
    private const string ServerMetricsDashboardHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Server Metrics Dashboard</title>
<style>
  :root {
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
    --color-background-error: light-dark(#fef2f2, #7f1d1d);
    --color-text-error: light-dark(#dc2626, #f87171);
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
  .summary-row {
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
    margin-bottom: 12px;
  }
  .summary-card {
    flex: 1;
    min-width: 120px;
    padding: 10px 14px;
    border-radius: var(--border-radius-md);
    border: 1px solid var(--color-border-primary);
    background: var(--color-background-secondary);
  }
  .summary-card .label {
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-text-secondary);
    margin-bottom: 2px;
  }
  .summary-card .value {
    font-size: 22px;
    font-weight: 700;
    font-family: var(--font-mono);
  }
  .summary-card .sub {
    font-size: 11px;
    color: var(--color-text-secondary);
  }
  .card-success .value { color: var(--color-text-success); }
  .card-error .value { color: var(--color-text-error); }
  .card-info .value { color: var(--color-text-info); }
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
  th.num { text-align: right; }
  td {
    padding: 6px 10px;
    border-bottom: 1px solid var(--color-border-primary);
  }
  td.num {
    text-align: right;
    font-family: var(--font-mono);
  }
  .bar-cell { width: 30%; }
  .bar-bg {
    background: var(--color-background-secondary);
    border-radius: 3px;
    height: 14px;
    overflow: hidden;
  }
  .bar-fill {
    height: 100%;
    border-radius: 3px;
    background: var(--color-text-info);
    transition: width 0.3s ease;
  }
  .gauge-row {
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
  }
  .gauge {
    flex: 1;
    min-width: 110px;
    padding: 10px;
    border-radius: var(--border-radius-md);
    border: 1px solid var(--color-border-primary);
    background: var(--color-background-secondary);
    text-align: center;
  }
  .gauge .gauge-label {
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-text-secondary);
    margin-bottom: 4px;
  }
  .gauge .gauge-value {
    font-size: 20px;
    font-weight: 700;
    font-family: var(--font-mono);
  }
  .gauge .gauge-bar {
    margin-top: 6px;
    height: 6px;
    background: var(--color-border-primary);
    border-radius: 3px;
    overflow: hidden;
  }
  .gauge .gauge-bar-fill {
    height: 100%;
    border-radius: 3px;
    transition: width 0.3s ease;
  }
  .fill-success { background: var(--color-text-success); }
  .fill-error { background: var(--color-text-error); }
  .fill-info { background: var(--color-text-info); }
  .loading, .error-msg {
    text-align: center;
    padding: 24px;
    color: var(--color-text-secondary);
  }
  .error-msg { color: var(--color-text-warning); }
  .action-bar {
    display: flex;
    gap: 8px;
    margin-bottom: 12px;
    align-items: center;
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
  .auto-label {
    font-size: 11px;
    color: var(--color-text-secondary);
    margin-left: auto;
  }
  .refresh-spin { animation: spin 1s linear infinite; display: inline-block; }
  @keyframes spin { to { transform: rotate(360deg); } }
  .token-savings {
    padding: 10px 14px;
    border-radius: var(--border-radius-md);
    border: 1px solid var(--color-border-primary);
    background: var(--color-background-secondary);
  }
  .savings-header {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    margin-bottom: 6px;
  }
  .savings-pct {
    font-size: 28px;
    font-weight: 700;
    font-family: var(--font-mono);
    color: var(--color-text-success);
  }
  .savings-detail {
    font-size: 12px;
    color: var(--color-text-secondary);
  }
</style>
</head>
<body>
  <div class="action-bar">
    <button id="btnRefresh" onclick="refreshAll()">&#x21bb; Refresh</button>
    <button onclick="resetMetrics()">&#x1f5d1; Reset</button>
    <button onclick="sendToChat()">&#x1f4cb; Send to Chat</button>
    <label class="auto-label"><input type="checkbox" id="chkAuto" onchange="toggleAuto()" /> Auto-refresh (15s)</label>
  </div>

  <div class="section">
    <h2>&#x1f4ca; Overview</h2>
    <div id="summary" class="summary-row">
      <div class="loading">Loading metrics&hellip;</div>
    </div>
  </div>

  <div class="section">
    <h2>&#x1f527; Tool Usage</h2>
    <div id="toolUsage"><div class="loading">Loading&hellip;</div></div>
  </div>

  <div class="section">
    <h2>&#x26a1; Performance &amp; Cache</h2>
    <div id="gauges" class="gauge-row"></div>
  </div>

  <div class="section" id="tokenSection" style="display:none;">
    <h2>&#x1f4b0; Token Savings</h2>
    <div id="tokenSavings"></div>
  </div>

  <div id="status" style="font-size:11px;color:var(--color-text-secondary);margin-top:8px;"></div>

<script>
let nextId = 1;
const pending = {};
let autoTimer = null;
let lastMetrics = null;
let lastCache = null;
let lastTokens = null;
let refreshInFlight = false;

function sendRequest(method, params) {
  const id = nextId++;
  window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
  return new Promise((resolve, reject) => {
    pending[id] = { resolve, reject };
    setTimeout(() => {
      if (pending[id]) { delete pending[id]; reject(new Error('Timeout: ' + method)); }
    }, 30000);
  });
}

function sendNotification(method, params) {
  window.parent.postMessage({ jsonrpc: '2.0', method, params }, '*');
}

let hostContext = null;
window.addEventListener('message', (e) => {
  const msg = e.data;
  if (!msg || !msg.jsonrpc) return;
  if (msg.id && pending[msg.id]) {
    const { resolve, reject } = pending[msg.id];
    delete pending[msg.id];
    if (msg.error) reject(new Error(msg.error.message || 'Unknown error'));
    else resolve(msg.result);
    return;
  }
  if (msg.method === 'ui/notifications/host-context-changed') {
    hostContext = Object.assign(hostContext || {}, msg.params || {});
    applyTheme();
  }
});

async function init() {
  try {
    const result = await sendRequest('ui/initialize', {
      protocolVersion: '2026-01-26',
      clientInfo: { name: 'dotnet-mcp-server-metrics', version: '1.0.0' },
      appCapabilities: { availableDisplayModes: ['inline'] }
    });
    hostContext = result?.hostContext;
    applyTheme();
    sendNotification('ui/notifications/initialized', {});
    setStatus('Connected to host');
  } catch (err) {
    setStatus('Init: ' + err.message);
  }
  refreshAll();
}

function applyTheme() {
  if (!hostContext?.styles?.variables) return;
  const root = document.documentElement;
  for (const [key, value] of Object.entries(hostContext.styles.variables)) {
    if (value) root.style.setProperty(key, value);
  }
  if (hostContext.theme) root.style.colorScheme = hostContext.theme;
}

async function refreshAll() {
  if (refreshInFlight) return;
  refreshInFlight = true;
  const btn = document.getElementById('btnRefresh');
  btn.disabled = true;
  btn.innerHTML = '<span class="refresh-spin">&#x21bb;</span> Refreshing';
  setStatus('Fetching metrics...');
  try {
    const [metricsResult, cacheResult, tokenResult] = await Promise.allSettled([
      sendRequest('tools/call', { name: 'DotnetServerMetrics', arguments: { action: 'Get' } }),
      sendRequest('tools/call', { name: 'DotnetSdk', arguments: { action: 'CacheMetrics' } }),
      sendRequest('tools/call', { name: 'DotnetServerMetrics', arguments: { action: 'TokenSavingsGet' } })
    ]);
    if (metricsResult.status === 'fulfilled') {
      lastMetrics = parseResult(metricsResult.value);
      renderSummary(lastMetrics);
      renderToolTable(lastMetrics);
    }
    if (cacheResult.status === 'fulfilled') {
      lastCache = parseResult(cacheResult.value);
    }
    renderGauges(lastMetrics, lastCache);
    if (tokenResult.status === 'fulfilled') {
      lastTokens = parseResult(tokenResult.value);
      renderTokenSavings(lastTokens);
    }
    setStatus('Updated ' + new Date().toLocaleTimeString());
  } catch (err) {
    setStatus('Error: ' + err.message);
  } finally {
    btn.disabled = false;
    btn.innerHTML = '&#x21bb; Refresh';
    refreshInFlight = false;
  }
}

async function resetMetrics() {
  try {
    await sendRequest('tools/call', { name: 'DotnetServerMetrics', arguments: { action: 'Reset' } });
    setStatus('Metrics reset');
    refreshAll();
  } catch (err) {
    setStatus('Reset failed: ' + err.message);
  }
}

function toggleAuto() {
  if (document.getElementById('chkAuto').checked) {
    autoTimer = setInterval(refreshAll, 15000);
  } else {
    clearInterval(autoTimer);
    autoTimer = null;
  }
}

function parseResult(result) {
  if (!result) return null;
  if (result.structuredContent) return result.structuredContent;
  if (result.content) {
    for (const c of result.content) {
      if (c.type === 'text' && c.text) {
        try { return JSON.parse(c.text); } catch { return null; }
      }
    }
  }
  return null;
}

function renderSummary(m) {
  if (!m) return;
  const total = m.totalInvocations || 0;
  const ok = m.totalSuccesses || 0;
  const fail = m.totalFailures || 0;
  const rate = total > 0 ? ((ok / total) * 100).toFixed(1) : '—';
  const tools = m.toolMetrics ? Object.keys(m.toolMetrics).length : 0;
  document.getElementById('summary').innerHTML =
    card('Total Calls', total, '', 'card-info') +
    card('Success', ok, rate + '% rate', 'card-success') +
    card('Failures', fail, '', fail > 0 ? 'card-error' : '') +
    card('Tools Used', tools, '', '');
}

function card(label, value, sub, cls) {
  return '<div class="summary-card ' + cls + '">' +
    '<div class="label">' + esc(label) + '</div>' +
    '<div class="value">' + esc(String(value)) + '</div>' +
    (sub ? '<div class="sub">' + esc(sub) + '</div>' : '') +
    '</div>';
}

function renderToolTable(m) {
  if (!m || !m.toolMetrics) {
    document.getElementById('toolUsage').innerHTML = '<div class="loading">No tool data</div>';
    return;
  }
  const entries = Object.entries(m.toolMetrics).sort((a, b) => b[1].invocationCount - a[1].invocationCount);
  if (!entries.length) {
    document.getElementById('toolUsage').innerHTML = '<div class="loading">No invocations yet</div>';
    return;
  }
  const maxCount = entries[0][1].invocationCount || 1;
  let html = '<table><thead><tr><th>Tool</th><th class="num">Calls</th><th class="bar-cell">Distribution</th><th class="num">Avg (ms)</th><th class="num">OK</th><th class="num">Fail</th></tr></thead><tbody>';
  for (const [name, s] of entries) {
    const pct = ((s.invocationCount / maxCount) * 100).toFixed(0);
    html += '<tr><td>' + esc(name) + '</td>' +
      '<td class="num">' + s.invocationCount + '</td>' +
      '<td class="bar-cell"><div class="bar-bg"><div class="bar-fill" style="width:' + pct + '%"></div></div></td>' +
      '<td class="num">' + (s.avgDurationMs != null ? s.avgDurationMs.toFixed(1) : '—') + '</td>' +
      '<td class="num">' + (s.successCount || 0) + '</td>' +
      '<td class="num">' + (s.failureCount || 0) + '</td></tr>';
  }
  html += '</tbody></table>';
  document.getElementById('toolUsage').innerHTML = html;
}

function renderGauges(m, cache) {
  const el = document.getElementById('gauges');
  let html = '';
  if (m && m.toolMetrics) {
    const entries = Object.values(m.toolMetrics);
    const totalCalls = entries.reduce((s, e) => s + e.invocationCount, 0);
    const totalOk = entries.reduce((s, e) => s + (e.successCount || 0), 0);
    const successRate = totalCalls > 0 ? ((totalOk / totalCalls) * 100) : 0;
    const avgDur = totalCalls > 0 ? entries.reduce((s, e) => s + (e.avgDurationMs || 0) * e.invocationCount, 0) / totalCalls : 0;
    html += gauge('Success Rate', successRate.toFixed(1) + '%', successRate, successRate > 90 ? 'fill-success' : successRate > 70 ? 'fill-info' : 'fill-error');
    html += gauge('Avg Latency', avgDur.toFixed(0) + 'ms', Math.min(avgDur / 50, 100), 'fill-info');
  }
  if (cache) {
    const hits = cache.hits || 0;
    const misses = cache.misses || 0;
    const total = hits + misses;
    const hitRate = total > 0 ? ((hits / total) * 100) : 0;
    html += gauge('Cache Hit Rate', hitRate.toFixed(1) + '%', hitRate, hitRate > 80 ? 'fill-success' : hitRate > 50 ? 'fill-info' : 'fill-error');
    html += gauge('Cache Hits / Misses', hits + ' / ' + misses, hitRate, 'fill-info');
  }
  if (!html) html = '<div class="loading">No performance data yet</div>';
  el.innerHTML = html;
}

function gauge(label, value, pct, cls) {
  return '<div class="gauge">' +
    '<div class="gauge-label">' + esc(label) + '</div>' +
    '<div class="gauge-value">' + esc(value) + '</div>' +
    '<div class="gauge-bar"><div class="gauge-bar-fill ' + cls + '" style="width:' + Math.min(pct, 100).toFixed(0) + '%"></div></div>' +
    '</div>';
}

function renderTokenSavings(t) {
  const sec = document.getElementById('tokenSection');
  const el = document.getElementById('tokenSavings');
  if (!t || !t.success || !t.totalWorkflowBaselineTokens) {
    sec.style.display = 'none';
    return;
  }
  sec.style.display = '';
  const baseline = t.totalWorkflowBaselineTokens;
  const mcp = t.totalWorkflowMcpTokens;
  const saved = t.totalWorkflowSavingsTokens;
  const pct = baseline > 0 ? ((saved / baseline) * 100).toFixed(1) : '0';
  const workflows = (t.workflowTokenSavings || []).length;
  let html = '<div class="token-savings">' +
    '<div class="savings-header"><span class="savings-pct">' + esc(pct) + '% saved</span>' +
    '<span class="savings-detail">' + esc(String(workflows)) + ' workflow(s) &middot; v' + esc(t.assumptionsVersion || '1') + '</span></div>' +
    '<div class="savings-detail">Baseline: ' + fmt(baseline) + ' tokens &rarr; MCP: ' + fmt(mcp) + ' tokens &rarr; Saved: ' + fmt(saved) + ' tokens</div>';
  if (t.workflowTokenSavings && t.workflowTokenSavings.length > 0) {
    html += '<table style="margin-top:8px"><thead><tr><th>Workflow</th><th class="num">Baseline</th><th class="num">MCP</th><th class="num">Saved</th><th class="num">%</th></tr></thead><tbody>';
    for (const w of t.workflowTokenSavings) {
      const wPct = w.baselineEstimatedTokens > 0 ? ((w.estimatedSavingsTokens / w.baselineEstimatedTokens) * 100).toFixed(1) : '—';
      html += '<tr><td>' + esc(w.workflowId || '—') + '</td>' +
        '<td class="num">' + fmt(w.baselineEstimatedTokens) + '</td>' +
        '<td class="num">' + fmt(w.mcpEstimatedTokens) + '</td>' +
        '<td class="num">' + fmt(w.estimatedSavingsTokens) + '</td>' +
        '<td class="num">' + wPct + '%</td></tr>';
    }
    html += '</tbody></table>';
  }
  html += '</div>';
  el.innerHTML = html;
}

function fmt(n) { return (n || 0).toLocaleString(); }

async function sendToChat() {
  const lines = [];
  if (lastMetrics) {
    lines.push('## Server Metrics');
    lines.push('- Total invocations: ' + (lastMetrics.totalInvocations || 0));
    lines.push('- Successes: ' + (lastMetrics.totalSuccesses || 0));
    lines.push('- Failures: ' + (lastMetrics.totalFailures || 0));
    if (lastMetrics.toolMetrics) {
      lines.push('');
      lines.push('| Tool | Calls | Avg ms | OK | Fail |');
      lines.push('|------|------:|-------:|---:|-----:|');
      for (const [name, s] of Object.entries(lastMetrics.toolMetrics).sort((a, b) => b[1].invocationCount - a[1].invocationCount)) {
        lines.push('| ' + name + ' | ' + s.invocationCount + ' | ' + (s.avgDurationMs || 0).toFixed(1) + ' | ' + (s.successCount || 0) + ' | ' + (s.failureCount || 0) + ' |');
      }
    }
  }
  if (lastTokens && lastTokens.totalWorkflowBaselineTokens) {
    lines.push('');
    lines.push('## Token Savings');
    lines.push('- Baseline: ' + (lastTokens.totalWorkflowBaselineTokens || 0).toLocaleString() + ' tokens');
    lines.push('- MCP: ' + (lastTokens.totalWorkflowMcpTokens || 0).toLocaleString() + ' tokens');
    lines.push('- Saved: ' + (lastTokens.totalWorkflowSavingsTokens || 0).toLocaleString() + ' tokens');
  }
  try {
    await sendRequest('ui/message', {
      role: 'user',
      content: { type: 'text', text: lines.join('\n') || 'No metrics data available.' }
    });
    setStatus('Sent to chat');
  } catch (err) {
    setStatus('Could not send: ' + err.message);
  }
}

function esc(str) {
  const d = document.createElement('div');
  d.textContent = str || '';
  return d.innerHTML;
}
function setStatus(msg) { document.getElementById('status').textContent = msg; }

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
""";
}
