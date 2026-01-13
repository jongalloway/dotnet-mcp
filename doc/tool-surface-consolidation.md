# Tool Surface Consolidation

## Executive Summary

The .NET MCP Server has successfully transitioned from **74 individual tools** to **10 consolidated tools** (8 high-level domain tools plus 2 utility tools) while preserving full functionality. This consolidation uses enum-driven subcommands, semantic grouping, and consistent parameter patterns—aligning with modern MCP best practices and improving the developer experience for both AI assistants and human contributors.

> **Status**: **Implemented** as of v1.0.0 (January 2026)
>
> The consolidated-only tool surface is the **sole supported interface** in the initial 1.0.0 release.

**Key Benefits (Achieved):**

- **✅ Improved AI Orchestration**: Smaller tool surface increases model accuracy in tool selection
- **✅ Better Discoverability**: Tools grouped by domain (project, package, EF, workload, etc.)
- **✅ Enhanced Maintainability**: Consistent patterns, shared validation, reduced code duplication
- **✅ Future-Proof**: Easy to extend with new actions without increasing top-level tool count

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Problem Statement](#problem-statement)
3. [Proposed Solution](#proposed-solution)
4. [Tool Definitions](#tool-definitions)
5. [Migration Strategy](#migration-strategy)
6. [Before/After Examples](#beforeafter-examples)
7. [Implementation Notes](#implementation-notes)
8. [Appendix: Complete Tool Inventory](#appendix-complete-tool-inventory)

---

## Current State Analysis

> Note: This section is **historical context** describing the pre-consolidation (74 individual tools) design.
> As of v1.0.0, the server exposes the **consolidated tool surface only**.

### Tool Inventory by Category

The legacy pre-consolidation 74 tools were distributed across 11 functional categories:

| Category | Tool Count | Examples |
| -------- | ---------- | -------- |
| **Templates & Frameworks** | 6 | `dotnet_template_list`, `dotnet_template_search`, `dotnet_template_info`, `dotnet_template_clear_cache`, `dotnet_cache_metrics`, `dotnet_framework_info` |
| **Project Management** | 15 | `dotnet_project_new`, `dotnet_project_build`, `dotnet_project_run`, `dotnet_project_test`, `dotnet_project_restore`, `dotnet_project_publish`, `dotnet_project_clean`, `dotnet_project_analyze`, `dotnet_project_dependencies`, `dotnet_project_validate`, `dotnet_pack_create`, `dotnet_watch_run`, `dotnet_watch_test`, `dotnet_watch_build`, `dotnet_format` |
| **Package Management** | 8 | `dotnet_package_add`, `dotnet_package_remove`, `dotnet_package_search`, `dotnet_package_update`, `dotnet_package_list`, `dotnet_reference_add`, `dotnet_reference_remove`, `dotnet_reference_list` |
| **Solution Management** | 4 | `dotnet_solution_create`, `dotnet_solution_add`, `dotnet_solution_list`, `dotnet_solution_remove` |
| **Entity Framework** | 9 | `dotnet_ef_migrations_add`, `dotnet_ef_migrations_list`, `dotnet_ef_migrations_remove`, `dotnet_ef_migrations_script`, `dotnet_ef_database_update`, `dotnet_ef_database_drop`, `dotnet_ef_dbcontext_list`, `dotnet_ef_dbcontext_info`, `dotnet_ef_dbcontext_scaffold` |
| **Tool Management** | 8 | `dotnet_tool_install`, `dotnet_tool_list`, `dotnet_tool_update`, `dotnet_tool_uninstall`, `dotnet_tool_restore`, `dotnet_tool_manifest_create`, `dotnet_tool_search`, `dotnet_tool_run` |
| **Workload Management** | 6 | `dotnet_workload_list`, `dotnet_workload_info`, `dotnet_workload_search`, `dotnet_workload_install`, `dotnet_workload_update`, `dotnet_workload_uninstall` |
| **Security & Certificates** | 9 | `dotnet_certificate_trust`, `dotnet_certificate_check`, `dotnet_certificate_clean`, `dotnet_certificate_export`, `dotnet_secrets_init`, `dotnet_secrets_set`, `dotnet_secrets_list`, `dotnet_secrets_remove`, `dotnet_secrets_clear` |
| **SDK Information** | 5 | `dotnet_sdk_version`, `dotnet_sdk_info`, `dotnet_sdk_list`, `dotnet_runtime_list`, `dotnet_nuget_locals` |
| **Code Quality** | 1 | `dotnet_format` |
| **Utilities** | 2 | `dotnet_help`, `dotnet_server_capabilities` |

### Legacy Naming Convention

Tools followed the pattern `dotnet_{noun}_{verb}`:

- **Noun**: Domain (project, package, solution, ef, workload, etc.)
- **Verb**: Action (new, build, add, remove, list, etc.)

This pattern naturally groups related operations, suggesting domain-based consolidation.

### Strengths of Current Design

- **Complete Coverage**: All major .NET SDK operations represented
- **Predictable Naming**: Consistent `dotnet_{noun}_{verb}` pattern
- **Clear Semantics**: Each tool does one thing well
- **Well-Documented**: Comprehensive descriptions and parameter validation
- **Type-Safe**: Strong validation using SDK integration (Template Engine, MSBuild)

### Weaknesses of Current Design

- **Large Tool Surface**: 74 tools reduce AI model's ability to select correctly
- **Parameter Duplication**: Similar parameters defined separately across tools (e.g., `project`, `machineReadable`)
- **Browsing Difficulty**: Long tool list is hard for humans to navigate
- **Maintenance Overhead**: Adding new operations increases complexity
- **Inconsistent Patterns**: Some tools have `machineReadable`, others don't; some use CLI, others use SDK

---

## Problem Statement

### 1. Tool Explosion

The server exposes 74 tools, many of which differ only by a single verb:

- **Project tools**: `new`, `restore`, `build`, `run`, `test`, `publish`, `clean`, `analyze`, `dependencies`, `validate`, `pack`, `watch_run`, `watch_test`, `watch_build`
- **Package tools**: `add`, `remove`, `search`, `update`, `list`
- **EF tools**: `migrations_add`, `migrations_list`, `migrations_remove`, `migrations_script`, `database_update`, `database_drop`, `dbcontext_list`, `dbcontext_info`, `dbcontext_scaffold`

### 2. Reduced AI Orchestration Accuracy

Large tool surfaces make it harder for language models to:

- **Select the correct tool** from 74 options
- **Remember tool capabilities** across conversation turns
- **Understand relationships** between related operations
- **Compose complex workflows** that span multiple tools

### 3. Human Discoverability Issues

Contributors and users face challenges:

- **Browsing**: Tool list is long and not semantically grouped in MCP clients
- **Learning**: Hard to understand the full capability surface
- **Contributing**: Adding new tools requires understanding 11+ files

### 4. Inconsistent Parameter Patterns

Current tools have varying parameter shapes:

- Some accept `machineReadable`, others don't
- Some use `project`, others use `path` or have no path parameter
- Some have `additionalOptions`, others have specific flags
- Validation logic is duplicated across tools

### 5. Difficult to Evolve

Adding new capabilities requires:

- Creating new top-level tools (increasing tool count)
- Duplicating common parameter validation
- Updating multiple documentation locations
- Testing each tool independently

---

## Proposed Solution

### Consolidation Strategy: Domain-Based Tools with Action Enums

Group tools by **semantic domain** and expose an `action` enum parameter for each domain:

```yaml
Consolidated Tool Surface (8 domain tools + 2 utilities):
  1. dotnet_project      # Project lifecycle: new, build, run, test, clean, publish, etc.
  2. dotnet_package      # Package/reference management: add, remove, search, update, list
  3. dotnet_solution     # Solution operations: create, add, list, remove
  4. dotnet_ef           # Entity Framework: migrations, database, dbcontext operations
  5. dotnet_workload     # Workload management: install, update, list, search, uninstall
  6. dotnet_dev_certs    # Developer certificates and secrets
  7. dotnet_sdk          # SDK, runtime, template, and framework information
  8. dotnet_tool         # Tool management: install, update, uninstall, search, run
  
  Utilities (unchanged):
  9. dotnet_help         # Get help for dotnet commands
  10. dotnet_server_capabilities  # Server metadata and concurrency info
```

### Why This Approach?

#### ✅ Models Handle Enums Extremely Well

- Modern LLMs excel at selecting from enumerated options
- Reduces tool selection from 74 choices to 8 domain choices + 1 action enum
- Clear hierarchy: "I need to work with projects → use dotnet_project → what action?"

#### ✅ Clear Semantic Grouping

- Tools organized by developer intent, not CLI syntax
- Related operations grouped together (e.g., all EF operations in one tool)
- Natural workflow composition

#### ✅ Easy to Extend

- Adding new actions doesn't increase top-level tool count
- Shared parameter validation and error handling
- Consistent patterns across all tools

#### ✅ Backward Compatible Migration Path

- Can deprecate old tools gradually
- Wrapper functions can redirect old tool calls to new format
- Clear migration guide for existing users

#### ✅ Improved Parameter Design

- Shared parameters across all tools: `machineReadable`, `workingDirectory`
- Action-specific parameters clearly documented
- Consistent validation and error handling

---

## Tool Definitions

### 1. dotnet_project

**Description**: Manage .NET project lifecycle including creation, building, testing, running, and publishing.

**Parameters:**

- `action` (required, enum): The project operation to perform
  - `New` - Create new project from template
  - `Restore` - Restore project dependencies
  - `Build` - Build project
  - `Run` - Build and run project
  - `Test` - Run unit tests
  - `Publish` - Publish project for deployment
  - `Clean` - Clean build outputs
  - `Analyze` - Analyze project file for metadata
  - `Dependencies` - Show dependency graph
  - `Validate` - Validate project health
  - `Pack` - Create NuGet package from project
  - `Watch` - Run with file watching and hot reload
  - `Format` - Format code per .editorconfig
- `project` (optional, string): Path to project file (defaults to current directory)
- `machineReadable` (optional, bool): Return structured JSON output (default: false)
- `workingDirectory` (optional, string): Working directory for command execution

**Action-Specific Parameters:**

**For action="New":**

- `template` (required): Template short name (e.g., 'console', 'webapi')
- `name` (optional): Project name
- `output` (optional): Output directory
- `framework` (optional): Target framework (e.g., 'net10.0')
- `additionalOptions` (optional): Template-specific options

**For action="Build":**

- `configuration` (optional): Debug or Release
- `framework` (optional): Build specific framework

**For action="Run":**

- `configuration` (optional): Debug or Release
- `framework` (optional): Run specific framework
- `noBuild` (optional, bool): Skip building

**For action="Test":**

- `configuration` (optional): Debug or Release
- `framework` (optional): Test specific framework
- `filter` (optional): Test filter expression
- `noBuild` (optional, bool): Skip building

**For action="Publish":**

- `configuration` (optional): Debug or Release
- `framework` (optional): Publish specific framework
- `runtime` (optional): Target runtime identifier
- `output` (optional): Output directory
- `selfContained` (optional, bool): Include runtime

**For action="Watch":**

- `watchAction` (required, enum): 'run', 'test', or 'build'
- `configuration` (optional): Debug or Release

**Replaces Current Tools:**

- `dotnet_project_new`
- `dotnet_project_restore`
- `dotnet_project_build`
- `dotnet_project_run`
- `dotnet_project_test`
- `dotnet_project_publish`
- `dotnet_project_clean`
- `dotnet_project_analyze`
- `dotnet_project_dependencies`
- `dotnet_project_validate`
- `dotnet_pack_create`
- `dotnet_watch_run`
- `dotnet_watch_test`
- `dotnet_watch_build`
- `dotnet_format`

---

### 2. dotnet_package

**Description**: Manage NuGet packages and project references.

**Parameters:**

- `action` (required, enum): The package operation to perform
  - `Add` - Add NuGet package to project
  - `Remove` - Remove NuGet package from project
  - `Search` - Search NuGet.org for packages
  - `Update` - Update packages to newer versions
  - `List` - List package references
  - `AddReference` - Add project-to-project reference
  - `RemoveReference` - Remove project-to-project reference
  - `ListReferences` - List project references
  - `ClearCache` - Clear NuGet local caches
- `project` (optional, string): Path to project file
- `machineReadable` (optional, bool): Return structured JSON output (default: false)
- `workingDirectory` (optional, string): Working directory for command execution

**Action-Specific Parameters:**

**For action="Add":**

- `packageId` (required): NuGet package ID
- `version` (optional): Package version
- `source` (optional): NuGet source URL
- `framework` (optional): Target framework

**For action="Remove":**

- `packageId` (required): Package ID to remove

**For action="Search":**

- `searchTerm` (required): Search query
- `take` (optional, int): Number of results (default: 20)
- `skip` (optional, int): Skip the first N results (pagination)
- `prerelease` (optional, bool): Include prerelease versions
- `exactMatch` (optional, bool): Return exact matches only

**For action="Update":**

- `packageId` (optional): Specific package to update (if omitted, updates all)
- `version` (optional): Target version
- `prerelease` (optional, bool): Include prerelease versions

**For action="List":**

- `outdated` (optional, bool): Show only outdated packages
- `deprecated` (optional, bool): Show only deprecated packages

**For action="AddReference":**

- `referencePath` (required): Path to referenced project

**For action="RemoveReference":**

- `referencePath` (required): Path to referenced project

**For action="ClearCache":**

- `cacheType` (optional, enum): 'http-cache', 'global-packages', 'temp', 'all' (default: 'all')

**Replaces Current Tools:**

- `dotnet_package_add`
- `dotnet_package_remove`
- `dotnet_package_search`
- `dotnet_package_update`
- `dotnet_package_list`
- `dotnet_reference_add`
- `dotnet_reference_remove`
- `dotnet_reference_list`
- `dotnet_nuget_locals`

---

### 3. dotnet_solution

**Description**: Manage solution files and project membership.

**Parameters:**

- `action` (required, enum): The solution operation to perform
  - `Create` - Create new solution file
  - `Add` - Add projects to solution
  - `List` - List projects in solution
  - `Remove` - Remove projects from solution
- `solution` (optional, string): Path to solution file (defaults to searching current directory)
- `machineReadable` (optional, bool): Return structured JSON output (default: false)
- `workingDirectory` (optional, string): Working directory for command execution

**Action-Specific Parameters:**

**For action="Create":**

- `name` (required): Solution name
- `output` (optional): Output directory
- `format` (optional, enum): 'sln' or 'slnx' (default: 'sln')

**For action="Add":**

- `projects` (required, array): Array of project paths to add

**For action="Remove":**

- `projects` (required, array): Array of project paths to remove

**Replaces Current Tools:**

- `dotnet_solution_create`
- `dotnet_solution_add`
- `dotnet_solution_remove`
- `dotnet_solution_list`

---

### 4. dotnet_ef

**Description**: Entity Framework Core database and migration management.

**Parameters:**

- `action` (required, enum): The EF operation to perform
  - `MigrationsAdd` - Create new migration
  - `MigrationsList` - List all migrations
  - `MigrationsRemove` - Remove last migration
  - `MigrationsScript` - Generate SQL script
  - `DatabaseUpdate` - Apply migrations to database
  - `DatabaseDrop` - Drop database
  - `DbContextList` - List DbContext classes
  - `DbContextInfo` - Get DbContext information
  - `DbContextScaffold` - Reverse engineer database
- `project` (optional, string): Path to project file
- `machineReadable` (optional, bool): Return structured JSON output (default: false)
- `workingDirectory` (optional, string): Working directory for command execution

**Action-Specific Parameters:**

**For action="MigrationsAdd":**

- `name` (required): Migration name
- `outputDir` (optional): Migrations output directory

**For action="MigrationsScript":**

- `from` (optional): Starting migration
- `to` (optional): Ending migration
- `idempotent` (optional, bool): Generate idempotent script
- `output` (optional): Output file path

**For action="DatabaseUpdate":**

- `migration` (optional): Target migration (defaults to latest)
- `connection` (optional): Database connection string

**For action="DatabaseDrop":**

- `force` (required, bool): Confirm database deletion
- `dryRun` (optional, bool): Show what would be dropped without executing

**For action="DbContextScaffold":**

- `connection` (required): Database connection string
- `provider` (required): EF provider (e.g., 'Microsoft.EntityFrameworkCore.SqlServer')
- `outputDir` (optional): Output directory for entities
- `contextDir` (optional): Output directory for DbContext
- `context` (optional): DbContext class name
- `tables` (optional): Comma-separated table list
- `schemas` (optional): Comma-separated schema list
- `useDatabaseNames` (optional, bool): Use database names directly

**Replaces Current Tools:**

- `dotnet_ef_migrations_add`
- `dotnet_ef_migrations_list`
- `dotnet_ef_migrations_remove`
- `dotnet_ef_migrations_script`
- `dotnet_ef_database_update`
- `dotnet_ef_database_drop`
- `dotnet_ef_dbcontext_list`
- `dotnet_ef_dbcontext_info`
- `dotnet_ef_dbcontext_scaffold`

---

### 5. dotnet_workload

**Description**: Manage .NET workloads for specialized development (MAUI, WASM, etc.).

**Parameters:**

- `action` (required, enum): The workload operation to perform
  - `List` - List installed workloads
  - `Info` - Get detailed workload information
  - `Search` - Search available workloads
  - `Install` - Install workloads
  - `Update` - Update all workloads
  - `Uninstall` - Uninstall workloads
- `workingDirectory` (optional, string): Working directory for command execution
- `machineReadable` (optional, bool): Return structured JSON output (default: false)

**Action-Specific Parameters:**

**For action="Search":**

- `searchTerm` (optional): Filter workloads by name

**For action="Install":**

- `workloadIds` (required, array): Array of workload IDs to install (e.g., ['maui-android', 'maui-ios'])
- `skipManifestUpdate` (optional, bool): Skip manifest updates
- `includePreviews` (optional, bool): Allow prerelease workload manifests
- `source` (optional): NuGet package source
- `configFile` (optional): Path to NuGet config file

**For action="Uninstall":**

- `workloadIds` (required, array): Array of workload IDs to uninstall

**Replaces Current Tools:**

- `dotnet_workload_list`
- `dotnet_workload_info`
- `dotnet_workload_search`
- `dotnet_workload_install`
- `dotnet_workload_update`
- `dotnet_workload_uninstall`

---

### 6. dotnet_dev_certs

**Description**: Manage developer certificates and user secrets for secure local development.

**Parameters:**

- `action` (required, enum): The operation to perform
  - `CertificateTrust` - Trust HTTPS development certificate
  - `CertificateCheck` - Check certificate status
  - `CertificateClean` - Remove all development certificates
  - `CertificateExport` - Export certificate to file
  - `SecretsInit` - Initialize user secrets
  - `SecretsSet` - Set secret value
  - `SecretsList` - List all secrets
  - `SecretsRemove` - Remove specific secret
  - `SecretsClear` - Clear all secrets
- `project` (optional, string): Path to project file (for secrets operations)
- `machineReadable` (optional, bool): Return structured JSON output (default: false)

**Action-Specific Parameters:**

**For action="CertificateTrust":**

- None (may require elevation)

**For action="CertificateCheck":**

- `trust` (optional, bool): Also check if certificate is trusted

**For action="CertificateExport":**

- `path` (required): Export file path
- `password` (optional): Certificate password
- `format` (optional, enum): 'pfx' or 'pem' (default: 'pfx')

**For action="SecretsSet":**

- `key` (required): Secret key (supports hierarchical keys like "ConnectionStrings:Default")
- `value` (required): Secret value

**For action="SecretsRemove":**

- `key` (required): Secret key to remove

**Replaces Current Tools:**

- `dotnet_certificate_trust`
- `dotnet_certificate_check`
- `dotnet_certificate_clean`
- `dotnet_certificate_export`
- `dotnet_secrets_init`
- `dotnet_secrets_set`
- `dotnet_secrets_list`
- `dotnet_secrets_remove`
- `dotnet_secrets_clear`

---

### 7. dotnet_sdk (New Consolidated Tool)

**Description**: Query .NET SDK, runtime, template, and framework information.

**Parameters:**

- `action` (required, enum): The information to retrieve
  - `Version` - Get SDK version
  - `Info` - Get detailed SDK and runtime info
  - `ListSdks` - List installed SDKs
  - `ListRuntimes` - List installed runtimes
  - `ListTemplates` - List installed templates
  - `SearchTemplates` - Search templates
  - `TemplateInfo` - Get template details
  - `ListTemplatePacks` - List installed template packs
  - `InstallTemplatePack` - Install a template pack
  - `UninstallTemplatePack` - Uninstall a template pack
  - `ClearTemplateCache` - Clear template cache
  - `FrameworkInfo` - Get framework information
  - `CacheMetrics` - Get cache performance metrics
- `workingDirectory` (optional, string): Working directory for command execution
- `machineReadable` (optional, bool): Return structured JSON output (default: false)

**Action-Specific Parameters:**

**For action="SearchTemplates":**

- `searchTerm` (required): Search query
- `forceReload` (optional, bool): Bypass cache

**For action="TemplateInfo":**

- `templateShortName` (required): Template short name
- `forceReload` (optional, bool): Bypass cache

**For action="ListTemplates":**

- `forceReload` (optional, bool): Bypass cache

**For action="InstallTemplatePack":**

- `templatePackage` (required): Template package ID or local path
- `templateVersion` (optional): Template package version
- `nugetSource` (optional): NuGet source URL/path
- `interactive` (optional, bool): Allow interactive restore/auth prompts
- `force` (optional, bool): Force install even if already installed

**For action="UninstallTemplatePack":**

- `templatePackage` (optional): Template package ID or local path (omit to list installed packs)

**For action="FrameworkInfo":**

- `framework` (optional): Specific framework to query (e.g., 'net10.0')

**Replaces Current Tools:**

- `dotnet_sdk_version`
- `dotnet_sdk_info`
- `dotnet_sdk_list`
- `dotnet_runtime_list`
- `dotnet_template_list`
- `dotnet_template_search`
- `dotnet_template_info`
- `dotnet_template_clear_cache`
- `dotnet_framework_info`
- `dotnet_cache_metrics`

---

### 8. dotnet_tool (Consolidated Tool Management)

**Description**: Manage .NET tools (global and local).

**Parameters:**

- `action` (required, enum): The tool operation to perform
  - `Install` - Install tool globally or locally
  - `List` - List installed tools
  - `Update` - Update tool
  - `Uninstall` - Uninstall tool
  - `Restore` - Restore tools from manifest
  - `CreateManifest` - Create tool manifest
  - `Search` - Search for tools on NuGet
  - `Run` - Run a tool
- `workingDirectory` (optional, string): Working directory for command execution
- `machineReadable` (optional, bool): Return structured JSON output (default: false)

**Action-Specific Parameters:**

**For action="Install":**

- `packageId` (required): Tool package ID
- `version` (optional): Specific version
- `global` (optional, bool): Install globally (default: false)
- `toolPath` (optional): Custom tool installation path

**For action="Update":**

- `packageId` (required): Tool package ID
- `global` (optional, bool): Update global tool

**For action="Uninstall":**

- `packageId` (required): Tool package ID
- `global` (optional, bool): Uninstall global tool

**For action="List":**

- `global` (optional, bool): List global tools (default: false lists local)

**For action="Search":**

- `searchTerm` (required): Search query

**For action="Run":**

- `toolName` (required): Tool command name
- `args` (optional): Arguments to pass to tool

**Replaces Current Tools:**

- `dotnet_tool_install`
- `dotnet_tool_update`
- `dotnet_tool_uninstall`
- `dotnet_tool_list`
- `dotnet_tool_restore`
- `dotnet_tool_search`
- `dotnet_tool_run`
- `dotnet_tool_manifest_create`

---

### Utilities (Unchanged)

**dotnet_help** and **dotnet_server_capabilities** remain as standalone utilities with their current signatures.

---

## Migration Strategy

### Implementation Complete (v1.0.0)

**Status**: The consolidation has been fully implemented as of v1.0.0 (January 2026).

**Note**: The .NET MCP Server launches with consolidated tools as the **only** tool interface. There were no prior public releases with legacy individual tools, so no migration is needed for users.

**Why Consolidated Tools from Day One?**

The consolidated tool design was chosen for the 1.0.0 release to:

- Provide better AI orchestration from the start
- Establish a clean, maintainable architecture
- Avoid future breaking changes and deprecation cycles
- Align with MCP best practices for tool design

### Tool Interface (v1.0.0)

All functionality is provided through 10 consolidated tools using action-based parameters:

**Consolidated Tools**:

```typescript
// Project operations
await callTool("dotnet_project", { action: "Build", project: "MyApp.csproj", configuration: "Release" });

// Package operations  
await callTool("dotnet_package", { action: "Add", packageId: "Serilog", project: "MyApp.csproj" });

// Solution operations
await callTool("dotnet_solution", { action: "Create", name: "MyApp", format: "slnx" });

// And so on for dotnet_ef, dotnet_workload, dotnet_tool, dotnet_dev_certs, dotnet_sdk
```

See the [Tool Definitions](#tool-definitions) section for complete details on all available consolidated tools and their actions.

---

## Before/After Examples

### Example 1: Create and Build Web API Project

**Before (Legacy - 2 separate tools):**

```typescript
// Tool 1: Create project
await mcp.callTool("dotnet_project_new", {
  template: "webapi",
  name: "MyApi",
  framework: "net10.0"
});

// Tool 2: Build project
await mcp.callTool("dotnet_project_build", {
  project: "MyApi/MyApi.csproj",
  configuration: "Release"
});
```

**After (Consolidated - 1 tool, 2 calls):**

```typescript
// Create project
await mcp.callTool("dotnet_project", {
  action: "New",
  template: "webapi",
  name: "MyApi",
  framework: "net10.0"
});

// Build project
await mcp.callTool("dotnet_project", {
  action: "Build",
  project: "MyApi/MyApi.csproj",
  configuration: "Release"
});
```

### Example 2: Package Management Workflow

**Before (Legacy - 3 separate tools):**

```typescript
// Search for package
await mcp.callTool("dotnet_package_search", {
  searchTerm: "Serilog",
  take: 5
});

// Add package
await mcp.callTool("dotnet_package_add", {
  packageId: "Serilog.AspNetCore",
  project: "MyApi/MyApi.csproj"
});

// List packages
await mcp.callTool("dotnet_package_list", {
  project: "MyApi/MyApi.csproj"
});
```

**After (Consolidated - 1 tool, 3 calls):**

```typescript
// Search for package
await mcp.callTool("dotnet_package", {
  action: "Search",
  searchTerm: "Serilog",
  take: 5
});

// Add package
await mcp.callTool("dotnet_package", {
  action: "Add",
  packageId: "Serilog.AspNetCore",
  project: "MyApi/MyApi.csproj"
});

// List packages
await mcp.callTool("dotnet_package", {
  action: "List",
  project: "MyApi/MyApi.csproj"
});
```

### Example 3: Entity Framework Migrations

**Before (Legacy - 3 separate tools):**

```typescript
// Add migration
await mcp.callTool("dotnet_ef_migrations_add", {
  name: "AddProductTable",
  project: "MyApi/MyApi.csproj"
});

// List migrations
await mcp.callTool("dotnet_ef_migrations_list", {
  project: "MyApi/MyApi.csproj"
});

// Apply migrations
await mcp.callTool("dotnet_ef_database_update", {
  project: "MyApi/MyApi.csproj"
});
```

**After (Consolidated - 1 tool, 3 calls):**

```typescript
// Add migration
await mcp.callTool("dotnet_ef", {
  action: "MigrationsAdd",
  name: "AddProductTable",
  project: "MyApi/MyApi.csproj"
});

// List migrations
await mcp.callTool("dotnet_ef", {
  action: "MigrationsList",
  project: "MyApi/MyApi.csproj"
});

// Apply migrations
await mcp.callTool("dotnet_ef", {
  action: "DatabaseUpdate",
  project: "MyApi/MyApi.csproj"
});
```

### Example 4: Solution Management

**Before (Legacy - 3 separate tools):**

```typescript
// Create solution
await mcp.callTool("dotnet_solution_create", {
  name: "MyApp",
  format: "slnx"
});

// Add projects
await mcp.callTool("dotnet_solution_add", {
  solution: "MyApp.slnx",
  projects: ["MyApi/MyApi.csproj", "MyWeb/MyWeb.csproj"]
});

// List projects
await mcp.callTool("dotnet_solution_list", {
  solution: "MyApp.slnx"
});
```

**After (Consolidated - 1 tool, 3 calls):**

```typescript
// Create solution
await mcp.callTool("dotnet_solution", {
  action: "Create",
  name: "MyApp",
  format: "slnx"
});

// Add projects
await mcp.callTool("dotnet_solution", {
  action: "Add",
  solution: "MyApp.slnx",
  projects: ["MyApi/MyApi.csproj", "MyWeb/MyWeb.csproj"]
});

// List projects
await mcp.callTool("dotnet_solution", {
  action: "List",
  solution: "MyApp.slnx"
});
```

### Example 5: Natural Language AI Orchestration

**Scenario**: User asks: *"Create a Blazor app with authentication, add Serilog for logging, build it, and run tests"*

**Before (Legacy - AI selects from 74 tools):**

```text
AI reasoning:
- Need to create project → dotnet_project_new
- Need to add package → dotnet_package_add
- Need to build → dotnet_project_build
- Need to test → dotnet_project_test
(4 different tools to remember and select from 74 options)
```

**After (Consolidated - AI selects from 8 tools):**

```text
AI reasoning:
- All project operations → dotnet_project tool
  - Create: action="New"
  - Build: action="Build"
  - Test: action="Test"
- Package operations → dotnet_package tool
  - Add: action="Add"
(2 tools to remember, clear action enums for each)
```

**Impact**: Reduced cognitive load, better tool selection accuracy, easier workflow composition.

---

## Implementation Notes

### Code Organization

**Recommended Structure:**

```text
DotNetMcp/Tools/
├── Cli/
│   ├── DotNetCliTools.Core.cs                          # Infrastructure
│   ├── DotNetCliTools.Project.Consolidated.cs          # dotnet_project
│   ├── DotNetCliTools.Package.Consolidated.cs          # dotnet_package
│   ├── DotNetCliTools.Solution.cs                      # dotnet_solution
│   ├── DotNetCliTools.EntityFramework.Consolidated.cs  # dotnet_ef
│   ├── DotNetCliTools.Workload.Consolidated.cs         # dotnet_workload
│   ├── DotNetCliTools.DevCerts.Consolidated.cs         # dotnet_dev_certs
│   ├── DotNetCliTools.Tool.Consolidated.cs             # dotnet_tool
│   └── DotNetCliTools.Utilities.cs                     # help, capabilities
├── Sdk/
│   └── DotNetCliTools.Sdk.Consolidated.cs               # dotnet_sdk
└── Legacy/
    └── DotNetCliTools.Deprecated.cs        # Old tools (v2.x only)
```

### Parameter Validation

**Shared Validation Helpers:**

- `ParameterValidator.ValidateAction<TEnum>(...)` - Action enum validation
- `ParameterValidator.ValidateRequiredParameter(...)` - Required parameter validation
- `ErrorResultFactory.CreateActionValidationError(...)` - Action validation error payloads
- `ErrorResultFactory.CreateValidationError(...)` - General validation error payloads

**Action-Specific Validation:**

- Each action should validate its required parameters
- Return clear error messages with suggested fixes
- Use `ErrorResultFactory` for consistent error formatting

### Error Handling

**Consistent Error Format:**

```csharp
if (!ParameterValidator.ValidateAction<DotnetProjectAction>(action, out var errorMessage))
{
  if (machineReadable)
  {
    var validActions = Enum.GetNames(typeof(DotnetProjectAction));
    var error = ErrorResultFactory.CreateActionValidationError(
      action.ToString(),
      validActions,
      toolName: "dotnet_project");
    return ErrorResultFactory.ToJson(error);
  }

  return $"Error: {errorMessage}";
}
```

### Testing Strategy

**Unit Tests:**

- Test each action enum with valid/invalid values
- Test required parameter validation
- Test action-specific parameter validation
- Test machine-readable vs plain text output

**Integration Tests:**

- Test complete workflows (create → build → test)
- Test error scenarios
- Test backward compatibility (v2.x)

**Migration Tests:**

- Verify old tools redirect to new tools
- Verify deprecation warnings are logged
- Test removal of old tools (v3.0.0)

### Documentation Updates

**README.md:**

- Update "Available Tools" section to show 8 consolidated tools
- Add "Action Reference" subsection for each tool
- Update usage examples to use new format
- Add migration guide link

**doc/tool-surface-consolidation.md:**

- This document (implementation reference)

**Documentation:**

- Update README.md to present consolidated tools as the primary interface
- Update doc/ai-assistant-guide.md with consolidated tool examples
- Update doc/machine-readable-contract.md with action validation examples

**server.json:**

- Update tool descriptors to include action enums
- Mark deprecated tools with "deprecated": true in v2.x
- Remove deprecated tools in v3.0.0

### McpMeta Attributes

**Recommended Metadata:**

```csharp
[McpServerTool]
[Description("Manage .NET project lifecycle...")]
[McpMeta("category", "project")]
[McpMeta("priority", 10.0)]
[McpMeta("commonlyUsed", true)]
[McpMeta("consolidatedTool", true)]
[McpMeta("actions", JsonValue = """["new","build","run","test","clean","publish","analyze","dependencies","validate","pack","watch","format"]""")]
public async Task<string> DotnetProject(
    [Description("Project operation: new, build, run, test, clean, publish, analyze, dependencies, validate, pack, watch, format")]
    string action,
    ...)
```

### Performance Considerations

**Action Dispatch:**

- Use switch expression for action dispatch (fast, type-safe)
- Avoid reflection-based dispatch
- Cache validation results when appropriate

**Parameter Parsing:**

- Validate early, fail fast
- Reuse validation logic across actions
- Leverage existing SDK integration (TemplateEngineHelper, FrameworkHelper)

**Backward Compatibility (v2.x):**

- Deprecated tools should be thin wrappers with minimal overhead
- Log deprecation warnings asynchronously to avoid blocking

---

## Appendix: Complete Tool Inventory

### Current Tools by Category (74 Total)

#### Templates & Frameworks (6 tools)

1. `dotnet_template_list` - List installed templates
1. `dotnet_template_search` - Search templates
1. `dotnet_template_info` - Get template details
1. `dotnet_template_clear_cache` - Clear template cache
1. `dotnet_cache_metrics` - Get cache metrics
1. `dotnet_framework_info` - Framework information

#### Project Management (15 tools)

1. `dotnet_project_new` - Create project
1. `dotnet_project_restore` - Restore dependencies
1. `dotnet_project_build` - Build project
1. `dotnet_project_run` - Run project
1. `dotnet_project_test` - Run tests
1. `dotnet_project_publish` - Publish project
1. `dotnet_project_clean` - Clean build outputs
1. `dotnet_project_analyze` - Analyze project file
1. `dotnet_project_dependencies` - Show dependency graph
1. `dotnet_project_validate` - Validate project health
1. `dotnet_pack_create` - Create NuGet package
1. `dotnet_watch_run` - Watch and run
1. `dotnet_watch_test` - Watch and test
1. `dotnet_watch_build` - Watch and build
1. `dotnet_format` - Format code

#### Package Management (8 tools)

1. `dotnet_package_add` - Add package
1. `dotnet_package_remove` - Remove package
1. `dotnet_package_search` - Search packages
1. `dotnet_package_update` - Update packages
1. `dotnet_package_list` - List packages
1. `dotnet_reference_add` - Add project reference
1. `dotnet_reference_remove` - Remove project reference
1. `dotnet_reference_list` - List project references

#### Solution Management (4 tools)

1. `dotnet_solution_create` - Create solution
1. `dotnet_solution_add` - Add projects to solution
1. `dotnet_solution_list` - List solution projects
1. `dotnet_solution_remove` - Remove projects from solution

#### Entity Framework (9 tools)

1. `dotnet_ef_migrations_add` - Add migration
1. `dotnet_ef_migrations_list` - List migrations
1. `dotnet_ef_migrations_remove` - Remove migration
1. `dotnet_ef_migrations_script` - Generate SQL script
1. `dotnet_ef_database_update` - Update database
1. `dotnet_ef_database_drop` - Drop database
1. `dotnet_ef_dbcontext_list` - List DbContext classes
1. `dotnet_ef_dbcontext_info` - Get DbContext info
1. `dotnet_ef_dbcontext_scaffold` - Scaffold from database

#### Tool Management (8 tools)

1. `dotnet_tool_install` - Install tool
1. `dotnet_tool_list` - List tools
1. `dotnet_tool_update` - Update tool
1. `dotnet_tool_uninstall` - Uninstall tool
1. `dotnet_tool_restore` - Restore tools
1. `dotnet_tool_manifest_create` - Create tool manifest
1. `dotnet_tool_search` - Search for tools
1. `dotnet_tool_run` - Run tool

#### Workload Management (6 tools)

1. `dotnet_workload_list` - List workloads
1. `dotnet_workload_info` - Get workload info
1. `dotnet_workload_search` - Search workloads
1. `dotnet_workload_install` - Install workload
1. `dotnet_workload_update` - Update workloads
1. `dotnet_workload_uninstall` - Uninstall workload

#### Security & Certificates (9 tools)

1. `dotnet_certificate_trust` - Trust certificate
1. `dotnet_certificate_check` - Check certificate
1. `dotnet_certificate_clean` - Clean certificates
1. `dotnet_certificate_export` - Export certificate
1. `dotnet_secrets_init` - Initialize secrets
1. `dotnet_secrets_set` - Set secret
1. `dotnet_secrets_list` - List secrets
1. `dotnet_secrets_remove` - Remove secret
1. `dotnet_secrets_clear` - Clear secrets

#### SDK Information (5 tools)

1. `dotnet_sdk_version` - Get SDK version
1. `dotnet_sdk_info` - Get SDK info
1. `dotnet_sdk_list` - List SDKs
1. `dotnet_runtime_list` - List runtimes
1. `dotnet_nuget_locals` - Manage NuGet cache

#### Code Quality (1 tool)

1. `dotnet_format` - Format code (duplicate - listed in Project Management)

#### Utilities (2 tools)

1. `dotnet_help` - Get help
1. `dotnet_server_capabilities` - Server capabilities

**Note:** `dotnet_format` appears in both Project Management and Code Quality categories in current implementation, but is functionally a single tool.

---

## Conclusion

This document describes the consolidated tool surface that shipped with .NET MCP Server v1.0.0. The server provides **10 consolidated tools** (8 domain tools + 2 utilities), delivering full .NET SDK functionality through a clean, maintainable architecture.

**Key Outcomes:**

- ✅ **Improved AI orchestration** through reduced tool count and enum-driven actions
- ✅ **Better discoverability** through semantic grouping
- ✅ **Enhanced maintainability** with consistent patterns and shared validation
- ✅ **Future-proof architecture** that's easy to extend
- ✅ **Clean 1.0 launch** without legacy baggage or deprecation cycles

---

- **Document Version**: 1.1
- **Last Updated**: 2026-01-09
- **Status**: Implemented (v1.0.0)
