# GitHub Copilot Setup Instructions

This document provides instructions for setting up and working with this .NET MCP server project using GitHub Copilot.

## Prerequisites

Before you start working on this project, ensure you have:

1. **.NET 9.0 SDK** installed (check with `dotnet --version`)
2. **GitHub Copilot** extension installed in your IDE (VS Code, Visual Studio, or JetBrains)
3. **Git** installed and configured
4. Basic familiarity with C#, .NET CLI, and the Model Context Protocol

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/jongalloway/dotnet-mcp.git
cd dotnet-mcp
```

### 2. Build the Project

```bash
cd DotNetMcp
dotnet restore
dotnet build
```

### 3. Run the Server

```bash
dotnet run
```

The server will start and listen on stdio for MCP protocol messages.

## Working with GitHub Copilot

### Using Copilot for Code Generation

GitHub Copilot can help you:

1. **Add New Tools**: Start typing a method signature with a comment describing what you want, and Copilot will suggest the implementation
2. **Write Tests**: Type test method stubs and let Copilot suggest test implementations
3. **Documentation**: Start writing XML comments or markdown, and Copilot will complete them
4. **Refactoring**: Highlight code and ask Copilot Chat to refactor or optimize it

### Example Prompts for Copilot Chat

- "Add a new tool that runs dotnet format on a project"
- "Create a helper method to validate project paths before executing commands"
- "Generate XML documentation comments for all public methods"
- "Refactor the command building logic to be more maintainable"
- "Add error handling for common dotnet CLI failures"

### Copilot Instructions

This project includes custom Copilot instructions in `.github/copilot-instructions.md`. These help Copilot understand:

- Project structure and conventions
- Code style preferences
- Common patterns to follow
- How to add new tools properly

## Project Structure

```
dotnet-mcp/
├── DotNetMcp/                  # Main MCP server project
│   ├── DotNetMcp.csproj       # Project file with dependencies
│   ├── Program.cs              # Application entry point and MCP setup
│   └── DotNetCliTools.cs       # All .NET CLI tool implementations
├── .github/
│   ├── workflows/
│   │   └── build.yml           # CI/CD pipeline
│   └── copilot-instructions.md # Custom Copilot instructions
├── COPILOT_SETUP.md           # This file
└── README.md                   # Project documentation
```

## Common Development Tasks

### Adding a New .NET CLI Tool

1. Open `DotNetMcp/DotNetCliTools.cs`
2. Add a new method following the existing pattern:
   ```csharp
   [McpServerTool, Description("Your tool description")]
   public async Task<string> DotnetYourCommand(
       [Description("Parameter description")]
       string parameter)
   {
       var args = $"your-command {parameter}";
       return await ExecuteDotNetCommand(args);
   }
   ```
3. Build and test the new tool

### Testing Your Changes

1. **Build**: `dotnet build`
2. **Run**: `dotnet run`
3. **Test with Claude Desktop**: Configure the server in your Claude Desktop config and test through conversation
4. **Test with MCP Inspector**: Use the MCP Inspector tool for debugging

### Debugging

- Set breakpoints in your IDE
- Use `dotnet run` with a debugger attached
- Check stderr output for MCP protocol messages
- Use logging statements (they go to stderr automatically)

## Best Practices

1. **Keep Tools Simple**: Each tool should wrap a single dotnet CLI command
2. **Handle Errors Gracefully**: Capture both stdout and stderr
3. **Use Descriptive Names**: Tool and parameter names should be self-explanatory
4. **Test Thoroughly**: Try various parameter combinations
5. **Document Well**: Add clear descriptions to all tools and parameters
6. **Follow Conventions**: Check `.github/copilot-instructions.md` for coding standards

## Troubleshooting

### Build Errors

- Ensure .NET 9.0 SDK is installed
- Run `dotnet restore` to restore NuGet packages
- Check that all using statements are present

### Runtime Errors

- Check that the dotnet CLI is accessible in PATH
- Verify project paths are absolute when needed
- Review stderr output for protocol errors

### MCP Connection Issues

- Ensure the server is running via stdio
- Check Claude Desktop configuration is correct
- Verify the project path in the configuration

## Resources

- [Model Context Protocol Documentation](https://modelcontextprotocol.io/)
- [.NET CLI Documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)

## Getting Help

- Review `.github/copilot-instructions.md` for coding guidelines
- Check existing tool implementations for examples
- Use GitHub Copilot Chat to ask questions about the codebase
- Open an issue on GitHub for bugs or feature requests

## Contributing

1. Create a feature branch
2. Make your changes following the project conventions
3. Test thoroughly
4. Update documentation as needed
5. Submit a pull request
6. CI/CD will automatically build and validate your changes