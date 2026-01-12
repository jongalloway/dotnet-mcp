# Testing

This repository uses an xUnit test project to validate the MCP server's behavior, schema validation, and command-building logic.

## Test Coverage Summary

- **Total Tests**: 973 passing tests (10 skipped interactive/integration tests)
- **Tool Coverage**: All 8 consolidated MCP tools have comprehensive unit tests
- **Code Coverage**: 73.2% line coverage
- **Test Organization**: Tests are organized by category (Templates, Packages, Projects, Solutions, References, etc.)
- **MCP Conformance**: 19 conformance tests validate MCP protocol compliance (including consolidated tool schema validation)

## Test Strategy: Consolidated Tools

This project uses **consolidated tools** as the primary test surface:

### Consolidated Tools (Primary Test Surface)

- **Comprehensive coverage**: Consolidated tool tests (`Consolidated*ToolTests.cs`) contain detailed parameter-matrix tests, command-building assertions, and validation logic
- **Machine-readable contract tests**: Verify both plain-text and JSON output formats
- **Action routing tests**: Validate that each action enum value correctly routes to underlying implementation
- **Schema validation**: MCP conformance tests verify action enums appear correctly in tool schemas

### Benefits of This Approach

- **Clear test organization**: Tests organized by domain (project, package, EF, etc.)
- **Improved signal-to-noise**: Focused tests on consolidated tool behavior
- **Easier maintenance**: Adding new actions means adding tests to existing consolidated tool test files
- **Future-proof**: Tool surface remains stable even as new capabilities are added

## Quick start

Run all tests (recommended):

```bash
dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

Important:

- When specifying a project for `dotnet test`, use `--project` (do not pass a `.csproj` as a positional argument). If you pass a project path positionally, `dotnet test` will emit: `Specifying a project for 'dotnet test' should be via '--project'`.
- When running from the solution, use `--solution`.

Examples:

```bash
dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
dotnet test --solution DotNetMcp.slnx -c Release
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

## Downloading CI coverage artifacts

CI uploads a Cobertura coverage artifact named `coverage-cobertura` on each run of the `build.yml` workflow.

To download the latest successful coverage artifact for `main` and print a quick hotspot summary:

```powershell
pwsh -File scripts/download-coverage-artifact.ps1
```

To download coverage from a specific workflow run (e.g. a run URL like `.../actions/runs/20865330584`):

```powershell
pwsh -File scripts/download-coverage-artifact.ps1 -RunId 20865330584
```

To download coverage for a specific pull request:

```powershell
pwsh -File scripts/download-coverage-artifact.ps1 -PullRequest 285
```

When using `-PullRequest`, the script also downloads the latest successful run for the base branch (defaults to `-Branch main`) and prints a PR-vs-base delta.

To disable the base branch comparison:

```powershell
pwsh -File scripts/download-coverage-artifact.ps1 -PullRequest 285 -NoBaseCompare
```

Notes:

- Requires GitHub CLI (`gh`) and auth (`gh auth login`).
- Output is saved under `artifacts/coverage/run-<runId>/`.

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

### For Consolidated Tools

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

### Machine-Readable Output Tests

When testing `machineReadable: true` behavior:

- Use `MachineReadableCommandAssertions.AssertExecutedDotnetCommand` to verify the command was executed
- Verify the response contains `"success": true` or `"success": false` as appropriate
- For validation errors, verify the response contains the expected error code and parameter name

Avoid creating redundant tests that only differ by the `machineReadable` flag unless they verify different contract behavior.

## Tips

- Prefer `-c Release` for CI parity.
- If you're iterating on a single area, use Microsoft Testing Platform filters after `--`, for example: `dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release -- --filter-class DotNetMcp.Tests.CacheMetricsTests`.

## Path handling

When writing tests (including scenario/release-scenario tests), prefer `Path.Join(...)` over `Path.Combine(...)` whenever possible.

- `Path.Combine` has "rooted path reset" behavior if a later segment is rooted (or begins with a directory separator), which can produce surprising results.
- Several analyzers/security checks recommend `Path.Join` to avoid those classes of issues.
