# .NET MCP Server

<!-- mcp-name: io.github.jongalloway/dotnet-mcp -->

[![Build and Test](https://github.com/jongalloway/dotnet-mcp/actions/workflows/build.yml/badge.svg)](https://github.com/jongalloway/dotnet-mcp/actions/workflows/build.yml)
[![Coverage](https://codecov.io/gh/jongalloway/dotnet-mcp/branch/main/graph/badge.svg)](https://codecov.io/gh/jongalloway/dotnet-mcp)
[![Dependabot](https://img.shields.io/badge/Dependabot-enabled-blue.svg)](https://github.com/jongalloway/dotnet-mcp/blob/main/.github/dependabot.yml)
[![NuGet](https://img.shields.io/nuget/v/Community.Mcp.DotNet.svg)](https://www.nuget.org/packages/Community.Mcp.DotNet/)
[![Published to MCP Registry](https://img.shields.io/badge/Published-MCP%20Registry-brightgreen)](https://registry.modelcontextprotocol.io/?q=io.github.jongalloway%2Fdotnet-mcp)
[![CodeQL Advanced](https://github.com/jongalloway/dotnet-mcp/actions/workflows/codeql.yml/badge.svg)](https://github.com/jongalloway/dotnet-mcp/actions/workflows/codeql.yml)

Give your AI assistant superpowers for .NET development! This MCP server connects GitHub Copilot, Claude, and other AI assistants directly to the .NET SDK, enabling them to create projects, manage packages, run builds, and more—all through natural language.

## Quick Install

Click to install in your preferred environment:
[![VS Code - Install .NET MCP](https://img.shields.io/badge/VS_Code-Install_.NET_MCP-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://vscode.dev/redirect/mcp/install?name=dotnet-mcp&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22Community.Mcp.DotNet%401.%2A%22%2C%22--yes%22%5D%7D)
[![VS Code Insiders - Install .NET MCP](https://img.shields.io/badge/VS_Code_Insiders-Install_.NET_MCP-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://insiders.vscode.dev/redirect/mcp/install?name=dotnet-mcp&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22Community.Mcp.DotNet%401.%2A%22%2C%22--yes%22%5D%7D&quality=insiders)
[![Visual Studio - Install .NET MCP](https://img.shields.io/badge/Visual_Studio-Install_.NET_MCP-5C2D91?style=flat-square&logo=visualstudio&logoColor=white)](https://vs-open.link/mcp-install?%7B%22name%22%3A%22Community.Mcp.DotNet%22%2C%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22Community.Mcp.DotNet%401.%2A%22%2C%22--yes%22%5D%7D)

> **Note**: Quick install requires the .NET 10 or .NET 11 SDK. Source builds require .NET 11 RC3 or a compatible GA SDK in the 11.0.1xx feature band.

## What is This?

The .NET MCP Server is a **Model Context Protocol (MCP) server** that connects AI assistants to the .NET SDK using the [Model Context Protocol](https://modelcontextprotocol.io/). Think of it as giving your AI assistant a direct line to `dotnet` commands, but with intelligence and context.

> **Important**: This package is designed exclusively as an **MCP server** for AI assistants. It is not intended for use as a library or for programmatic consumption in other .NET applications. The only supported use case is running it as an MCP server via `dnx` or `dotnet run`.

```mermaid
graph LR
    A[AI Assistant<br/>Copilot/Claude] -->|Natural Language| B[.NET MCP Server]
    B -->|dotnet commands| C[.NET SDK]
    C -->|Results| B
    B -->|Structured Response| A
    
    style A fill:#e1f5ff
    style B fill:#fff4e6
    style C fill:#e8f5e9
```

## Why Use It?

### 🚀 **Faster Development**

Instead of remembering exact `dotnet` commands and syntax, just ask:

- *"Create a new web API project with Entity Framework"*
- *"Add the Serilog package and configure structured logging"*
- *"Update all my NuGet packages to the latest versions"*

### 🧠 **Smarter AI Assistance**

Your AI assistant gets direct access to:

- All installed .NET templates and their parameters
- NuGet package search and metadata
- Framework version information (including LTS status)
- Your solution and project structure

### ⚡ **Token-Efficient by Design**

Instead of relying on the LLM to infer state by reading and re-reading console output, the server exposes purpose-built .NET tools with **structured inputs and outputs**. In practice, this typically means fewer tokens per task and less back-and-forth.

- Deterministic tool schemas reduce prompt/response overhead
- `machineReadable: true` returns structured JSON (no log scraping)
- MCP Resources provide lightweight metadata without running commands

### 🎯 **Why Not Just Let the AI Call `dotnet` Directly?**

The .NET MCP Server provides **context and intelligence** that raw CLI execution cannot:

#### **1. Template Discovery & Validation**

- **With MCP**: AI knows exactly which templates are installed (`console`, `webapi`, `blazor`, etc.) and their parameters
- **Without MCP**: AI guesses template names and parameters, often getting them wrong

#### **2. Framework Intelligence**

- **With MCP**: AI knows which .NET versions are installed, which are LTS, and can recommend appropriately
- **Without MCP**: AI suggests `net8.0` when you only have `net10.0` installed, leading to errors

#### **3. Rich Tool Descriptions**

- **With MCP**: Each tool has detailed parameter descriptions and constraints (e.g., "configuration must be Debug or Release")
- **Without MCP**: AI constructs commands from general knowledge, missing version-specific changes

#### **4. Parameter Information**

- **With MCP**: AI sees template parameters like `--use-controllers`, `--auth`, framework options
- **Without MCP**: AI doesn't know what optional parameters exist for each template

#### **5. Package Search Integration**

- **With MCP**: AI can search NuGet.org to find exact package names and versions
- **Without MCP**: AI guesses package names, often suggesting outdated or incorrect ones

#### **6. Structured Error Handling with Enhanced Diagnostics**

- **With MCP**: Errors are parsed and enriched with explanations, documentation links, and suggested fixes
- **Without MCP**: AI gets raw stderr output and may misinterpret errors

The server provides **enhanced error diagnostics** for 52 common error codes:

- Plain English explanations of what went wrong
- Direct links to official Microsoft documentation
- Specific suggested fixes with commands to resolve issues
- Support for CS####, MSB####, NU####, and NETSDK#### error codes

See [Error Diagnostics Documentation](doc/error-diagnostics.md) for details.

#### **7. MCP Resources**

MCP Resources provide read-only access to structured metadata about your .NET environment:

- **dotnet://sdk-info** - Information about installed .NET SDKs (versions and paths)
- **dotnet://runtime-info** - Information about installed .NET runtimes (versions and types)
- **dotnet://templates** - Complete catalog of installed .NET templates with metadata
- **dotnet://frameworks** - Information about supported .NET frameworks (TFMs) including LTS status
- **dotnet://workspace** - Workspace snapshot with solution, projects, target frameworks, and package counts

This enables AI assistants to:

- Answer questions without executing commands ("What .NET versions do I have installed?")
- Provide context-aware suggestions based on your actual environment
- Access structured JSON data more efficiently than parsing CLI output
- Reference official .NET metadata for accurate recommendations

**Example**: Using resources for context-aware assistance

```text
❌ Without Resources:
User: "What .NET versions do I have?"
AI: Executes dotnet --list-sdks and parses output
User: "Which is LTS?"
AI: Executes dotnet --info and tries to parse support info

✅ With Resources:
User: "What .NET versions do I have?"
AI: Reads dotnet://sdk-info resource (no execution needed)
    Returns: .NET 8.0 (LTS), .NET 9.0 (STS), .NET 10.0 (LTS)
User: "Which is LTS?"
AI: Already knows from resource metadata - .NET 8.0, .NET 10.0
```

**Example**: Creating a Blazor project with authentication

```text
❌ Without MCP:
AI: "I'll run: dotnet new blazor --auth Individual"
Result: Error - template 'blazor' doesn't support --auth parameter

✅ With MCP:
AI: Uses dotnet_sdk with action: "TemplateInfo" for 'blazor'
  Sees available parameters: --interactivity, --use-program-main, --empty
  Uses dotnet_sdk with action: "SearchTemplates" to find authentication templates
AI: "I'll create a Blazor Web App with authentication using the correct template..."
Result: Success - uses 'blazor' template with proper authentication configuration
```

### ✨ **Consistent Results**

The MCP server uses official .NET SDK APIs and CLI commands, ensuring:

- Accurate template information from the Template Engine
- Reliable command execution
- Proper error handling and validation

### 🔒 **Secure by Design**

- Runs locally on your machine
- No data sent to external servers
- You control what commands execute
- Standard .NET security model applies
- **Automatic secret redaction** to protect sensitive information in CLI output
  - Connection strings, passwords, and API keys are automatically redacted
  - Implemented using optimized regular expressions with <1% performance overhead
  - References Microsoft.Extensions.Compliance.Redaction for future integration
  - Opt-out available with `unsafeOutput=true` for advanced debugging
  - Patterns include: database credentials, cloud provider keys, tokens, certificates, and more
  - Performance impact is minimal and tested to complete within 500ms for 10,000 lines
- **Optional MCP governance controls** (preview)
  - Uses `Microsoft.AgentGovernance.Extensions.ModelContextProtocol` when enabled via configuration
  - Disabled by default to preserve existing behavior and compatibility
  - Does not ship with a default enforced policy; provide your own policy file(s) when needed
  - See `doc/mcp-governance.md` for a sample `mcp.yaml` policy template
  - Sources:
    - Agent Governance Toolkit repository: https://github.com/microsoft/agent-governance-toolkit
    - Agent Governance Toolkit for .NET: https://github.com/microsoft/agent-governance-toolkit/tree/main/agent-governance-dotnet
    - Announcement: https://devblogs.microsoft.com/dotnet/announcing-agent-governance-toolkit-mcp-extensions-for-dotnet/
  - Configuration for startup scanning, policy-based tool authorization, and response sanitization uses the following shape:

> **Governance startup limitation:** Setting `McpGovernance:Enabled` to `true` with the full tool catalog currently **fails startup** because the toolkit scanner rejects the existing `dotnet_dev_certs.password` input property. The example below is not a working full-catalog workaround, and an allow policy does not bypass startup scanning. See the [startup restriction and operating guidance](doc/mcp-governance.md#sdk-compatibility-and-startup-restriction). Leaving governance disabled permits ordinary server use but provides no governance enforcement.

```json
"McpGovernance": {
  "Enabled": true,
  "DefaultAgentId": "did:mcp:anonymous",
  "ServerName": "dotnet-mcp",
  "PolicyPaths": ["/path/to/your/policies/mcp.yaml"]
}
```

## How It Works

```mermaid
sequenceDiagram
    participant User
    participant AI as AI Assistant
    participant MCP as .NET MCP Server
    participant SDK as .NET SDK

    User->>AI: "Create a console app called MyApp"
    AI->>MCP: dotnet_project(action: "New", template: "console", name: "MyApp")
    MCP->>SDK: dotnet new console -n MyApp
    SDK-->>MCP: Project created successfully
    MCP-->>AI: Structured result with output
    AI-->>User: "I've created a new console app called MyApp"
```

The .NET MCP Server acts as an intelligent middleware that:

1. **Translates** natural language requests into structured .NET SDK operations
2. **Validates** parameters using official SDK metadata (templates, frameworks, packages)
3. **Executes** commands safely with proper error handling
4. **Returns** structured results that AI assistants can understand and explain

### 🧩 **MCP C# SDK Features Used**

dotnet-mcp uses the official [MCP C# SDK](https://csharp.sdk.modelcontextprotocol.io/) and exercises a broad set of MCP features beyond basic tool invocation:

The server uses SDK **2.3.0**, supports protocol **2026-07-28**, and retains older stdio clients, including **2025-11-25**. Async execution requires the new Tasks extension and per-request opt-in; legacy clients continue to call tools inline. See [SDK compatibility](doc/sdk-v1.1-compatibility.md) for wire-protocol differences and [governance configuration](doc/mcp-governance.md) for the existing fail-closed catalog limitation.

Use `dotnet_server_capabilities` to discover the negotiated protocol and feature availability for the active request. On 2026-07-28, logging needs a per-request log level, and Sampling/Elicitation depend on the request's client capability metadata. These flags do not carry over from earlier requests. A no-connection snapshot reports implementation support, not a guarantee that a client can use each feature; see the [capability contract](doc/machine-readable-contract.md#connection-dependent-capabilities-and-tasks).

| Feature | How dotnet-mcp uses it | Why |
| ------- | ---------------------- | --- |
| [Resources](https://modelcontextprotocol.io/specification/latest/server/resources) | Exposes `dotnet://sdk-info`, `dotnet://runtime-info`, `dotnet://templates`, `dotnet://frameworks`, and `dotnet://workspace` as read-only MCP resources. | Lets clients answer environment and workspace topology questions quickly without shelling out, and reduces token churn from repeated CLI discovery. |
| [Prompts](https://modelcontextprotocol.io/specification/latest/server/prompts) | Publishes reusable prompts such as `create_new_webapi`, `add_package_and_restore`, and `run_tests_with_coverage`. | Gives MCP clients guided, discoverable workflows instead of rebuilding common prompt scaffolding from scratch. |
| [Roots](https://modelcontextprotocol.io/specification/latest/client/roots) | Retains deprecated stdio Roots discovery to auto-detect a single project or solution when the client advertises Roots and the user omits an explicit path. | Improves out-of-box behavior in compatible IDE clients while preserving explicit-path precedence. |
| [Sampling](https://modelcontextprotocol.io/specification/latest/client/sampling) | Retains deprecated client-mediated Sampling for build and test failure summaries when advertised and supported by the transport. Roots/Sampling MRTR replacement is deferred. | Keeps intelligent diagnostics inside the MCP flow without requiring separate server-side model credentials; successful commands and unsupported clients retain their original output. |
| [Elicitation](https://modelcontextprotocol.io/specification/latest/client/elicitation) | Confirms project Clean, solution Remove, actual EF DatabaseDrop, CertificateClean, SecretsRemove, and SecretsClear when supported, using native MRTR, legacy elicitation, or task input. See the [confirmation policy](doc/sdk-v1.1-compatibility.md#destructive-operation-confirmation). | Only accepted, checked confirmation proceeds. EF drops still require `force=true`; dry runs do not prompt. Clients without elicitation retain unprompted behavior, so confirmation is not an authorization boundary. |
| [Progress notifications](https://modelcontextprotocol.io/specification/latest/basic/utilities/progress) | Reports start/completion progress for long-running restore, build, test, publish, pack, tool, and workload operations. | Gives clients a better UX for slow operations and makes orchestration easier for agents that surface progress. |
| [Tasks](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/docs/concepts/tasks/tasks.md) | Uses `WithTasks` with a hosting-owned, process-local in-memory store; tool handlers do not depend on storage. | Opted-in 2026-07-28 calls support task creation, polling via `tasks/get`, input updates via `tasks/update`, and cancellation via `tasks/cancel`; ordinary calls remain inline. |
| [Tools and tool metadata](https://modelcontextprotocol.io/specification/latest/server/tools) | Uses consolidated tools with rich metadata such as titles, categories, priorities, and action lists. | Improves discoverability, routing, and client-side presentation, especially in MCP-aware IDE integrations. |
| [Prompt and resource completion](https://modelcontextprotocol.io/specification/latest/server/utilities/completion) | Implements a completion handler so clients can autocomplete prompt and resource argument values. | Makes guided workflows faster and reduces invalid argument entry in clients that support completion UX. |
| [Icons and server metadata](https://csharp.sdk.modelcontextprotocol.io/) | Configures server-level icons plus per-tool `IconSource`, title, and descriptive metadata across the consolidated surface. | Helps clients present the server and tool catalog more clearly, especially in visual MCP management UIs. |

## Installation

### Requirements

- **For package installation**: .NET 10 SDK or .NET 11 SDK (RC3 or later)
- **For source builds**: .NET SDK `11.0.100-rc.3.26522.148` or a GA/servicing SDK in the 11.0.1xx feature band; `global.json` selects the latest installed compatible patch. Install the .NET 10 SDK alongside it to run both test targets.
- Visual Studio Code, Visual Studio 2022 (v17.13+), or Claude Desktop

### Runtime and SDK compatibility

One package contains `net10.0` and `net11.0` builds with the same MCP tool surface. .NET 10-only machines remain supported. The .NET 11 build uses RC3 Template Engine and Hosting packages; this does not add new .NET 11-only tools or change the default .NET 10 LTS project recommendation.

| Environment | Fresh installation with the selected SDK | Project support |
| --- | --- | --- |
| .NET 10 only | .NET 10 tool assets and runtime | .NET 10 projects |
| .NET 11 RC3 only | .NET 11 tool assets and RC3 runtime | .NET 11 projects |
| .NET 10 and 11 RC3 side by side, .NET 11 SDK selected | .NET 11 tool assets and runtime | .NET 10 projects with a project-local .NET 10 SDK pin, and .NET 11 projects |
| .NET 11 GA only | Intended to use the unchanged RC3-built .NET 11 assets | GA-only execution is pending verification |

Tool asset selection follows the SDK used to install or launch the tool, not simply the highest runtime installed. A `global.json` can select an older SDK. Installing .NET 11 later does not retarget an existing .NET 10 tool installation; reinstall or update it using the desired SDK. For an explicit tool installation target, `dotnet tool install` also supports `--framework net11.0`.

Both `dnx` and a tool-path install were exercised against the same package on isolated .NET 10, RC3, and side-by-side installations. Neither required `--allow-roll-forward`. For a specific package version, use:

```text
dnx Community.Mcp.DotNet@<exact-version> --yes
dotnet tool install Community.Mcp.DotNet@<exact-version> --tool-path ./tools
```

Replace `<exact-version>` with the desired published version. An exact prerelease version does not require `--prerelease`; floating prerelease selection does. Point MCP clients at the installed `dotnet-mcp` executable when using a tool-path install.

The package explicitly uses `RollForward=Minor`, keeping runtime resolution within the target major version. The RC3-built package's .NET 11 runtime floor is `11.0.0-rc.3.26522.148`. RC3-to-GA is a same-major runtime transition, not a reason to require `Major` roll-forward. A compatible runtime must already be installed; roll-forward does not download one. CI automatically installs the GA SDK when published and verifies an unchanged RC3-built package on GA, while retaining exact-RC3 coverage. **GA execution is pending until GA is available.** A .NET 11-only build cannot run backward on .NET 10; the separate .NET 10 assets provide that support.

The server runtime and the SDK used for project commands are separate choices. Future SDK-specific CLI features should be checked against the selected project's SDK; only features using .NET 11-only in-process APIs need the .NET 11 server build.

### Opt-in NativeAOT publishing (Windows x64)

The managed NuGet/`dnx` package remains the default distribution, with both `net10.0` and `net11.0` assets. To publish **the MCP server itself** as a native executable, use an x64 Windows machine with the source-build SDKs above and Visual Studio's **Desktop development with C++** workload, including its Windows SDK:

```powershell
pwsh -File .\scripts\publish-native.ps1
pwsh -File .\scripts\test-native.ps1
```

Configure an MCP client's stdio command as the absolute path to `artifacts\native\win-x64\DotNetMcp.exe`, with no `dotnet run` arguments. Copy the **entire output directory**, including `sdk-helper`, when deploying it. Native publishing is opt-in and currently supports only `net11.0` / `win-x64`; it does not change ordinary builds or tool packaging.

The native process owns the complete MCP session: tools, resources, prompts, MCP Apps, Tasks, cancellation, and existing governance behavior. Template Engine loading and the nine MSBuild project-analysis/editing operations run in a narrow managed SDK helper, not a second MCP server. **This is not an SDK-free or single-file distribution**: a .NET 11 runtime is required for the helper, and installed .NET SDKs remain necessary for project commands. Projects can still select their SDK through `global.json`.

The validation script relocates the bundle and checks native PE/runtime identity, managed/native contract and SDK-operation parity, real stdio conformance, SDK 11 scenarios, and release gates. Transitive AOT analysis warnings, including governance's YamlDotNet dependency, remain visible; publishing is not a claim that arbitrary transitive reflection paths are AOT-safe. The existing [fail-closed full-catalog governance restriction](doc/mcp-governance.md#sdk-compatibility-and-startup-restriction) is unchanged. See [native validation details](doc/testing.md#windows-nativeaot-distribution-validation).

### Option 1: Quick Install (Recommended)

Use the install badges at the top of this page to automatically configure the MCP server in your environment. The server will be downloaded from NuGet.org on first use.

### Option 2: Manual Configuration

Follow the instructions below for your specific development environment:

### Visual Studio Code

**Using Quick Install** (recommended - .NET 10 required):

1. Click the [VS Code Install badge](#quick-install) at the top of this page
2. Or manually add via Command Palette: press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS), run **"GitHub Copilot: Add MCP Server"**, then enter **Name** `dotnet`, **Type** `stdio`, **Command** `dnx`, and **Arguments** `Community.Mcp.DotNet@1.* --yes`.

**Manual Configuration** (for source builds or custom setups):

Edit your VS Code settings (`Ctrl+,` or `Cmd+,`, search for "mcp"):

```json
{
  "github.copilot.chat.mcp.servers": {
    "dotnet": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--framework", "net11.0", "--project", "/path/to/dotnet-mcp/DotNetMcp/DotNetMcp.csproj"]
    }
  }
}
```

📖 [Full VS Code MCP documentation](https://code.visualstudio.com/docs/copilot/customization/mcp-servers#_add-an-mcp-server)

### Visual Studio 2022

**Requirements**: Visual Studio 2022 version 17.13 or later

**Using Quick Install** (recommended - .NET 10 required):

1. Click the [Visual Studio Install badge](#quick-install) at the top of this page
2. Or manually add via Options: go to **Tools** > **Options** > **GitHub Copilot** > **MCP Servers**, click **Add**, then enter **Name** `dotnet`, **Type** `stdio`, **Command** `dnx`, and **Arguments** `Community.Mcp.DotNet@1.* --yes`.

**Manual Configuration** (for source builds or custom setups):

1. Go to **Tools** > **Options** > **GitHub Copilot** > **MCP Servers**
2. Click **Add**
3. Enter:
   - **Name**: `dotnet`
   - **Type**: `stdio`
   - **Command**: `dotnet`
   - **Arguments**: `run --framework net11.0 --project C:\path\to\dotnet-mcp\DotNetMcp\DotNetMcp.csproj`

📖 [Full Visual Studio MCP documentation](https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=vs-2022)

### Claude Desktop

**Using DNX** (recommended - .NET 10 required):

**macOS**: Edit `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "dotnet": {
      "command": "dnx",
      "args": ["Community.Mcp.DotNet@1.*", "--yes"]
    }
  }
}
```

**Windows**: Edit `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "dotnet": {
      "command": "dnx",
      "args": ["Community.Mcp.DotNet@1.*", "--yes"]
    }
  }
}
```

**Manual Configuration** (for source builds or custom setups):

**macOS**:

```json
{
  "mcpServers": {
    "dotnet": {
      "command": "dotnet",
      "args": ["run", "--framework", "net11.0", "--project", "/path/to/dotnet-mcp/DotNetMcp/DotNetMcp.csproj"]
    }
  }
}
```

**Windows**:

```json
{
  "mcpServers": {
    "dotnet": {
      "command": "dotnet",
      "args": ["run", "--framework", "net11.0", "--project", "C:\\path\\to\\dotnet-mcp\\DotNetMcp\\DotNetMcp.csproj"]
    }
  }
}
```

### GitHub Copilot coding agent (Repository MCP configuration)

You can also configure this MCP server at the **repository** level for GitHub Copilot coding agent.

1. Open your repo on GitHub
2. Go to **Settings** > **Copilot** > **Coding agent**
3. Paste the JSON below into the MCP configuration box
4. Click **Save**

For details, see GitHub's documentation: [Extending GitHub Copilot coding agent with the Model Context Protocol (MCP)](https://docs.github.com/copilot/how-tos/use-copilot-agents/coding-agent/extend-coding-agent-with-mcp)

```json
{
  "mcpServers": {
    "dotnet": {
      "type": "local",
      "command": "dnx",
      "args": ["Community.Mcp.DotNet@1.*", "--yes"],
      "tools": [
        "dotnet_project",
        "dotnet_package",
        "dotnet_solution",
        "dotnet_sdk",
        "dotnet_help",
        "dotnet_server_capabilities",
        "dotnet_server_info"
      ]
    }
  }
}
```

This configuration uses a curated allowlist of tools (instead of `"*"`) so the agent can use the .NET MCP server safely and predictably.

Note: Copilot coding agent runs on GitHub Actions runners. If you need to ensure .NET 10 is installed for your repository, add a setup workflow like [.github/workflows/copilot-setup-steps.yml](.github/workflows/copilot-setup-steps.yml).

## Usage Examples

Once configured, you can use natural language with your AI assistant:

### Quick Start - Single Project

**User**: *"Create a new web API project called WeatherService"*

**AI**: *I'll create a new web API project for you...*

```bash
dotnet new webapi -n WeatherService
```

*The project has been created successfully in the WeatherService directory.*

### Complex Application - Microservices with Aspire

**User**: *"Create a server-side web app and minimal API, orchestrated by .NET Aspire, with unit tests for the API using xunit. Use slnx format for the solution."*

**AI**: *I'll create a complete microservices solution with .NET Aspire orchestration...*

```bash
# Create solution with new XML format
dotnet new sln -n MyMicroservices --use-slnx

# Create .NET Aspire AppHost for orchestration
dotnet new aspire-apphost -n MyMicroservices.AppHost

# Create web app
dotnet new webapp -n MyMicroservices.Web

# Create minimal API
dotnet new webapi -n MyMicroservices.Api --use-minimal-apis

# Create test project
dotnet new xunit -n MyMicroservices.Api.Tests

# Add projects to solution
dotnet sln add MyMicroservices.AppHost/MyMicroservices.AppHost.csproj
dotnet sln add MyMicroservices.Web/MyMicroservices.Web.csproj
dotnet sln add MyMicroservices.Api/MyMicroservices.Api.csproj
dotnet sln add MyMicroservices.Api.Tests/MyMicroservices.Api.Tests.csproj

# Add project references for tests
dotnet add MyMicroservices.Api.Tests/MyMicroservices.Api.Tests.csproj reference MyMicroservices.Api/MyMicroservices.Api.csproj
```

*Solution created with 4 projects: AppHost for orchestration, Web app, API, and unit tests. Ready to run with `dotnet run --project MyMicroservices.AppHost`.*

### Database-Driven Application with Entity Framework

**User**: *"Set up Entity Framework Core with SQL Server for my API and create the initial database migration"*

**AI**: *I'll add EF Core packages, install the dotnet-ef tool, and create your initial migration...*

```bash
# Add Entity Framework packages
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design

# Install dotnet-ef tool globally
dotnet tool install dotnet-ef --global

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to create database
dotnet ef database update
```

*Entity Framework Core with SQL Server provider added. Initial migration created and applied. Database is ready for use.*

**User**: *"I added a Product entity to my DbContext. Update the database schema"*

**AI**: *I'll create a migration for your Product entity and apply it...*

```bash
# Create migration for new entity
dotnet ef migrations add AddProductEntity

# List migrations to see what's pending
dotnet ef migrations list

# Apply the migration
dotnet ef database update
```

*Migration created for Product entity (new table with Id, Name, Price, CreatedDate columns). Database updated successfully.*

**User**: *"Generate SQL script for the last migration so I can review it before production deployment"*

**AI**: *I'll generate an idempotent SQL script from your migration...*

```bash
# Generate idempotent SQL script (can be run multiple times safely)
dotnet ef migrations script --idempotent --output deploy.sql
```

*SQL script generated to deploy.sql. This script can be safely run multiple times and will only apply changes if needed.*

### Database-First Development

**User**: *"I have an existing SQL Server database. Reverse engineer it to Entity Framework classes"*

**AI**: *I'll scaffold your database into Entity Framework Core entities...*

```bash
# Reverse engineer (scaffold) database to EF Core entities
dotnet ef dbcontext scaffold "Server=localhost;Database=MyExistingDb;Trusted_Connection=true;" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models

# List the generated DbContext classes
dotnet ef dbcontext list

# Get detailed info about the DbContext
dotnet ef dbcontext info
```

*Database scaffolded successfully. Generated MyExistingDbContext.cs and 15 entity classes (Customer, Order, Product, etc.) in the Models folder.*

### Multi-Target Testing Strategy

**User**: *"Add integration tests using NUnit that target both .NET 8 and .NET 10, and configure them to run with code coverage"*

**AI**: *I'll set up multi-targeted integration tests with coverage...*

```bash
# Create NUnit test project
dotnet new nunit -n MyMicroservices.IntegrationTests

# Update project to multi-target (edit .csproj)
# <TargetFrameworks>net8.0;net10.0</TargetFrameworks>

# Add integration test packages
dotnet add MyMicroservices.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing
dotnet add MyMicroservices.IntegrationTests package Microsoft.Testing.Extensions.CodeCoverage

# Run tests with coverage for both frameworks
dotnet test -- --coverage --coverage-output-format cobertura
```

*Integration test project created with multi-targeting. Tests will run on both .NET 8 and .NET 10 with code coverage enabled.*

### Exploring and Discovering

**User**: *"What templates are available for creating cloud-native applications?"*

**AI**: *Let me search for cloud-native related templates...*

```bash
dotnet new search aspire
dotnet new search dapr
dotnet new search azure
```

*Found templates:*

- `aspire` - .NET Aspire Application
- `aspire-apphost` - .NET Aspire App Host
- `aspire-servicedefaults` - .NET Aspire Service Defaults
- `dapr` - Dapr application templates
- Various Azure Functions and Container Apps templates

*Each template is designed for cloud-native microservices and distributed applications.*

### Modernization Workflow

**User**: *"Update all packages to latest versions, format all code to match our editorconfig rules, then build and run tests"*

**AI**: *I'll update packages, format code, build, and test...*

```bash
# Update all packages to latest compatible versions
dotnet package update

# Format all code according to .editorconfig
dotnet format

# Clean and rebuild
dotnet clean
dotnet build -c Release

# Run all tests
dotnet test --verbosity detailed
```

*All packages updated to latest versions. Code formatted consistently across solution. Build succeeded with 0 warnings. All 156 tests passed.*

### HTTPS Development Setup

**User**: *"I'm getting HTTPS certificate errors when running my web app"*

**AI**: *Let me check your HTTPS development certificate...*

```bash
# Check certificate status
dotnet dev-certs https --check --trust
```

*Your certificate isn't trusted. Let me fix that...*

```bash
# Trust the certificate (may require elevation)
dotnet dev-certs https --trust
```

*Certificate is now trusted. Try running your app again. If you're using Docker, you can export the certificate:*

```bash
# Export for Docker containers
dotnet dev-certs https --export-path ./certs/aspnetapp.pfx --password "SecurePassword123!"
```

*Certificate exported to ./certs/aspnetapp.pfx. You can now mount it in your Docker container.*

### Secure Configuration with User Secrets

**User**: *"Set up user secrets for my API project with a database connection string"*

**AI**: *I'll initialize user secrets for your project...*

```bash
# Initialize user secrets (adds UserSecretsId to project file)
dotnet user-secrets init --project MyApi/MyApi.csproj
```

*Now I'll add your database connection string securely...*

```bash
# Store connection string (not committed to Git)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=MyDb;User=dev;Password=secret123"
```

*Secret stored! You can also add API keys:*

```bash
# Add Azure OpenAI API key
dotnet user-secrets set "Azure:OpenAI:ApiKey" "sk-..."

# Add SendGrid API key
dotnet user-secrets set "SendGrid:ApiKey" "SG...."
```

*Let me show you what's configured:*

```bash
# List all secrets
dotnet user-secrets list
```

*Output:*

```text
ConnectionStrings:DefaultConnection = Server=localhost;Database=MyDb;User=dev;Password=secret123
Azure:OpenAI:ApiKey = sk-...
SendGrid:ApiKey = SG....
```

*These secrets are stored in your user profile (not in the project) and won't be committed to Git. In your code, access them like:*

```csharp
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
var apiKey = builder.Configuration["Azure:OpenAI:ApiKey"];
```

*Note: User secrets are for **development only**. Use Azure Key Vault or environment variables for production.*

### Mobile and MAUI Development Setup

**User**: *"I want to develop a .NET MAUI app for Android and iOS"*

**AI**: *I'll help you set up the necessary workloads for MAUI development...*

```bash
# First, let's see what MAUI workloads are available
dotnet workload search maui
```

*Found workloads:*

```text
Workload ID       Description              
-------------------------------------------
maui-android      .NET MAUI SDK for Android
maui-ios          .NET MAUI SDK for iOS    
maui-windows      .NET MAUI SDK for Windows
maui-tizen        .NET MAUI SDK for Tizen  
```

*Now I'll install the workloads you need:*

```bash
# Install MAUI workloads for Android and iOS
# Warning: This will download several GB of SDKs and tools
dotnet workload install maui-android maui-ios
```

> **Note**: The command above shows the raw `dotnet` CLI syntax, which uses space-separated workload IDs. When calling the .NET MCP workload install tool, pass workload IDs as a single comma-separated parameter, for example: `workloadIds: "maui-android,maui-ios"`. The MCP server will translate this into the equivalent CLI command.

*Workloads installed! Let's verify:*

```bash
# List installed workloads
dotnet workload list
```

*Output:*

```text
Installed Workload Id    Manifest Version      Installation Source
----------------------------------------------------------------------
maui-android            10.0.100/10.0.100     SDK 10.0.100        
maui-ios                10.0.100/10.0.100     SDK 10.0.100        
```

*Now you can create a MAUI project:*

```bash
# Create a new MAUI app
dotnet new maui -n MyMauiApp
```

*Your MAUI development environment is ready! When SDK updates are available, update your workloads:*

```bash
# Update all installed workloads
dotnet workload update
```

## Available Tools

All tools include **visual icons** for improved discoverability in AI assistant interfaces. Icons use [Microsoft Fluent UI Emoji](https://github.com/microsoft/fluentui-emoji) for consistent visual representation (📁 projects, 📦 packages, ⚙️ SDK, 🛠️ tools, etc.).

The server provides comprehensive .NET development capabilities through **consolidated MCP tools** that group related operations by domain, making it easier for AI assistants to select the right tool and compose workflows.

> **Note**: Domain tools group related operations through an `action` parameter. Additive, restricted query companions let clients distinguish verified read-only requests using standard MCP annotations; all existing consolidated calls remain supported.

### Read-Only Query Tools

Prefer these tools for qualifying queries:

| Tool | Allowed actions | Parameters besides `action` |
|------|-----------------|-----------------------------|
| `dotnet_sdk_query` | `Version`, `Info`, `ListSdks`, `ListRuntimes`, `FrameworkInfo`, `CacheMetrics` | Optional `framework`, `workingDirectory` |
| `dotnet_project_query` | `Logs` | Required `sessionId`; optional `tailLines`, `since` |
| `dotnet_server_metrics_query` | `Get`, `TokenSavingsGet` | None |

These tools explicitly advertise read-only, idempotent, closed-world hints and reject other actions at runtime. The eight mixed domain tools retain conservative mutating/open-world hints, while metrics resets are mutating but closed-world. `dotnet_help` is also conservative because free-form help commands can invoke external tools or CLI initialization.

Not every `List`, `Info`, or `Search` action qualifies: CLI initialization can modify SDK/certificate state, EF discovery can execute application code, and project analysis evaluates MSBuild. Read-only is not synonymous with offline, and annotations are hints rather than permissions or a sandbox. See [annotation rationale and action eligibility](doc/tool-surface-consolidation.md#tool-annotations-and-read-only-query-contracts).

### MCP Resources (Read-Only Context)

The server exposes read-only resources that provide efficient access to .NET environment metadata:

- **dotnet://sdk-info** - Information about installed .NET SDKs (versions and paths)
- **dotnet://runtime-info** - Information about installed .NET runtimes (versions and types)
- **dotnet://templates** - Complete catalog of installed .NET templates with metadata
- **dotnet://frameworks** - Information about supported .NET frameworks (TFMs) including LTS status
- **dotnet://workspace** - Workspace snapshot with solution, projects, target frameworks, and package counts

Resources provide structured JSON data and are more efficient than tool calls for frequently accessed read-only information.

### Consolidated Tools

**These tools group related operations using action enums, providing better AI orchestration and clearer semantic organization.**

#### dotnet_project - Project Lifecycle Management

Unified interface for all project operations: **New**, **Restore**, **Build**, **Run**, **Test**, **Publish**, **Clean**, **Analyze**, **Dependencies**, **Validate**, **Pack**, **Watch**, **Format**, **Stop**, **Logs**

**New in this release:**

- **Logs action**: Retrieve stdout/stderr logs from background process sessions
- **Background mode for Run**: Start processes in the background with `startMode: "Background"` and retrieve logs via session ID
- **Stop action**: Terminates long-running process sessions (like `dotnet run`) by session ID
- **noBuild parameter**: Skip building before running (mirrors `dotnet run --no-build`)

**Test Runner Compatibility**: In Auto mode, the Test action honors `DOTNET_TEST_RUNNER` on .NET SDK 11+ before reading `global.json`. Recognized values are `Microsoft.Testing.Platform` and `VSTest`, case-insensitively; empty or unknown values fall through to `global.json`. MTP uses `--project`; VSTest uses a positional project argument. An explicit `testRunner` (or the deprecated legacy selector) takes precedence; on SDK 11+, the matching runner is set only in the test child process, without changing the server environment or `global.json`.

You can explicitly specify the test runner using the `testRunner` parameter:

- `Auto` (default) - Auto-detect from SDK-supported environment settings, then global.json
- `MicrosoftTestingPlatform` - Use --project flag (requires .NET SDK 10+ and MTP 1.7+)
- `VSTest` - Use positional argument (compatible with all SDK versions)

The `useLegacyProjectArgument` parameter is deprecated; use `testRunner: "VSTest"` instead.

**.NET 11 opt-ins:** Run accepts an `environmentVariables` string map, serialized as repeated `--environment` options in both foreground and background modes. Empty values, spaces, quotes, and embedded `=` are supported; names cannot contain `=` or control characters, and values cannot contain control characters. Format honors the existing `framework` parameter to select the target framework to analyze. These opt-ins require SDK 11+ in the command's working directory, regardless of the server's runtime.

```typescript
await callTool("dotnet_project", {
  action: "Run", project: "MyApi/MyApi.csproj",
  environmentVariables: { ASPNETCORE_ENVIRONMENT: "Development", LOG_LEVEL: "Debug" }
});
await callTool("dotnet_project", {
  action: "Format", project: "MyApi/MyApi.csproj", framework: "net10.0"
});
```

Example:

```typescript
// Create a new web API project
await callTool("dotnet_project", { 
  action: "New", 
  template: "webapi", 
  name: "MyApi" 
});

// Build the project
await callTool("dotnet_project", { 
  action: "Build", 
  project: "MyApi/MyApi.csproj", 
  configuration: "Release" 
});

// Run in background mode to get session ID
const runResult = await callTool("dotnet_project", {
  action: "Run",
  project: "MyApi/MyApi.csproj",
  noBuild: true,
  startMode: "Background",
  machineReadable: true
});
const sessionId = runResult.metadata.sessionId;

// Wait a moment for the application to start...

// Retrieve logs from the background session without invoking a mutating tool
await callTool("dotnet_project_query", {
  action: "Logs",
  sessionId: sessionId
});

// Retrieve only the last 50 lines of logs
await callTool("dotnet_project_query", {
  action: "Logs",
  sessionId: sessionId,
  tailLines: 50
});

// Retrieve logs since a specific timestamp
await callTool("dotnet_project_query", {
  action: "Logs",
  sessionId: sessionId,
  since: "2024-01-01T12:00:00Z"
});

// Stop the background process
await callTool("dotnet_project", {
  action: "Stop",
  sessionId: sessionId
});

// Run tests (auto-detects test runner from global.json)
await callTool("dotnet_project", { 
  action: "Test", 
  project: "MyApi.Tests/MyApi.Tests.csproj" 
});

// Run tests with explicit test runner (MTP)
await callTool("dotnet_project", { 
  action: "Test", 
  project: "MyApi.Tests/MyApi.Tests.csproj",
  testRunner: "MicrosoftTestingPlatform"
});

// Run tests with explicit test runner (VSTest)
await callTool("dotnet_project", { 
  action: "Test", 
  project: "MyApi.Tests/MyApi.Tests.csproj",
  testRunner: "VSTest"
});
```

#### dotnet_package - Package and Reference Management

Manage NuGet packages and project references: **Add**, **Remove**, **Search**, **Update**, **List**, **AddReference**, **RemoveReference**, **ListReferences**, **ClearCache**

Example:

```typescript
// Search for a package
await callTool("dotnet_package", { 
  action: "Search", 
  searchTerm: "serilog" 
});

// Add package to project
await callTool("dotnet_package", { 
  action: "Add", 
  packageId: "Serilog.AspNetCore", 
  project: "MyApi/MyApi.csproj" 
});
```

#### dotnet_solution - Solution File Management

Supports solution creation and project membership through `dotnet sln` / `dotnet solution`.

Manage solution files and project membership: **Create**, **Add**, **List**, **Remove**

SDK 11+ also supports `format: "slnf"` with a required `parentSolution` pointing to an existing `.sln` or `.slnx`. Relative parent paths are resolved from the filter's output directory. Add/List/Remove require an explicit `.slnf` path; filters are not added to automatic solution discovery. The default Create format remains `.sln`. Analyze/Dependencies/Validate require the parent solution, not a filter.

```typescript
await callTool("dotnet_solution", {
  action: "Create", name: "Tests", output: "filters", format: "slnf",
  parentSolution: "../MyApp.slnx"
});
await callTool("dotnet_solution", {
  action: "Add", solution: "filters/Tests.slnf", projects: ["MyApp.Tests/MyApp.Tests.csproj"]
});
```

Examples:

```typescript
// Create a solution (classic .sln format)
await callTool("dotnet_solution", { 
  action: "Create", 
  name: "MyApp"
});

// Create a solution (new .slnx XML format)
await callTool("dotnet_solution", { 
  action: "Create", 
  name: "MyApp", 
  format: "slnx" 
});

// Add projects to solution
await callTool("dotnet_solution", { 
  action: "Add", 
  solution: "MyApp.slnx", 
  projects: ["MyApi/MyApi.csproj", "MyWeb/MyWeb.csproj"] 
});

// List projects in solution
await callTool("dotnet_solution", { 
  action: "List", 
  solution: "MyApp.slnx"
});

// Remove projects from solution
await callTool("dotnet_solution", { 
  action: "Remove", 
  solution: "MyApp.slnx", 
  projects: ["MyWeb/MyWeb.csproj"] 
});
```

#### dotnet_ef - Entity Framework Core Operations

Database migrations, DbContext management, and scaffolding: **MigrationsAdd**, **MigrationsList**, **MigrationsRemove**, **MigrationsScript**, **DatabaseUpdate**, **DatabaseDrop**, **DbContextList**, **DbContextInfo**, **DbContextScaffold**

Example:

```typescript
// Create a migration
await callTool("dotnet_ef", { 
  action: "MigrationsAdd", 
  name: "InitialCreate", 
  project: "MyApi/MyApi.csproj" 
});

// Update database
await callTool("dotnet_ef", { 
  action: "DatabaseUpdate", 
  project: "MyApi/MyApi.csproj" 
});
```

#### dotnet_workload - Workload Management

Install and manage .NET workloads (MAUI, WASM, etc.): **List**, **Info**, **Search**, **Install**, **Update**, **Uninstall**

Example:

```typescript
// Search for workloads
await callTool("dotnet_workload", { 
  action: "Search", 
  searchTerm: "maui" 
});

// Install workloads
await callTool("dotnet_workload", { 
  action: "Install", 
  workloadIds: "maui-android,maui-ios" 
});
```

#### dotnet_tool - .NET Tool Management

Manage global and local .NET tools: **Install**, **List**, **Update**, **Uninstall**, **Restore**, **CreateManifest**, **Search**, **Run**

Example:

```typescript
// Install a tool globally
await callTool("dotnet_tool", { 
  action: "Install", 
  packageId: "dotnet-ef", 
  global: true 
});

// Search for tools
await callTool("dotnet_tool", { 
  action: "Search", 
  searchTerm: "format" 
});
```

#### dotnet_sdk - SDK and Template Information

Query SDK, runtime, template, and framework information: **Version**, **Info**, **ListSdks**, **ListRuntimes**, **ListTemplates**, **SearchTemplates**, **TemplateInfo**, **ListTemplatePacks**, **InstallTemplatePack**, **UninstallTemplatePack**, **ClearTemplateCache**, **FrameworkInfo**, **CacheMetrics**

Example:

```typescript
// Get SDK version
await callTool("dotnet_sdk", { 
  action: "Version" 
});

// List available templates
await callTool("dotnet_sdk", { 
  action: "ListTemplates" 
});

// Search for templates
await callTool("dotnet_sdk", { 
  action: "SearchTemplates", 
  searchTerm: "web" 
});

// List installed template packs
await callTool("dotnet_sdk", {
  action: "ListTemplatePacks"
});

// Install a template pack (NuGet package ID + version)
await callTool("dotnet_sdk", {
  action: "InstallTemplatePack",
  templatePackage: "Aspire.ProjectTemplates",
  templateVersion: "13.1.0"
});

// Uninstall a template pack
await callTool("dotnet_sdk", {
  action: "UninstallTemplatePack",
  templatePackage: "Aspire.ProjectTemplates"
});
```

On SDK 11+, `InstallTemplatePack` accepts `prerelease: true` to include preview versions when selecting the latest template package. An explicit `templateVersion` or inline `Package@version` still selects that exact version.

The existing Project New `additionalOptions` supports the SDK 11 test templates without extra tool parameters:

```typescript
await callTool("dotnet_project", {
  action: "New", template: "xunit", name: "MyTests", framework: "net11.0",
  additionalOptions: "--xunit-version v3"
});
await callTool("dotnet_project", {
  action: "New", template: "nunit", name: "MyNUnitTests", framework: "net11.0",
  additionalOptions: "--test-runner Microsoft.Testing.Platform"
});
```

The SDK's MTP template post-actions can update the applicable `global.json` to select `Microsoft.Testing.Platform`, even with `--no-restore`; inspect that change before running other test projects. The pinned RC3 templates reject these MTP options with an explicit `net10.0` target despite advertising it in help, so the examples opt in to `net11.0`. Ordinary project creation keeps the server's .NET 10 LTS default.

#### dotnet_dev_certs - Developer Certificates and Secrets

Manage HTTPS certificates and user secrets: **CertificateTrust**, **CertificateCheck**, **CertificateClean**, **CertificateExport**, **SecretsInit**, **SecretsSet**, **SecretsList**, **SecretsRemove**, **SecretsClear**

Example:

```typescript
// Trust development certificate
await callTool("dotnet_dev_certs", { 
  action: "CertificateTrust" 
});

// Set a user secret
await callTool("dotnet_dev_certs", { 
  action: "SecretsSet", 
  key: "ConnectionStrings:DefaultConnection", 
  value: "Server=localhost;Database=MyDb", 
  project: "MyApi/MyApi.csproj" 
});
```

### Utility Tools

- **dotnet_help** - Get help for any dotnet command
- **dotnet_server_capabilities** - Get MCP server capabilities and concurrency guidance

## Building from Source

For development or contributing:

Install the RC3 baseline SDK from `global.json` or a GA/servicing SDK in the 11.0.1xx feature band. The `latestPatch` SDK policy automatically selects the latest compatible installed SDK without crossing feature bands or major versions; CI installs the selected release automatically. The repository's `NuGet.Config` adds Microsoft's public `dotnet-tools` feed because Template Engine RC3 depends on NuGet `8.0.0-rc.2448` packages that are not yet on NuGet.org. Retain your normal NuGet.org source for the other dependencies; this extra feed is needed to build, not to install the tool's bundled dependencies. The tool still requires a compatible .NET runtime.

```bash
git clone https://github.com/jongalloway/dotnet-mcp.git
cd dotnet-mcp
dotnet build "$PWD/DotNetMcp/DotNetMcp.csproj"
```

**Run the server**:

```bash
dotnet run --framework net11.0 --project DotNetMcp/DotNetMcp.csproj
```

Choose `--framework net10.0` instead to run the .NET 10 build. `dotnet run` requires an explicit framework for this multi-targeted project. Package consumers do not need the RC3 build SDK to run the .NET 10 assets.

The server communicates via stdio transport and is designed to be invoked by MCP clients.

## Project Structure

This project has grown beyond the point where a complete file-by-file listing stays useful.
Instead, this section highlights the key entry points and the main areas of the repo.

```text
dotnet-mcp/
├── DotNetMcp/                  # Main MCP server project (packed as a .NET tool)
│   ├── Program.cs              # Hosting + MCP server wiring
│   ├── DotNetMcp.csproj        # NuGet/package metadata (PackAsTool, server.json packing)
│   ├── .mcp/server.json        # MCP server metadata (packed into the NuGet package)
│   ├── Resources/              # MCP resources (SDK/runtime/templates/frameworks)
│   ├── DotNetCliTools.cs       # Tool surface area (split into partials in Tools/)
│   └── Tools/                  # Tool implementations grouped by domain (project, package, EF, etc.)
├── DotNetMcp.Tests/            # Unit + integration tests
├── doc/                        # Long-form documentation (architecture, concurrency, testing, etc.)
├── scripts/                    # Maintenance & validation scripts
├── artifacts*/                 # Build outputs (CI + local)
├── .github/                    # CI workflows and repo automation
├── DotNetMcp.slnx              # Solution file (.slnx)
├── global.json                 # SDK pinning for consistent builds
├── .config/dotnet-tools.json   # Local tool manifest (dotnet local tools)
└── LICENSE                     # MIT License
```

Key files to start with:

- `DotNetMcp/Program.cs` - server startup and registration
- `DotNetMcp/DotNetCliTools.cs` and `DotNetMcp/Tools/` - MCP tool implementations
- `DotNetMcp/Resources/DotNetResources.cs` - read-only MCP resources
- `DotNetMcp/.mcp/server.json` - packaged MCP server metadata
- `DotNetMcp.Tests/` - tests (including server.json validation and XML doc coverage)

## Technology Stack

- **Protocol**: [Model Context Protocol (MCP)](https://modelcontextprotocol.io/)
- **SDK**: [MCP SDK for .NET](https://github.com/modelcontextprotocol/csharp-sdk) v2.3.0, with matching Tasks and Apps extensions
- **Runtime**: .NET 10.0 and .NET 11.0 RC3 (dual-targeted tool)
- **Transport**: stdio (standard input/output)
- **NuGet Packages**:
  - `Microsoft.TemplateEngine.Abstractions` & `Edge` (v10.0.403 for .NET 10; v11.0.100-rc.3.26522.148 for .NET 11) - Template metadata
  - `Microsoft.Build.Utilities.Core` & `Microsoft.Build` (v18.9.6) - Project validation

## Documentation

- 📖 [CHANGELOG](CHANGELOG.md) - **Version history and release notes**
- 📖 [AI Assistant Best Practices Guide](doc/ai-assistant-guide.md) - **Workflows, prompts, integration patterns, and troubleshooting**
- 📖 [Machine-Readable JSON Contract](doc/machine-readable-contract.md) - **v1.0 stable contract for programmatic tool consumption**
- 📖 [Tool Surface Consolidation](doc/tool-surface-consolidation.md) - **Consolidated tool design and architecture**
- 📖 [Telemetry and Observability](doc/telemetry.md) - **Request duration logging, OpenTelemetry integration, and performance monitoring**
- 📖 [SDK Integration Details](doc/sdk-integration.md) - Technical architecture and SDK usage
- 📖 [Advanced Topics](doc/advanced-topics.md) - Performance, logging, and security details
- 📖 [Releasing](doc/releasing.md) - How to cut a release (checklists + scripts)
- 📖 [Concurrency Safety](doc/concurrency.md) - Parallel execution guidance for AI orchestrators
- 📖 [Testing](doc/testing.md) - How to run tests (including opt-in interactive tests)
- 📖 [Model Context Protocol](https://modelcontextprotocol.io/) - Official MCP specification
- 📖 [MCP C# SDK Docs](https://csharp.sdk.modelcontextprotocol.io/) - SDK documentation

## Interoperability

The .NET MCP Server follows the Model Context Protocol specification and provides rich metadata for tool discovery and AI orchestration:

### Server Metadata

The server includes an MCP Registry `server.json` configuration file (`.mcp/server.json`) that provides:

- **Environment Variables**: Optimized .NET CLI settings for MCP usage
  - `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1` - Skips first-time setup
  - `DOTNET_NOLOGO=1` - Suppresses startup messages
  
- **Package Information**: NuGet package details for easy installation via `dnx` (requires .NET 10)
  - Package: `Community.Mcp.DotNet`
  - Registry: NuGet.org
  - Transport: stdio

- **Repository Information**: Links back to the source repo

Tool and resource metadata is discovered at runtime via the MCP protocol (tool/resource listing), using XML docs and `[McpMeta]` attributes in the server code.

### Tool Namespacing

All tools use the `dotnet_` prefix in their external IDs to prevent naming collisions with other MCP servers. This follows best practices for MCP server interoperability:

**Examples:**

- `dotnet_project` - Project lifecycle operations (create/build/test/run/publish)
- `dotnet_package` - NuGet packages and project references
- `dotnet_solution` - Solution operations
- `dotnet_sdk` - SDK/runtime/template/framework metadata

### Discovery Metadata

Tool discovery metadata (categories, tags, commonly-used hints) is provided via `[McpMeta]` attributes and surfaced through MCP tool listing.

### Metadata Access

AI orchestrators can access server metadata through:

1. **MCP Protocol**: Standard tool/resource listing via MCP SDK
2. **server.json**: Static metadata file for registration and discovery
3. **Tool Attributes**: Runtime metadata via `McpMeta` attributes in code

For detailed integration examples, see the [MCP specification](https://modelcontextprotocol.io/) and our [SDK integration documentation](doc/sdk-integration.md).

## Contributing

Contributions are welcome! This is a community-maintained project.

**Ways to contribute**:

- 🐛 Report bugs or request features via [GitHub Issues](https://github.com/jongalloway/dotnet-mcp/issues)
- 💡 Submit pull requests for new tools or improvements
- 📝 Improve documentation
- ⭐ Star the repo to show support

**Development setup**:

1. Fork the repository
2. Clone your fork
3. Create a feature branch
4. Make your changes
5. Test thoroughly
6. Submit a pull request

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for development guidelines.

## Troubleshooting

### "dnx not found"

- **Cause**: .NET 10 SDK not installed
- **Solution**: Install the [.NET 10 or .NET 11 SDK](https://dotnet.microsoft.com/download). Source-based manual configuration additionally requires an SDK compatible with the RC3 minimum in `global.json` and an explicit `--framework`.

### "No templates found"

- **Cause**: .NET SDK templates not installed
- **Solution**: Run `dotnet new --install` to install default templates

### "Server not responding"

- **Cause**: Server crashed or failed to start
- **Solution**: Check logs in your MCP client, ensure .NET SDK is in PATH

### "Unrecognized option '--project'" or "Unknown switch" when running tests

- **Cause**: Test runner mismatch or SDK version incompatibility
- **Background**: The `--project` flag is only supported by [Microsoft Testing Platform (MTP)](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp). The legacy [VSTest](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-vstest) runner uses positional arguments instead.
- **Solution**: The MCP server now auto-detects the test runner from `global.json`:
  1. **Auto mode** (default): On SDK 11+, recognized `DOTNET_TEST_RUNNER` values override `global.json`; otherwise add `{ "test": { "runner": "Microsoft.Testing.Platform" } }` to `global.json` for MTP. With neither setting, the default is VSTest.
  2. **Explicit mode**: Set `testRunner: "MicrosoftTestingPlatform"` or `testRunner: "VSTest"` when calling `dotnet_project` Test action
  3. **Legacy parameter**: Use `useLegacyProjectArgument: true` (deprecated, use `testRunner: "VSTest"` instead)
- **Verify support**: Run `dotnet test --help | grep -- --project` to check if your SDK supports the `--project` flag
- **More info**: See [doc/testing.md](doc/testing.md) for detailed test runner compatibility and troubleshooting
- **References**: [dotnet test overview](https://learn.microsoft.com/dotnet/core/tools/dotnet-test) | [MTP](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp) | [VSTest](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-vstest)

### Need Help?

- 📖 Check the [documentation](#documentation)
- 💬 Open a [GitHub Issue](https://github.com/jongalloway/dotnet-mcp/issues)
- 🔍 Search [existing issues](https://github.com/jongalloway/dotnet-mcp/issues?q=is%3Aissue)

## Related Microsoft MCPs

This .NET MCP server focuses on .NET SDK operations (build, run, test, templates, SDK management). For specialized scenarios, consider these complementary official Microsoft MCP servers:

- **[NuGet MCP Server](https://www.nuget.org/packages/NuGet.Mcp.Server)** - Advanced NuGet package search, metadata, and automation scenarios beyond basic package management
- **[Aspire MCP Server](https://aspire.dev/dashboard/mcp-server/)** - Runtime monitoring, telemetry, distributed tracing, and resource management for .NET Aspire applications

These MCPs work alongside the .NET MCP to provide comprehensive coverage of the .NET development lifecycle:

| Feature | .NET MCP | NuGet MCP | Aspire MCP |
| ------- | -------- | --------- | ---------- |
| **Primary Focus** | .NET SDK operations | Package metadata/discovery | Runtime monitoring |
| **Scope** | CLI commands (build, run, test) | NuGet search & automation | Aspire app telemetry |
| **Stage** | Development time | Development/discovery time | Runtime/production |
| **Example Operations** | `dotnet build`, `dotnet new` | Package search, READMEs | Log viewing, tracing |

## License

MIT License - see [LICENSE](LICENSE) file for details.
