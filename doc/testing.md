# Testing

This repository uses an xUnit test project to validate the MCP server's behavior, schema validation, and command-building logic.

## Test Coverage Summary

- **Total Tests**: Large suite (unit + conformance + opt-in scenarios)
- **Tool Coverage**: Consolidated MCP tools have comprehensive unit tests
- **Code Coverage**: Tracked in CI and uploaded to Codecov
- **Test Organization**: Tests are organized by category (Templates, Packages, Projects, Solutions, References, etc.)
- **MCP Conformance**: Real stdio and in-process compatibility tests validate protocol behavior and consolidated tool schemas

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

Install the .NET 11 RC3 baseline from `global.json` or a compatible GA/servicing SDK in the 11.0.1xx feature band, plus the .NET 10 SDK/runtime. The `latestPatch` SDK policy prefers the latest compatible installed patch, including GA after RC3, without moving to later feature bands or major versions. The server and tests target both `net10.0` and `net11.0`; the commands below run both targets unless `--framework` selects one. Stdio tests discover the server binary for their own framework. Discovery probes allow for cold startup when both suites run concurrently.

The shared `.github/actions/setup-dotnet` action reads official release metadata to install .NET 10 alongside RC3 until GA is published, then automatically installs the latest stable SDK in 11.0.1xx. Normal build and publish jobs use that current selection. Build validation retains an additional exact-RC3 lane once GA is available. Each job uses an isolated SDK root so preinstalled SDKs cannot change its lane, without modifying the checkout's `global.json` or MTP runner configuration. Release metadata or installation errors fail setup rather than silently reverting to RC3.

Run the offline SDK-selection fixtures with `pwsh -File scripts/test-sdk-selection.ps1`. They cover the RC3-to-GA transition, servicing, feature-band bounds, and invalid metadata without requiring an unreleased SDK. `pwsh -File scripts/resolve-dotnet-sdk.ps1` displays the live selection.

### Selecting an external server executable

Real-stdio conformance, MCP Apps wire-format, and scenario/release-scenario tests use the same server selection. By default they launch the managed server built for the test's configuration and target framework.

Set `DOTNET_MCP_TEST_SERVER_PATH` to an absolute executable path to exercise a separately published server instead. The tests launch that exact file directly, without `dotnet` arguments; an empty, relative, or missing path fails instead of falling back to managed build output.

```powershell
$env:DOTNET_MCP_TEST_SERVER_PATH = 'C:\published-server\DotNetMcp.exe'
try {
    dotnet test --solution DotNetMcp.slnx -c Release -- --filter-class '*McpConformanceTests' --filter-class '*McpAppsMetaWireFormatTest'
}
finally {
    Remove-Item Env:\DOTNET_MCP_TEST_SERVER_PATH
}
```

This override selects a process; it does not prove that the executable is NativeAOT. A managed apphost is also an executable. Native validation must independently establish native identity and exercise the published bundle's functionality.

### Windows NativeAOT distribution validation

Native publishing targets the actual MCP server, independently of the SDK's own NativeAOT CLI host. It is opt-in and currently limited to `net11.0` / `win-x64`. Install the source-build SDKs and Visual Studio's x64 C++ build tools/Windows SDK, then run:

```powershell
pwsh -File .\scripts\publish-native.ps1
pwsh -File .\scripts\test-native.ps1
```

Publishing uses an isolated `artifacts\native-build` tree so native restore/build properties cannot contaminate the managed tool's build output. The distribution is `artifacts\native\win-x64`, including its managed `sdk-helper` directory. The publishing script rejects unsupported targets and verifies an AMD64 PE without a CLR header, dynamic-code support disabled, and the .NET 11 native runtime identity. `-UseEnvironmentalTools` is available only for an explicitly prepared linker `PATH` and CRT/Windows SDK `LIB`; it does not install or discover substitute toolchains.

`test-native.ps1` copies the complete bundle to a unique temporary directory, selects that exact executable, and removes its owned copy afterward. It builds managed references, compares full tool schemas/descriptions/annotations and resource/prompt discovery, compares all nine MSBuild operations and template/framework metadata, and runs real stdio conformance and MCP Apps tests. It then invokes the existing scenario and release gates, including background run/watch lifecycle, package/project operations, cold-cache publishing, local tools, concurrency, and EF SQLite migrations. These gates require network access to their public package feeds. `-SkipReleaseScenarios` runs only the focused identity/parity/conformance checks; it is not full release validation.

The Windows NativeAOT workflow performs these checks using the current SDK lane and retains the exact-RC3 regression lane when those differ. It uploads the complete native distribution and publish/validation logs. AOT warnings are deliberately not globally suppressed. In particular, governance's transitive YamlDotNet warnings remain visible, and the pre-existing full-catalog scanner rejection remains fail-closed rather than being bypassed to make native startup pass.

For environments that cannot reach NuGet.org, set `DOTNET_MCP_TEST_NUGET_SOURCE` to an explicit public HTTP(S) feed before running scenario/release gates. The fixture package operations and fixture-local `NuGet.Config` then use that feed, including cold-cache and EF tool restores. The default remains NuGet.org. Invalid overrides fail rather than falling back, and restore/test/database assertions are unchanged; there is no automatic source fallback or change to the repository/user NuGet configuration.

### SDK 11 compatibility scenarios

```powershell
$env:DOTNET_MCP_SCENARIO_TESTS = '1'
try {
    dotnet test --solution DotNetMcp.slnx -c Release --framework net11.0 -- --filter-class '*Sdk11CompatibilityScenarioTests'
}
finally {
    Remove-Item Env:\DOTNET_MCP_SCENARIO_TESTS
}
```

These scenarios pin the actual .NET 11 SDK in isolated fixtures and cover:

- The SDK NativeAOT CLI host and `DOTNET_CLI_ENABLEAOT=false` opt-out. Host tracing distinguishes actual managed CLI loading; the initial "Using AOT-ed SDK" marker alone does not establish which path executed.
- Launch-settings notices on stderr without treating a successful run as a tool error.
- MCP Tasks cancellation of a real build worker, with observed process termination rather than a mocked cancellation result. Shared MSBuild servers and node reuse are disabled only in server-owned child environments; shared servers are never shut down.
- Restore selection from a local source versus a higher version already in an isolated global-packages cache, including unavailable and exact-cache cases.
- A real NU5052 package-ID warning with successful package creation, independently of restore version selection.
- Actual VSTest execution with project-local Newtonsoft.Json rather than an SDK-bundled copy.

The packaged-tool compatibility script separately compares installed tools, `dotnet dnx`, and the actual `dnx` launcher script. On .NET 11, it proves that the launcher can initialize the MCP server from a directory whose `global.json` requests a missing SDK, while `dotnet dnx` honors that pin and fails. Project CLI commands still honor their own SDK selection; launcher bypass does not bypass project SDK requirements.

Each invocation runs fresh **2025-11-25** and **2026-07-28** sessions. Legacy sessions use `initialize`; current sessions use `server/discover` and explicit per-request protocol/client metadata. The package probes check:

- Ordinary inline calls, negotiated capabilities, existing domain-tool discovery, template/project analysis, and a real build.
- Apps extension discovery, SDK/metrics dashboard linkage in modern and legacy metadata, and readable HTML resources with the expected MIME type and resource metadata.
- All SDK and metrics query actions, restricted read-only query annotations/schemas, rejected invalid/mutating actions, and project-log queries against an owned running application. Background sessions are stopped in cleanup.
- Current-protocol Tasks creation and polling, completed application errors, request-local opt-in, and cancellation of an observed CLI worker process. Legacy Tasks are explicitly recorded as not applicable; ordinary legacy calls remain inline.

Run the harness with an already-built package and an isolated SDK root containing exactly one SDK for the selected server major:

```powershell
pwsh -File scripts/test-tool-compatibility.ps1 `
    -PackagePath C:\packages\Community.Mcp.DotNet.1.2.0-rc.1.nupkg `
    -DotnetRoot C:\isolated-dotnet `
    -ExpectedFramework net11.0 `
    -ProjectSdkVersion 11.0.100-rc.3.26522.148 `
    -ExpectedRuntimeVersion 11.0.0-rc.3.26522.148 `
    -OutputDirectory C:\results\new-compatibility-run
```

The output directory must be new. `results.json` is written only after all six invocation/protocol combinations pass and identifies the package version/hash, SDK/runtime inventory, and feature outcomes. Per-session RPC transcripts, stderr and host traces remain available even on failure. These fixture transcripts contain test-generated paths and payloads; do not use this harness to capture real secrets.

The manual release workflow also runs `Sdk20CompatibilityTests`, `McpConformanceTests`, `McpAppsMetaWireFormatTest`, `OutputSchemaTests`, `ToolAnnotationTests`, and `ServerCapabilitiesTests` explicitly for each source SDK/framework/OS lane. This retains deeper confirmation/input-required, governance and contract checks that are not duplicated in the package smoke harness. See [release preparation and publication](releasing.md#release-preparation-and-publication).

Run all tests (recommended):

```bash
dotnet test --solution DotNetMcp.slnx -c Release
```

Important:

- Use `--solution DotNetMcp.slnx` for this repository. The pinned .NET 11 RC3 SDK has a `--project` discovery regression that can report no test projects or duplicate the project path before tests run.
- Solution discovery avoids that SDK regression while preserving both target frameworks, MTP filters, and coverage options. Do not replace it with a positional project path.

### Why `--solution`?

This repository uses the **Microsoft Testing Platform (MTP)** as configured in `global.json`:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Both server and test projects are in `DotNetMcp.slnx`. Solution-based discovery runs the same test project without the pinned SDK's broken project-discovery path. The general runner compatibility information below describes the MCP server's Test action, not the entry point to use for this repository's own tests.

#### Test Runner Compatibility

The `--project` flag support depends on which test runner is being used:

- **[Microsoft Testing Platform (MTP)](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp)**: Supports `--project` flag
  - Used when `global.json` configures `"runner": "Microsoft.Testing.Platform"`
  - Default in .NET SDK 10.0+
  - **Recommended**: `dotnet test --project MyTests.csproj`

- **[VSTest](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-vstest)**: Does **not** support `--project` flag
  - Legacy test runner (default in .NET SDK 7.0 and earlier)
  - **Uses positional argument**: `dotnet test MyTests.csproj`

**References**:
- [dotnet test - Overview](https://learn.microsoft.com/dotnet/core/tools/dotnet-test)
- [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp)
- [VSTest](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-vstest)

### Compatibility: .NET SDK Requirements

The `dotnet_project` Test action now supports **automatic test runner detection**:
- Detects Microsoft Testing Platform (MTP) from `global.json` configuration
- Defaults to VSTest for legacy compatibility when MTP is not detected
- Supports explicit test runner selection via `testRunner` parameter

**Test Runner Selection**:

The MCP server chooses between `--project` flag (MTP) and positional argument (VSTest) based on:
1. **Explicit `testRunner` parameter**: `MicrosoftTestingPlatform`, `VSTest`, or `Auto` (default)
2. **Auto-detection**: Checks for `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in `global.json`
3. **Legacy fallback**: Uses `useLegacyProjectArgument: true` parameter (deprecated)
4. **Default**: VSTest mode (positional argument) for safety with legacy projects

**SDK Requirements**:
- **MTP mode** (`--project` flag) requires:
  - .NET SDK 8.0+ with Microsoft Testing Platform enabled, OR
  - .NET SDK 10.0+ (MTP enabled by default)
- **VSTest mode** (positional argument) works with all SDK versions

**Troubleshooting**: If you encounter `Unrecognized option '--project'` or `MSB1001: Unknown switch`:
1. **Auto mode** (recommended): Add to your `global.json`:
   ```json
   {
     "test": {
       "runner": "Microsoft.Testing.Platform"
     }
   }
   ```
2. **Explicit mode**: Use `testRunner: "VSTest"` for VSTest or `testRunner: "MicrosoftTestingPlatform"` for MTP
3. **Legacy parameter**: Use `useLegacyProjectArgument: true` (deprecated, use `testRunner` instead)
4. **Verify support**: Run `dotnet test --help | grep -- --project` to check if your SDK supports `--project`

**Machine-Readable Output**: When `machineReadable: true`, test results include metadata:
- `selectedTestRunner`: `microsoft-testing-platform` or `vstest`
- `projectArgumentStyle`: `--project`, `positional`, or `none`
- `selectionSource`: `global.json`, `testRunner-parameter`, `useLegacyProjectArgument-parameter`, or `default`

Run tests from the solution:

```bash
dotnet test --solution DotNetMcp.slnx -c Release
```

## MCP SDK compatibility checks

Both projects use matching MCP SDK and extension versions. The SDK 2.x compatibility matrix remains applicable to the current 2.3.0 packages; do not restore the removed experimental Tasks API or change legacy application error annotations to match native wire errors.

Run the focused protocol matrix:

```powershell
dotnet test --solution DotNetMcp.slnx -c Release -- --filter-class '*Sdk20CompatibilityTests' --filter-class '*ServerCapabilitiesTests'
```

`Sdk20CompatibilityTests` covers 2026-07-28 and 2025-11-25 discovery, required tool input schemas, negotiated capabilities, resource error-code mapping, native/legacy resource-update delivery, native listener cancellation, confirmation outcomes and continuation validation, execution after confirmation, Tasks polling/update/cancellation, and governance policy composition. Deprecated-feature regressions cover logging thresholds and notification payloads, request-level logging/Sampling isolation, connected legacy Roots discovery and explicit-path precedence, and Sampling analysis of failures with successful-command and unsupported-client bypass. Cancellation coverage includes pending Roots/Sampling input and termination of a real CLI child process without subsequent AI analysis. The full-catalog governance test intentionally verifies fail-closed rejection of the existing certificate password schema; it is not evidence that governance-enabled full-catalog startup succeeds.

Confirmation tests also use a transport-only native client to inspect `inputRequests` and send `inputResponses` without the SDK's automatic retry loop. Real Clean and solution Remove fixtures verify that side effects wait for accepted boolean confirmation, while malformed or missing confirmation data do not execute the operation. Legacy tests verify one wire-level tool call; a test-only explicit-MRTR tool separately exercises the SDK's backward-compatibility resolver with one input round and two handler executions. Native SDK-client tests assert two tool calls, keeping the simple flow below the SDK's 10-retry limits.

DatabaseDrop, CertificateClean, SecretsRemove, and SecretsClear use the existing command-executor override to record exact commands instead of deleting real databases, certificates, or secrets. Their matrix covers accepted/true, decline, cancel, false/missing/mistyped confirmation, invalid native continuations, task input updates/cancellation, request cancellation/input failure, and connected clients without elicitation. EF force/dry-run rules, required-key validation, unchanged unprompted actions, and SDK-injected parameters versus the public EF DbContext `context` input are checked separately. Direct null-server fallback tests record commands as well.

The Tasks capability matrix includes servers with and without `WithTasks`, legacy inline fallback, and opted-in/ordinary/opted-in calls on one connection. Assertions check task versus ordinary result shapes and request-scoped `asyncTasks`, independently of task-store injection into tools.

Run the focused real-stdio Tasks checks:

```powershell
dotnet test --solution DotNetMcp.slnx -c Release -- --filter-class '*McpConformanceTests' --filter-method '*Task*'
```

Focused error regressions distinguish SDK wire errors from application annotations: missing resource reads produce `-32602` on the current revision and `-32002` on the legacy revision, while factory-generated tool errors retain their annotations, `IsError`, object envelopes, and equivalent structured/text JSON content on both. Unit tests cover all five missing-resource diagnostic mappings, both capability factory overloads, and separation from the SDK's renumbered `-32020`/`-32021`/`-32022` codes.

`McpConformanceTests` starts the actual stdio server and checks discovery, resources/prompts, legacy and native confirmation for Clean, solution Remove, DatabaseDrop, CertificateClean, SecretsRemove, and SecretsClear, actual resource updates after `subscriptions/listen`, removed-method rejection, and opted-in logging. Stdio confirmation checks decline the operation so no destructive command executes. SDK 2.0's client drops the request log level during metadata injection, so the test-only logging transport writes it at the wire boundary. The logging workaround is test-only.

MTP selectors of the same kind can be repeated to match multiple classes or methods. Combining `--filter-class` with `--filter-method` narrows to their intersection, not their union.

### Structured output contracts

`OutputSchemaTests` obtains the production schemas through actual `tools/list` calls and validates structured responses on both MCP 2026-07-28 and 2025-11-25. Coverage includes all six schema-bearing tools, action-restricted text fallbacks, domain metadata, empty/non-empty lists, diagnostic failures, capability/validation errors, optional runtime enrichment, lock contention, and object envelopes without a new `result` wrapper. Deterministic command producers avoid network-dependent package operations; project metadata fixtures are isolated and deleted after each test. Compatibility checks preserve the first text block and require serialized JSON text matching the payload. Negative schema cases reject missing fields, incorrect nested types, and fallback misuse. Production stdio conformance tests also verify that returned application errors increment failure metrics rather than success metrics.

```powershell
dotnet test --solution DotNetMcp.slnx -- --filter-class '*OutputSchemaTests' --filter-class '*StructuredContentTests' --filter-class '*ServerMetricsToolTests'
```

## Test isolation and runtime dependencies

Tool command-building tests record `dotnet tool` commands through a per-instance executor override instead of installing, updating, or removing real tools. Other commands in those test classes still use the normal executor. Real local-tool installation coverage belongs to isolated opt-in release scenarios, not the source-controlled `.config/dotnet-tools.json` or the user's global tools.

Tests that change the process-wide current directory use the nonparallel `ProcessWideStateTests` collection, including workspace snapshot resource tests. Project analysis tests exercise evaluated MSBuild properties: an omitted property can still have an SDK default, so unset-property fixtures use a custom property name.

An SDK project with no target framework can either complete validation (`success:true`, `isValid:false`, and `"No target framework specified"` in `errors`) or fail evaluation (`success:false` with an evaluation `error`). The SDK/import environment determines this, not OS name alone. The SDK missing-framework regression test selects strict assertions for the actual response without OS-specific skips. Non-SDK missing-framework and malformed-XML fixtures deterministically cover completed invalid validation and evaluation failure, respectively.

The normal source CI runs on Ubuntu; PR checks run `ProjectAnalysisHelperTests` alongside MCP conformance tests, and main runs them in the full suite. Packaged compatibility and manual release validation cover Windows and Ubuntu. The same portable tests can run on-device on Windows or macOS with the same class selector. For example, from a Windows checkout:

```powershell
dotnet test --solution DotNetMcp.slnx -c Release -- --filter-class '*ProjectAnalysisHelperTests'
```

On macOS or Linux, use the same solution command and selector from the repository root. Each platform runs all fixtures and selects assertions based on its actual evaluation result.

In-process MSBuild analysis requires runtime assets compatible with the project's target framework. Keep `Microsoft.Build` and `Microsoft.Build.Utilities.Core` aligned on a .NET 10-compatible release, and retain the explicit `NuGet.Frameworks` dependency needed by MSBuild property functions rather than relying on the older version supplied by TemplateEngine.

## Scenario Tests (Opt-in)

This repo includes end-to-end scenario tests that start the real `DotNetMcp` server binary and communicate with it over stdio.
These are integration-style tests (slower and more environment-dependent than unit tests).

- **Namespace**: `DotNetMcp.Tests.Scenarios`
- **Gate**: `DOTNET_MCP_SCENARIO_TESTS=1` (or `true` / `yes`)
- **How they run in CI**: `build.yml` runs them by setting `DOTNET_MCP_SCENARIO_TESTS=1` and filtering to the scenario namespace.

Run scenario tests locally:

```powershell
$env:DOTNET_MCP_SCENARIO_TESTS = "1"
dotnet test --solution DotNetMcp.slnx -c Release -- --filter-namespace "*DotNetMcp.Tests.Scenarios*"
```

### Per-test diagnostics with ITestOutputHelper

All scenario and conformance test classes receive `ITestOutputHelper` via constructor injection. The harness (`McpScenarioClient`) writes diagnostic lines to the per-test output channel so that CI logs for **failing tests** include actionable context without interleaving output across tests.

Logged events (no arg values are logged to avoid leaking secrets):

| Event | Example output |
|---|---|
| Server start command | `[MCP] Server command: dotnet /path/to/DotNetMcp.dll` |
| Server connected | `[MCP] Server connected.` |
| Tool call (arg keys only) | `[MCP] → dotnet_project (args: {action, project, configuration})` |
| Tool response (first 500 chars) | `[MCP] ← dotnet_project: Build succeeded...` |

For conformance tests the server command is also logged in `InitializeAsync` via `[MCP] Conformance server command: ...`.

**Adding output to new scenario tests**: Every new scenario or release-scenario test class should accept `ITestOutputHelper` in its constructor and pass it to `McpScenarioClient.CreateAsync`:

```csharp
public class MyNewScenarioTests
{
    private readonly ITestOutputHelper _output;

    public MyNewScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [ScenarioFact]
    public async Task MyScenario()
    {
        await using var client = await McpScenarioClient.CreateAsync(
            TestContext.Current.CancellationToken, _output);
        // ...
    }
}
```

## Release-Gate Scenario Tests (Manual / workflow_dispatch)

Release-gate scenarios are a second tier of **long-running** integration tests intended to run before shipping a release.
They may:

- perform real NuGet restores,
- install local .NET tools (e.g., `dotnet-ef`),
- run EF Core migrations against a local SQLite file,
- stress concurrency by running many tool calls in parallel.

- **Namespace**: `DotNetMcp.Tests.ReleaseScenarios`
- **Gate**: `DOTNET_MCP_RELEASE_SCENARIO_TESTS=1` (or `true` / `yes`)

### Run locally (recommended)

Use the helper script which enables both scenario tiers and runs them in a predictable order:

```powershell
pwsh -File scripts/run-release-gate-tests.ps1
```

Common variants:

```powershell
# Fast path (assumes you already built)
pwsh -File scripts/run-release-gate-tests.ps1 -NoRestore -NoBuild

# Skip server.json validation
pwsh -File scripts/run-release-gate-tests.ps1 -SkipServerJsonValidation
```

### Run in GitHub Actions (manual)

The workflow [.github/workflows/release-scenarios.yml](../.github/workflows/release-scenarios.yml) is designed to be run via `workflow_dispatch`.
It runs both scenario tiers and target frameworks on the selected OS, with current and exact-RC3 build SDK lanes when distinct. Each target framework gets its own runner so local tool installations cannot collide in shared SDK caches. The local helper runs framework suites sequentially and accepts `-Framework net10.0` or `-Framework net11.0` to select one. Select `all` to cover Windows and Ubuntu. The workflow also calls the packaged-tool compatibility workflow, so package failures fail the release validation. Each scenario suite is exit-code checked separately; logs and SDK inventories are retained in `release-scenarios-<os>-<lane>-<framework>` artifacts.

```powershell
gh workflow run release-scenarios.yml --ref <branch-or-sha> -f os=all
gh workflow run build.yml --ref <branch-or-sha> -f runFullValidation=true -f runScenarioTests=true
```

Inspect the completed runs and their checked-out SHA; dispatching a workflow is not evidence that it passed. These workflows do not publish packages or create release tags.

## MCP Conformance Tests

The repository includes conformance tests that validate the server's compliance with the Model Context Protocol (MCP) specification.

### Running Conformance Tests

To run only the conformance tests:

```bash
dotnet test --solution DotNetMcp.slnx -c Release -- --filter-class "*McpConformanceTests"
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
dotnet test --solution DotNetMcp.slnx -c Release -- --coverage --coverage-output-format cobertura
```

The Cobertura XML will be written under the test output folder, typically:

- `DotNetMcp.Tests/bin/Release/net10.0/TestResults/*.cobertura.xml`
- `DotNetMcp.Tests/bin/Release/net11.0/TestResults/*.cobertura.xml`

CI retains framework-specific reports under `TestResults/coverage/` using `--results-directory-layout per-module` and `--no-artifact-post-processing`, so the SDK's multi-application merge cannot delete them. Both reports are uploaded and submitted to Codecov from the current SDK lane. The .NET 10 report retains the artifact filename `coverage.cobertura.xml` for the existing download/diagnostic script; the additional report is `net11.0.cobertura.xml`.

## Packaged-tool compatibility

The `Packaged Tool Compatibility` workflow builds one dual-targeted package with the exact RC3 SDK, even after normal builds adopt GA. On Windows and Linux it tests that package using `dnx` and a fresh tool-path install in isolated .NET 10-only, RC3-only, and side-by-side SDK roots. Side-by-side cases explicitly select each project SDK independently of the .NET 11 tool SDK/runtime. Tests assert `dotnet --version` in the pinned project workspace, initialize MCP, list tools and templates, create/analyze/build a console project, and inspect host traces to assert the selected tool assets and loaded runtime.

When official release metadata contains GA in the allowed feature band, the matrix automatically adds GA-only and GA/.NET 10 side-by-side cases for both project SDKs. GA roots contain no preview SDKs or runtimes. The same RC3-built package is used, not a rebuild with GA.

To run a case locally, supply a complete isolated SDK installation (not just a runtime), the exact SDK/runtime versions, and a new output directory:

```powershell
& C:\isolated-sdk-rc3\dotnet.exe pack "$PWD\DotNetMcp\DotNetMcp.csproj" -c Release -o artifacts/package
pwsh -File scripts/test-tool-compatibility.ps1 `
  -PackagePath artifacts/package/Community.Mcp.DotNet.<version>.nupkg `
  -DotnetRoot C:\isolated-sdk-rc3 `
  -ExpectedFramework net11.0 `
  -ProjectSdkVersion 11.0.100-rc.3.26522.148 `
  -ExpectedRuntimeVersion 11.0.0-rc.3.26522.148 `
  -OutputDirectory artifacts/compatibility-rc3
```

Use the official `dotnet-install` script to provision each isolated root without modifying system installations. Build the baseline package through a root containing RC3 and no GA SDK so `latestPatch` cannot select GA for that build. On Linux, use that root's `dotnet` executable instead of `dotnet.exe`. For a side-by-side execution case install both SDKs into one root and pass either project SDK version with `-ExpectedFramework net11.0`. The harness pins tool installation to the SDK matching `ExpectedFramework`, independently of the project SDK. It sets isolated CLI homes, package caches, tool paths, and host roots; inherited runtime roll-forward overrides are removed. Neither invocation uses `--allow-roll-forward`.

The workflow preserves `rc3-tool-package` for 90 days, including `provenance.json` with the exact build SDK, source SHA, run URL, and package SHA-256. Download and retain that artifact before expiry if needed. **Real GA execution remains pending until .NET 11 GA is available; fixture tests are not a GA execution pass.** Subsequent workflow runs automatically exercise GA once published. To verify a package preserved before GA, dispatch with `rc3_run_id` pointing to a run containing the package and provenance artifact. The package job is skipped; the existing package is downloaded and its hash is verified before execution without RC3 installed in the GA roots. Artifacts from older workflow revisions without provenance must be regenerated with the updated workflow before using this override.

```powershell
gh workflow run tool-compatibility.yml --ref <branch-or-sha>
gh workflow run tool-compatibility.yml --ref <branch-or-sha> -f rc3_run_id=<preserved-run-id>
```

Each `compatibility-<os>-<environment>` artifact retains selection results, provenance, inventories, and host traces. Compare package hashes across runs to establish artifact identity. Failure to obtain GA, metadata, or the preserved artifact fails the check; it is not counted as a pass. No `--allow-roll-forward` flags are added: the packaged `Minor` runtime policy permits an installed GA runtime, independently of SDK selection and installation.

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

### Coverage Exclusions Policy

This project follows a clear policy on what code is included in coverage measurements and what is intentionally excluded.

#### What We Measure

Coverage metrics focus on **production code** that we author and maintain:
- All source files in `DotNetMcp/` (excluding generated code)
- Tool implementations, helpers, and business logic
- Error handling and validation code
- SDK integration code

#### What We Exclude

The following categories are excluded from coverage reports:

1. **Build Artifacts** (`obj/`, `bin/`)
   - Intermediate build outputs
   - Compiled assemblies
   - These are build products, not source code

2. **Auto-Generated Files**
   - `*.g.cs` - Generated C# files (e.g., Regex source generators)
   - `*.GlobalUsings.g.cs` - SDK-generated global usings
   - `*.AssemblyInfo.cs` - SDK-generated assembly metadata
   - `*.AssemblyAttributes.cs` - SDK-generated assembly attributes

3. **Source Generator Outputs**
   - `generated/` directories
   - Files in generator-specific directories (e.g., `System.Text.RegularExpressions.Generator/`)
   - These are generated at build time by source generators

4. **Test Projects**
   - `**/*.Tests/**` - Test code itself is not measured
   - `**/Tests/**` - Directories named `Tests` that contain test-only projects and assets
   - We measure how well tests cover production code, not the tests themselves

#### Where Exclusions Are Enforced

Exclusions are implemented in multiple locations to ensure consistency:

1. **Codecov Configuration** (`codecov.yml`)
   - Primary exclusion mechanism for Codecov reporting
   - Defines patterns for files to ignore
   - See the `ignore:` section in `codecov.yml` for the complete list

2. **CI Workflow** (`.github/workflows/build.yml`)
   - Coverage is collected using Microsoft Testing Platform
   - Raw coverage data includes all files; exclusions happen at report processing
   - Coverage artifact is uploaded for diagnostics

3. **Coverage Collector** (Microsoft Testing Platform)
   - Collects coverage from all compiled code by default
   - Exclusions are applied post-collection by Codecov

#### Why This Policy

- **Generated code** should not affect coverage metrics because:
  - We don't author or maintain it
  - It's auto-generated at build time
  - Changes to it are not under our control
  
- **Build artifacts** (`obj/`, `bin/`) should be excluded because:
  - They are compiler outputs, not source code
  - Including them would duplicate coverage of the same code
  
- **Test code** should be excluded because:
  - Coverage measures how well tests cover production code
  - Measuring test coverage of tests is not useful

#### Validation

The exclusion patterns are validated by:
- Codecov reporting (visible in PR comments and coverage reports)
- CI uploads coverage artifacts that can be inspected locally
- The `download-coverage-artifact.ps1` script can be used to examine raw coverage data
- The `validate-coverage-exclusions.ps1` script validates exclusion patterns locally

To validate exclusion patterns against the latest coverage data:

```powershell
pwsh -File scripts/validate-coverage-exclusions.ps1
```

This script will:
- Parse the codecov.yml configuration
- Analyze the latest coverage report
- Show which files will be excluded/included
- Warn if expected patterns are not working

If you notice coverage anomalies (e.g., generated files appearing in reports), check:
1. The `codecov.yml` configuration has the correct patterns
2. The file paths match the exclusion patterns
3. Codecov is processing the exclusions correctly:
   - For uploads from `main`, review the latest report in the Codecov UI
   - For pull requests, use the `coverage-cobertura` artifact and/or `scripts/download-coverage-artifact.ps1` to inspect coverage locally and confirm exclusions

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

This includes:

- Scenario tests (real server over stdio)
- Release-gate scenarios (NuGet network access, tool install, EF migrations, concurrency stress)

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
dotnet test --solution DotNetMcp.slnx -c Release
```

bash:

```bash
DOTNET_MCP_INTERACTIVE_TESTS=1 dotnet test --solution DotNetMcp.slnx -c Release
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
- If you're iterating on a single area, use Microsoft Testing Platform filters after `--`, for example: `dotnet test --solution DotNetMcp.slnx -c Release -- --filter-class DotNetMcp.Tests.CacheMetricsTests`.

## Path handling

When writing tests (including scenario/release-scenario tests), prefer `Path.Join(...)` over `Path.Combine(...)` whenever possible.

- `Path.Combine` has "rooted path reset" behavior if a later segment is rooted (or begins with a directory separator), which can produce surprising results.
- Several analyzers/security checks recommend `Path.Join` to avoid those classes of issues.
