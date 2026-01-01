# Testing

This repository uses an xUnit test project to validate the MCP server's behavior, schema validation, and command-building logic.

## Test Coverage Summary

- **Total Tests**: 551 passing tests (8 skipped interactive/integration tests)
- **Tool Coverage**: All 67 MCP tools have comprehensive unit tests
- **Code Coverage**: 73.2% line coverage
- **Test Organization**: Tests are organized by category (Templates, Packages, Projects, Solutions, References, etc.)

## Quick start

Run all tests (recommended):

```bash
dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

Run tests from the solution:

```bash
dotnet test --solution DotNetMcp.slnx -c Release
```

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
DOTNET_MCP_INTERACTIVE_TESTS=1 dotnet test --project DotnetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

If interactive tests are disabled, they will appear as skipped with a message explaining how to enable them.

## Tips

- Prefer `-c Release` for CI parity.
- If you're iterating on a single area, use Microsoft Testing Platform filters after `--`, for example: `dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release -- --filter-class DotNetMcp.Tests.CacheMetricsTests`.
