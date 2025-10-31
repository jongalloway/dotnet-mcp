# Concurrency Safety Matrix

This document provides guidance for AI orchestrators and MCP clients on which .NET MCP Server tools can safely run in parallel.

## Overview

The .NET MCP Server provides 48 tools across 13 categories. Understanding which tools can run concurrently is essential for:

- **AI orchestrators** that execute multiple operations simultaneously
- **MCP clients** that batch or parallelize requests
- **Performance optimization** when working with large solutions or multiple projects

## Quick Reference

| Can Run in Parallel | Tool Categories |
|---------------------|-----------------|
| ✅ **Yes - Safe** | Read-only operations (Info, List, Search, Check) |
| ⚠️ **Conditional** | Mutating operations on different files/projects |
| ❌ **No - Unsafe** | Mutating operations on same file/project, long-running operations |

## Concurrency Safety Matrix

### Fully Thread-Safe Operations (Read-Only)

These tools **can always run in parallel** with any other tools, including themselves. They do not modify state and are safe to execute concurrently.

| Category | Tool | Description | Parallel Safe |
|----------|------|-------------|---------------|
| **SDK** | `dotnet_sdk_version` | Get .NET SDK version | ✅ Always |
| **SDK** | `dotnet_sdk_info` | Get SDK and runtime information | ✅ Always |
| **SDK** | `dotnet_sdk_list` | List installed SDKs | ✅ Always |
| **SDK** | `dotnet_runtime_list` | List installed runtimes | ✅ Always |
| **SDK** | `dotnet_help` | Get help for dotnet commands | ✅ Always |
| **Template** | `dotnet_template_list` | List installed templates | ✅ Always |
| **Template** | `dotnet_template_search` | Search for templates | ✅ Always |
| **Template** | `dotnet_template_info` | Get template details | ✅ Always |
| **Template** | `dotnet_cache_metrics` | Get cache hit/miss statistics | ✅ Always |
| **Framework** | `dotnet_framework_info` | Get framework version info | ✅ Always |
| **Package** | `dotnet_package_search` | Search NuGet packages | ✅ Always |
| **Package** | `dotnet_package_list` | List package references | ✅ Always |
| **Reference** | `dotnet_reference_list` | List project references | ✅ Always |
| **Solution** | `dotnet_solution_list` | List projects in solution | ✅ Always |
| **Tool** | `dotnet_tool_list` | List installed .NET tools | ✅ Always |
| **Tool** | `dotnet_tool_search` | Search for .NET tools | ✅ Always |
| **Security** | `dotnet_certificate_check` | Check HTTPS certificate status | ✅ Always |

**Key Characteristics:**
- No file system modifications
- No state changes
- Idempotent operations
- Can be cached safely
- Multiple concurrent executions produce identical results

### Conditionally Safe Operations (Mutating - Different Targets)

These tools **can run in parallel IF they operate on different targets** (different projects, packages, or solutions). Running them on the same target concurrently may cause conflicts.

| Category | Tool | Parallel Conditions | Risk Level |
|----------|------|---------------------|------------|
| **Project** | `dotnet_project_build` | Different projects or solutions | ⚠️ Medium |
| **Project** | `dotnet_project_restore` | Different projects | ⚠️ Medium |
| **Project** | `dotnet_project_clean` | Different projects | ⚠️ Low |
| **Project** | `dotnet_project_test` | Different test projects | ⚠️ Medium |
| **Project** | `dotnet_project_publish` | Different projects or output paths | ⚠️ Medium |
| **Project** | `dotnet_pack_create` | Different projects | ⚠️ Medium |
| **Package** | `dotnet_package_add` | Different projects | ⚠️ High |
| **Package** | `dotnet_package_remove` | Different projects | ⚠️ High |
| **Package** | `dotnet_package_update` | Different projects | ⚠️ High |
| **Reference** | `dotnet_reference_add` | Different projects | ⚠️ High |
| **Reference** | `dotnet_reference_remove` | Different projects | ⚠️ High |
| **Solution** | `dotnet_solution_add` | Different solutions | ⚠️ High |
| **Solution** | `dotnet_solution_remove` | Different solutions | ⚠️ High |
| **Tool** | `dotnet_tool_install` | Different tools or scopes (global vs local) | ⚠️ Medium |
| **Tool** | `dotnet_tool_uninstall` | Different tools | ⚠️ Medium |
| **Tool** | `dotnet_tool_update` | Different tools | ⚠️ Medium |
| **Tool** | `dotnet_tool_restore` | Different manifest files | ⚠️ Low |
| **Security** | `dotnet_certificate_export` | Different output paths | ⚠️ Low |
| **Format** | `dotnet_format` | Different projects or directories | ⚠️ Medium |
| **NuGet** | `dotnet_nuget_locals` | Different cache types | ⚠️ Low |

**Safety Guidelines:**
- ✅ **Safe**: Build Project A and Project B simultaneously (if no dependencies between them)
- ✅ **Safe**: Add different packages to different projects concurrently
- ❌ **Unsafe**: Add two packages to the same project concurrently
- ❌ **Unsafe**: Build the same project twice simultaneously
- ❌ **Unsafe**: Modify solution file from multiple tools at once

### Never Run in Parallel (Mutating - Global State or Long-Running)

These tools should **NEVER run in parallel** with themselves or similar operations, as they modify global state, run indefinitely, or create file system conflicts.

| Category | Tool | Reason | Risk Level |
|----------|------|--------|------------|
| **Project** | `dotnet_project_new` | Creates files/directories; may conflict | ❌ Critical |
| **Project** | `dotnet_project_run` | Long-running process; holds resources | ❌ Critical |
| **Solution** | `dotnet_solution_create` | Creates solution file | ❌ Critical |
| **Watch** | `dotnet_watch_run` | Long-running file watcher | ❌ Critical |
| **Watch** | `dotnet_watch_test` | Long-running file watcher | ❌ Critical |
| **Watch** | `dotnet_watch_build` | Long-running file watcher | ❌ Critical |
| **Tool** | `dotnet_tool_run` | May be long-running; depends on tool | ❌ High |
| **Security** | `dotnet_certificate_trust` | Modifies system trust store | ❌ Critical |
| **Security** | `dotnet_certificate_clean` | Removes all certificates globally | ❌ Critical |
| **Template** | `dotnet_template_clear_cache` | Clears global template cache | ❌ High |

**Key Considerations:**
- These operations often modify global state (certificates, global tools, caches)
- File watchers run indefinitely and should not be duplicated
- Project creation can conflict if output directories overlap
- Running applications hold ports and resources

## Dependency-Based Constraints

Beyond simple parallelization, consider project dependencies:

### Build Order Dependencies

```text
❌ UNSAFE: Parallel Execution Ignoring Dependencies
┌─────────────────┐     ┌─────────────────┐
│ Build Project A │     │ Build Project B │
│ (references B)  │ ──X→│                 │
└─────────────────┘     └─────────────────┘
     Run simultaneously? NO - A depends on B
```

```text
✅ SAFE: Sequential Execution Respecting Dependencies
┌─────────────────┐     ┌─────────────────┐
│ Build Project B │ ──→ │ Build Project A │
│                 │     │ (references B)  │
└─────────────────┘     └─────────────────┘
     Step 1: Build B first, then A
```

```text
✅ SAFE: Parallel Independent Projects
┌─────────────────┐     ┌─────────────────┐
│ Build Project A │     │ Build Project C │
│                 │     │                 │
└─────────────────┘     └─────────────────┘
     No dependencies? Safe to run in parallel
```

**Rules:**
- Always build dependencies before dependent projects
- Test projects can usually run in parallel if they don't share state
- Multiple independent projects in a solution can build concurrently

### Restore Before Build

Many tools have implicit ordering requirements:

1. **dotnet_project_restore** must complete before **dotnet_project_build**
2. **dotnet_tool_restore** must complete before **dotnet_tool_run**
3. **dotnet_package_add** should complete before **dotnet_project_build**

## File System Conflict Scenarios

### Same Project File (❌ Unsafe)

```text
Thread 1: dotnet_package_add → MyProject.csproj
Thread 2: dotnet_package_add → MyProject.csproj
Result: Race condition, possible corruption or lost changes
```

### Same Solution File (❌ Unsafe)

```text
Thread 1: dotnet_solution_add ProjectA → MySolution.sln
Thread 2: dotnet_solution_add ProjectB → MySolution.sln
Result: One operation may be lost or file corrupted
```

### Different Projects in Same Solution (✅ Safe with Caution)

```text
Thread 1: dotnet_project_build ProjectA
Thread 2: dotnet_project_build ProjectB
Result: Generally safe if no interdependencies, but MSBuild may serialize internally
```

### Overlapping Output Directories (❌ Unsafe)

```text
Thread 1: dotnet_project_publish → /output
Thread 2: dotnet_project_publish → /output
Result: File conflicts, overwritten outputs
```

## Resource Contention

### Port Conflicts

Running multiple web applications simultaneously can cause port conflicts:

```text
❌ UNSAFE:
Thread 1: dotnet_project_run (uses port 5000)
Thread 2: dotnet_project_run (tries to use port 5000)
Result: Second process fails with "address already in use"
```

### NuGet Package Cache

The global NuGet cache can handle concurrent access, but operations may serialize:

```text
⚠️ SLOWED BUT SAFE:
Thread 1: dotnet_package_add (downloads package X)
Thread 2: dotnet_package_add (downloads package Y)
Result: Both succeed but may be slower due to NuGet lock files
```

## Orchestrator Guidance

### Pattern 1: Parallel Read Operations

**Scenario**: Gather information about the development environment

```text
✅ SAFE - Execute in Parallel:
┌─────────────────────────┐
│ dotnet_sdk_list         │ ───┐
└─────────────────────────┘    │
┌─────────────────────────┐    │
│ dotnet_template_list    │ ───┤ All execute
└─────────────────────────┘    │ concurrently
┌─────────────────────────┐    │
│ dotnet_package_search   │ ───┘
└─────────────────────────┘
```

### Pattern 2: Sequential Project Modifications

**Scenario**: Add multiple packages to a project

```text
✅ SAFE - Execute Sequentially:
┌───────────────────────────────┐
│ dotnet_package_add "Package1" │
└───────────────────────────────┘
           ↓
┌───────────────────────────────┐
│ dotnet_package_add "Package2" │
└───────────────────────────────┘
           ↓
┌───────────────────────────────┐
│ dotnet_project_restore        │
└───────────────────────────────┘
```

### Pattern 3: Parallel Independent Projects

**Scenario**: Build multiple independent projects

```text
✅ SAFE - Execute in Parallel (if no dependencies):
┌─────────────────────────┐     ┌─────────────────────────┐
│ dotnet_project_build A  │     │ dotnet_project_build C  │
└─────────────────────────┘     └─────────────────────────┘
```

### Pattern 4: Dependency-Aware Build

**Scenario**: Build projects with dependencies

```text
✅ SAFE - Respect Dependencies:
       ┌─────────────────────────┐
       │ dotnet_project_build B  │
       │ (no dependencies)       │
       └─────────────────────────┘
                  ↓
       ┌─────────────────────────┐
       │ dotnet_project_build A  │
       │ (references B)          │
       └─────────────────────────┘
```

## Caching Considerations

The .NET MCP Server implements caching for read-only resources:

- **Templates**: Cached for 5 minutes (300 seconds)
- **SDK Info**: Cached for 5 minutes (300 seconds)
- **Runtime Info**: Cached for 5 minutes (300 seconds)

### Cache Safety

✅ **Thread-Safe**: All cache operations use `SemaphoreSlim` for async locking
✅ **Concurrent Reads**: Multiple parallel reads are safe and efficient
⚠️ **Cache Invalidation**: `dotnet_template_clear_cache` should not run concurrently with template operations

## Summary Table: Tool Parallelization

| Tool Type | Same Target | Different Targets | With Any Tool |
|-----------|-------------|-------------------|---------------|
| Read-only | ✅ Safe | ✅ Safe | ✅ Safe |
| Mutating (projects) | ❌ Unsafe | ⚠️ Conditional* | ⚠️ Conditional* |
| Long-running (watch, run) | ❌ Unsafe | ❌ Unsafe | ❌ Unsafe |
| Global state (cache, certs) | ❌ Unsafe | ❌ Unsafe | ❌ Unsafe |

\* Conditional on no dependencies or shared resources

## Best Practices for AI Orchestrators

1. **Default to Sequential** - When in doubt, execute operations sequentially
2. **Parallelize Reads** - Read-only operations can always be parallelized safely
3. **Check Dependencies** - Analyze project references before parallel builds
4. **Separate by Scope** - Parallel operations should work on separate projects/solutions
5. **Batch Similar Operations** - Group package additions into a single sequential batch
6. **Avoid Parallel Writes** - Never modify the same file from multiple threads
7. **Monitor Long-Running** - Track and manage long-running operations separately
8. **Respect Lock Files** - Be aware that NuGet and MSBuild use lock files internally

## Error Handling

When parallel operations fail, consider these common causes:

| Error Pattern | Likely Cause | Solution |
|---------------|--------------|----------|
| "File is being used by another process" | Concurrent writes to same file | Serialize operations on same file |
| "Port already in use" | Multiple run commands | Use different ports or serialize |
| "Project file could not be loaded" | Simultaneous solution modifications | Serialize solution operations |
| "Unable to acquire lock" | NuGet package restore conflicts | Retry or serialize restore operations |

## Testing Concurrency

To test concurrent tool execution:

1. Use the `dotnet_cache_metrics` tool to verify caching behavior
2. Monitor file system for lock contention
3. Check process handles for resource conflicts
4. Review MCP server logs for race conditions

## Additional Resources

- [SDK Integration Details](sdk-integration.md) - Learn about caching implementation
- [Advanced Topics](advanced-topics.md) - Performance optimization and logging
- [Model Context Protocol](https://modelcontextprotocol.io/) - Official MCP specification

## Version History

- **v1.0** (2025-10-31) - Initial concurrency safety documentation

---

**Note**: This documentation applies to .NET MCP Server v1.0+. Concurrency characteristics may change in future versions based on .NET SDK updates and MCP protocol enhancements.
