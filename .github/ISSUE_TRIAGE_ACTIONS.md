# Issue Triage Actions for v1.0

This document provides the concrete GitHub commands to implement the issue review recommendations. Execute these commands to organize issues into appropriate milestones and close out-of-scope items.

## Prerequisites

Ensure you have GitHub CLI (`gh`) installed and authenticated:
```bash
gh auth login
```

## Step 1: Create Milestones

```bash
# Create v1.0 milestone
gh api repos/jongalloway/dotnet-mcp/milestones -f title="v1.0" -f description="Core MCP compliance, essential CLI coverage, quality and performance" -f state="open"

# Create v1.1 milestone  
gh api repos/jongalloway/dotnet-mcp/milestones -f title="v1.1" -f description="Enhanced intelligence, solution-level features, AI assistance improvements" -f state="open"

# Create v2.0 milestone
gh api repos/jongalloway/dotnet-mcp/milestones -f title="v2.0" -f description="Advanced features and specialized tools" -f state="open"
```

## Step 2: Move Issues to v1.0 Milestone

```bash
# Get milestone number for v1.0 (run this first to get the number)
gh api repos/jongalloway/dotnet-mcp/milestones --jq '.[] | select(.title=="v1.0") | .number'

# Then use that number (replace MILESTONE_NUMBER) in these commands:
gh issue edit 114 --milestone "v1.0" --repo jongalloway/dotnet-mcp
gh issue edit 45 --milestone "v1.0" --repo jongalloway/dotnet-mcp
gh issue edit 46 --milestone "v1.0" --repo jongalloway/dotnet-mcp
gh issue edit 47 --milestone "v1.0" --repo jongalloway/dotnet-mcp
gh issue edit 61 --milestone "v1.0" --repo jongalloway/dotnet-mcp
```

## Step 3: Move Issues to v1.1 Milestone

```bash
gh issue edit 50 --milestone "v1.1" --repo jongalloway/dotnet-mcp
gh issue edit 62 --milestone "v1.1" --repo jongalloway/dotnet-mcp
gh issue edit 63 --milestone "v1.1" --repo jongalloway/dotnet-mcp
```

## Step 4: Move Issues to v2.0 Milestone

```bash
gh issue edit 52 --milestone "v2.0" --repo jongalloway/dotnet-mcp
gh issue edit 53 --milestone "v2.0" --repo jongalloway/dotnet-mcp
```

## Step 5: Close Out-of-Scope Issues

```bash
# Issue #48: NuGet API Integration (overlaps with official NuGet MCP)
gh issue close 48 --repo jongalloway/dotnet-mcp --comment "Closing as out of scope. This functionality overlaps with the official Microsoft NuGet MCP Server (https://www.nuget.org/packages/NuGet.Mcp.Server) which is mentioned in our README. Advanced NuGet metadata scenarios should be handled by the official NuGet MCP server to avoid duplication and maintenance burden."

# Issue #49: Code Generation & Scaffolding (feature creep - beyond CLI scope)
gh issue close 49 --repo jongalloway/dotnet-mcp --comment "Closing as out of scope. Code generation and scaffolding with Roslyn integration extends beyond our focus of 'quality MCP tools tightly scoped to .NET CLI'. The .NET CLI doesn't provide these commands directly. AI assistants can already generate code without requiring MCP tools for this."

# Issue #51: DevOps Integration for CI/CD (template generation, not CLI execution)
gh issue close 51 --repo jongalloway/dotnet-mcp --comment "Closing as out of scope. Generating CI/CD configuration files and Dockerfiles is template generation, not .NET CLI execution. This falls outside our core focus of wrapping .NET CLI commands. AI assistants can already generate these configuration files without MCP tools."
```

## Step 6: Create New Issues for v1.0

### Issue 1: Comprehensive Test Coverage

```bash
gh issue create --repo jongalloway/dotnet-mcp \
  --title "Add comprehensive test coverage for all MCP tools" \
  --body "## Description
Add comprehensive test coverage for all 44+ MCP tools to ensure quality and reliability.

## Motivation
Quality is a stated priority. Comprehensive testing ensures:
- Tools work correctly across different scenarios
- Changes don't break existing functionality
- Edge cases are handled properly
- MCP protocol compliance

## Scope
- Unit tests for all tools
- Integration tests with real .NET CLI where appropriate
- Mock external dependencies
- Test parameter validation
- Test error handling
- Achieve >80% code coverage

## Success Criteria
- [ ] Unit tests for all tool methods
- [ ] Integration tests for key workflows
- [ ] Code coverage >80%
- [ ] CI runs tests on every PR
- [ ] Documentation on running tests

## Related
- Foundation for quality assurance
- Part of v1.0 readiness" \
  --milestone "v1.0" \
  --label "enhancement,area: testing,priority: high,size: L"
```

### Issue 2: Tool Parameter Validation Enhancement

```bash
gh issue create --repo jongalloway/dotnet-mcp \
  --title "Add pre-CLI parameter validation for all tools" \
  --body "## Description
Enhance parameter validation to catch errors before executing CLI commands, providing better error messages and faster feedback.

## Motivation
Currently some tools pass invalid parameters to CLI, resulting in cryptic error messages. Pre-validation can:
- Provide clearer error messages
- Fail faster (no need to spawn process)
- Guide users to correct values
- Leverage SDK integration for validation

## Scope
- Validate framework TFMs against FrameworkHelper
- Validate template names against TemplateEngineHelper
- Check file/directory paths exist before passing to CLI
- Validate enum-like parameters (configuration, verbosity)
- Use existing SDK helpers where available

## Success Criteria
- [ ] Framework validation using FrameworkHelper
- [ ] Template validation using TemplateEngineHelper
- [ ] Path validation for file-based operations
- [ ] Clear error messages for invalid parameters
- [ ] Tests for validation logic

## Related
- Enhances: All tool methods
- Uses: FrameworkHelper, TemplateEngineHelper" \
  --milestone "v1.0" \
  --label "enhancement,area: sdk-integration,priority: medium,size: S"
```

### Issue 3: Performance Benchmarking and Optimization

```bash
gh issue create --repo jongalloway/dotnet-mcp \
  --title "Add performance benchmarking and set performance budgets" \
  --body "## Description
Establish performance benchmarks and budgets to ensure the MCP server remains fast and responsive.

## Motivation
Performance is a stated priority. Need to:
- Measure current performance
- Set acceptable performance budgets
- Detect performance regressions
- Optimize hot paths

## Scope
- Benchmark tool execution overhead (target: <100ms)
- Benchmark resource access (template cache, framework info)
- Measure memory usage
- Set up performance tests in CI
- Optimize based on measurements

## Success Criteria
- [ ] BenchmarkDotNet integration
- [ ] Performance budgets defined (<100ms overhead per tool)
- [ ] Baseline measurements documented
- [ ] CI fails on regression >20%
- [ ] Optimization of identified hot paths

## Related
- Part of: Quality and performance focus
- Existing: Template caching already implemented" \
  --milestone "v1.0" \
  --label "enhancement,feature: performance,priority: medium,size: M"
```

### Issue 4: AI Assistant Best Practices Documentation

```bash
gh issue create --repo jongalloway/dotnet-mcp \
  --title "Document AI assistant best practices and integration patterns" \
  --body "## Description
Create documentation to help users get the most value from the .NET MCP server with AI assistants.

## Motivation
Users need guidance on:
- Common workflows and patterns
- Example prompts for complex scenarios
- Integration with other Microsoft MCP servers (NuGet, Aspire)
- Best practices for AI orchestration

## Scope
- Common workflow examples (new project, add EF, etc.)
- Complex scenario prompts (microservices with Aspire)
- Integration guide for NuGet and Aspire MCP servers
- Troubleshooting common issues
- Concurrency and orchestration guidance

## Success Criteria
- [ ] Best practices guide (doc/ai-assistant-guide.md)
- [ ] 10+ example workflows with prompts
- [ ] Integration patterns with official MS MCPs
- [ ] Troubleshooting section
- [ ] Link from README

## Related
- Complements: Existing documentation
- References: NuGet MCP, Aspire MCP (in README)" \
  --milestone "v1.0" \
  --label "documentation,priority: medium,size: S"
```

## Summary

After executing all commands above:

**v1.0 Milestone** (9 issues total):
- #114: MCP conformance checks
- #45: Workload management tools
- #46: Project file analysis
- #47: Enhanced error diagnostics
- #61: CapabilityNotAvailable error strategy
- New: Comprehensive test coverage
- New: Parameter validation enhancement
- New: Performance benchmarking
- New: AI assistant best practices

**v1.1 Milestone** (3 issues):
- #50: Solution-wide analysis
- #62: Prompt definitions
- #63: Telemetry & metrics

**v2.0 Milestone** (2 issues):
- #52: Performance profiling
- #53: Migration assistant

**Closed** (3 issues):
- #48: NuGet API integration
- #49: Code generation & scaffolding
- #51: DevOps integration

## Verification

Check milestone assignments:
```bash
gh issue list --repo jongalloway/dotnet-mcp --milestone "v1.0" --state all
gh issue list --repo jongalloway/dotnet-mcp --milestone "v1.1" --state all
gh issue list --repo jongalloway/dotnet-mcp --milestone "v2.0" --state all
gh issue list --repo jongalloway/dotnet-mcp --state closed --limit 10
```
