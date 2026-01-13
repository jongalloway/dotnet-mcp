# .NET SDK Integration

This document describes how the .NET MCP Server integrates directly with the .NET SDK through official NuGet packages.

## Overview

The .NET MCP Server uses a hybrid architecture that combines:

1. **SDK Integration** - Direct access to .NET SDK internals via NuGet packages
2. **CLI Execution** - Proven, reliable command execution

This approach provides rich metadata access, type-safe operations, and validation capabilities while maintaining the reliability of the official CLI.

## SDK Packages

### Template Engine

- **Microsoft.TemplateEngine.Abstractions** (v10.0.101)
- **Microsoft.TemplateEngine.Edge** (v10.0.101)

Provides programmatic access to the .NET template system:

- List all installed templates with metadata
- Access template parameters and options
- Search and filter templates
- Validate template existence

### MSBuild

- **Microsoft.Build.Utilities.Core** (v18.0.2)
- **Microsoft.Build** (v18.0.2)

Provides framework validation, metadata, and project analysis:

- Parse and validate Target Framework Monikers (TFMs)
- Access framework version information
- Framework classification (modern .NET, .NET Core, .NET Framework, .NET Standard)
- Parse and analyze .csproj files without building
- Extract package references, project references, and build properties
- Validate project configuration and detect common issues

## Helper Classes

### CachedResourceManager<T>

Located in `DotNetMcp/CachedResourceManager.cs`

Generic cache manager for readonly resources with configurable TTL and metrics tracking:

**Features:**

- Configurable TTL (default: 300 seconds)
- Thread-safe async operations using `SemaphoreSlim`
- Cache hit/miss metrics tracking
- Force reload capability
- Automatic cache expiration
- JSON response generation with cache metadata

**Usage Example:**

```csharp
var cache = new CachedResourceManager<string>("MyResource", defaultTtlSeconds: 300);

// Load or get from cache
var entry = await cache.GetOrLoadAsync(async () => {
    return await LoadDataFromSource();
});

// Force reload
var freshEntry = await cache.GetOrLoadAsync(LoadDataFromSource, forceReload: true);

// Check metrics
Console.WriteLine($"Cache metrics: {cache.Metrics}");

// Clear cache
await cache.ClearAsync();
```

### CacheMetrics

Located in `DotNetMcp/CacheMetrics.cs`

Thread-safe metrics tracker for monitoring cache performance:

**Properties:**

- `Hits` - Number of cache hits
- `Misses` - Number of cache misses
- `HitRatio` - Cache hit ratio (0.0 to 1.0)

**Methods:**

- `RecordHit()` - Increment hit counter
- `RecordMiss()` - Increment miss counter
- `Reset()` - Reset all metrics to zero

### DotNetSdkConstants

Located in `DotNetMcp/DotNetSdkConstants.cs`

Provides strongly-typed constants for common SDK values:

**Target Frameworks**

```csharp
DotNetSdkConstants.TargetFrameworks.Net110     // "net11.0"
DotNetSdkConstants.TargetFrameworks.Net100     // "net10.0"
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

Integrates with the .NET Template Engine for template operations with built-in caching:

**Methods:**

- `GetInstalledTemplatesAsync(forceReload)` - Get all installed templates with metadata
- `GetTemplateDetailsAsync(shortName, forceReload)` - Get detailed template information
- `SearchTemplatesAsync(searchTerm, forceReload)` - Search templates by name/description
- `ValidateTemplateExistsAsync(shortName, forceReload)` - Check if a template exists
- `ClearCacheAsync()` - Clear template cache and reset metrics
- `Metrics` - Property to access cache hit/miss statistics

**Caching:**

- Templates are cached for 5 minutes (300 seconds) by default
- Cache can be bypassed with `forceReload: true` parameter
- Cache is automatically invalidated after expiry or when cleared
- Thread-safe implementation using `CachedResourceManager<T>`

**Usage Example:**

```csharp
// Normal usage (uses cache)
var templates = await TemplateEngineHelper.GetInstalledTemplatesAsync();

// Force reload from disk
var freshTemplates = await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload: true);

// Check cache metrics
var metrics = TemplateEngineHelper.Metrics;
Console.WriteLine($"Cache hits: {metrics.Hits}, Misses: {metrics.Misses}");

// Clear cache after installing new templates
await TemplateEngineHelper.ClearCacheAsync();
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

### ProjectAnalysisHelper

Located in `DotNetMcp/ProjectAnalysisHelper.cs`

Provides comprehensive .csproj file analysis using MSBuild APIs without requiring a build:

**Methods:**

- `AnalyzeProjectAsync(projectPath, logger)` - Comprehensive project analysis
  - Returns JSON with target frameworks, package references, project references, build properties
- `AnalyzeDependenciesAsync(projectPath, logger)` - Dependency graph analysis
  - Returns JSON with direct package and project dependencies
- `ValidateProjectAsync(projectPath, logger)` - Project health check
  - Returns JSON with errors, warnings, and recommendations

**Usage Example:**

```csharp
// Analyze a project file
var analysisJson = await ProjectAnalysisHelper.AnalyzeProjectAsync("MyApp.csproj", logger);
// Returns: target frameworks, packages, project refs, build properties, analyzers, etc.

// Analyze dependencies
var depsJson = await ProjectAnalysisHelper.AnalyzeDependenciesAsync("MyApp.csproj", logger);
// Returns: direct package dependencies, project dependencies, framework info

// Validate project health
var validationJson = await ProjectAnalysisHelper.ValidateProjectAsync("MyApp.csproj", logger);
// Returns: errors, warnings, recommendations (e.g., use LTS framework, enable nullable)
```

**Features:**

- Parses .csproj files using MSBuild's Project class
- No build required - reads project structure directly
- Extracts package references with versions and metadata
- Extracts project references with paths
- Reads build properties (PublishAot, InvariantGlobalization, etc.)
- Validates framework versions and suggests LTS alternatives
- Detects missing configuration (OutputType, Nullable, etc.)
- Thread-safe and async-first design

## MCP Tools Using SDK Integration

### Template + SDK Metadata Tools (Consolidated)

Template, SDK, runtime, and framework metadata is surfaced via the consolidated `dotnet_sdk` tool:

- `dotnet_sdk` (action: `ListTemplates`) - Lists installed templates (Template Engine API; CLI fallback when needed)
- `dotnet_sdk` (action: `SearchTemplates`) - Searches templates by name/description
- `dotnet_sdk` (action: `TemplateInfo`) - Detailed template info, including parameters/options
- `dotnet_sdk` (action: `ClearTemplateCache`) - Clears template cache (use after installing/uninstalling templates)
- `dotnet_sdk` (action: `CacheMetrics`) - Cache hit/miss stats for SDK/runtime/templates caching

Framework metadata is also provided via `dotnet_sdk`:

- `dotnet_sdk` (action: `FrameworkInfo`) - Framework validation and LTS classification (modern .NET / .NET Core / .NET Framework / .NET Standard)

## MCP Resources with Caching

### Resource Endpoints

**`dotnet://sdk-info`**

- Returns JSON with installed .NET SDK versions and paths
- Cached for 5 minutes (300 seconds)
- Includes cache metadata: timestamp, age, duration, metrics

**`dotnet://runtime-info`**

- Returns JSON with installed .NET runtime information
- Cached for 5 minutes (300 seconds)
- Includes cache metadata: timestamp, age, duration, metrics

**`dotnet://templates`**

- Returns JSON catalog of installed templates with full metadata
- Cached for 5 minutes (300 seconds)
- Uses TemplateEngineHelper cache

**`dotnet://frameworks`**

- Returns JSON with framework information (not cached)
- Provides static metadata about .NET TFMs

### Cache Response Format

All cached resources include metadata:

```json
{
  "data": {
    // Actual resource data
  },
  "cache": {
    "timestamp": "2025-10-31T05:50:00.000Z",
    "cacheAgeSeconds": 15,
    "cacheDurationSeconds": 300,
    "metrics": {
      "hits": 5,
      "misses": 1,
      "hitRatio": 0.8333
    }
  }
}
```

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

### Use SDK Integration For

- Metadata and discovery (templates, frameworks)
- Input validation before execution
- Structured data access
- Type-safe operations

### Use CLI Execution For

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

- Foundation for MSBuild project analysis ✅ (Implemented)
- Ready for NuGet API integration
- Prepared for Roslyn integration

### Project Analysis Tools

MSBuild-based analysis is surfaced via the consolidated `dotnet_project` tool:

- `dotnet_project` (action: `Analyze`) - Extract target frameworks, package refs, project refs, and key properties from a project file
- `dotnet_project` (action: `Dependencies`) - Direct package + project dependency graph
- `dotnet_project` (action: `Validate`) - Health checks (framework support, common project config issues)

## Testing SDK Integration

Test template, framework, and project analysis tools:

```
# Template discovery
"What .NET templates are available?"
→ Uses dotnet_sdk (action: "ListTemplates")

# Template search
"Show me web-related templates"
→ Uses dotnet_sdk (action: "SearchTemplates")

# Template details
"What options does the console template have?"
→ Uses dotnet_sdk (action: "TemplateInfo")

# Framework information
"Which .NET versions are LTS?"
→ Uses dotnet_sdk (action: "FrameworkInfo")

# Project analysis
"Analyze the MyApp.csproj file and show me what packages it uses"
→ Uses dotnet_project (action: "Analyze")

# Dependency analysis
"What dependencies does this project have?"
→ Uses dotnet_project (action: "Dependencies")

# Project validation
"Check if MyApp.csproj has any configuration issues"
→ Uses dotnet_project (action: "Validate")
```

## Future Enhancement Opportunities

The SDK integration foundation enables:

1. **NuGet API Integration** - Package querying and dependency analysis
2. **MSBuild Project Analysis** ✅ (Implemented) - Parse and analyze project files
3. **Roslyn Integration** - Code analysis and generation
4. **Workload Management** - .NET workload installation and management
5. **Template Creation** - Assist in creating custom templates
