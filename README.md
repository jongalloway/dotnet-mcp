# .NET MCP Server

[![Build and Test](https://github.com/jongalloway/dotnet-mcp/actions/workflows/build.yml/badge.svg)](https://github.com/jongalloway/dotnet-mcp/actions/workflows/build.yml)

<a href="https://vscode.dev/redirect/mcp/install?name=dotnet-mcp&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dotnet%22%2C%22args%22%3A%5B%22run%22%2C%22--project%22%2C%22%2Fpath%2Fto%2Fdotnet-mcp%2FDotNetMcp%2FDotNetMcp.csproj%22%5D%7D"><img src="https://img.shields.io/badge/VS_Code-Install_.NET_MCP-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white"></a>
<a href="https://insiders.vscode.dev/redirect/mcp/install?name=dotnet-mcp&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dotnet%22%2C%22args%22%3A%5B%22run%22%2C%22--project%22%2C%22%2Fpath%2Fto%2Fdotnet-mcp%2FDotNetMcp%2FDotNetMcp.csproj%22%5D%7D&quality=insiders"><img src="https://img.shields.io/badge/VS_Code_Insiders-Install_.NET_MCP-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white"></a>
<a href="https://vs-open.link/mcp-install"><img src="https://img.shields.io/badge/Visual_Studio-Install_.NET_MCP-5C2D91?style=flat-square&logo=visualstudio&logoColor=white"></a>

> **Note**: The install badges above will prompt you to configure the server. You'll need to update the project path to match your local installation location.

An MCP (Model Context Protocol) server that provides tools for interacting with the .NET SDK CLI commands. This allows AI assistants to help with .NET development tasks through a standardized protocol.

## Features

This MCP server exposes the following .NET CLI commands as tools:

### Project Management
- **dotnet_new** - Create new .NET projects from templates (console, classlib, webapi, etc.)
- **dotnet_restore** - Restore project dependencies
- **dotnet_build** - Build .NET projects
- **dotnet_run** - Build and run .NET projects
- **dotnet_test** - Run unit tests
- **dotnet_publish** - Publish projects for deployment
- **dotnet_clean** - Clean build outputs

### Package Management
- **dotnet_add_package** - Add NuGet package references
- **dotnet_list_packages** - List package references (including outdated/deprecated)
- **dotnet_add_reference** - Add project-to-project references
- **dotnet_list_references** - List project references

### SDK Information
- **dotnet_version** - Get .NET SDK version
- **dotnet_info** - Get detailed SDK and runtime information
- **dotnet_list_sdks** - List installed SDKs
- **dotnet_list_runtimes** - List installed runtimes

## Requirements

- .NET 9.0 SDK or later
- The MCP server uses stdio transport for communication

## Building

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

1. Open the Command Palette (`Ctrl+Shift+P` or `Cmd+Shift+P` on macOS)
2. Run the command **"GitHub Copilot: Add MCP Server"**
3. Enter the following configuration:
   - **Name**: `dotnet`
   - **Type**: `stdio`
   - **Command**: `dotnet`
   - **Arguments**: `run --project /path/to/dotnet-mcp/DotNetMcp/DotNetMcp.csproj`

Alternatively, you can manually edit your VS Code settings by opening Settings (`Ctrl+,` or `Cmd+,`), searching for "mcp", and adding the server configuration to the `github.copilot.chat.mcp.servers` setting:

```json
{
  "github.copilot.chat.mcp.servers": {
    "dotnet": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/dotnet-mcp/DotNetMcp/DotNetMcp.csproj"]
    }
  }
}
```

For more information, see the [VS Code MCP documentation](https://code.visualstudio.com/docs/copilot/customization/mcp-servers#_add-an-mcp-server).

## Using with Visual Studio

To use this MCP server with GitHub Copilot in Visual Studio 2022:

1. Ensure you have Visual Studio 2022 version 17.13 or later
2. Go to **Tools** > **Options** > **GitHub Copilot** > **MCP Servers**
3. Click **Add** to add a new MCP server
4. Enter the following configuration:
   - **Name**: `dotnet`
   - **Type**: `stdio`
   - **Command**: `dotnet`
   - **Arguments**: `run --project C:\path\to\dotnet-mcp\DotNetMcp\DotNetMcp.csproj`

For more information, see the [Visual Studio MCP documentation](https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=vs-2022).

## Using with Claude Desktop

Add the following to your Claude Desktop configuration file:

### macOS
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

### Windows
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

Once connected to your AI assistant (Visual Studio Code, Visual Studio, or Claude Desktop), you can ask questions like:

- "Create a new console application called MyApp"
- "Add the Newtonsoft.Json package to my project"
- "Build my project in Release configuration"
- "Run the tests in my solution"
- "What version of .NET SDK is installed?"
- "List all the .NET runtimes installed on my system"

## Project Structure

```
dotnet-mcp/
├── DotNetMcp/
│   ├── DotNetMcp.csproj       # Project file
│   ├── Program.cs              # MCP server setup
│   └── DotNetCliTools.cs       # Tool implementations
├── .github/
│   └── workflows/
│       └── build.yml           # CI/CD workflow
└── README.md                   # This file
```

## Technology

- Built with [Model Context Protocol SDK for .NET](https://github.com/modelcontextprotocol/csharp-sdk)
- Uses .NET 9.0
- Implements stdio transport for communication

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

See [LICENSE](LICENSE) file for details.
