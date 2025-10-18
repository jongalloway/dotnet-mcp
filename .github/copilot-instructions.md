# GitHub Copilot Instructions for .NET MCP Server

## Project Overview

This project is an MCP (Model Context Protocol) server that wraps the .NET SDK CLI commands. It allows AI assistants to interact with the .NET tooling through a standardized protocol.

## Key Technologies

- **.NET 9.0**: The latest version of .NET (STS - Standard Term Support)
- **Model Context Protocol SDK**: Version 0.4.0-preview.2
- **Stdio Transport**: Communication via standard input/output
- **Microsoft.Extensions.Hosting**: For application lifecycle management

## Code Style and Conventions

### Naming Conventions
- Use PascalCase for class names, method names, and public members
- Use camelCase for local variables and parameters
- Use descriptive names that clearly indicate purpose
- Tool method names should follow the pattern: `Dotnet{Command}` (e.g., `DotnetBuild`, `DotnetRun`)

### Attribute Usage
- Mark tool classes with `[McpServerToolType]`
- Mark tool methods with `[McpServerTool]` and `[Description("...")]`
- Mark parameters with `[Description("...")]`
- Keep descriptions clear and concise

### Code Organization
- Each tool method should handle a single .NET CLI command
- Use the `ExecuteDotNetCommand` helper method for all dotnet executions
- Always use `async`/`await` for I/O operations
- Return meaningful string results that include both stdout and stderr

## Adding New Tools

When adding a new .NET CLI tool:

1. Add a new method to the `DotNetCliTools` class
2. Apply `[McpServerTool, Description("...")]` attributes
3. Use `[Description("...")]` on all parameters
4. Use nullable types for optional parameters with default values
5. Build the command string carefully, escaping paths with quotes
6. Call `ExecuteDotNetCommand(args)` to execute
7. Test the tool manually before committing

### Example
```csharp
[McpServerTool, Description("Example command description")]
public async Task<string> DotnetExample(
    [Description("Required parameter description")]
    string requiredParam,
    [Description("Optional parameter description")]
    string? optionalParam = null)
{
    var args = new StringBuilder("example");
    
    args.Append($" {requiredParam}");
    
    if (!string.IsNullOrEmpty(optionalParam))
        args.Append($" --option \"{optionalParam}\"");
    
    return await ExecuteDotNetCommand(args.ToString());
}
```

## Testing

- Build the project: `dotnet build`
- Run the server: `dotnet run`
- Test with MCP Inspector or Claude Desktop
- Verify all commands work with various parameter combinations

## Common Patterns

### Building Command Arguments
```csharp
var args = new StringBuilder("command");

// Add required parameters
args.Append($" {value}");

// Add optional flags
if (condition)
    args.Append(" --flag");

// Add optional parameters with values
if (!string.IsNullOrEmpty(value))
    args.Append($" --option \"{value}\"");
```

### Process Execution
Always use the `ExecuteDotNetCommand` helper method which:
- Captures stdout and stderr
- Reports exit codes
- Handles process lifecycle correctly

## Deployment

The server is deployed as a stdio-based MCP server:
- Claude Desktop users add it to their config
- The server communicates via stdin/stdout
- Logging goes to stderr to avoid interfering with MCP messages

## Documentation

- Keep README.md up to date with new tools
- Document all tool parameters clearly
- Include usage examples in the README
- Update this file when adding new patterns or conventions

## CI/CD

- GitHub Actions runs on push and PR
- Builds in Release configuration
- No tests currently (add when needed)
- Ensures code compiles before merge