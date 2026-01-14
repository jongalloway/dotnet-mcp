# Machine-Readable JSON Contract (v1.0)

## Overview

The .NET MCP Server provides a stable machine-readable JSON contract for programmatic consumption of tool results. When the `machineReadable: true` parameter is set, all tools return structured JSON instead of plain text, enabling automated processing, error handling, and integration with AI orchestrators.

This document defines the v1.0 contract that all tools must comply with when returning machine-readable output.

## Design Principles

1. **Consistency**: All tools follow the same response structure
2. **Completeness**: Responses include all necessary information for automation
3. **Actionability**: Error responses include actionable guidance and alternatives
4. **Security**: Sensitive information is automatically redacted
5. **Backwards Compatibility**: Plain text mode remains the default

## Response Envelopes

All machine-readable responses use one of two envelope types:

### Success Envelope

Used when a command executes successfully (exit code 0).

**Schema:**

```typescript
interface SuccessResult {
  success: true;                // Always true for success
  command?: string;            // The executed command (optional, for diagnostics)
  output: string;              // Command output (required)
  exitCode: 0;                 // Always 0 for success
  metadata?: Record<string, string>; // Optional metadata (tool-specific context)
}
```

**Required Fields:**

- `success` - Always `true`
- `output` - Command output (may be empty string)
- `exitCode` - Always `0`

**Optional Fields:**

- `command` - The executed dotnet command (included for logging/diagnostics)
- `metadata` - Tool-specific metadata providing additional context (e.g., test runner selection)

**Example:**

```json
{
  "success": true,
  "command": "dotnet test --project MyTests.csproj",
  "output": "Test run passed",
  "exitCode": 0,
  "metadata": {
    "selectedTestRunner": "microsoft-testing-platform",
    "projectArgumentStyle": "--project",
    "selectionSource": "global.json"
  }
}
```

### Error Envelope

Used when a command fails (non-zero exit code) or validation fails.

**Schema:**

```typescript
interface ErrorResponse {
  success: false;              // Always false for errors
  errors: ErrorResult[];       // Array of one or more errors (required)
  exitCode: number;            // Process exit code (required)
  metadata?: Record<string, string>; // Optional metadata (tool-specific context)
}

interface ErrorResult {
  code: string;                // Error code (required)
  message: string;             // Human-readable error message (required)
  category: string;            // Error category (required)
  hint?: string;               // Suggestion for fixing (optional)
  explanation?: string;        // Plain English explanation (optional)
  documentationUrl?: string;   // Link to docs (optional)
  suggestedFixes?: string[];   // Suggested fix commands (optional)
  alternatives?: string[];     // Alternative actions (optional)
  rawOutput: string;           // Original output, redacted (required)
  mcpErrorCode?: number;       // MCP error code per JSON-RPC 2.0 (optional)
  data?: ErrorData;            // Structured error data (optional)
}

interface ErrorData {
  command?: string;            // The executed command, redacted (optional)
  exitCode?: number;           // Process exit code (optional)
  stderr?: string;             // Standard error output, redacted (optional)
  additionalData?: Record<string, string>; // Context-specific data (optional)
}
```

**Required Fields:**

- `success` - Always `false`
- `errors` - Array with at least one error
- `exitCode` - Process exit code (or -1 for pre-execution errors)
- Each error must have: `code`, `message`, `category`, `rawOutput`

**Optional Fields:**

- `metadata` - Tool-specific metadata (e.g., for dotnet_project Test action, includes test runner selection)
- `hint`, `explanation`, `documentationUrl`, `suggestedFixes`, `alternatives`
- `mcpErrorCode` - Maps to JSON-RPC 2.0 error codes
- `data` - Structured data for programmatic error handling

## Error Categories

Errors are classified into the following categories:

| Category | Description | Example Codes |
|----------|-------------|---------------|
| `Validation` | Parameter validation failures (before execution) | `INVALID_PARAMS` |
| `Compilation` | C# compiler errors | `CS0103`, `CS1001`, `CS0246` |
| `Build` | MSBuild errors | `MSB3644`, `MSB4236`, `MSB1003` |
| `Package` | NuGet package errors | `NU1101`, `NU1102`, `NU1605` |
| `Runtime` | .NET SDK/host/runtime errors not covered by Build/Package/Compilation | `NETSDK1045`, `NETSDK1004` |
| `Capability` | Feature not available in current environment | `CAPABILITY_NOT_AVAILABLE` |
| `Concurrency` | Resource locked by another operation | `CONCURRENCY_CONFLICT` |
| `Cancellation` | Operation cancelled by user | `OPERATION_CANCELLED` |
| `Unknown` | Unclassified errors | `EXIT_1`, `EXIT_127` |

## Common Error Scenarios

### 1. Validation Failures

Validation errors occur **before** command execution when parameters are invalid.

**Characteristics:**

- Error code: `INVALID_PARAMS`
- Category: `Validation`
- Exit code: `-1` (no command executed)
- MCP error code: `-32602` (InvalidParams)
- `data.command` is `null` (no command was executed)
- `data.additionalData` contains `parameter` and `reason` fields

**Example: Empty Required Parameter**

```json
{
  "success": false,
  "errors": [
    {
      "code": "INVALID_PARAMS",
      "message": "Parameter 'packageName' is required and cannot be empty",
      "category": "Validation",
      "hint": "Verify the parameter values and try again.",
      "rawOutput": "",
      "mcpErrorCode": -32602,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "parameter": "packageName",
          "reason": "required"
        }
      }
    }
  ],
  "exitCode": -1
}
```

**Example: Invalid Parameter Value**

```json
{
  "success": false,
  "errors": [
    {
      "code": "INVALID_PARAMS",
      "message": "Parameter 'format' must be 'pfx' or 'pem'",
      "category": "Validation",
      "hint": "Verify the parameter values and try again.",
      "rawOutput": "",
      "mcpErrorCode": -32602,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "parameter": "format",
          "reason": "invalid value"
        }
      }
    }
  ],
  "exitCode": -1
}
```

**Example: Invalid Characters in Parameter**

```json
{
  "success": false,
  "errors": [
    {
      "code": "INVALID_PARAMS",
      "message": "Parameter 'additionalOptions' contains invalid characters",
      "category": "Validation",
      "hint": "Verify the parameter values and try again.",
      "rawOutput": "",
      "mcpErrorCode": -32602,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "parameter": "additionalOptions",
          "reason": "invalid characters"
        }
      }
    }
  ],
  "exitCode": -1
}
```

#### Action Parameter Validation (Consolidated Tools)

**Consolidated tools** use an `action` parameter (enum) to select which operation to perform. Action validation occurs before execution and follows the same pattern as other parameter validation.

**Characteristics:**

- Error code: `INVALID_PARAMS`
- Category: `Validation`
- Exit code: `-1`
- `data.additionalData` contains `parameter: "action"` and `reason: "invalid value"`

**Example: Invalid Action Value**

```json
{
  "success": false,
  "errors": [
    {
      "code": "INVALID_PARAMS",
      "message": "Invalid action 'build' is not supported. For tool 'dotnet_project'.",
      "category": "Validation",
      "hint": "Valid actions are: New, Restore, Build, Run, Test, Publish, Clean, Analyze, Dependencies, Validate, Pack, Watch, Format",
      "rawOutput": "",
      "mcpErrorCode": -32602,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "parameter": "action",
          "providedValue": "build",
          "validActions": "New, Restore, Build, Run, Test, Publish, Clean, Analyze, Dependencies, Validate, Pack, Watch, Format"
        }
      }
    }
  ],
  "exitCode": -1
}
```

**Note:** Action values are case-sensitive and must use PascalCase (e.g., `"New"` not `"new"`, `"MigrationsAdd"` not `"migrations_add"`).

**Example: Successful Consolidated Tool Call**

Request:

```typescript
await callTool("dotnet_project", {
  action: "New",
  template: "webapi",
  name: "MyApi",
  machineReadable: true
});
```

Response:

```json
{
  "success": true,
  "command": "dotnet new webapi -n MyApi",
  "output": "The template \"ASP.NET Core Web API\" was created successfully.\n\nProcessing post-creation actions...",
  "exitCode": 0
}
```

### 2. Command Execution Failures

Command execution errors occur when the dotnet CLI command runs but fails.

**Characteristics:**

- Error code: Varies (`CS####`, `MSB####`, `NU####`, `EXIT_N`)
- Category: `Compilation`, `Build`, `Package`, `Unknown`
- Exit code: Non-zero process exit code
- `data.command` contains the executed command (redacted)
- `data.stderr` contains error output (redacted)
- May include enhanced diagnostics (explanation, docs, suggested fixes)

**Example: Package Not Found**

```json
{
  "success": false,
  "errors": [
    {
      "code": "NU1101",
      "message": "Unable to find package 'NonExistentPackage'. No packages exist with this id in source(s): nuget.org",
      "category": "Package",
      "hint": "Unable to find package. Check package name and source.",
      "explanation": "The specified NuGet package does not exist in the configured package sources.",
      "documentationUrl": "https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1101",
      "suggestedFixes": [
        "Verify the package name spelling",
        "Check if the package exists on nuget.org",
        "Try searching: dotnet package search NonExistentPackage"
      ],
      "rawOutput": "error NU1101: Unable to find package 'NonExistentPackage'...",
      "mcpErrorCode": -32002,
      "data": {
        "command": "dotnet add package NonExistentPackage",
        "exitCode": 1,
        "stderr": "error NU1101: Unable to find package..."
      }
    }
  ],
  "exitCode": 1
}
```

**Example: Compilation Error**

```json
{
  "success": false,
  "errors": [
    {
      "code": "CS0103",
      "message": "The name 'Console' does not exist in the current context",
      "category": "Compilation",
      "hint": "The name does not exist in the current context. Check for typos or missing using directives.",
      "explanation": "The compiler could not find the specified identifier in the current scope.",
      "documentationUrl": "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103",
      "suggestedFixes": [
        "Add 'using System;' directive",
        "Check for typos in the identifier name",
        "Verify the namespace is referenced"
      ],
      "rawOutput": "Program.cs(5,9): error CS0103: The name 'Console' does not exist...",
      "data": {
        "command": "dotnet build MyProject.csproj",
        "exitCode": 1,
        "stderr": "Program.cs(5,9): error CS0103..."
      }
    }
  ],
  "exitCode": 1
}
```

**Example: Build Error**

```json
{
  "success": false,
  "errors": [
    {
      "code": "MSB3644",
      "message": "The reference assemblies for .NETFramework,Version=v4.7.2 were not found",
      "category": "Build",
      "hint": "The reference assemblies were not found. Install the .NET SDK or targeting pack for the specified framework.",
      "explanation": "MSBuild could not locate the reference assemblies needed to build for the target framework.",
      "documentationUrl": "https://learn.microsoft.com/en-us/visualstudio/msbuild/errors/msb3644",
      "suggestedFixes": [
        "Install the .NET Framework 4.7.2 Developer Pack",
        "Update TargetFramework to a supported version like net8.0",
        "Install Visual Studio with .NET Framework development workload"
      ],
      "rawOutput": "error MSB3644: The reference assemblies for .NETFramework,Version=v4.7.2...",
      "data": {
        "command": "dotnet build LegacyProject.csproj",
        "exitCode": 1,
        "stderr": "error MSB3644: The reference assemblies..."
      }
    }
  ],
  "exitCode": 1
}
```

### 3. Capability Not Available

Capability errors occur when a feature exists but cannot be executed in the current environment.

**Characteristics:**

- Error code: `CAPABILITY_NOT_AVAILABLE`
- Category: `Capability`
- Exit code: `-1`
- MCP error code: `-32001` (CapabilityNotAvailable) or `-32603` (InternalError), depending on the factory method overload used
- `alternatives` array provides actionable suggestions
- `data.additionalData` contains `feature` and `reason` (when using the reason-based overload)
- `data.command` and `data.stderr` may be present (when using the command/details overload)

**Example: Feature Not Yet Implemented**

```json
{
  "success": false,
  "errors": [
    {
      "code": "CAPABILITY_NOT_AVAILABLE",
      "message": "The 'telemetry reporting' capability is not available: Not yet implemented - planned for future release",
      "category": "Capability",
      "hint": "Consider using one of the suggested alternatives.",
      "explanation": "This tool/feature exists but cannot be executed in the current environment or configuration. This may be due to missing dependencies, disabled feature flags, OS limitations, or features that are planned but not yet implemented.",
      "alternatives": [
        "Use dotnet_server_capabilities to check current feature support",
        "Monitor SDK usage manually through build logs",
        "Use external telemetry tools like Application Insights"
      ],
      "rawOutput": "",
      "mcpErrorCode": -32603,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "feature": "telemetry reporting",
          "reason": "Not yet implemented - planned for future release"
        }
      }
    }
  ],
  "exitCode": -1
}
```

**Example: Missing dotnet CLI**

```json
{
  "success": false,
  "errors": [
    {
      "code": "CAPABILITY_NOT_AVAILABLE",
      "message": "Capability 'dotnet CLI' is not available in the current environment. Details: The system cannot find the file specified",
      "category": "Capability",
      "hint": "Try one of the alternatives or adjust the environment to enable this capability.",
      "alternatives": [
        "Install the .NET SDK from https://dotnet.microsoft.com/download",
        "Verify 'dotnet' is on PATH (try: dotnet --info)",
        "If using global.json, ensure the requested SDK is installed"
      ],
      "rawOutput": "",
      "mcpErrorCode": -32001,
      "data": {
        "command": "dotnet --version",
        "exitCode": -1,
        "stderr": "The system cannot find the file specified"
      }
    }
  ],
  "exitCode": -1
}
```

**Example: OS Platform Limitation**

```json
{
  "success": false,
  "errors": [
    {
      "code": "CAPABILITY_NOT_AVAILABLE",
      "message": "The 'Windows authentication' capability is not available: Requires Windows operating system",
      "category": "Capability",
      "hint": "Consider using one of the suggested alternatives.",
      "explanation": "This tool/feature exists but cannot be executed in the current environment or configuration. This may be due to missing dependencies, disabled feature flags, OS limitations, or features that are planned but not yet implemented.",
      "alternatives": [
        "Use JWT authentication instead",
        "Configure OAuth 2.0 authentication"
      ],
      "rawOutput": "",
      "mcpErrorCode": -32603,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "feature": "Windows authentication",
          "reason": "Requires Windows operating system"
        }
      }
    }
  ],
  "exitCode": -1
}
```

### 4. Concurrency Conflicts

Concurrency errors occur when a resource is locked by another operation.

**Characteristics:**

- Error code: `CONCURRENCY_CONFLICT`
- Category: `Concurrency`
- Exit code: `-1`
- `data.additionalData` contains operation details

**Example:**

```json
{
  "success": false,
  "errors": [
    {
      "code": "CONCURRENCY_CONFLICT",
      "message": "Cannot execute 'build' on 'MyProject.csproj' because a conflicting operation is already in progress: restore in progress",
      "category": "Concurrency",
      "hint": "Wait for the conflicting operation to complete, or cancel it before retrying this operation.",
      "rawOutput": "",
      "mcpErrorCode": -32603,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "operationType": "build",
          "target": "MyProject.csproj",
          "conflictingOperation": "restore in progress"
        }
      }
    }
  ],
  "exitCode": -1
}
```

### 5. Operation Cancelled

Cancellation errors occur when a user cancels a long-running operation.

**Characteristics:**

- Error code: `OPERATION_CANCELLED`
- Category: `Cancellation`
- Exit code: `-1`
- May include partial output in `rawOutput`

**Example:**

```json
{
  "success": false,
  "errors": [
    {
      "code": "OPERATION_CANCELLED",
      "message": "The operation was cancelled by the user",
      "category": "Cancellation",
      "hint": "The command was terminated before completion",
      "rawOutput": "Restoring packages for MyProject.csproj...\n[Output truncated]",
      "mcpErrorCode": -32603,
      "data": {
        "command": "dotnet restore MyProject.csproj",
        "exitCode": -1
      }
    }
  ],
  "exitCode": -1
}
```

## MCP Error Code Mapping

Machine-readable responses map to JSON-RPC 2.0 error codes for MCP compatibility:

| Error Category | MCP Error Code(s) | Description |
|----------------|-------------------|-------------|
| Validation (`INVALID_PARAMS`) | `-32602` | InvalidParams - Invalid method parameters |
| Capability Not Available | `-32001`, `-32603` | CapabilityNotAvailable (`-32001`) or InternalError (`-32603`) depending on factory method |
| Concurrency Conflict | `-32603` | InternalError - Resource temporarily unavailable |
| Cancellation | `-32603` | InternalError - Operation interrupted |
| Command Execution Failures | `-32603` or `-32002` | InternalError for generic failures, ResourceNotFound for missing resources (e.g., NU1101, MSB1003) |

**Notes:**

- Not all errors have MCP error codes. The `mcpErrorCode` field is optional and only set when applicable.
- `CAPABILITY_NOT_AVAILABLE` may surface as either `-32001` (when using the factory overload that accepts feature, alternatives, command, and details) or `-32603` (when using the overload with feature, reason, and alternatives). Clients should treat both codes as representing the same high-level capability-not-available condition.
- Resource-not-found errors (NU1101, NU1102, MSB1003, NETSDK1004, MSB4236) use `-32002` (ResourceNotFound).
- Compilation and build errors that are not resource-related (e.g., CS0103, MSB3644) typically have no MCP error code (`null`).

## Security and Redaction

All machine-readable output applies **automatic security redaction** to protect sensitive information:

**Redacted Patterns:**

- Connection strings (database credentials, passwords)
- API keys and tokens (Azure, AWS, SendGrid, etc.)
- Certificates and private keys
- Passwords and secrets
- Authorization headers

**Redacted Fields:**

- `command` - Command arguments with secrets removed
- `output` - Command output with secrets removed
- `stderr` - Error output with secrets removed
- `rawOutput` - Original output with secrets removed

**Example Redaction:**

Before:

```
Server=localhost;Database=MyDb;User=admin;Password=SuperSecret123!
```

After:

```
Server=localhost;Database=MyDb;User=admin;Password=[REDACTED]
```

To disable redaction for debugging (use with caution):

```csharp
await _tools.DotnetSdkVersion(machineReadable: true, unsafeOutput: true);
```

## Consolidated Tools Examples

The following examples demonstrate machine-readable responses from consolidated tools, which group related operations using action enums.

### Creating a Project with Consolidated Tools

**Request:**

```typescript
await callTool("dotnet_project", {
  action: "New",
  template: "webapi",
  name: "ProductApi",
  framework: "net10.0",
  machineReadable: true
});
```

**Success Response:**

```json
{
  "success": true,
  "command": "dotnet new webapi -n ProductApi --framework net10.0",
  "output": "The template \"ASP.NET Core Web API\" was created successfully.\n\nProcessing post-creation actions...\nRestoring NuGet packages...\nRestore completed in 2.3 sec for ProductApi.csproj.",
  "exitCode": 0
}
```

### Building a Project

**Request:**

```typescript
await callTool("dotnet_project", {
  action: "Build",
  project: "ProductApi/ProductApi.csproj",
  configuration: "Release",
  machineReadable: true
});
```

**Success Response:**

```json
{
  "success": true,
  "command": "dotnet build ProductApi/ProductApi.csproj -c Release",
  "output": "Microsoft (R) Build Engine version 10.0.100+xyz\nBuild succeeded.\n    0 Warning(s)\n    0 Error(s)\n\nTime Elapsed 00:00:03.45",
  "exitCode": 0
}
```

### Managing Packages with Consolidated Tools

**Request:**

```typescript
await callTool("dotnet_package", {
  action: "Search",
  searchTerm: "serilog",
  take: 3,
  machineReadable: true
});
```

**Success Response:**

```json
{
  "success": true,
  "command": "dotnet package search serilog --take 3",
  "output": "Package ID                        | Latest Version | Total Downloads\n----------------------------------+----------------+----------------\nSerilog                           | 4.0.0          | 250M+\nSerilog.AspNetCore                | 8.0.0          | 180M+\nSerilog.Sinks.Console             | 5.0.0          | 200M+",
  "exitCode": 0
}
```

**Adding a package:**

```typescript
await callTool("dotnet_package", {
  action: "Add",
  packageId: "Serilog.AspNetCore",
  project: "ProductApi/ProductApi.csproj",
  machineReadable: true
});
```

**Success Response:**

```json
{
  "success": true,
  "command": "dotnet add ProductApi/ProductApi.csproj package Serilog.AspNetCore",
  "output": "Determining projects to restore...\nWriting /tmp/tmpXYZ.tmp\ninfo : Adding PackageReference for package 'Serilog.AspNetCore' into project 'ProductApi.csproj'.\ninfo : Restoring packages for ProductApi.csproj...\ninfo : Package 'Serilog.AspNetCore' is compatible with all the specified frameworks.\ninfo : PackageReference for package 'Serilog.AspNetCore' version '8.0.0' added to file 'ProductApi.csproj'.",
  "exitCode": 0
}
```

### Entity Framework with Consolidated Tools

**Request:**

```typescript
await callTool("dotnet_ef", {
  action: "MigrationsAdd",
  name: "InitialCreate",
  project: "ProductApi/ProductApi.csproj",
  machineReadable: true
});
```

**Success Response:**

```json
{
  "success": true,
  "command": "dotnet ef migrations add InitialCreate --project ProductApi/ProductApi.csproj",
  "output": "Build started...\nBuild succeeded.\nDone. To undo this action, use 'ef migrations remove'",
  "exitCode": 0
}
```

**Invalid action example:**

```typescript
await callTool("dotnet_ef", {
  action: "InvalidAction",
  project: "ProductApi/ProductApi.csproj",
  machineReadable: true
});
```

**Error Response:**

```json
{
  "success": false,
  "errors": [
    {
      "code": "INVALID_PARAMS",
      "message": "Invalid action 'InvalidAction'. Allowed values: MigrationsAdd, MigrationsList, MigrationsRemove, MigrationsScript, DatabaseUpdate, DatabaseDrop, DbContextList, DbContextInfo, DbContextScaffold",
      "category": "Validation",
      "hint": "Verify the parameter values and try again.",
      "rawOutput": "",
      "mcpErrorCode": -32602,
      "data": {
        "exitCode": -1,
        "additionalData": {
          "parameter": "action",
          "reason": "invalid value"
        }
      }
    }
  ],
  "exitCode": -1
}
```

### Solution Management

**Request:**

```typescript
await callTool("dotnet_solution", {
  action: "Create",
  name: "MyApp",
  format: "slnx",
  machineReadable: true
});
```

**Success Response:**

```json
{
  "success": true,
  "command": "dotnet new sln -n MyApp --use-slnx",
  "output": "The template \"Solution File\" was created successfully.",
  "exitCode": 0
}
```

**Request:**

```typescript
await callTool("dotnet_solution", {
  action: "Add",
  solution: "MyApp.slnx",
  projects: ["ProductApi/ProductApi.csproj", "ProductApi.Tests/ProductApi.Tests.csproj"],
  machineReadable: true
});
```

**Success Response:**

```json
{
  "success": true,
  "command": "dotnet sln MyApp.slnx add ProductApi/ProductApi.csproj ProductApi.Tests/ProductApi.Tests.csproj",
  "output": "Project `ProductApi/ProductApi.csproj` added to the solution.\nProject `ProductApi.Tests/ProductApi.Tests.csproj` added to the solution.",
  "exitCode": 0
}
```

## Tool-Specific Metadata

Some tools include additional metadata in both success and error responses to provide context about how the operation was performed. This is particularly useful for AI orchestrators to understand the decisions made during execution.

### dotnet_project Test Action Metadata

The `dotnet_project` Test action includes metadata about test runner selection when `machineReadable: true`:

**Metadata Fields:**

- `selectedTestRunner`: The test runner that was used (`microsoft-testing-platform` or `vstest`)
- `projectArgumentStyle`: The argument style used (`--project`, `positional`, or `none`)
- `selectionSource`: How the runner was selected (`global.json`, `testRunner-parameter`, `useLegacyProjectArgument-parameter`, or `default`)

**Success Example with Metadata:**

```json
{
  "success": true,
  "command": "dotnet test --project MyTests.csproj",
  "output": "Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 1.2s",
  "exitCode": 0,
  "metadata": {
    "selectedTestRunner": "microsoft-testing-platform",
    "projectArgumentStyle": "--project",
    "selectionSource": "global.json"
  }
}
```

**Error Example with Metadata:**

```json
{
  "success": false,
  "errors": [
    {
      "code": "MSB1001",
      "message": "Unknown switch.",
      "category": "Build",
      "hint": "Unrecognized option. Check the command syntax.",
      "rawOutput": "MSBUILD : error MSB1001: Unknown switch.\nSwitch: --project",
      "data": {
        "command": "dotnet test --project MyTests.csproj",
        "exitCode": 1,
        "stderr": "MSBUILD : error MSB1001: Unknown switch."
      }
    }
  ],
  "exitCode": 1,
  "metadata": {
    "selectedTestRunner": "microsoft-testing-platform",
    "projectArgumentStyle": "--project",
    "selectionSource": "testRunner-parameter"
  }
}
```

**Metadata Selection Sources:**

- `global.json` - MTP detected from `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in global.json
- `testRunner-parameter` - Explicit `testRunner` parameter value used (MicrosoftTestingPlatform or VSTest)
- `useLegacyProjectArgument-parameter` - Legacy `useLegacyProjectArgument: true` parameter used (deprecated)
- `default` - No configuration found, defaulted to VSTest for legacy compatibility

This metadata helps AI orchestrators understand why a particular command failed and what to try next.

## Compliance Requirements

All tools with a `machineReadable` parameter must:

1. **Return valid JSON** when `machineReadable: true`
2. **Use SuccessResult** for exit code 0
3. **Use ErrorResponse** for non-zero exit codes or validation failures
4. **Include required fields** in all envelopes
5. **Apply security redaction** to all output (unless `unsafeOutput: true`)
6. **Set appropriate error categories** based on error type
7. **Include actionable guidance** in hints and alternatives when possible

## Testing Compliance

Tools can be validated using the test utilities:

```csharp
using DotNetMcp.Tests;

[Fact]
public async Task MyTool_WithMachineReadableTrue_ReturnsValidJson()
{
    var result = await _tools.MyTool(machineReadable: true);
    
    Assert.True(TryParseJson(result, out var jsonDoc));
    var root = jsonDoc.RootElement;
    
    Assert.True(root.TryGetProperty("success", out _));
    Assert.True(root.TryGetProperty("exitCode", out _));
}
```

## Version History

### v1.0 (Current)

- Initial stable contract
- Success envelope: `SuccessResult`
- Error envelope: `ErrorResponse` with `ErrorResult[]`
- Error categories: Validation, Compilation, Build, Package, Runtime, Capability, Concurrency, Cancellation
- MCP error code mapping for JSON-RPC 2.0 compatibility
- Automatic security redaction
- Enhanced error diagnostics (explanation, documentation URLs, suggested fixes)

## Related Documentation

- [Error Diagnostics](error-diagnostics.md) - Enhanced error diagnostics for 52+ error codes
- [Capability Not Available Strategy](capability-not-available.md) - Detailed guide for capability errors
- [Advanced Topics](advanced-topics.md) - Security redaction implementation details
- [Testing Guide](testing.md) - How to test machine-readable output

## References

- [Model Context Protocol](https://modelcontextprotocol.io/) - MCP specification
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification) - Error code standards
- [.NET Error Code Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/) - Microsoft documentation
