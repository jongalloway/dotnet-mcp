# .NET MCP Server

[![Build and Test](https://github.com/jongalloway/dotnet-mcp/actions/workflows/build.yml/badge.svg)](https://github.com/jongalloway/dotnet-mcp/actions/workflows/build.yml)
[![Dependabot](https://img.shields.io/badge/Dependabot-enabled-blue.svg)](https://github.com/jongalloway/dotnet-mcp/blob/main/.github/dependabot.yml)
[![NuGet](https://img.shields.io/nuget/v/Community.Mcp.DotNet.svg)](https://www.nuget.org/packages/Community.Mcp.DotNet/)

[![VS Code - Install .NET MCP](https://img.shields.io/badge/VS_Code-Install_.NET_MCP-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://vscode.dev/redirect/mcp/install?name=dotnet-mcp&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dotnet%22%2C%22args%22%3A%5B%22run%22%2C%22--project%22%2C%22%2Fpath%2Fto%2Fdotnet-mcp%2FDotNetMcp%2FDotNetMcp.csproj%22%5D%7D)
[![VS Code Insiders - Install .NET MCP](https://img.shields.io/badge/VS_Code_Insiders-Install_.NET_MCP-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://insiders.vscode.dev/redirect/mcp/install?name=dotnet-mcp&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dotnet%22%2C%22args%22%3A%5B%22run%22%2C%22--project%22%2C%22%2Fpath%2Fto%2Fdotnet-mcp%2FDotNetMcp%2FDotNetMcp.csproj%22%5D%7D&quality=insiders)
[![Visual Studio - Install .NET MCP](https://img.shields.io/badge/Visual_Studio-Install_.NET_MCP-5C2D91?style=flat-square&logo=visualstudio&logoColor=white)](https://vs-open.link/mcp-install)

> **Note**: The install badges above will prompt you to configure the server. You'll need to update the project path to match your local installation location.

A community-maintained MCP (Model Context Protocol) server that provides AI assistants with direct access to the .NET SDK. The server integrates with the .NET SDK through both official NuGet packages and CLI execution, enabling AI assistants to help with .NET development tasks.

## Features

The server provides comprehensive .NET development capabilities through MCP tools:

### Template & Framework Information

- **dotnet_template_list** - List all installed .NET templates with metadata
- **dotnet_template_search** - Search for templates by name or description
- **dotnet_template_info** - Get detailed template information and parameters
- **dotnet_framework_info** - Get .NET framework version information and LTS status

### Project Management

- **dotnet_project_new** - Create new .NET projects from templates
- **dotnet_project_restore** - Restore project dependencies
- **dotnet_project_build** - Build .NET projects
- **dotnet_project_run** - Build and run .NET projects
- **dotnet_project_test** - Run unit tests
- **dotnet_project_publish** - Publish projects for deployment
- **dotnet_project_clean** - Clean build outputs

### Package Management

- **dotnet_package_add** - Add NuGet package references
- **dotnet_package_list** - List package references (including outdated/deprecated)
- **dotnet_reference_add** - Add project-to-project references
- **dotnet_reference_list** - List project references

### SDK Information

- **dotnet_sdk_version** - Get .NET SDK version
- **dotnet_sdk_info** - Get detailed SDK and runtime information
- **dotnet_sdk_list** - List installed SDKs
- **dotnet_runtime_list** - List installed runtimes

### Help

- **dotnet_help** - Get help for any dotnet command

## Architecture

The server uses a hybrid approach:

- **SDK Integration** - Uses official Microsoft NuGet packages (Template Engine, MSBuild) for metadata, discovery, and validation
- **CLI Execution** - Executes actual dotnet commands for reliable, proven operations

For detailed information about SDK integration, see [doc/sdk-integration.md](doc/sdk-integration.md).

## Requirements

- .NET 9.0 SDK or later for building
- .NET 10.0 SDK or later for DNX support (launching November 2025)
- The MCP server uses stdio transport for communication

## Installation

### Using DNX (Recommended - .NET 10 Required)

> **Note**: DNX support requires .NET 10, which launches in November 2025. Until then, use the source build method below.

The MCP server can be executed directly using `dnx` (introduced in .NET 10 Preview 6):

```bash
dnx Community.Mcp.DotNet@<latest-version> --yes
```

This will download the package from NuGet.org and execute it. Your MCP client will typically invoke this command automatically via MCP configuration.

### Building from Source

```bash
cd DotNetMcp
dotnet build
```

## Running

The server is designed to be run as an MCP server using stdio transport:

```bash
cd DotNetMcp
dotnet run
```

## Using with Visual Studio Code

To use this MCP server with GitHub Copilot in Visual Studio Code:

### VS Code: Using DNX (Recommended - .NET 10 Required)

1. Open the Command Palette (`Ctrl+Shift+P` or `Cmd+Shift+P` on macOS)
2. Run the command **"GitHub Copilot: Add MCP Server"**
3. Enter the following configuration:
   - **Name**: `dotnet`
   - **Type**: `stdio`
   - **Command**: `dnx`
   - **Arguments**: `Community.Mcp.DotNet@1.0.0 --yes`

Or manually edit your VS Code settings and add:

```json
{
  "github.copilot.chat.mcp.servers": {
    "dotnet": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Community.Mcp.DotNet@1.0.0", "--yes"]
    }
  }
}
```

### VS Code: Using Source Build

If you're running from source:

1. Open the Command Palette (`Ctrl+Shift+P` or `Cmd+Shift+P` on macOS)
2. Run the command **"GitHub Copilot: Add MCP Server"**
3. Enter the following configuration:
   - **Name**: `dotnet`
   - **Type**: `stdio`
   - **Command**: `dotenv`
   - **Arguments**: `run --project /path/to/dotnet-mcp/DotNetMcp/DotNetMcp.csproj`

Alternatively, you can manually edit your VS Code settings by opening Settings (`Ctrl+,` or `Cmd+,`), searching for "mcp", and adding the server configuration to the `github.copilot.chat.mcp.servers` setting:

```json
{
  "github.copilot.chat.mcp.servers": {
    "dotnet": {
      "type": "stdio",
      "command": "dotenv",
      "args": ["run", "--project", "/path/to/dotnet-mcp/DotNetMcp/DotNetMcp.csproj"]
    }
  }
}
```

For more information, see the [VS Code MCP documentation](https://code.visualstudio.com/docs/copilot/customization/mcp-servers#_add-an-mcp-server).

## Using with Visual Studio

To use this MCP server with GitHub Copilot in Visual Studio 2022:

### Visual Studio: Using DNX (Recommended - .NET 10 Required)

1. Ensure you have Visual Studio 2022 version 17.13 or later
2. Go to **Tools** > **Options** > **GitHub Copilot** > **MCP Servers**
3. Click **Add** to add a new MCP server
4. Enter the following configuration:
   - **Name**: `dotnet`
   - **Type**: `stdio`
   - **Command**: `dnx`
   - **Arguments**: `Community.Mcp.DotNet@1.0.0 --yes`

### Visual Studio: Using Source Build

If you're running from source:

1. Ensure you have Visual Studio 2022 version 17.13 or later
2. Go to **Tools** > **Options** > **GitHub Copilot** > **MCP Servers**
3. Click **Add** to add a new MCP server
4. Enter the following configuration:
   - **Name**: `dotnet`
   - **Type**: `stdio`
   - **Command**: `dotenv`
   - **Arguments**: `run --project C:\path\to\dotnet-mcp\DotNetMcp\DotNetMcp.csproj`

For more information, see the [Visual Studio MCP documentation](https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=vs-2022).

## Using with Claude Desktop

### Claude Desktop: Using DNX (Recommended - .NET 10 Required)

Add the following to your Claude Desktop configuration file:

#### macOS (DNX)

Edit `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "dotnet": {
      "command": "dnx",
      "args": ["Community.Mcp.DotNet@1.0.0", "--yes"]
    }
  }
}
```

#### Windows (DNX)

Edit `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "dotnet": {
      "command": "dnx",
      "args": ["Community.Mcp.DotNet@1.0.0", "--yes"]
    }
  }
}
```

### Claude Desktop: Using Source Build

If you're running from source, add the following to your Claude Desktop configuration file:

#### macOS (Source Build)

Edit `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "dotnet": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/dotnet-mcp/DotNetMcp/DotNetMcp.csproj"]
    }
  }
}
```

#### Windows (Source Build)

Edit `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "dotnet": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\dotnet-mcp\\DotNetMcp\\DotNetMcp.csproj"]
    }
  }
}
```

## Example Usage

Once connected to your AI assistant, you can ask questions like:

- "Create a new console application called MyApp"
- "What templates are available for web development?"
- "Add the Newtonsoft.Json package to my project"
- "Build my project in Release configuration"
- "Run the tests in my solution"
- "What version of .NET SDK is installed?"
- "Which .NET versions are LTS releases?"

## Documentation

- [SDK Integration Details](doc/sdk-integration.md) - How the server integrates with .NET SDK internals

## Project Structure

```text
dotnet-mcp/
├── DotNetMcp/
│   ├── DotNetMcp.csproj           # Project file
│   ├── Program.cs                  # MCP server setup
│   ├── DotNetCliTools.cs           # MCP tool implementations
│   ├── DotNetSdkConstants.cs       # Strongly-typed SDK constants
│   ├── TemplateEngineHelper.cs     # Template Engine integration
│   └── FrameworkHelper.cs          # Framework validation helpers
├── doc/
│   └── sdk-integration.md          # SDK integration documentation
├── .github/
│   ├── copilot-instructions.md     # Copilot development guidelines
│   └── workflows/
│       └── build.yml               # CI/CD workflow
└── README.md                       # This file
```

## Technology

- Built with [Model Context Protocol SDK for .NET](https://github.com/modelcontextprotocol/csharp-sdk)
- Uses .NET 9.0
- Integrates with .NET SDK via official NuGet packages:
  - Microsoft.TemplateEngine.Abstractions & Edge
  - Microsoft.Build.Utilities.Core
  - Microsoft.Build
- Implements stdio transport for communication

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

See [LICENSE](LICENSE) file for details.
