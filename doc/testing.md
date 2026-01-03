# Testing

This repository uses an xUnit test project to validate the MCP server's behavior, schema validation, and command-building logic.

## Test Coverage Summary

- **Total Tests**: 703 passing tests (9 skipped interactive/integration tests)
- **Tool Coverage**: All 74 MCP tools have comprehensive unit tests
- **Code Coverage**: 73.2% line coverage
- **Test Organization**: Tests are organized by category (Templates, Packages, Projects, Solutions, References, etc.)
- **MCP Conformance**: 16 conformance tests validate MCP protocol compliance

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

## Tips

- Prefer `-c Release` for CI parity.
- If you're iterating on a single area, use Microsoft Testing Platform filters after `--`, for example: `dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release -- --filter-class DotNetMcp.Tests.CacheMetricsTests`.
