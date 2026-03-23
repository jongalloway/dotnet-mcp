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
  .interactions {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 4px;
  }
  .interaction-btn {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    border: 1px solid var(--color-border-primary);
    border-radius: var(--border-radius-sm);
    background: var(--color-background-secondary);
    color: var(--color-text-primary);
    padding: 6px 12px;
    font-size: var(--font-text-sm-size);
    cursor: pointer;
    font-family: var(--font-sans);
    transition: background 0.15s, border-color 0.15s;
  }
  .interaction-btn:hover {
    background: var(--color-background-info);
    border-color: var(--color-text-info);
    color: var(--color-text-info);
  }
  .interaction-btn .icon { font-size: 14px; }
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

  <div class="section">
    <h2>&#x1f680; Ask Copilot</h2>
    <div class="interactions">
      <button class="interaction-btn" onclick="askChat('What .NET templates are available? Help me choose the right one for my project.')">
        <span class="icon">&#x1f4cb;</span> Choose a Template
      </button>
      <button class="interaction-btn" onclick="askChat('Create a new .NET project. What template should I use?')">
        <span class="icon">&#x2795;</span> New Project
      </button>
      <button class="interaction-btn" onclick="askChat('Can I upgrade my project to a newer .NET version? What are the breaking changes?')">
        <span class="icon">&#x1f504;</span> Upgrade Guidance
      </button>
      <button class="interaction-btn" onclick="askChat('Set up a global.json to pin the SDK version for this repository')">
        <span class="icon">&#x1f4cc;</span> Pin SDK Version
      </button>
      <button class="interaction-btn" onclick="askChat('Compare the LTS and current .NET versions. Which should I target?')">
        <span class="icon">&#x2696;</span> LTS vs Current
      </button>
      <button class="interaction-btn" onclick="askChat('What workloads are available for mobile, WASM, or desktop development?')">
        <span class="icon">&#x1f4f1;</span> Explore Workloads
      </button>
    </div>
  </div>

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
      name: 'dotnet_sdk', arguments: { action: 'Version' }
    });
    // Call ListSdks
    const sdksResult = await sendRequest('tools/call', {
      name: 'dotnet_sdk', arguments: { action: 'ListSdks' }
    });
    // Call ListRuntimes
    const runtimesResult = await sendRequest('tools/call', {
      name: 'dotnet_sdk', arguments: { action: 'ListRuntimes' }
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
async function askChat(prompt) {
  try {
    await sendRequest('ui/message', {
      role: 'user',
      content: { type: 'text', text: prompt }
    });
    setStatus('Sent to chat: ' + prompt.substring(0, 40) + '...');
  } catch (err) {
    setStatus('Could not send: ' + err.message);
  }
}

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

    // ─────────────────────────────────────────────────────────────
    // Project Health Dashboard
    // ─────────────────────────────────────────────────────────────

    [McpServerResource(
        UriTemplate = "ui://dotnet-mcp/project-dashboard",
        Name = "project_dashboard_ui",
        MimeType = "text/html;profile=mcp-app")]
    [McpMeta("ui", JsonValue = """{"prefersBorder": true}""")]
    public static ResourceContents GetProjectDashboardUI() => new TextResourceContents
    {
        Uri = "ui://dotnet-mcp/project-dashboard",
        MimeType = "text/html;profile=mcp-app",
        Text = ProjectDashboardHtml,
        Meta = new JsonObject
        {
            ["ui"] = new JsonObject { ["prefersBorder"] = true }
        }
    };

    private const string ProjectDashboardHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>.NET Project Dashboard</title>
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
    --color-background-error: light-dark(#fef2f2, #450a0a);
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
  h2 { font-size: 16px; font-weight: 600; margin-bottom: 8px; display: flex; align-items: center; gap: 6px; }
  .section { margin-bottom: 16px; }
  .badge { display: inline-block; font-size: 11px; font-weight: 500; padding: 2px 8px; border-radius: 9999px; }
  .badge-success { background: var(--color-background-success); color: var(--color-text-success); }
  .badge-warning { background: var(--color-background-warning); color: var(--color-text-warning); }
  .badge-error { background: var(--color-background-error); color: var(--color-text-error); }
  .badge-info { background: var(--color-background-info); color: var(--color-text-info); }
  table { width: 100%; border-collapse: collapse; font-size: var(--font-text-sm-size); }
  th { text-align: left; padding: 6px 10px; background: var(--color-background-secondary); color: var(--color-text-secondary); font-weight: 500; font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid var(--color-border-primary); }
  td { padding: 6px 10px; border-bottom: 1px solid var(--color-border-primary); }
  .mono { font-family: var(--font-mono); font-weight: 500; }
  .loading, .error { text-align: center; padding: 24px; color: var(--color-text-secondary); }
  .error { color: var(--color-text-warning); }
  .action-bar { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; }
  button { border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-sm); background: var(--color-background-secondary); color: var(--color-text-primary); padding: 4px 12px; font-size: var(--font-text-sm-size); cursor: pointer; font-family: var(--font-sans); }
  button:hover { opacity: 0.8; }
  button:disabled { opacity: 0.5; cursor: default; }
  .interactions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 4px; }
  .interaction-btn { display: inline-flex; align-items: center; gap: 4px; border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-sm); background: var(--color-background-secondary); color: var(--color-text-primary); padding: 6px 12px; font-size: var(--font-text-sm-size); cursor: pointer; font-family: var(--font-sans); transition: background 0.15s, border-color 0.15s; }
  .interaction-btn:hover { background: var(--color-background-info); border-color: var(--color-text-info); color: var(--color-text-info); }
  .interaction-btn .icon { font-size: 14px; }
  .summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 8px; margin-top: 4px; }
  .summary-card { background: var(--color-background-secondary); border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-md); padding: 10px 12px; }
  .summary-card .label { font-size: 11px; color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.05em; }
  .summary-card .value { font-size: 18px; font-weight: 600; margin-top: 2px; font-family: var(--font-mono); }
  .refresh-spin { animation: spin 1s linear infinite; display: inline-block; }
  @keyframes spin { to { transform: rotate(360deg); } }
</style>
</head>
<body>
  <div class="action-bar">
    <button onclick="sendToChat()">&#x1f4cb; Send to Chat</button>
  </div>

  <div class="section">
    <h2>&#x1f4ca; Project Summary</h2>
    <div id="summary"><div class="loading">Waiting for project data&hellip;</div></div>
  </div>

  <div class="section">
    <h2>&#x1f4e6; Package References</h2>
    <div id="packages"><div class="loading">Waiting for package data&hellip;</div></div>
  </div>

  <div class="section">
    <h2>&#x1f680; Ask Copilot</h2>
    <div class="interactions">
      <button class="interaction-btn" onclick="askChat('Audit my project dependencies for security vulnerabilities and deprecated packages')">
        <span class="icon">&#x1f6e1;</span> Security Audit
      </button>
      <button class="interaction-btn" onclick="askChat('Check for outdated NuGet packages and recommend which ones to update')">
        <span class="icon">&#x1f504;</span> Check Updates
      </button>
      <button class="interaction-btn" onclick="askChat('Can I upgrade this project to the latest .NET version? Walk me through it.')">
        <span class="icon">&#x2b06;</span> Upgrade .NET
      </button>
      <button class="interaction-btn" onclick="askChat('Analyze my project structure and suggest improvements or best practices')">
        <span class="icon">&#x1f50d;</span> Architecture Review
      </button>
      <button class="interaction-btn" onclick="askChat('Help me set up CI/CD for this project with GitHub Actions')">
        <span class="icon">&#x2699;</span> Setup CI/CD
      </button>
      <button class="interaction-btn" onclick="askChat('Help me publish this project. What are my deployment options?')">
        <span class="icon">&#x1f4e4;</span> Deployment Guide
      </button>
    </div>
  </div>

  <div id="status" style="font-size:11px;color:var(--color-text-secondary);margin-top:8px;"></div>

<script>
let nextId = 1;
const pending = {};

function sendRequest(method, params) {
  const id = nextId++;
  window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
  return new Promise((resolve, reject) => {
    pending[id] = { resolve, reject };
    setTimeout(() => { if (pending[id]) { delete pending[id]; reject(new Error('Timeout: ' + method)); } }, 30000);
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
  if (msg.method === 'ui/notifications/tool-result') renderFromToolResult(msg.params);
  if (msg.method === 'ui/notifications/host-context-changed') { Object.assign(hostContext || {}, msg.params); applyTheme(); }
});

async function init() {
  try {
    const result = await sendRequest('ui/initialize', {
      protocolVersion: '2026-01-26',
      clientInfo: { name: 'dotnet-mcp-project-dashboard', version: '1.0.0' },
      appCapabilities: { availableDisplayModes: ['inline'] }
    });
    hostContext = result?.hostContext;
    applyTheme();
    sendNotification('ui/notifications/initialized', {});
    setStatus('Connected');
  } catch (err) {
    setStatus('Init: ' + err.message);
  }
}

function applyTheme() {
  if (!hostContext?.styles?.variables) return;
  const root = document.documentElement;
  for (const [k, v] of Object.entries(hostContext.styles.variables)) { if (v) root.style.setProperty(k, v); }
  if (hostContext.theme) root.style.colorScheme = hostContext.theme;
}

function renderFromToolResult(params) {
  const structured = params?.structuredContent;
  const text = extractText(params);
  if (structured?.packages) renderPackages(structured.packages);
  if (structured?.action === 'new') renderNewProject(structured);
  else if (structured?.action === 'build') renderBuild(structured.buildResult);
  else if (structured?.action === 'analyze') renderAnalysis(structured);
  else if (text && !structured) {
    const summaryEl = document.getElementById('summary');
    if (summaryEl && text) summaryEl.innerHTML = '<pre style="font-size:12px;white-space:pre-wrap;font-family:var(--font-mono);color:var(--color-text-secondary);">' + escapeHtml(text) + '</pre>';
  }
}

function renderNewProject(data) {
  const summaryEl = document.getElementById('summary');
  const icon = data.success ? '&#x2705;' : '&#x274c;';
  const statusClass = data.success ? 'badge-success' : 'badge-error';
  let html = '<div class="summary-grid">';
  html += '<div class="summary-card"><div class="label">Status</div><div class="value">' + icon + ' <span class="badge ' + statusClass + '">' + (data.success ? 'Created' : 'Failed') + '</span></div></div>';
  html += '<div class="summary-card"><div class="label">Template</div><div class="value">' + escapeHtml(data.template || '') + '</div></div>';
  if (data.projectName) html += '<div class="summary-card"><div class="label">Name</div><div class="value">' + escapeHtml(data.projectName) + '</div></div>';
  if (data.framework) html += '<div class="summary-card"><div class="label">Framework</div><div class="value">' + escapeHtml(data.framework) + '</div></div>';
  html += '</div>';
  if (data.outputDirectory) html += '<div style="margin-top:8px;font-size:12px;color:var(--color-text-secondary);font-family:var(--font-mono);">' + escapeHtml(data.outputDirectory) + '</div>';
  summaryEl.innerHTML = html;
  // New project has no packages yet
  document.getElementById('packages').innerHTML = '<div class="loading" style="font-size:12px;">New project — no packages added yet</div>';
}

function renderBuild(result) {
  if (!result) return;
  const summaryEl = document.getElementById('summary');
  const icon = result.success ? '&#x2705;' : '&#x274c;';
  const statusClass = result.success ? 'badge-success' : 'badge-error';
  let html = '<div class="summary-grid">';
  html += '<div class="summary-card"><div class="label">Build</div><div class="value">' + icon + ' <span class="badge ' + statusClass + '">' + (result.success ? 'Succeeded' : 'Failed') + '</span></div></div>';
  html += '<div class="summary-card"><div class="label">Configuration</div><div class="value">' + escapeHtml(result.configuration || 'Debug') + '</div></div>';
  html += '<div class="summary-card"><div class="label">Warnings</div><div class="value">' + (result.warningCount || 0) + '</div></div>';
  html += '<div class="summary-card"><div class="label">Errors</div><div class="value">' + (result.errorCount || 0) + '</div></div>';
  html += '</div>';
  summaryEl.innerHTML = html;
}

function renderAnalysis(data) {
  const summaryEl = document.getElementById('summary');
  if (data.analysisText) {
    summaryEl.innerHTML = '<pre style="font-size:12px;white-space:pre-wrap;font-family:var(--font-mono);color:var(--color-text-secondary);">' + escapeHtml(data.analysisText) + '</pre>';
  }
}

function renderPackages(pkgs) {
  if (!pkgs.length) { document.getElementById('packages').innerHTML = '<div class="loading">No packages</div>'; return; }
  let html = '<table><thead><tr><th>Package</th><th>Version</th><th>Status</th></tr></thead><tbody>';
  for (const p of pkgs) {
    const badge = p.deprecated ? '<span class="badge badge-error">deprecated</span>'
      : p.outdated ? '<span class="badge badge-warning">update available</span>'
      : '<span class="badge badge-success">current</span>';
    html += '<tr><td class="mono">' + escapeHtml(p.id || p.name) + '</td><td class="mono">' + escapeHtml(p.version || p.resolved || '') + '</td><td>' + badge + '</td></tr>';
  }
  html += '</tbody></table>';
  document.getElementById('packages').innerHTML = html;
}



async function askChat(prompt) {
  try {
    await sendRequest('ui/message', { role: 'user', content: { type: 'text', text: prompt } });
    setStatus('Sent: ' + prompt.substring(0, 50) + '...');
  } catch (err) { setStatus('Could not send: ' + err.message); }
}

async function sendToChat() {
  const summaryText = document.getElementById('summary').innerText;
  const packageText = document.getElementById('packages').innerText;
  try {
    await sendRequest('ui/message', { role: 'user', content: { type: 'text', text: 'Project Summary:\n' + summaryText + '\n\nPackages:\n' + packageText } });
    setStatus('Sent to chat');
  } catch (err) { setStatus('Could not send: ' + err.message); }
}

function extractText(result) {
  if (!result) return '';
  if (result.content) { for (const c of result.content) { if (c.type === 'text' && c.text) return c.text; } }
  if (typeof result === 'string') return result;
  return '';
}
function escapeHtml(str) { const d = document.createElement('div'); d.textContent = str || ''; return d.innerHTML; }
function setStatus(msg) { document.getElementById('status').textContent = msg; }

const ro = new ResizeObserver(() => {
  sendNotification('ui/notifications/size-changed', { width: document.documentElement.scrollWidth, height: document.documentElement.scrollHeight });
});
ro.observe(document.documentElement);
init();
</script>
</body>
</html>
""";

    // ─────────────────────────────────────────────────────────────
    // Package Explorer Dashboard
    // ─────────────────────────────────────────────────────────────

    [McpServerResource(
        UriTemplate = "ui://dotnet-mcp/package-explorer",
        Name = "package_explorer_ui",
        MimeType = "text/html;profile=mcp-app")]
    [McpMeta("ui", JsonValue = """{"prefersBorder": true}""")]
    public static ResourceContents GetPackageExplorerUI() => new TextResourceContents
    {
        Uri = "ui://dotnet-mcp/package-explorer",
        MimeType = "text/html;profile=mcp-app",
        Text = PackageExplorerHtml,
        Meta = new JsonObject
        {
            ["ui"] = new JsonObject { ["prefersBorder"] = true }
        }
    };

    private const string PackageExplorerHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>NuGet Package Explorer</title>
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
    --color-background-error: light-dark(#fef2f2, #450a0a);
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
  h2 { font-size: 16px; font-weight: 600; margin-bottom: 8px; display: flex; align-items: center; gap: 6px; }
  .section { margin-bottom: 16px; }
  .badge { display: inline-block; font-size: 11px; font-weight: 500; padding: 2px 8px; border-radius: 9999px; }
  .badge-success { background: var(--color-background-success); color: var(--color-text-success); }
  .badge-warning { background: var(--color-background-warning); color: var(--color-text-warning); }
  .badge-error { background: var(--color-background-error); color: var(--color-text-error); }
  .badge-info { background: var(--color-background-info); color: var(--color-text-info); }
  table { width: 100%; border-collapse: collapse; font-size: var(--font-text-sm-size); }
  th { text-align: left; padding: 6px 10px; background: var(--color-background-secondary); color: var(--color-text-secondary); font-weight: 500; font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid var(--color-border-primary); }
  td { padding: 6px 10px; border-bottom: 1px solid var(--color-border-primary); }
  .mono { font-family: var(--font-mono); font-weight: 500; }
  .loading, .error { text-align: center; padding: 24px; color: var(--color-text-secondary); }
  .error { color: var(--color-text-warning); }
  .action-bar { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; }
  button { border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-sm); background: var(--color-background-secondary); color: var(--color-text-primary); padding: 4px 12px; font-size: var(--font-text-sm-size); cursor: pointer; font-family: var(--font-sans); }
  button:hover { opacity: 0.8; }
  button:disabled { opacity: 0.5; cursor: default; }
  .search-bar { display: flex; gap: 8px; margin-bottom: 12px; }
  .search-bar input { flex: 1; border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-sm); background: var(--color-background-primary); color: var(--color-text-primary); padding: 6px 10px; font-size: var(--font-text-sm-size); font-family: var(--font-sans); outline: none; }
  .search-bar input:focus { border-color: var(--color-text-info); }
  .interactions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 4px; }
  .interaction-btn { display: inline-flex; align-items: center; gap: 4px; border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-sm); background: var(--color-background-secondary); color: var(--color-text-primary); padding: 6px 12px; font-size: var(--font-text-sm-size); cursor: pointer; font-family: var(--font-sans); transition: background 0.15s, border-color 0.15s; }
  .interaction-btn:hover { background: var(--color-background-info); border-color: var(--color-text-info); color: var(--color-text-info); }
  .interaction-btn .icon { font-size: 14px; }
  .refresh-spin { animation: spin 1s linear infinite; display: inline-block; }
  @keyframes spin { to { transform: rotate(360deg); } }
</style>
</head>
<body>
  <div class="search-bar">
    <input id="searchInput" type="text" placeholder="Search NuGet packages..." onkeydown="if(event.key==='Enter')searchPackages()" />
    <button id="btnSearch" onclick="searchPackages()">&#x1f50d; Search</button>
  </div>

  <div class="section">
    <h2>&#x1f4e6; Installed Packages</h2>
    <div id="installed"><div class="loading">Waiting for package data&hellip;</div></div>
  </div>

  <div class="section" id="searchSection" style="display:none;">
    <h2>&#x1f50e; Search Results</h2>
    <div id="searchResults"></div>
  </div>

  <div class="section">
    <h2>&#x1f680; Ask Copilot</h2>
    <div class="interactions">
      <button class="interaction-btn" onclick="askChat('Audit my project dependencies for security vulnerabilities and suggest fixes')">
        <span class="icon">&#x1f6e1;</span> Security Audit
      </button>
      <button class="interaction-btn" onclick="askChat('Check for deprecated packages in my project and recommend modern replacements')">
        <span class="icon">&#x26a0;</span> Find Deprecated
      </button>
      <button class="interaction-btn" onclick="askChat('Analyze my NuGet package licenses for compatibility issues')">
        <span class="icon">&#x1f4dc;</span> License Check
      </button>
      <button class="interaction-btn" onclick="askChat('Compare popular packages for logging in .NET and recommend the best fit for my project')">
        <span class="icon">&#x2696;</span> Compare Packages
      </button>
      <button class="interaction-btn" onclick="askChat('Find lighter or faster alternatives to my heaviest NuGet dependencies')">
        <span class="icon">&#x1f50d;</span> Find Alternatives
      </button>
      <button class="interaction-btn" onclick="askChat('Check for outdated packages and explain what changed in the newer versions')">
        <span class="icon">&#x1f504;</span> Update Analysis
      </button>
    </div>
  </div>

  <div id="status" style="font-size:11px;color:var(--color-text-secondary);margin-top:8px;"></div>

<script>
let nextId = 1;
const pending = {};

function sendRequest(method, params) {
  const id = nextId++;
  window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
  return new Promise((resolve, reject) => {
    pending[id] = { resolve, reject };
    setTimeout(() => { if (pending[id]) { delete pending[id]; reject(new Error('Timeout: ' + method)); } }, 30000);
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
  if (msg.method === 'ui/notifications/tool-result') renderFromToolResult(msg.params);
  if (msg.method === 'ui/notifications/host-context-changed') { Object.assign(hostContext || {}, msg.params); applyTheme(); }
});

async function init() {
  try {
    const result = await sendRequest('ui/initialize', {
      protocolVersion: '2026-01-26',
      clientInfo: { name: 'dotnet-mcp-package-explorer', version: '1.0.0' },
      appCapabilities: { availableDisplayModes: ['inline'] }
    });
    hostContext = result?.hostContext;
    applyTheme();
    sendNotification('ui/notifications/initialized', {});
    setStatus('Connected');
  } catch (err) { setStatus('Init: ' + err.message); }
}

function applyTheme() {
  if (!hostContext?.styles?.variables) return;
  const root = document.documentElement;
  for (const [k, v] of Object.entries(hostContext.styles.variables)) { if (v) root.style.setProperty(k, v); }
  if (hostContext.theme) root.style.colorScheme = hostContext.theme;
}

function renderFromToolResult(params) {
  const structured = params?.structuredContent;
  const text = extractText(params);
  if (structured?.packages) renderInstalled(structured.packages);
  if (structured?.searchResults) renderSearch(structured.searchResults);
  // Parse text-based package list
  if (text && !structured) {
    if (text.includes('>') || text.includes('Package')) renderInstalledFromText(text);
  }
}

function renderInstalled(pkgs) {
  if (!pkgs.length) { document.getElementById('installed').innerHTML = '<div class="loading">No packages found</div>'; return; }
  let html = '<table><thead><tr><th>Package</th><th>Version</th><th>Status</th></tr></thead><tbody>';
  for (const p of pkgs) {
    const name = p.id || p.name || p.packageId || '';
    const ver = p.version || p.resolved || p.requested || '';
    const badge = p.deprecated ? '<span class="badge badge-error">deprecated</span>'
      : p.outdated ? '<span class="badge badge-warning">outdated</span>'
      : '<span class="badge badge-success">current</span>';
    html += '<tr><td class="mono">' + escapeHtml(name) + '</td><td class="mono">' + escapeHtml(ver) + '</td><td>' + badge + '</td></tr>';
  }
  html += '</tbody></table>';
  document.getElementById('installed').innerHTML = html;
}

function renderInstalledFromText(text) {
  const lines = text.split('\n').filter(l => l.trim() && l.includes('>'));
  if (!lines.length) return;
  let html = '<table><thead><tr><th>Package</th><th>Details</th></tr></thead><tbody>';
  for (const line of lines.slice(0, 30)) {
    const parts = line.trim().replace(/^>\s*/, '').split(/\s{2,}/);
    html += '<tr><td class="mono">' + escapeHtml(parts[0] || line) + '</td><td>' + escapeHtml(parts.slice(1).join(' ')) + '</td></tr>';
  }
  html += '</tbody></table>';
  document.getElementById('installed').innerHTML = html;
}

function renderSearch(results) {
  document.getElementById('searchSection').style.display = '';
  if (!results.length) { document.getElementById('searchResults').innerHTML = '<div class="loading">No results</div>'; return; }
  let html = '<table><thead><tr><th>Package</th><th>Version</th><th>Downloads</th></tr></thead><tbody>';
  for (const r of results.slice(0, 20)) {
    const name = r.id || r.name || '';
    const ver = r.version || r.latestVersion || '';
    const dl = r.totalDownloads ? Number(r.totalDownloads).toLocaleString() : '';
    html += '<tr><td class="mono">' + escapeHtml(name) + '</td><td class="mono">' + escapeHtml(ver) + '</td><td>' + escapeHtml(dl) + '</td></tr>';
  }
  html += '</tbody></table>';
  document.getElementById('searchResults').innerHTML = html;
}

async function searchPackages() {
  const term = document.getElementById('searchInput').value.trim();
  if (!term) return;
  const btn = document.getElementById('btnSearch');
  btn.disabled = true; btn.innerHTML = '<span class="refresh-spin">&#x1f50d;</span> Searching';
  document.getElementById('searchSection').style.display = '';
  document.getElementById('searchResults').innerHTML = '<div class="loading">Searching...</div>';
  try {
    const result = await sendRequest('tools/call', { name: 'dotnet_package', arguments: { action: 'Search', searchTerm: term } });
    const text = extractText(result);
    if (result?.structuredContent?.searchResults) renderSearch(result.structuredContent.searchResults);
    else renderSearchFromText(text);
    setStatus('Search complete');
  } catch (err) {
    document.getElementById('searchResults').innerHTML = '<div class="error">' + escapeHtml(err.message) + '</div>';
    setStatus('Search error: ' + err.message);
  } finally { btn.disabled = false; btn.innerHTML = '&#x1f50d; Search'; }
}

function renderSearchFromText(text) {
  document.getElementById('searchSection').style.display = '';
  if (!text) { document.getElementById('searchResults').innerHTML = '<div class="loading">No results</div>'; return; }
  const lines = text.split('\n').filter(l => l.trim() && !l.startsWith('Exit') && !l.startsWith('Source'));
  let html = '<table><thead><tr><th>Package</th><th>Details</th></tr></thead><tbody>';
  for (const line of lines.slice(0, 20)) {
    const parts = line.trim().split(/\s*\|\s*|\s{2,}/);
    html += '<tr><td class="mono">' + escapeHtml(parts[0] || '') + '</td><td>' + escapeHtml(parts.slice(1).join(' | ')) + '</td></tr>';
  }
  html += '</tbody></table>';
  document.getElementById('searchResults').innerHTML = html;
}

async function askChat(prompt) {
  try {
    await sendRequest('ui/message', { role: 'user', content: { type: 'text', text: prompt } });
    setStatus('Sent: ' + prompt.substring(0, 50) + '...');
  } catch (err) { setStatus('Could not send: ' + err.message); }
}

function extractText(result) {
  if (!result) return '';
  if (result.content) { for (const c of result.content) { if (c.type === 'text' && c.text) return c.text; } }
  if (typeof result === 'string') return result;
  return '';
}
function escapeHtml(str) { const d = document.createElement('div'); d.textContent = str || ''; return d.innerHTML; }
function setStatus(msg) { document.getElementById('status').textContent = msg; }

const ro = new ResizeObserver(() => {
  sendNotification('ui/notifications/size-changed', { width: document.documentElement.scrollWidth, height: document.documentElement.scrollHeight });
});
ro.observe(document.documentElement);
init();
</script>
</body>
</html>
""";

    // ── Template Wizard ────────────────────────────────────────────────
    [McpServerResource(
        UriTemplate = "ui://dotnet-mcp/template-wizard",
        Name = "template_wizard_ui",
        MimeType = "text/html;profile=mcp-app")]
    [McpMeta("ui", JsonValue = """{"prefersBorder": true}""")]
    public static ResourceContents GetTemplateWizardUI() => new TextResourceContents
    {
        Uri = "ui://dotnet-mcp/template-wizard",
        MimeType = "text/html;profile=mcp-app",
        Text = TemplateWizardHtml,
        Meta = new JsonObject
        {
            ["ui"] = new JsonObject { ["prefersBorder"] = true }
        }
    };

    private const string TemplateWizardHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>.NET Template Wizard</title>
<style>
  :root {
    --color-background-primary: light-dark(#ffffff, #1e1e1e);
    --color-background-secondary: light-dark(#f9fafb, #2d2d2d);
    --color-background-tertiary: light-dark(#f3f4f6, #374151);
    --color-text-primary: light-dark(#1a1a1a, #e0e0e0);
    --color-text-secondary: light-dark(#6b7280, #9ca3af);
    --color-border-primary: light-dark(#e5e7eb, #404040);
    --color-background-info: light-dark(#eff6ff, #1e3a5f);
    --color-text-info: light-dark(#1d4ed8, #60a5fa);
    --color-background-success: light-dark(#f0fdf4, #14532d);
    --color-text-success: light-dark(#15803d, #4ade80);
    --color-background-warning: light-dark(#fffbeb, #78350f);
    --color-text-warning: light-dark(#b45309, #fbbf24);
    --color-background-error: light-dark(#fef2f2, #450a0a);
    --color-text-error: light-dark(#dc2626, #f87171);
    --color-accent: light-dark(#6d28d9, #a78bfa);
    --color-accent-bg: light-dark(#f5f3ff, #2e1065);
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
  h2 { font-size: 16px; font-weight: 600; margin-bottom: 8px; display: flex; align-items: center; gap: 6px; }
  .section { margin-bottom: 16px; }
  .badge { display: inline-block; font-size: 11px; font-weight: 500; padding: 2px 8px; border-radius: 9999px; }
  .badge-info { background: var(--color-background-info); color: var(--color-text-info); }
  .badge-success { background: var(--color-background-success); color: var(--color-text-success); }
  .badge-warning { background: var(--color-background-warning); color: var(--color-text-warning); }
  .badge-accent { background: var(--color-accent-bg); color: var(--color-accent); }
  .search-bar { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; }
  .search-bar input, .search-bar select {
    border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-sm);
    background: var(--color-background-primary); color: var(--color-text-primary);
    padding: 6px 10px; font-size: var(--font-text-sm-size); font-family: var(--font-sans); outline: none;
  }
  .search-bar input { flex: 1; min-width: 150px; }
  .search-bar input:focus, .search-bar select:focus { border-color: var(--color-text-info); }
  .template-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 8px; max-height: 320px; overflow-y: auto; padding-right: 4px; }
  .template-card {
    border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-md);
    padding: 10px 12px; cursor: pointer; transition: border-color 0.15s, background 0.15s;
    background: var(--color-background-secondary);
  }
  .template-card:hover { border-color: var(--color-text-info); background: var(--color-background-info); }
  .template-card.selected { border-color: var(--color-accent); background: var(--color-accent-bg); }
  .template-card .name { font-weight: 600; font-size: var(--font-text-sm-size); margin-bottom: 2px; }
  .template-card .short-name { font-family: var(--font-mono); font-size: 12px; color: var(--color-text-secondary); }
  .template-card .desc { font-size: 12px; color: var(--color-text-secondary); margin-top: 4px; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
  .template-card .tags { margin-top: 6px; display: flex; gap: 4px; flex-wrap: wrap; }
  .group-label { font-size: 12px; font-weight: 600; color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.05em; margin: 12px 0 6px; }
  .group-label:first-child { margin-top: 0; }

  /* Parameter form */
  .param-form { display: none; border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-md); padding: 16px; background: var(--color-background-secondary); }
  .param-form.visible { display: block; }
  .param-form h3 { font-size: 15px; font-weight: 600; margin-bottom: 4px; }
  .param-form .template-desc { font-size: 13px; color: var(--color-text-secondary); margin-bottom: 12px; }
  .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
  .form-field { display: flex; flex-direction: column; gap: 3px; }
  .form-field.full-width { grid-column: 1 / -1; }
  .form-field label { font-size: 12px; font-weight: 500; color: var(--color-text-secondary); }
  .form-field input, .form-field select {
    border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-sm);
    background: var(--color-background-primary); color: var(--color-text-primary);
    padding: 6px 10px; font-size: var(--font-text-sm-size); font-family: var(--font-sans); outline: none;
  }
  .form-field input:focus, .form-field select:focus { border-color: var(--color-accent); }
  .form-field .hint { font-size: 11px; color: var(--color-text-secondary); }
  .form-actions { display: flex; gap: 8px; margin-top: 14px; align-items: center; }
  .btn-primary {
    border: none; border-radius: var(--border-radius-sm); background: var(--color-accent);
    color: #fff; padding: 8px 18px; font-size: var(--font-text-sm-size); font-weight: 600;
    cursor: pointer; font-family: var(--font-sans); transition: opacity 0.15s;
  }
  .btn-primary:hover { opacity: 0.9; }
  .btn-primary:disabled { opacity: 0.5; cursor: default; }
  .btn-secondary {
    border: 1px solid var(--color-border-primary); border-radius: var(--border-radius-sm);
    background: var(--color-background-secondary); color: var(--color-text-primary);
    padding: 7px 14px; font-size: var(--font-text-sm-size); cursor: pointer; font-family: var(--font-sans);
  }
  .btn-secondary:hover { opacity: 0.8; }
  .loading { text-align: center; padding: 24px; color: var(--color-text-secondary); }
  .count-label { font-size: 12px; color: var(--color-text-secondary); margin-left: auto; }
  #status { font-size: 11px; color: var(--color-text-secondary); margin-top: 8px; }
</style>
</head>
<body>

<div class="section">
  <h2>&#x1f9d9; .NET Template Wizard</h2>
</div>

<div class="search-bar">
  <input id="searchInput" type="text" placeholder="Filter templates..." oninput="filterTemplates()" />
  <select id="langFilter" onchange="filterTemplates()">
    <option value="">All Languages</option>
    <option value="C#">C#</option>
    <option value="F#">F#</option>
    <option value="VB">VB</option>
  </select>
  <select id="categoryFilter" onchange="filterTemplates()">
    <option value="">All Categories</option>
  </select>
  <span id="countLabel" class="count-label"></span>
</div>

<div id="templateList" class="section">
  <div class="loading" id="loadingMsg">Loading templates&hellip;</div>
  <div id="templateGrid"></div>
</div>

<div id="paramForm" class="param-form section">
  <h3 id="selectedName"></h3>
  <div id="selectedDesc" class="template-desc"></div>
  <div class="form-grid">
    <div class="form-field">
      <label for="projectName">Project Name</label>
      <input id="projectName" type="text" placeholder="MyApp" />
    </div>
    <div class="form-field">
      <label for="outputDir">Output Directory (optional)</label>
      <input id="outputDir" type="text" placeholder="./src/MyApp" />
    </div>
  </div>
  <div id="dynamicParams" class="form-grid" style="margin-top:10px;"></div>
  <div class="form-actions">
    <button class="btn-primary" id="btnCreate" onclick="createProject()">&#x1f680; Create Project</button>
    <button class="btn-secondary" onclick="clearSelection()">Cancel</button>
    <span id="paramStatus" style="font-size:12px;color:var(--color-text-secondary);"></span>
  </div>
</div>

<div id="status"></div>

<script>
let nextId = 1;
const pending = {};
let allTemplates = [];
let selectedTemplate = null;
let templateParams = {};

function sendRequest(method, params) {
  const id = nextId++;
  window.parent.postMessage({ jsonrpc: '2.0', id, method, params }, '*');
  return new Promise((resolve, reject) => {
    pending[id] = { resolve, reject };
    setTimeout(() => { if (pending[id]) { delete pending[id]; reject(new Error('Timeout: ' + method)); } }, 30000);
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
  if (msg.method === 'ui/notifications/host-context-changed') { Object.assign(hostContext || {}, msg.params); applyTheme(); }
});

async function init() {
  try {
    const result = await sendRequest('ui/initialize', {
      protocolVersion: '2026-01-26',
      clientInfo: { name: 'dotnet-mcp-template-wizard', version: '1.0.0' },
      appCapabilities: { availableDisplayModes: ['inline'] }
    });
    hostContext = result?.hostContext;
    applyTheme();
    sendNotification('ui/notifications/initialized', {});
    setStatus('Connected — loading templates...');
    loadTemplates();
  } catch (err) { setStatus('Init: ' + err.message); }
}

function applyTheme() {
  if (!hostContext?.styles?.variables) return;
  const root = document.documentElement;
  for (const [k, v] of Object.entries(hostContext.styles.variables)) { if (v) root.style.setProperty(k, v); }
  if (hostContext.theme) root.style.colorScheme = hostContext.theme;
}

async function loadTemplates() {
  try {
    const result = await sendRequest('tools/call', { name: 'dotnet_sdk', arguments: { action: 'ListTemplates' } });
    const text = extractText(result);
    allTemplates = parseTemplateList(text);
    document.getElementById('loadingMsg').style.display = 'none';
    populateCategoryFilter();
    renderTemplates(allTemplates);
    setStatus('Loaded ' + allTemplates.length + ' templates');
  } catch (err) {
    document.getElementById('loadingMsg').textContent = 'Could not load templates: ' + err.message;
    setStatus('Error: ' + err.message);
  }
}

function parseTemplateList(text) {
  // Parse the tabular output from 'dotnet new list'
  // Columns: Template Name | Short Name | Language | Type | Author | Tags
  // Use the separator line (---) to detect column boundaries dynamically
  const lines = text.split('\n');
  const templates = [];
  let colRanges = [];
  let separatorFound = false;

  for (const line of lines) {
    if (!separatorFound && /^-{3,}/.test(line)) {
      // Detect column boundaries from dash groups
      const re = /(-+)/g;
      let m;
      colRanges = [];
      while ((m = re.exec(line)) !== null) {
        colRanges.push({ start: m.index, end: m.index + m[0].length });
      }
      separatorFound = true;
      continue;
    }
    if (!separatorFound) continue;
    if (!line.trim()) continue;

    if (colRanges.length < 4) continue;
    const col = (i) => {
      const s = colRanges[i]?.start ?? 0;
      const e = colRanges[i + 1]?.start ?? line.length;
      return line.substring(s, e).trim();
    };

    const name = col(0);                       // Template Name
    const shortNames = col(1);                 // Short Name (may be comma-separated)
    const langRaw = col(2);                    // Language e.g. "[C#],F#"
    const type = col(3);                       // Type e.g. "project"
    const tags = colRanges.length > 5 ? line.substring(colRanges[5].start).trim() : '';

    // Clean language: strip brackets "[C#]" -> "C#"
    const language = langRaw.replace(/[\[\]]/g, '');
    const shortName = shortNames.split(',')[0].trim();

    if (shortName) {
      const categories = extractCategories(tags, type);
      templates.push({ name, shortName, shortNames, language, type: type.toLowerCase(), tags, categories, desc: name });
    }
  }
  return templates;
}

const categoryMap = {
  'Web': t => t.tags.startsWith('Web/') || t.tags.includes('/Web'),
  'API': t => t.tags.includes('API'),
  'Test': t => t.tags.startsWith('Test/'),
  'Console': t => t.tags.includes('Console'),
  'Desktop': t => t.tags.includes('WinForms') || t.tags.includes('WPF'),
  'Service': t => t.tags.includes('Worker') || t.tags.includes('Service'),
  'Library': t => t.tags.includes('Library'),
  'Blazor': t => t.tags.includes('Blazor'),
  'Config': t => t.tags === 'Config' || t.tags.startsWith('Config') || t.tags.startsWith('MSBuild'),
};

function extractCategories(tags, type) {
  const cats = [];
  const t = { tags: tags || '' };
  for (const [cat, test] of Object.entries(categoryMap)) {
    if (test(t)) cats.push(cat);
  }
  // Always include the type as a category
  if (type && type.trim()) cats.push(type.trim().charAt(0).toUpperCase() + type.trim().slice(1).toLowerCase());
  return cats.length ? cats : ['Other'];
}

function populateCategoryFilter() {
  const cats = new Set();
  for (const t of allTemplates) { for (const c of t.categories) cats.add(c); }
  const sel = document.getElementById('categoryFilter');
  const order = ['Web', 'API', 'Blazor', 'Console', 'Desktop', 'Service', 'Library', 'Test', 'Config', 'Project', 'Item', 'Solution', 'Other'];
  for (const c of order) { if (cats.has(c)) { const o = document.createElement('option'); o.value = c; o.textContent = c; sel.appendChild(o); } }
  // Add any remaining
  for (const c of [...cats].sort()) { if (!order.includes(c)) { const o = document.createElement('option'); o.value = c; o.textContent = c; sel.appendChild(o); } }
}

function filterTemplates() {
  const search = document.getElementById('searchInput').value.toLowerCase();
  const lang = document.getElementById('langFilter').value;
  const cat = document.getElementById('categoryFilter').value;
  const filtered = allTemplates.filter(t => {
    if (search && !t.name.toLowerCase().includes(search) && !t.shortNames.toLowerCase().includes(search) && !(t.tags && t.tags.toLowerCase().includes(search))) return false;
    if (lang && !t.language.includes(lang)) return false;
    if (cat && !t.categories.includes(cat)) return false;
    return true;
  });
  renderTemplates(filtered);
}

function renderTemplates(templates) {
  const grid = document.getElementById('templateGrid');
  document.getElementById('countLabel').textContent = templates.length + ' templates';
  if (!templates.length) { grid.innerHTML = '<div class="loading">No matching templates</div>'; return; }

  // Group by primary category (first category)
  const groups = {};
  for (const t of templates) {
    const g = t.categories[0] || 'Other';
    if (!groups[g]) groups[g] = [];
    groups[g].push(t);
  }

  let html = '';
  const order = ['Web', 'API', 'Blazor', 'Console', 'Desktop', 'Service', 'Library', 'Test', 'Config', 'Project', 'Item', 'Solution', 'Other'];
  for (const g of order) {
    if (!groups[g]) continue;
    html += '<div class="group-label">' + escapeHtml(g) + ' (' + groups[g].length + ')</div>';
    html += '<div class="template-grid">';
    for (const t of groups[g]) {
      const sel = selectedTemplate && selectedTemplate.shortName === t.shortName ? ' selected' : '';
      html += '<div class="template-card' + sel + '" onclick="selectTemplate(\'' + escapeAttr(t.shortName) + '\')">';
      html += '<div class="name">' + escapeHtml(t.name) + '</div>';
      html += '<div class="short-name">' + escapeHtml(t.shortNames) + '</div>';
      if (t.tags) html += '<div class="desc">' + escapeHtml(t.tags) + '</div>';
      html += '<div class="tags">';
      if (t.language) {
        for (const l of t.language.split(',')) { if (l.trim()) html += '<span class="badge badge-info">' + escapeHtml(l.trim()) + '</span>'; }
      }
      html += '</div></div>';
    }
    html += '</div>';
  }
  // Render any groups not in the predefined order
  for (const g of Object.keys(groups)) {
    if (order.includes(g)) continue;
    html += '<div class="group-label">' + escapeHtml(g) + ' (' + groups[g].length + ')</div>';
    html += '<div class="template-grid">';
    for (const t of groups[g]) {
      const sel = selectedTemplate && selectedTemplate.shortName === t.shortName ? ' selected' : '';
      html += '<div class="template-card' + sel + '" onclick="selectTemplate(\'' + escapeAttr(t.shortName) + '\')">';
      html += '<div class="name">' + escapeHtml(t.name) + '</div>';
      html += '<div class="short-name">' + escapeHtml(t.shortNames) + '</div>';
      if (t.tags) html += '<div class="desc">' + escapeHtml(t.tags) + '</div>';
      html += '<div class="tags">';
      if (t.language) {
        for (const l of t.language.split(',')) { if (l.trim()) html += '<span class="badge badge-info">' + escapeHtml(l.trim()) + '</span>'; }
      }
      html += '</div></div>';
    }
    html += '</div>';
  }
  grid.innerHTML = html;
}

async function selectTemplate(shortName) {
  const t = allTemplates.find(x => x.shortName === shortName);
  if (!t) return;
  selectedTemplate = t;
  // Highlight selection
  renderTemplates(getFilteredTemplates());
  // Show param form with basic info
  document.getElementById('selectedName').textContent = t.shortName;
  document.getElementById('selectedDesc').textContent = t.desc || '';
  document.getElementById('paramForm').classList.add('visible');
  document.getElementById('dynamicParams').innerHTML = '<div class="loading" style="grid-column:1/-1;padding:8px;">Loading parameters&hellip;</div>';
  document.getElementById('paramStatus').textContent = '';
  // Fetch template details for parameters
  try {
    const result = await sendRequest('tools/call', { name: 'dotnet_sdk', arguments: { action: 'TemplateInfo', templateShortName: shortName } });
    const text = extractText(result);
    renderParams(text);
  } catch (err) {
    document.getElementById('dynamicParams').innerHTML = '<div style="grid-column:1/-1;color:var(--color-text-warning);font-size:13px;">Could not load parameters: ' + escapeHtml(err.message) + '</div>';
  }
}

function getFilteredTemplates() {
  const search = document.getElementById('searchInput').value.toLowerCase();
  const lang = document.getElementById('langFilter').value;
  const cat = document.getElementById('categoryFilter').value;
  return allTemplates.filter(t => {
    if (search && !t.name.toLowerCase().includes(search) && !t.shortNames.toLowerCase().includes(search) && !(t.tags && t.tags.toLowerCase().includes(search))) return false;
    if (lang && !t.language.includes(lang)) return false;
    if (cat && !t.categories.includes(cat)) return false;
    return true;
  });
}

function renderParams(text) {
  // Parse parameters from 'dotnet new <template> --help' output
  templateParams = {};
  const container = document.getElementById('dynamicParams');
  const lines = text.split('\n');
  const params = [];
  let current = null;
  let inChoiceValues = false;

  // Extract template name and description from header
  for (const line of lines) {
    const headerMatch = line.match(/^(.+?)\s+\(C#|F#|VB\)/);
    if (headerMatch && !document.getElementById('selectedName').textContent) {
      document.getElementById('selectedName').textContent = headerMatch[0].trim();
    }
    const descMatch = line.match(/^Description:\s*(.+)/);
    if (descMatch) document.getElementById('selectedDesc').textContent = descMatch[1].trim();
  }

  // Find "Template options:" section
  let inTemplateOptions = false;
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    if (/^Template options:/.test(line)) { inTemplateOptions = true; continue; }
    if (!inTemplateOptions) continue;

    // New parameter line: starts with whitespace then -short, --long or just --long
    const paramMatch = line.match(/^\s+(?:-\S+,\s+)?--(\S+)\s*/);
    if (paramMatch && !line.match(/^\s{20,}/)) {
      // Save previous param
      inChoiceValues = false;
      // Extract description from the same line (after the angle brackets or spaces)
      const descPart = line.replace(/^\s+(?:-\S+,\s+)?--\S+\s*(?:<[^>]*>\s*)?/, '').trim();
      current = { name: paramMatch[1], desc: descPart, type: 'string', defaultValue: '', choices: [] };
      params.push(current);
      continue;
    }

    if (current) {
      const trimmed = line.trim();
      // Type line
      const typeM = trimmed.match(/^Type:\s*(.+)/);
      if (typeM) {
        current.type = typeM[1].trim().toLowerCase();
        if (current.type === 'choice') inChoiceValues = true;
        continue;
      }
      // Default line
      const defM = trimmed.match(/^Default:\s*(.+)/);
      if (defM) { current.defaultValue = defM[1].trim(); inChoiceValues = false; continue; }
      // Enabled if line
      if (trimmed.startsWith('Enabled if:')) continue;
      // Choice value line (when we're inside a choice type): "value  description"
      if (inChoiceValues && trimmed && !trimmed.startsWith('Default:')) {
        const choiceMatch = trimmed.match(/^(\S+)\s+(.*)/);
        if (choiceMatch) {
          current.choices.push({ value: choiceMatch[1], desc: choiceMatch[2].trim() });
        } else if (trimmed) {
          current.choices.push({ value: trimmed, desc: '' });
        }
        continue;
      }
      // Empty line ends current context
      if (!trimmed) { inChoiceValues = false; }
    }
  }

  // Filter out internal/implicit params
  const skip = new Set(['language', 'langversion', 'type', 'skipRestore', 'NuGetRetry', 'name', 'no-restore', 'dry-run', 'force', 'no-update-check', 'project']);
  const visible = params.filter(p => !skip.has(p.name) && !p.name.startsWith('_'));

  if (!visible.length) {
    container.innerHTML = '<div style="grid-column:1/-1;font-size:13px;color:var(--color-text-secondary);">This template has no additional parameters.</div>';
    return;
  }

  let html = '';
  for (const p of visible) {
    templateParams[p.name] = p;
    if (p.type === 'bool' || p.type === 'boolean') {
      const checked = p.defaultValue === 'true' || p.defaultValue === 'True' ? ' checked' : '';
      html += '<div class="form-field"><label>' + escapeHtml(p.name) + '</label>';
      html += '<label style="display:flex;align-items:center;gap:6px;font-size:13px;cursor:pointer;">';
      html += '<input type="checkbox" id="param_' + escapeAttr(p.name) + '"' + checked + ' style="width:16px;height:16px;" />';
      html += escapeHtml(p.desc || 'Enable ' + p.name) + '</label></div>';
    } else if (p.type === 'choice' && p.choices.length > 0) {
      html += '<div class="form-field"><label>' + escapeHtml(p.name) + '</label>';
      html += '<select id="param_' + escapeAttr(p.name) + '">';
      for (const c of p.choices) {
        const sel = c.value === p.defaultValue ? ' selected' : '';
        const label = c.desc ? c.value + ' — ' + c.desc : c.value;
        html += '<option value="' + escapeAttr(c.value) + '"' + sel + '>' + escapeHtml(label) + '</option>';
      }
      html += '</select>';
      if (p.desc) html += '<div class="hint">' + escapeHtml(p.desc) + '</div>';
      html += '</div>';
    } else {
      html += '<div class="form-field"><label>' + escapeHtml(p.name) + '</label>';
      html += '<input id="param_' + escapeAttr(p.name) + '" type="text" placeholder="' + escapeAttr(p.defaultValue || '') + '" />';
      if (p.desc) html += '<div class="hint">' + escapeHtml(p.desc) + '</div>';
      html += '</div>';
    }
  }
  container.innerHTML = html;
}

function clearSelection() {
  selectedTemplate = null;
  document.getElementById('paramForm').classList.remove('visible');
  document.getElementById('dynamicParams').innerHTML = '';
  document.getElementById('projectName').value = '';
  document.getElementById('outputDir').value = '';
  templateParams = {};
  renderTemplates(getFilteredTemplates());
}

async function createProject() {
  if (!selectedTemplate) return;
  const name = document.getElementById('projectName').value.trim();
  const output = document.getElementById('outputDir').value.trim();

  // Build the prompt with all parameter values
  let prompt = 'Create a new .NET project using the "' + selectedTemplate.shortName + '" template';
  if (name) prompt += ' named "' + name + '"';
  if (output) prompt += ' in directory "' + output + '"';

  // Collect non-default parameter values
  const paramArgs = [];
  for (const [pName, pDef] of Object.entries(templateParams)) {
    const el = document.getElementById('param_' + pName);
    if (!el) continue;
    if (pDef.type === 'bool' || pDef.type === 'boolean') {
      const checked = el.checked;
      const defaultChecked = pDef.defaultValue === 'true' || pDef.defaultValue === 'True';
      if (checked !== defaultChecked) paramArgs.push('--' + pName + ' ' + checked);
    } else {
      const val = el.value.trim();
      if (val && val !== pDef.defaultValue) paramArgs.push('--' + pName + ' ' + val);
    }
  }
  if (paramArgs.length) prompt += ' with options: ' + paramArgs.join(', ');

  try {
    const btn = document.getElementById('btnCreate');
    btn.disabled = true; btn.textContent = 'Creating...';
    await sendRequest('ui/message', { role: 'user', content: { type: 'text', text: prompt } });
    setStatus('Sent project creation request to Copilot');
    btn.disabled = false; btn.textContent = '\u{1f680} Create Project';
  } catch (err) {
    setStatus('Could not send: ' + err.message);
    document.getElementById('btnCreate').disabled = false;
    document.getElementById('btnCreate').textContent = '\u{1f680} Create Project';
  }
}

function extractText(result) {
  if (!result) return '';
  if (result.content) { for (const c of result.content) { if (c.type === 'text' && c.text) return c.text; } }
  if (typeof result === 'string') return result;
  return '';
}
function escapeHtml(str) { const d = document.createElement('div'); d.textContent = str || ''; return d.innerHTML; }
function escapeAttr(str) { return (str || '').replace(/"/g, '&quot;').replace(/'/g, '&#39;'); }
function setStatus(msg) { document.getElementById('status').textContent = msg; }

const ro = new ResizeObserver(() => {
  sendNotification('ui/notifications/size-changed', { width: document.documentElement.scrollWidth, height: document.documentElement.scrollHeight });
});
ro.observe(document.documentElement);
init();
</script>
</body>
</html>
""";
}
