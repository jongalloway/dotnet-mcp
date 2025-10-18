# .NET SDK Integration

This document describes how the .NET MCP Server integrates directly with the .NET SDK through official NuGet packages.

## Overview

The .NET MCP Server uses a hybrid architecture that combines:

1. **SDK Integration** - Direct access to .NET SDK internals via NuGet packages
2. **CLI Execution** - Proven, reliable command execution

This approach provides rich metadata access, type-safe operations, and validation capabilities while maintaining the reliability of the official CLI.

## SDK Packages

### Template Engine
- **Microsoft.TemplateEngine.Abstractions** (v9.0.306)
- **Microsoft.TemplateEngine.Edge** (v9.0.306)

Provides programmatic access to the .NET template system:
- List all installed templates with metadata
- Access template parameters and options
- Search and filter templates
- Validate template existence

### MSBuild
- **Microsoft.Build.Utilities.Core** (v17.14.x)
- **Microsoft.Build** (v17.14.x)

Provides framework validation and metadata:
- Parse and validate Target Framework Monikers (TFMs)
- Access framework version information
- Framework classification (modern .NET, .NET Core, .NET Framework, .NET Standard)

## Helper Classes

### DotNetSdkConstants

Located in `DotNetMcp/DotNetSdkConstants.cs`

Provides strongly-typed constants for common SDK values:

**Target Frameworks**
```csharp
DotNetSdkConstants.TargetFrameworks.Net90      // "net9.0"
DotNetSdkConstants.TargetFrameworks.Net80      // "net8.0"
DotNetSdkConstants.TargetFrameworks.Net60      // "net6.0"
```

**Build Configurations**
```csharp
DotNetSdkConstants.Configurations.Debug
DotNetSdkConstants.Configurations.Release
```

**Runtime Identifiers**
```csharp
DotNetSdkConstants.RuntimeIdentifiers.WinX64
DotNetSdkConstants.RuntimeIdentifiers.LinuxX64
DotNetSdkConstants.RuntimeIdentifiers.OsxArm64
```

**Common Templates**
```csharp
DotNetSdkConstants.Templates.Console
DotNetSdkConstants.Templates.WebApi
DotNetSdkConstants.Templates.Blazor
```

**Common NuGet Packages**
```csharp
DotNetSdkConstants.CommonPackages.NewtonsoftJson
DotNetSdkConstants.CommonPackages.EFCore
DotNetSdkConstants.CommonPackages.XUnit
```

### TemplateEngineHelper

Located in `DotNetMcp/TemplateEngineHelper.cs`

Integrates with the .NET Template Engine for template operations:

**Methods:**
- `GetInstalledTemplatesAsync()` - Get all installed templates with metadata
- `GetTemplateDetailsAsync(shortName)` - Get detailed template information
- `SearchTemplatesAsync(searchTerm)` - Search templates by name/description
- `ValidateTemplateExistsAsync(shortName)` - Check if a template exists

**Usage Example:**
```csharp
var templates = await TemplateEngineHelper.GetInstalledTemplatesAsync();
var details = await TemplateEngineHelper.GetTemplateDetailsAsync("webapi");
var matches = await TemplateEngineHelper.SearchTemplatesAsync("web");
```

### FrameworkHelper

Located in `DotNetMcp/FrameworkHelper.cs`

Provides framework validation and information:

**Methods:**
- `IsValidFramework(framework)` - Validate a TFM
- `GetFrameworkDescription(framework)` - Get friendly name (e.g., ".NET 8.0 (LTS)")
- `IsLtsFramework(framework)` - Check if framework is Long-Term Support
- `IsModernNet(framework)` - Check if .NET 5.0+
- `IsNetCore(framework)` - Check if .NET Core
- `IsNetFramework(framework)` - Check if .NET Framework
- `IsNetStandard(framework)` - Check if .NET Standard
- `GetLatestRecommendedFramework()` - Get latest recommended version
- `GetLatestLtsFramework()` - Get latest LTS version
- `GetSupportedModernFrameworks()` - List modern .NET versions
- `GetSupportedNetCoreFrameworks()` - List .NET Core versions
- `GetSupportedNetStandardFrameworks()` - List .NET Standard versions

**Usage Example:**
```csharp
if (FrameworkHelper.IsValidFramework("net8.0"))
{
    var description = FrameworkHelper.GetFrameworkDescription("net8.0");
    var isLts = FrameworkHelper.IsLtsFramework("net8.0");
}
```

## MCP Tools Using SDK Integration

### Template Tools

**`dotnet_template_list`**
Lists all installed templates using the Template Engine API. Returns structured data with template names, languages, types, and descriptions.

**`dotnet_template_search`**
Searches templates by name or description. More powerful than CLI text parsing.

**`dotnet_template_info`**
Gets detailed template information including all available parameters and their types.

### Framework Tools

**`dotnet_framework_info`**
Provides framework version information, identifies LTS releases, and classifies frameworks (modern .NET, .NET Core, etc.).

## Architecture

```
???????????????????????????????????????
?     AI Assistant / User             ?
???????????????????????????????????????
                 ? MCP Protocol
                 ?
???????????????????????????????????????
?      .NET MCP Server                ?
?  ????????????????????????????????  ?
?  ?   DotNetCliTools             ?  ?
?  ?   (MCP Tool Methods)         ?  ?
?  ????????????????????????????????  ?
?              ?                      ?
?  ????????????????????????????????  ?
?  ?  TemplateEngineHelper        ?  ?
?  ?  FrameworkHelper             ?  ?
?  ?  DotNetSdkConstants          ?  ?
?  ????????????????????????????????  ?
?              ?                      ?
?  ????????????????????????????????  ?
?  ?  ExecuteDotNetCommand        ?  ?
?  ?  (CLI Execution)             ?  ?
?  ????????????????????????????????  ?
???????????????????????????????????????
                 ?
???????????????????????????????????????
?  .NET SDK                           ?
?  - Template Engine (NuGet)          ?
?  - MSBuild (NuGet)                  ?
?  - dotnet CLI                       ?
???????????????????????????????????????
```

## When to Use SDK Integration vs CLI

### Use SDK Integration For:
- Metadata and discovery (templates, frameworks)
- Input validation before execution
- Structured data access
- Type-safe operations

### Use CLI Execution For:
- Actual command operations (build, run, test)
- Features not exposed by SDK packages
- Proven, tested functionality

### Example Pattern: Validation + Execution

```csharp
[McpServerTool, Description("Create project with validation")]
public async Task<string> DotnetProjectNew(string template)
{
    // 1. SDK: Validate template exists
    if (!await TemplateEngineHelper.ValidateTemplateExistsAsync(template))
    {
        return $"Template '{template}' not found.";
    }
    
    // 2. CLI: Execute the command
    return await ExecuteDotNetCommand($"new {template}");
}
```

## Benefits

**Type Safety**
- Strongly-typed constants prevent typos
- IntelliSense support for SDK values
- Compile-time validation

**Rich Metadata**
- Direct access to template information
- Framework classification and LTS status
- Template parameter discovery

**Better Validation**
- Validate inputs before execution
- Provide helpful error messages
- Guide users to correct values

**Future Extensibility**
- Foundation for MSBuild project analysis
- Ready for NuGet API integration
- Prepared for Roslyn integration

## Testing SDK Integration

Test template and framework tools:

```
# Template discovery
"What .NET templates are available?"
? Uses dotnet_template_list

# Template search
"Show me web-related templates"
? Uses dotnet_template_search

# Template details
"What options does the console template have?"
? Uses dotnet_template_info

# Framework information
"Which .NET versions are LTS?"
? Uses dotnet_framework_info
```

## Future Enhancement Opportunities

The SDK integration foundation enables:

1. **NuGet API Integration** - Package querying and dependency analysis
2. **MSBuild Project Analysis** - Parse and analyze project files
3. **Roslyn Integration** - Code analysis and generation
4. **Workload Management** - .NET workload installation and management
5. **Template Creation** - Assist in creating custom templates
