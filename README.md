# .NET MCP Server

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

Once connected to Claude Desktop, you can ask questions like:

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
