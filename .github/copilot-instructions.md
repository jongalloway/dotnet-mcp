# GitHub Copilot Instructions for .NET MCP Server

## Project Overview

This project is an MCP (Model Context Protocol) server that integrates with the .NET SDK. It provides both CLI command execution and **direct SDK integration** through official NuGet packages, offering rich metadata access and type-safe operations.

## Key Technologies

- **.NET 10.0**: The latest version of .NET (LTS - Long Term Support)
- **Model Context Protocol SDK**: Version 0.5.0-preview.1
- **Stdio Transport**: Communication via standard input/output
- **Microsoft.Extensions.Hosting**: For application lifecycle management
- **Microsoft.TemplateEngine.\***: Direct integration with .NET Template Engine (v10.0.101)
- **Microsoft.Build.\***: MSBuild integration for framework validation (v18.0.2)

## Architecture

The server uses a **hybrid architecture**:
1. **SDK Integration** - For metadata, discovery, and validation (Template Engine, MSBuild)
2. **CLI Execution** - For actual command execution (proven, reliable)

## Code Style and Conventions

### Naming Conventions
- Use PascalCase for class names, method names, and public members
- Use camelCase for local variables and parameters
- Use descriptive names that clearly indicate purpose
- **Tool method names follow the pattern: `Dotnet{Noun}{Verb}`** (e.g., `DotnetProjectBuild`, `DotnetTemplateList`)
  - This aligns with the .NET 10 CLI naming direction: `dotnet_{noun}_{verb}`
- Helper class methods should be descriptive: `GetInstalledTemplatesAsync`, `IsLtsFramework`

### Attribute Usage
- Mark tool classes with `[McpServerToolType]`
- Mark tool methods with `[McpServerTool]` and `[Description("...")]`
- Mark parameters with `[Description("...")]`
- Keep descriptions clear and concise
- **Use `[McpMeta]` attributes** to provide additional metadata for AI assistants:
  - **Category tags**: `[McpMeta("category", "template")]` - Groups related tools (template, project, package, solution, sdk, etc.)
  - **Priority hints**: `[McpMeta("priority", 10.0)]` - Suggests relative importance (1.0-10.0 scale)
  - **Boolean flags**: `[McpMeta("commonlyUsed", true)]` - Indicates frequently used tools
  - **Capability markers**: `[McpMeta("isLongRunning", true)]` - Warns about long-running operations
  - **Version requirements**: `[McpMeta("minimumSdkVersion", "6.0")]` - Documents SDK version dependencies
  - **JSON values**: `[McpMeta("tags", JsonValue = """["a","b"]""")]` - Complex metadata as JSON

### Code Organization
- Each tool method should handle a single .NET CLI command
- Use the `ExecuteDotNetCommand` helper method for all dotnet executions
- Always use `async`/`await` for I/O operations
- Return meaningful string results that include both stdout and stderr
- **Helper classes** for SDK integration:
  - `TemplateEngineHelper` - Template operations
  - `FrameworkHelper` - Framework validation
  - `DotNetSdkConstants` - Type-safe constants

## Build and Compilation

### Building the Project

When building the project, always use the full path to the project file:

```bash
dotnet build C:\Users\jonga\Documents\GitHub\dotnet-mcp\DotNetMcp\DotNetMcp.csproj
```

**Important**: If a build fails due to inability to find the project file, pass the full path to the `.csproj` file. Do not rely on relative paths or current directory assumptions.

### Testing Changes

After making code changes:
1. Build with full path: `dotnet build --project [full path to .csproj]`
2. Check for compilation errors
3. Test the MCP server manually if significant changes were made

## SDK Integration Patterns

### Using Template Engine
```csharp
// Get templates programmatically
var engineEnvironmentSettings = new EngineEnvironmentSettings(
    new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0"),
    virtualizeSettings: true);

var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
var templates = await templatePackageManager.GetTemplatesAsync(default);
```

### Using Framework Helpers
```csharp
// Validate and get framework info
if (FrameworkHelper.IsValidFramework(framework))
{
    var description = FrameworkHelper.GetFrameworkDescription(framework);
    var isLts = FrameworkHelper.IsLtsFramework(framework);
}
```

### Using SDK Constants
```csharp
// Use strongly-typed constants
var framework = DotNetSdkConstants.TargetFrameworks.Net100;
var config = DotNetSdkConstants.Configurations.Release;
var runtime = DotNetSdkConstants.RuntimeIdentifiers.LinuxX64;
```

## Adding New Tools

When adding a new .NET CLI tool:

1. Add a new method to the `DotNetCliTools` class
2. Follow the naming convention: `Dotnet{Noun}{Verb}` (e.g., `DotnetProjectBuild`, `DotnetPackageAdd`)
3. Apply `[McpServerTool, Description("...")]` attributes
4. Use `[Description("...")]` on all parameters
5. Use nullable types for optional parameters with default values
6. Build the command string carefully, escaping paths with quotes
7. Call `ExecuteDotNetCommand(args)` to execute
8. Test the tool manually before committing

### Tool Naming Examples

Following the `dotnet_{noun}_{verb}` pattern:

- **Template tools**: `DotnetTemplateList`, `DotnetTemplateSearch`, `DotnetTemplateInfo`
- **Project tools**: `DotnetProjectNew`, `DotnetProjectBuild`, `DotnetProjectRun`
- **Package tools**: `DotnetPackageAdd`, `DotnetPackageList`
- **SDK tools**: `DotnetSdkInfo`, `DotnetSdkVersion`, `DotnetSdkList`

### When to Use SDK Integration vs CLI

**Use SDK Integration when:**
- You need metadata or discovery (templates, frameworks)
- You want to validate input before execution
- You need structured data
- You want type-safe operations

**Use CLI Execution when:**
- Actually performing operations (build, run, test)
- The SDK doesn't expose the functionality
- CLI is the proven, tested method

### Example: SDK Integration + CLI Execution
```csharp
[McpServerTool, Description("Create project with validation")]
public async Task<string> DotnetProjectNewValidated(
    [Description("Template short name")]
    string template)
{
    // 1. SDK Integration: Validate template exists
    if (!await TemplateEngineHelper.ValidateTemplateExistsAsync(template))
    {
        return $"Template '{template}' not found. Use dotnet_template_list to see available templates.";
    }
    
    // 2. CLI Execution: Actually create the project
    return await ExecuteDotNetCommand($"new {template}");
}
```

## Helper Classes Documentation

### DotNetSdkConstants
Provides strongly-typed constants for common SDK values.
- **TargetFrameworks**: TFMs (net10.0, net8.0, etc.)
- **Configurations**: Debug, Release
- **RuntimeIdentifiers**: win-x64, linux-x64, etc.
- **Templates**: Common template short names
- **CommonPackages**: Well-known NuGet packages

### TemplateEngineHelper
Integration with .NET Template Engine.
- `GetInstalledTemplatesAsync()` - List all templates
- `GetTemplateDetailsAsync(shortName)` - Get template details
- `SearchTemplatesAsync(searchTerm)` - Search templates
- `ValidateTemplateExistsAsync(shortName)` - Validate template

### FrameworkHelper
Framework validation and information.
- `IsValidFramework(framework)` - Validate TFM
- `GetFrameworkDescription(framework)` - Get friendly name
- `IsLtsFramework(framework)` - Check if LTS
- `IsModernNet/IsNetCore/IsNetFramework/IsNetStandard()` - Classification
- `GetLatestRecommendedFramework()` - Latest version
- `GetLatestLtsFramework()` - Latest LTS

## Testing

- **Automated tests**: Run the test suite with `dotnet test --solution DotNetMcp.slnx`
  - 703 passing tests covering all 74 MCP tools
  - MCP conformance tests validate protocol compliance
  - See [doc/testing.md](../doc/testing.md) for details
- **Build the project**: `dotnet build --project [full path to .csproj]`
- **Run the server**: `dotnet run --project [full path to .csproj]`
- **Manual testing**: Test with MCP Inspector or Claude Desktop
- Verify all commands work with various parameter combinations
- **Test SDK integration**: Try the new template and framework info tools

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

### Template Engine Usage
```csharp
try
{
    var engineEnvironmentSettings = new EngineEnvironmentSettings(
        new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0"),
        virtualizeSettings: true);

    var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
    var templates = await templatePackageManager.GetTemplatesAsync(default);
    
    // Process templates...
}
catch (Exception ex)
{
    return $"Error accessing template engine: {ex.Message}";
}
```

## Deployment

The server is deployed as a stdio-based MCP server:
- Claude Desktop users add it to their config
- The server communicates via stdin/stdout
- Logging goes to stderr to avoid interfering with MCP messages

## Documentation Guidelines

### What to Document

Focus on **evergreen documentation** that provides lasting value to users:

- **README.md** - Project overview, features, installation, usage
- **doc/sdk-integration.md** - Technical details about SDK integration
- **This file** - Development guidelines and conventions

### What NOT to Document

**Avoid creating summary documents for Copilot activities or individual changes:**
- ? "Changes made in this session"
- ? "Summary of what was implemented"
- ? "List of files modified"
- ? Temporary activity logs

These are unnecessary overhead and become stale quickly. Instead:
- ? Update existing documentation to reflect new features
- ? Add examples and usage patterns
- ? Document architectural decisions
- ? Keep documentation focused on helping users and future developers

### Documentation Update Process

When adding features:
1. Update README.md if new tools are added
2. Update doc/sdk-integration.md if SDK integration changes
3. Update this file if new patterns or conventions are introduced
4. Do NOT create separate "summary" or "change log" documents

## CI/CD

- GitHub Actions runs on push and PR
- Builds in Release configuration
- Runs comprehensive test suite:
  - MCP conformance tests (protocol compliance)
  - Unit tests with code coverage (Cobertura format)
  - Performance smoke tests (informational)
- Uploads coverage to Codecov
- Ensures code compiles and tests pass before merge

## Package Management

The project uses the following NuGet packages for SDK integration:
- `Microsoft.TemplateEngine.Abstractions` - Template metadata
- `Microsoft.TemplateEngine.Edge` - Template engine APIs
- `Microsoft.Build.Utilities.Core` - MSBuild utilities
- `Microsoft.Build` - MSBuild APIs

When adding new SDK integration:
1. Add appropriate NuGet package
2. Create helper class in separate file
3. Add MCP tool methods in DotNetCliTools (following naming convention)
4. Update doc/sdk-integration.md if adding new SDK capabilities
5. Update README.md to list new tools