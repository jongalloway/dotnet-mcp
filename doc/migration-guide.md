# Migration Guide: Legacy to Consolidated Tools

## Overview

The .NET MCP Server introduced **consolidated tools** in Phase 1 of the tool surface consolidation initiative. These new tools group related operations by domain using action enums, reducing the tool count from 74 to 8 domain tools while preserving full functionality.

**Why migrate?**

- ✅ **Improved AI orchestration** - Fewer tools mean better tool selection by AI assistants
- ✅ **Consistent patterns** - All consolidated tools follow the same action-based structure
- ✅ **Better discoverability** - Related operations grouped together semantically
- ✅ **Future-proof** - New features added as actions without increasing tool count
- ✅ **Easier maintenance** - Shared parameter validation and error handling

**Current status:**

- **Phase 1 (Complete)**: Consolidated tools introduced alongside legacy tools
- **Both tool sets fully supported** - Choose whichever fits your workflow
- **No breaking changes** - Legacy tools continue to work as before

## Quick Migration Reference

### Consolidated Tool Summary

| Consolidated Tool | Purpose | Actions | Replaces Legacy Tools |
|-------------------|---------|---------|----------------------|
| **dotnet_project** | Project lifecycle management | New, Restore, Build, Run, Test, Publish, Clean, Analyze, Dependencies, Validate, Pack, Watch, Format | 15 tools |
| **dotnet_package** | Package and reference management | Add, Remove, Search, Update, List, AddReference, RemoveReference, ListReferences, ClearCache | 9 tools |
| **dotnet_solution** | Solution file management | Create, Add, List, Remove | 4 tools |
| **dotnet_ef** | Entity Framework Core operations | MigrationsAdd, MigrationsList, MigrationsRemove, MigrationsScript, DatabaseUpdate, DatabaseDrop, DbContextList, DbContextInfo, DbContextScaffold | 9 tools |
| **dotnet_workload** | Workload management | List, Info, Search, Install, Update, Uninstall | 6 tools |
| **dotnet_tool** | .NET tool management | Install, List, Update, Uninstall, Restore, CreateManifest, Search, Run | 8 tools |
| **dotnet_sdk** | SDK and template information | Version, Info, ListSdks, ListRuntimes, ListTemplates, SearchTemplates, TemplateInfo, ClearTemplateCache, FrameworkInfo, CacheMetrics | 10 tools |
| **dotnet_dev_certs** | Certificates and secrets | CertificateTrust, CertificateCheck, CertificateClean, CertificateExport, SecretsInit, SecretsSet, SecretsList, SecretsRemove, SecretsClear | 9 tools |

## Migration Examples

### 1. Project Operations (dotnet_project)

#### Creating a New Project

**Before (Legacy):**
```typescript
await callTool("dotnet_project_new", {
  template: "webapi",
  name: "MyApi",
  framework: "net10.0"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_project", {
  action: "New",
  template: "webapi",
  name: "MyApi",
  framework: "net10.0"
});
```

#### Building a Project

**Before (Legacy):**
```typescript
await callTool("dotnet_project_build", {
  project: "MyApi/MyApi.csproj",
  configuration: "Release"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_project", {
  action: "Build",
  project: "MyApi/MyApi.csproj",
  configuration: "Release"
});
```

#### Running Tests

**Before (Legacy):**
```typescript
await callTool("dotnet_project_test", {
  project: "MyApi.Tests/MyApi.Tests.csproj",
  configuration: "Debug",
  filter: "Category=Unit"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_project", {
  action: "Test",
  project: "MyApi.Tests/MyApi.Tests.csproj",
  configuration: "Debug",
  filter: "Category=Unit"
});
```

#### Publishing for Deployment

**Before (Legacy):**
```typescript
await callTool("dotnet_project_publish", {
  project: "MyApi/MyApi.csproj",
  configuration: "Release",
  runtime: "linux-x64",
  output: "./publish"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_project", {
  action: "Publish",
  project: "MyApi/MyApi.csproj",
  configuration: "Release",
  runtime: "linux-x64",
  output: "./publish"
});
```

#### Complete Action Reference

| Legacy Tool | Consolidated Action | Notes |
|-------------|-------------------|-------|
| `dotnet_project_new` | `action: "New"` | Same parameters |
| `dotnet_project_restore` | `action: "Restore"` | Same parameters |
| `dotnet_project_build` | `action: "Build"` | Same parameters |
| `dotnet_project_run` | `action: "Run"` | Same parameters |
| `dotnet_project_test` | `action: "Test"` | Same parameters |
| `dotnet_project_publish` | `action: "Publish"` | Same parameters |
| `dotnet_project_clean` | `action: "Clean"` | Same parameters |
| `dotnet_project_analyze` | `action: "Analyze"` | Same parameters |
| `dotnet_project_dependencies` | `action: "Dependencies"` | Same parameters |
| `dotnet_project_validate` | `action: "Validate"` | Same parameters |
| `dotnet_pack_create` | `action: "Pack"` | Same parameters |
| `dotnet_watch_run` | `action: "Watch"` | Use `watchAction: "run"` |
| `dotnet_watch_test` | `action: "Watch"` | Use `watchAction: "test"` |
| `dotnet_watch_build` | `action: "Watch"` | Use `watchAction: "build"` |
| `dotnet_format` | `action: "Format"` | Same parameters |

### 2. Package Management (dotnet_package)

#### Adding a Package

**Before (Legacy):**
```typescript
await callTool("dotnet_package_add", {
  packageId: "Serilog.AspNetCore",
  project: "MyApi/MyApi.csproj",
  version: "8.0.0"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_package", {
  action: "Add",
  packageId: "Serilog.AspNetCore",
  project: "MyApi/MyApi.csproj",
  version: "8.0.0"
});
```

#### Searching for Packages

**Before (Legacy):**
```typescript
await callTool("dotnet_package_search", {
  searchTerm: "entityframework",
  take: 10,
  prerelease: false
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_package", {
  action: "Search",
  searchTerm: "entityframework",
  take: 10,
  prerelease: false
});
```

#### Adding Project References

**Before (Legacy):**
```typescript
await callTool("dotnet_reference_add", {
  project: "MyApi.Tests/MyApi.Tests.csproj",
  reference: "MyApi/MyApi.csproj"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_package", {
  action: "AddReference",
  project: "MyApi.Tests/MyApi.Tests.csproj",
  referencePath: "MyApi/MyApi.csproj"
});
```

#### Complete Action Reference

| Legacy Tool | Consolidated Action | Notes |
|-------------|-------------------|-------|
| `dotnet_package_add` | `action: "Add"` | Same parameters |
| `dotnet_package_remove` | `action: "Remove"` | Same parameters |
| `dotnet_package_search` | `action: "Search"` | Same parameters |
| `dotnet_package_update` | `action: "Update"` | Same parameters |
| `dotnet_package_list` | `action: "List"` | Same parameters |
| `dotnet_reference_add` | `action: "AddReference"` | Parameter: `referencePath` |
| `dotnet_reference_remove` | `action: "RemoveReference"` | Parameter: `referencePath` |
| `dotnet_reference_list` | `action: "ListReferences"` | Same parameters |
| `dotnet_nuget_locals` | `action: "ClearCache"` | Parameter: `cacheType` |

### 3. Solution Management (dotnet_solution)

#### Creating a Solution

**Before (Legacy):**
```typescript
await callTool("dotnet_solution_create", {
  name: "MyApp",
  output: "./src",
  format: "slnx"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_solution", {
  action: "Create",
  name: "MyApp",
  output: "./src",
  format: "slnx"
});
```

#### Adding Projects to Solution

**Before (Legacy):**
```typescript
await callTool("dotnet_solution_add", {
  solution: "MyApp.slnx",
  projects: ["MyApi/MyApi.csproj", "MyWeb/MyWeb.csproj"]
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_solution", {
  action: "Add",
  solution: "MyApp.slnx",
  projects: ["MyApi/MyApi.csproj", "MyWeb/MyWeb.csproj"]
});
```

#### Complete Action Reference

| Legacy Tool | Consolidated Action | Notes |
|-------------|-------------------|-------|
| `dotnet_solution_create` | `action: "Create"` | Same parameters |
| `dotnet_solution_add` | `action: "Add"` | Same parameters |
| `dotnet_solution_list` | `action: "List"` | Same parameters |
| `dotnet_solution_remove` | `action: "Remove"` | Same parameters |

### 4. Entity Framework Core (dotnet_ef)

#### Adding a Migration

**Before (Legacy):**
```typescript
await callTool("dotnet_ef_migrations_add", {
  name: "InitialCreate",
  project: "MyApi/MyApi.csproj",
  outputDir: "Data/Migrations"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_ef", {
  action: "MigrationsAdd",
  name: "InitialCreate",
  project: "MyApi/MyApi.csproj",
  outputDir: "Data/Migrations"
});
```

#### Updating Database

**Before (Legacy):**
```typescript
await callTool("dotnet_ef_database_update", {
  project: "MyApi/MyApi.csproj",
  migration: "InitialCreate"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_ef", {
  action: "DatabaseUpdate",
  project: "MyApi/MyApi.csproj",
  migration: "InitialCreate"
});
```

#### Scaffolding from Database

**Before (Legacy):**
```typescript
await callTool("dotnet_ef_dbcontext_scaffold", {
  connectionString: "Server=localhost;Database=MyDb;Trusted_Connection=true;",
  provider: "Microsoft.EntityFrameworkCore.SqlServer",
  project: "MyApi/MyApi.csproj",
  outputDir: "Models",
  contextName: "MyDbContext"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_ef", {
  action: "DbContextScaffold",
  connectionString: "Server=localhost;Database=MyDb;Trusted_Connection=true;",
  provider: "Microsoft.EntityFrameworkCore.SqlServer",
  project: "MyApi/MyApi.csproj",
  outputDir: "Models",
  contextName: "MyDbContext"
});
```

#### Complete Action Reference

| Legacy Tool | Consolidated Action | Notes |
|-------------|-------------------|-------|
| `dotnet_ef_migrations_add` | `action: "MigrationsAdd"` | Same parameters |
| `dotnet_ef_migrations_list` | `action: "MigrationsList"` | Same parameters |
| `dotnet_ef_migrations_remove` | `action: "MigrationsRemove"` | Same parameters |
| `dotnet_ef_migrations_script` | `action: "MigrationsScript"` | Same parameters |
| `dotnet_ef_database_update` | `action: "DatabaseUpdate"` | Same parameters |
| `dotnet_ef_database_drop` | `action: "DatabaseDrop"` | Same parameters |
| `dotnet_ef_dbcontext_list` | `action: "DbContextList"` | Same parameters |
| `dotnet_ef_dbcontext_info` | `action: "DbContextInfo"` | Same parameters |
| `dotnet_ef_dbcontext_scaffold` | `action: "DbContextScaffold"` | Same parameters |

### 5. Workload Management (dotnet_workload)

#### Installing a Workload

**Before (Legacy):**
```typescript
await callTool("dotnet_workload_install", {
  workloadIds: "maui-android,maui-ios"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_workload", {
  action: "Install",
  workloadIds: "maui-android,maui-ios"
});
```

#### Searching for Workloads

**Before (Legacy):**
```typescript
await callTool("dotnet_workload_search", {
  searchTerm: "maui"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_workload", {
  action: "Search",
  searchTerm: "maui"
});
```

#### Complete Action Reference

| Legacy Tool | Consolidated Action | Notes |
|-------------|-------------------|-------|
| `dotnet_workload_list` | `action: "List"` | Same parameters |
| `dotnet_workload_info` | `action: "Info"` | Same parameters |
| `dotnet_workload_search` | `action: "Search"` | Same parameters |
| `dotnet_workload_install` | `action: "Install"` | Same parameters |
| `dotnet_workload_update` | `action: "Update"` | Same parameters |
| `dotnet_workload_uninstall` | `action: "Uninstall"` | Same parameters |

### 6. Tool Management (dotnet_tool)

#### Installing a Tool

**Before (Legacy):**
```typescript
await callTool("dotnet_tool_install", {
  packageId: "dotnet-ef",
  global: true
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_tool", {
  action: "Install",
  packageId: "dotnet-ef",
  global: true
});
```

#### Searching for Tools

**Before (Legacy):**
```typescript
await callTool("dotnet_tool_search", {
  searchTerm: "format"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_tool", {
  action: "Search",
  searchTerm: "format"
});
```

#### Complete Action Reference

| Legacy Tool | Consolidated Action | Notes |
|-------------|-------------------|-------|
| `dotnet_tool_install` | `action: "Install"` | Same parameters |
| `dotnet_tool_list` | `action: "List"` | Same parameters |
| `dotnet_tool_update` | `action: "Update"` | Same parameters |
| `dotnet_tool_uninstall` | `action: "Uninstall"` | Same parameters |
| `dotnet_tool_restore` | `action: "Restore"` | Same parameters |
| `dotnet_tool_manifest_create` | `action: "CreateManifest"` | Same parameters |
| `dotnet_tool_search` | `action: "Search"` | Same parameters |
| `dotnet_tool_run` | `action: "Run"` | Same parameters |

### 7. SDK Information (dotnet_sdk)

#### Getting SDK Version

**Before (Legacy):**
```typescript
await callTool("dotnet_sdk_version", {
  machineReadable: true
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_sdk", {
  action: "Version",
  machineReadable: true
});
```

#### Listing Templates

**Before (Legacy):**
```typescript
await callTool("dotnet_template_list", {
  forceReload: false
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_sdk", {
  action: "ListTemplates",
  forceReload: false
});
```

#### Searching for Templates

**Before (Legacy):**
```typescript
await callTool("dotnet_template_search", {
  searchTerm: "web"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_sdk", {
  action: "SearchTemplates",
  searchTerm: "web"
});
```

#### Complete Action Reference

| Legacy Tool | Consolidated Action | Notes |
|-------------|-------------------|-------|
| `dotnet_sdk_version` | `action: "Version"` | Same parameters |
| `dotnet_sdk_info` | `action: "Info"` | Same parameters |
| `dotnet_sdk_list` | `action: "ListSdks"` | Same parameters |
| `dotnet_runtime_list` | `action: "ListRuntimes"` | Same parameters |
| `dotnet_template_list` | `action: "ListTemplates"` | Same parameters |
| `dotnet_template_search` | `action: "SearchTemplates"` | Same parameters |
| `dotnet_template_info` | `action: "TemplateInfo"` | Same parameters |
| `dotnet_template_clear_cache` | `action: "ClearTemplateCache"` | Same parameters |
| `dotnet_framework_info` | `action: "FrameworkInfo"` | Same parameters |
| `dotnet_cache_metrics` | `action: "CacheMetrics"` | Same parameters |

### 8. Developer Certificates and Secrets (dotnet_dev_certs)

#### Trusting Development Certificate

**Before (Legacy):**
```typescript
await callTool("dotnet_certificate_trust", {});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_dev_certs", {
  action: "CertificateTrust"
});
```

#### Setting User Secrets

**Before (Legacy):**
```typescript
await callTool("dotnet_secrets_set", {
  key: "ConnectionStrings:DefaultConnection",
  value: "Server=localhost;Database=MyDb",
  project: "MyApi/MyApi.csproj"
});
```

**After (Consolidated):**
```typescript
await callTool("dotnet_dev_certs", {
  action: "SecretsSet",
  key: "ConnectionStrings:DefaultConnection",
  value: "Server=localhost;Database=MyDb",
  project: "MyApi/MyApi.csproj"
});
```

#### Complete Action Reference

| Legacy Tool | Consolidated Action | Notes |
|-------------|-------------------|-------|
| `dotnet_certificate_trust` | `action: "CertificateTrust"` | Same parameters |
| `dotnet_certificate_check` | `action: "CertificateCheck"` | Same parameters |
| `dotnet_certificate_clean` | `action: "CertificateClean"` | Same parameters |
| `dotnet_certificate_export` | `action: "CertificateExport"` | Same parameters |
| `dotnet_secrets_init` | `action: "SecretsInit"` | Same parameters |
| `dotnet_secrets_set` | `action: "SecretsSet"` | Same parameters |
| `dotnet_secrets_list` | `action: "SecretsList"` | Same parameters |
| `dotnet_secrets_remove` | `action: "SecretsRemove"` | Same parameters |
| `dotnet_secrets_clear` | `action: "SecretsClear"` | Same parameters |

## Common Workflow Patterns

### Creating a Complete Web Application

**Before (Legacy - 7 separate tool calls):**
```typescript
// Create solution
await callTool("dotnet_solution_create", { name: "MyApp", format: "slnx" });

// Create web API
await callTool("dotnet_project_new", { template: "webapi", name: "MyApp.Api" });

// Create tests
await callTool("dotnet_project_new", { template: "xunit", name: "MyApp.Tests" });

// Add to solution
await callTool("dotnet_solution_add", { 
  solution: "MyApp.slnx", 
  projects: ["MyApp.Api/MyApp.Api.csproj", "MyApp.Tests/MyApp.Tests.csproj"]
});

// Add EF Core
await callTool("dotnet_package_add", { 
  packageId: "Microsoft.EntityFrameworkCore.SqlServer", 
  project: "MyApp.Api/MyApp.Api.csproj" 
});

// Add project reference
await callTool("dotnet_reference_add", { 
  project: "MyApp.Tests/MyApp.Tests.csproj", 
  reference: "MyApp.Api/MyApp.Api.csproj" 
});

// Build
await callTool("dotnet_project_build", { 
  project: "MyApp.slnx", 
  configuration: "Release" 
});
```

**After (Consolidated - same operations, clearer grouping):**
```typescript
// Create solution
await callTool("dotnet_solution", { action: "Create", name: "MyApp", format: "slnx" });

// Create projects
await callTool("dotnet_project", { action: "New", template: "webapi", name: "MyApp.Api" });
await callTool("dotnet_project", { action: "New", template: "xunit", name: "MyApp.Tests" });

// Add to solution
await callTool("dotnet_solution", { 
  action: "Add",
  solution: "MyApp.slnx", 
  projects: ["MyApp.Api/MyApp.Api.csproj", "MyApp.Tests/MyApp.Tests.csproj"]
});

// Add packages and references
await callTool("dotnet_package", { 
  action: "Add",
  packageId: "Microsoft.EntityFrameworkCore.SqlServer", 
  project: "MyApp.Api/MyApp.Api.csproj" 
});

await callTool("dotnet_package", { 
  action: "AddReference",
  project: "MyApp.Tests/MyApp.Tests.csproj", 
  referencePath: "MyApp.Api/MyApp.Api.csproj" 
});

// Build
await callTool("dotnet_project", { 
  action: "Build",
  project: "MyApp.slnx", 
  configuration: "Release" 
});
```

**Benefits:**
- Same number of operations
- Clearer semantic grouping (solution ops, project ops, package ops)
- Easier to understand which domain each operation belongs to
- Better for AI assistants to reason about workflows

### Database-First Development with EF Core

**Before (Legacy - 4 separate tools):**
```typescript
await callTool("dotnet_package_add", {
  packageId: "Microsoft.EntityFrameworkCore.SqlServer",
  project: "MyApi/MyApi.csproj"
});

await callTool("dotnet_ef_dbcontext_scaffold", {
  connectionString: "Server=localhost;Database=ExistingDb;Trusted_Connection=true;",
  provider: "Microsoft.EntityFrameworkCore.SqlServer",
  project: "MyApi/MyApi.csproj",
  outputDir: "Models"
});

await callTool("dotnet_ef_dbcontext_list", {
  project: "MyApi/MyApi.csproj"
});

await callTool("dotnet_ef_dbcontext_info", {
  project: "MyApi/MyApi.csproj"
});
```

**After (Consolidated - clear domain separation):**
```typescript
await callTool("dotnet_package", {
  action: "Add",
  packageId: "Microsoft.EntityFrameworkCore.SqlServer",
  project: "MyApi/MyApi.csproj"
});

await callTool("dotnet_ef", {
  action: "DbContextScaffold",
  connectionString: "Server=localhost;Database=ExistingDb;Trusted_Connection=true;",
  provider: "Microsoft.EntityFrameworkCore.SqlServer",
  project: "MyApi/MyApi.csproj",
  outputDir: "Models"
});

await callTool("dotnet_ef", {
  action: "DbContextList",
  project: "MyApi/MyApi.csproj"
});

await callTool("dotnet_ef", {
  action: "DbContextInfo",
  project: "MyApi/MyApi.csproj"
});
```

**Benefits:**
- Clear separation: `dotnet_package` for packages, `dotnet_ef` for EF operations
- All EF operations use the same tool
- Easier to discover related EF capabilities

## AI Assistant Integration

### Natural Language Prompts

Consolidated tools work seamlessly with AI assistants. The AI will automatically use consolidated tools when appropriate:

**User Prompt:**
```text
"Create a new web API project, add Entity Framework Core, create an initial migration, and build the project"
```

**AI Response (using consolidated tools):**
```text
I'll help you create a web API with Entity Framework Core:

1. Creating web API project...
   [calls dotnet_project with action: "New"]

2. Adding Entity Framework Core packages...
   [calls dotnet_package with action: "Add"]

3. Creating initial migration...
   [calls dotnet_ef with action: "MigrationsAdd"]

4. Building project...
   [calls dotnet_project with action: "Build"]

All steps completed successfully!
```

The AI recognizes that:
- Project operations → use `dotnet_project` tool
- Package operations → use `dotnet_package` tool
- EF operations → use `dotnet_ef` tool

This is clearer than selecting from 74 individual tools.

## Troubleshooting

### Action Name Case Sensitivity

**Issue:** Action parameter is case-sensitive (uses PascalCase)

❌ **Incorrect:**
```typescript
await callTool("dotnet_project", { action: "new" }); // lowercase
await callTool("dotnet_project", { action: "NEW" }); // uppercase
```

✅ **Correct:**
```typescript
await callTool("dotnet_project", { action: "New" }); // PascalCase
```

**Solution:** Always use PascalCase for action values:
- `New`, `Build`, `Test`, `Publish`
- `MigrationsAdd`, `DatabaseUpdate`
- `CertificateTrust`, `SecretsSet`

### Invalid Action Error

If you receive an error about an invalid action:

```json
{
  "success": false,
  "errors": [{
    "code": "INVALID_PARAMS",
    "message": "Invalid action 'build'. Allowed values: New, Restore, Build, Run, Test, Publish, Clean, Analyze, Dependencies, Validate, Pack, Watch, Format",
    "category": "Validation"
  }]
}
```

**Common causes:**
1. Incorrect case (use PascalCase: `Build` not `build`)
2. Typo in action name
3. Using wrong tool (e.g., EF action on project tool)

**Solution:** Check the action reference tables in this guide for the correct spelling and case.

### Missing Required Parameters

Each action may require specific parameters:

**Example - New project without template:**
```typescript
await callTool("dotnet_project", { action: "New", name: "MyApp" });
// Error: template parameter required for New action
```

**Solution:** Refer to the original legacy tool documentation for required parameters. Consolidated tools use the same parameters as their legacy equivalents.

### Watch Action Confusion

The `Watch` action requires an additional `watchAction` parameter:

❌ **Incorrect:**
```typescript
await callTool("dotnet_project", { action: "Watch" });
```

✅ **Correct:**
```typescript
await callTool("dotnet_project", { 
  action: "Watch",
  watchAction: "run"  // or "test" or "build"
});
```

## Migration Checklist

Use this checklist when migrating code or AI prompts:

- [ ] **Identify tool usage** - Find all calls to legacy `dotnet_*` tools
- [ ] **Map to consolidated tools** - Use the action reference tables above
- [ ] **Update tool names** - Change tool name to consolidated equivalent
- [ ] **Add action parameter** - Add `action` with appropriate PascalCase value
- [ ] **Verify parameters** - Ensure all other parameters remain the same
- [ ] **Update error handling** - Action validation errors have category "Validation"
- [ ] **Test thoroughly** - Verify each migrated operation works as expected
- [ ] **Update documentation** - Update any internal docs or comments

## Search and Replace Patterns

For large-scale migrations, use these patterns (adjust for your language/framework):

### TypeScript/JavaScript

```javascript
// Pattern 1: Project tools
s/callTool\("dotnet_project_new"/callTool("dotnet_project", { action: "New"/g
s/callTool\("dotnet_project_build"/callTool("dotnet_project", { action: "Build"/g
s/callTool\("dotnet_project_test"/callTool("dotnet_project", { action: "Test"/g

// Pattern 2: Package tools
s/callTool\("dotnet_package_add"/callTool("dotnet_package", { action: "Add"/g
s/callTool\("dotnet_package_search"/callTool("dotnet_package", { action: "Search"/g

// Pattern 3: EF tools
s/callTool\("dotnet_ef_migrations_add"/callTool("dotnet_ef", { action: "MigrationsAdd"/g
s/callTool\("dotnet_ef_database_update"/callTool("dotnet_ef", { action: "DatabaseUpdate"/g
```

### Python

```python
# Pattern 1: Project tools
s/call_tool\("dotnet_project_new"/call_tool("dotnet_project", action="New"/g
s/call_tool\("dotnet_project_build"/call_tool("dotnet_project", action="Build"/g

# Pattern 2: Package tools
s/call_tool\("dotnet_package_add"/call_tool("dotnet_package", action="Add"/g
```

**Note:** These are starting patterns. You'll need to adjust for:
- Parameter object structure in your language
- Existing parameter names that might conflict
- Error handling differences

## Benefits Summary

### For AI Assistants

**Before (74 tools):**
- 74 tools to choose from
- Easy to select the wrong tool
- Hard to discover related operations
- Difficult to compose workflows

**After (8 consolidated tools):**
- 8 tools to choose from
- Clear domain-based organization
- Related operations grouped together
- Easier workflow composition
- Better tool selection accuracy

### For Developers

**Before:**
```typescript
// Unclear which tools to use
dotnet_project_new()
dotnet_package_add()
dotnet_package_search()
dotnet_reference_add()
dotnet_solution_create()
// ... 69 more tools
```

**After:**
```typescript
// Clear domain organization
dotnet_project({ action: "New" | "Build" | "Test" | ... })
dotnet_package({ action: "Add" | "Search" | "AddReference" | ... })
dotnet_solution({ action: "Create" | "Add" | ... })
// ... 5 more domain tools
```

### For Maintainers

- **Consistent patterns** - All tools follow the same structure
- **Shared validation** - Action validation reused across tools
- **Easy to extend** - Add new actions without new top-level tools
- **Better testing** - Test action dispatch logic once per domain
- **Clearer codebase** - Related code grouped in consolidated tool files

## Additional Resources

- [Tool Surface Consolidation Proposal](tool-surface-consolidation.md) - Full technical specification
- [AI Assistant Best Practices Guide](ai-assistant-guide.md) - Workflow examples and integration patterns
- [Machine-Readable Contract](machine-readable-contract.md) - JSON schema and error handling
- [Testing Guide](testing.md) - How to test consolidated tools

## Questions or Feedback?

If you have questions about migration or encounter issues:

1. Check the [troubleshooting section](#troubleshooting) above
2. Review the [action reference tables](#migration-examples) for your tool
3. Consult the [tool surface consolidation proposal](tool-surface-consolidation.md)
4. Open an issue on [GitHub](https://github.com/jongalloway/dotnet-mcp/issues)

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-09  
**Status:** Phase 1 Complete - Both legacy and consolidated tools fully supported
