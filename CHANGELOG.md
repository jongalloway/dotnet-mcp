# Changelog

All notable changes to the .NET MCP Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-01-09

### Added

- **Consolidated Tool Interface**: 10 consolidated MCP tools (8 domain tools + 2 utilities) providing comprehensive .NET SDK functionality
  - `dotnet_project` - Project lifecycle management (New, Restore, Build, Run, Test, Publish, Clean, Analyze, Dependencies, Validate, Pack, Watch, Format)
  - `dotnet_package` - NuGet package and reference management (Add, Remove, Search, Update, List, AddReference, RemoveReference, ListReferences, ClearCache)
  - `dotnet_solution` - Solution file management (Create, Add, List, Remove)
  - `dotnet_ef` - Entity Framework Core operations (MigrationsAdd, MigrationsList, MigrationsRemove, MigrationsScript, DatabaseUpdate, DatabaseDrop, DbContextList, DbContextInfo, DbContextScaffold)
  - `dotnet_workload` - Workload management (List, Info, Search, Install, Update, Uninstall)
  - `dotnet_tool` - .NET tool management (Install, List, Update, Uninstall, Restore, CreateManifest, Search, Run)
  - `dotnet_dev_certs` - Developer certificates and secrets (CertificateTrust, CertificateCheck, CertificateClean, CertificateExport, SecretsInit, SecretsSet, SecretsList, SecretsRemove, SecretsClear)
  - `dotnet_sdk` - SDK and template information (Version, Info, ListSdks, ListRuntimes, ListTemplates, SearchTemplates, TemplateInfo, ClearTemplateCache, FrameworkInfo, CacheMetrics)
  - `dotnet_help` - Get help for any dotnet command
  - `dotnet_server_capabilities` - Get MCP server capabilities and concurrency guidance

- **MCP Resources**: Read-only resources for efficient metadata access
  - `dotnet://sdk-info` - Installed .NET SDKs (versions and paths)
  - `dotnet://runtime-info` - Installed .NET runtimes (versions and types)
  - `dotnet://templates` - Complete catalog of installed .NET templates with metadata
  - `dotnet://frameworks` - Supported .NET frameworks (TFMs) including LTS status

- **SDK Integration**: Direct integration with official .NET SDK packages
  - Microsoft.TemplateEngine.* (v10.0.101) for template metadata and validation
  - Microsoft.Build.* (v18.0.2) for project analysis and framework validation
  - Type-safe constants for frameworks, configurations, runtime identifiers, and common packages

- **Enhanced Error Diagnostics**: Enriched error messages with explanations, documentation links, and suggested fixes
  - Support for 52 common error codes (CS####, MSB####, NU####, NETSDK####)
  - Plain English explanations with actionable guidance
  - Direct links to official Microsoft documentation

- **Secret Redaction**: Automatic redaction of sensitive information in CLI output
  - Connection strings, passwords, API keys, and tokens automatically masked
  - Optimized implementation with <1% performance overhead
  - Opt-out available with `unsafeOutput=true` for advanced debugging

- **Machine-Readable Output**: Optional JSON output for all tools via `machineReadable` parameter
  - Structured success and error responses
  - Consistent schema across all tools
  - Enhanced diagnostics in error responses

- **Caching Infrastructure**: Intelligent caching for template and framework metadata
  - Reduces repeated SDK queries
  - Configurable TTL and cache metrics
  - Force reload option for fresh data

- **Concurrency Safety**: Thread-safe implementation with concurrency guidance
  - Safe parallel execution of independent operations
  - Concurrency manager for resource protection
  - Documentation of concurrency patterns for AI orchestrators

- **Comprehensive Testing**: 936 tests covering all functionality
  - MCP protocol conformance tests
  - Unit tests with code coverage (Cobertura format)
  - Integration tests for SDK interactions
  - Performance smoke tests

### Documentation

- Complete README with installation instructions for VS Code, Visual Studio, and Claude Desktop
- AI Assistant Best Practices Guide with workflows and examples
- Tool Surface Consolidation documentation (design rationale and architecture)
- Machine-Readable JSON Contract specification (v1.0)
- SDK Integration technical details
- Error Diagnostics reference
- Concurrency and performance documentation
- Testing guide including opt-in interactive tests

### Infrastructure

- GitHub Actions CI/CD pipeline (build, test, coverage)
- CodeQL security scanning
- Automated NuGet package publishing
- MCP Registry publishing workflow
- MinVer-based automatic versioning from Git tags
- Dependabot for dependency updates

### Notes

- **MCP Server Only**: This package is designed exclusively as an MCP server for AI assistants. It is not intended for use as a library or for programmatic consumption in other .NET applications.
- **Consolidated Tools**: The server uses consolidated tools from day one (v1.0.0), providing better AI orchestration and maintainability compared to individual tools for each operation.
- **Target Framework**: .NET 10.0 (LTS)
- **MCP SDK**: ModelContextProtocol v0.5.0-preview.1
- **License**: MIT

[Unreleased]: https://github.com/jongalloway/dotnet-mcp/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/jongalloway/dotnet-mcp/releases/tag/v1.0.0
