# Testing

This repository uses an xUnit test project to validate the MCP server's behavior, schema validation, and command-building logic.

## Test Coverage Summary

- **Total Tests**: ~1054 passing tests (10 skipped interactive/integration tests)
- **Tool Coverage**: All 74 legacy MCP tools + 8 consolidated tools have comprehensive unit tests
- **Code Coverage**: 73.2% line coverage
- **Test Organization**: Tests are organized by category (Templates, Packages, Projects, Solutions, References, etc.)
- **MCP Conformance**: 19 conformance tests validate MCP protocol compliance (including consolidated tool schema validation)

## Test Strategy: Consolidated-First Approach

This project follows a **consolidated-first testing strategy** following the tool surface consolidation:

### Consolidated Tools (Primary Test Surface)
- **Comprehensive coverage**: Consolidated tool tests (`Consolidated*ToolTests.cs`) contain detailed parameter-matrix tests, command-building assertions, and validation logic
- **Machine-readable contract tests**: Verify both plain-text and JSON output formats
- **Action routing tests**: Validate that each action enum value correctly routes to underlying implementation
- **Schema validation**: MCP conformance tests verify action enums appear correctly in tool schemas

### Legacy Tools (Backward Compatibility)
- **Slim smoke tests**: Legacy tool tests (e.g., `ProjectToolsTests.cs`, `ReferenceToolsTests.cs`) contain minimal back-compat smoke tests
- **Focus**: Ensure legacy tools still work for existing integrations
- **Coverage**: One representative test per legacy tool to verify basic functionality

### Benefits of This Approach
- **Reduced duplication**: Avoids testing the same command construction twice
- **Improved signal-to-noise**: Clearer separation between comprehensive tests and compatibility tests
- **Easier maintenance**: When adding features, update consolidated tests; legacy tests remain stable
- **Future-proof**: As legacy tools are eventually deprecated, removing them won't impact test coverage

## Quick start

Run all tests (recommended):

```bash
dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

Run tests from the solution:

```bash
dotnet test --solution DotNetMcp.slnx -c Release
```

## MCP Conformance Tests

The repository includes conformance tests that validate the server's compliance with the Model Context Protocol (MCP) specification.

### Running Conformance Tests

To run only the conformance tests:

```bash
dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release -- --filter-class "*McpConformanceTests"
```

### What Conformance Tests Validate

The conformance tests verify:

- **Server Initialization**: Handshake protocol, server info, capabilities, and protocol version negotiation
- **Tool Discovery**: Tool listing with proper metadata (names, descriptions, input schemas)
- **Tool Invocation**: Successful tool execution and response format
- **Error Handling**: Proper MCP error responses with error codes and messages
- **Resource Listing**: Resource discovery API (if resources are provided)

### Conformance Test Architecture

The conformance tests use an **in-process stdio** approach:

- Tests start the actual DotNetMcp server binary as a child process
- Communication happens via stdin/stdout using the MCP SDK's `StdioClientTransport`
- Tests are deterministic and require no external services
- The same server binary used in production is tested for conformance

This approach ensures that:
- Tests validate the actual deployed server behavior
- No mocking or test doubles are used for the core server
- Protocol conformance is verified end-to-end

### CI Integration

Conformance tests run automatically in GitHub Actions as part of the `build.yml` workflow. They run before the full test suite to provide early feedback on protocol compliance issues.

## Code coverage

To collect coverage with Microsoft Testing Platform, run:

```bash
dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release -- --coverage --coverage-output-format cobertura
```

The Cobertura XML will be written under the test output folder, typically:

- `DotNetMcp.Tests/bin/Release/net10.0/TestResults/*.cobertura.xml`

## Test project layout

- `DotNetMcp.Tests/` contains the test suite.
- Most tests are "pure" unit tests (no network, no machine state changes).

### Test Files by Category

- `McpConformanceTests.cs` - **MCP protocol conformance validation** (handshake, tool listing/invocation, error handling)
- `TemplateToolsTests.cs` - Template-related tools (list, search, info, cache)
- `PackageToolsTests.cs` - Package management tools (add, remove, update, search, pack)
- `ProjectToolsTests.cs` - Project operations (restore, clean)
- `ReferenceToolsTests.cs` - Project reference management
- `SolutionToolsTests.cs` - Solution file operations
- `MiscellaneousToolsTests.cs` - Watch, format, NuGet, help, and framework tools
- `SdkAndServerInfoToolsTests.cs` - SDK info and server capability tools
- `EdgeCaseAndIntegrationTests.cs` - Comprehensive edge cases and parameter combinations
- `DotNetCliToolsTests.cs` - Core CLI tools (build, test, run, publish, certificates, secrets, tools)
- `EntityFrameworkCoreToolsTests.cs` - EF Core migration and database tools
- Plus helper/infrastructure tests for caching, concurrency, error handling, etc.

## Integration and environment-dependent tests

Some tests are intentionally skipped by default because they require external state (for example, an actual `dotnet` CLI invocation with a valid project on disk).

If you are working on command execution behavior, you may want to:

- Run tests locally (not in a restricted CI sandbox).
- Ensure the .NET SDK is installed and available on `PATH`.

## Interactive / modal tests (opt-in)

Certain operations (notably development certificate trust/clean) can trigger OS prompts (UAC dialogs on Windows, trust prompts on macOS). Tests that can produce modal dialogs are opt-in.

To enable interactive tests, set:

- `DOTNET_MCP_INTERACTIVE_TESTS=1`

Examples:

PowerShell:

```powershell
$env:DOTNET_MCP_INTERACTIVE_TESTS = "1"
dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

bash:

```bash
DOTNET_MCP_INTERACTIVE_TESTS=1 dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

If interactive tests are disabled, they will appear as skipped with a message explaining how to enable them.

## Adding Tests for New Features

### For Consolidated Tools (Recommended)
When adding a new action to a consolidated tool:
1. Add action routing test in the corresponding `Consolidated*ToolTests.cs` file
2. Add parameter validation tests (both machineReadable and plain text)
3. Add command-building assertion tests using `MachineReadableCommandAssertions.AssertExecutedDotnetCommand`
4. If the action has required parameters, add validation error tests

Example:
```csharp
[Fact]
public async Task DotnetProject_NewAction_RoutesCorrectly()
{
    var result = await _tools.DotnetProject(
        action: DotnetProjectAction.NewAction,
        requiredParam: "value",
        machineReadable: true);

    Assert.NotNull(result);
    MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet new-command \"value\"");
}
```

### For Legacy Tools (Not Recommended)
Only add legacy tool tests if:
- You're maintaining backward compatibility for an existing tool
- The test is a simple smoke test to verify the tool still works

**Do not** add comprehensive parameter-matrix tests to legacy tool files.

### Machine-Readable Output Tests
When testing `machineReadable: true` behavior:
- Use `MachineReadableCommandAssertions.AssertExecutedDotnetCommand` to verify the command was executed
- Verify the response contains `"success": true` or `"success": false` as appropriate
- For validation errors, verify the response contains the expected error code and parameter name

Avoid creating redundant tests that only differ by the `machineReadable` flag unless they verify different contract behavior.

## Tips

- Prefer `-c Release` for CI parity.
- If you're iterating on a single area, use Microsoft Testing Platform filters after `--`, for example: `dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release -- --filter-class DotNetMcp.Tests.CacheMetricsTests`.
