# Testing

This repository uses an xUnit test project to validate the MCP server’s behavior, schema validation, and command-building logic.

## Quick start

Run all tests (recommended):

```bash
dotnet test DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

Run tests from the solution:

```bash
dotnet test DotNetMcp.slnx -c Release
```

## Test project layout

- `DotNetMcp.Tests/` contains the test suite.
- Most tests are “pure” unit tests (no network, no machine state changes).

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
dotnet test DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

bash:

```bash
DOTNET_MCP_INTERACTIVE_TESTS=1 dotnet test DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release
```

If interactive tests are disabled, they will appear as skipped with a message explaining how to enable them.

## Tips

- Prefer `-c Release` for CI parity.
- If you’re iterating on a single area, use `dotnet test --filter "FullyQualifiedName~SomeClass"` to narrow the run.
