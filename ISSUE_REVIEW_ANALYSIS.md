# Issue Review and Milestone Recommendations
**Date**: December 31, 2025
**Project**: dotnet-mcp
**Focus**: Quality MCP tools tightly scoped to .NET CLI

## Executive Summary

After reviewing all 13 open issues against the project's stated goals of **"providing quality MCP tools that are tightly scoped to the .NET CLI"**, I recommend:

- **Keep and prioritize**: 5 issues (aligned with core .NET CLI focus)
- **Defer to future milestone**: 6 issues (valuable but not core to 1.0)
- **Close or reconsider**: 2 issues (feature creep or out of scope)

## Project Focus Reminder

From the problem statement and copilot-instructions.md:
- **Primary Goal**: Provide quality MCP tools tightly scoped to .NET CLI
- **Architecture**: Hybrid approach - SDK integration for metadata, CLI execution for operations
- **Avoid**: Feature creep
- **Prioritize**: Completeness, quality, MCP spec support, and performance

---

## Issue Analysis by Category

### ✅ HIGH PRIORITY - Core MCP/CLI Alignment (Recommend for v1.0)

#### **Issue #114: Add MCP conformance checks to CI (server)**
- **Labels**: enhancement, priority: medium, area: mcp-protocol, area: testing
- **Status**: KEEP - High Priority
- **Rationale**: 
  - Directly supports MCP spec compliance (stated priority)
  - Quality assurance for MCP protocol implementation
  - Uses C# MCP SDK v0.5.0-preview.1 features already available
  - Low risk, high value for ensuring protocol correctness
- **Recommendation**: **Move to v1.0 milestone** - Critical for MCP spec support
- **Estimated Effort**: M (1-3 days)

#### **Issue #45: Add .NET Workload Management Tools**
- **Labels**: enhancement, feature: workloads, priority: high, size: M
- **Status**: KEEP - High Priority
- **Rationale**:
  - Core .NET CLI functionality (`dotnet workload` commands)
  - Required for MAUI, Blazor WASM, and mobile development
  - Directly wraps CLI commands (aligned with architecture)
  - Fills gap in current tool coverage
- **Recommendation**: **Move to v1.0 milestone** - Essential CLI feature
- **Estimated Effort**: M (1-3 days)

#### **Issue #46: Add Project File Analysis & Introspection**
- **Labels**: enhancement, feature: project-analysis, priority: high, area: sdk-integration, size: L
- **Status**: KEEP - High Priority
- **Rationale**:
  - Leverages existing MSBuild API integration
  - Provides context AI assistants need for smart recommendations
  - Foundation for better error diagnostics
  - Uses SDK integration appropriately (metadata/discovery)
- **Recommendation**: **Move to v1.0 milestone** - Critical for intelligent assistance
- **Estimated Effort**: L (3-7 days)
- **Note**: Should be completed before #47 (enhanced error diagnostics)

#### **Issue #47: Add Enhanced Error Diagnostics with Code Explanations**
- **Labels**: enhancement, feature: diagnostics, priority: high, size: M
- **Status**: KEEP - High Priority
- **Rationale**:
  - Improves quality of existing tools (build, test)
  - Enhances user experience without adding new commands
  - Aligns with quality focus
  - Error code dictionary is maintainable
- **Recommendation**: **Move to v1.0 milestone** - Quality improvement
- **Estimated Effort**: M (1-3 days)
- **Note**: Depends on #46 for best results

#### **Issue #61: Add Fallback CapabilityNotAvailable Error Strategy**
- **Labels**: enhancement, feature: diagnostics, priority: medium
- **Status**: KEEP - Medium Priority
- **Rationale**:
  - Standardizes error handling across all tools
  - Improves MCP protocol compliance
  - Low effort, high value for user experience
- **Recommendation**: **Move to v1.0 milestone** - Error handling consistency
- **Estimated Effort**: S (< 1 day)

---

### ⚠️ MEDIUM PRIORITY - Valuable but Not Critical (Defer to v1.1 or v2.0)

#### **Issue #48: Add NuGet API Integration for Rich Package Metadata**
- **Labels**: enhancement, feature: nuget, priority: medium, area: sdk-integration, size: L
- **Status**: DEFER to v1.1
- **Rationale**:
  - **Overlaps with official Microsoft NuGet MCP Server** (mentioned in README)
  - Project already delegates advanced NuGet scenarios to official NuGet MCP
  - `dotnet package search` already provides basic functionality
  - Adding this creates feature overlap and maintenance burden
- **Recommendation**: **CLOSE or defer to v2.0** - Let official NuGet MCP handle this
- **Alternative**: Document integration with NuGet MCP server for advanced scenarios

#### **Issue #50: Add Solution-Wide Analysis Tools**
- **Labels**: enhancement, feature: project-analysis, priority: medium, area: sdk-integration, size: L
- **Status**: DEFER to v1.1
- **Rationale**:
  - Valuable but extends beyond individual project analysis
  - Depends on #46 (project file analysis)
  - More complex graph algorithms
  - Not essential for basic .NET development workflows
- **Recommendation**: **Defer to v1.1 milestone** - Natural evolution after #46
- **Estimated Effort**: L (3-7 days)

#### **Issue #62: Add Prompt Definitions & Discovery Optimization**
- **Labels**: enhancement, feature: scaffolding, priority: medium
- **Status**: DEFER to v1.1
- **Rationale**:
  - Enhances AI experience but not core .NET CLI functionality
  - MCP spec may evolve in this area
  - Better to wait for SDK/spec maturity
- **Recommendation**: **Defer to v1.1 milestone** - Wait for MCP spec evolution
- **Estimated Effort**: M (1-3 days)

#### **Issue #63: Add Telemetry & Metrics Tool (dotnet_server_metrics)**
- **Labels**: enhancement, feature: diagnostics, priority: medium
- **Status**: DEFER to v1.1
- **Rationale**:
  - Useful for monitoring but not essential
  - No PII concerns due to local-only execution
  - Lower priority than core functionality
- **Recommendation**: **Defer to v1.1 milestone** - Nice to have
- **Estimated Effort**: S (< 1 day)

---

### ❌ LOW PRIORITY - Feature Creep or Out of Scope (Close or Defer to v2.0+)

#### **Issue #49: Add Code Generation & Scaffolding Tools**
- **Labels**: enhancement, feature: scaffolding, priority: medium, size: XL
- **Status**: RECONSIDER - Possible Feature Creep
- **Rationale**:
  - **Significantly beyond .NET CLI scope** - CLI doesn't have these commands
  - Requires Roslyn integration (major dependency)
  - Large effort (XL - > 1 week)
  - AI assistants can already generate code without this
  - Not aligned with "tightly scoped to .NET CLI" focus
- **Recommendation**: **CLOSE or defer to v3.0** - Outside core scope
- **Alternative**: Let AI assistants handle code generation; focus on CLI tools

#### **Issue #51: Add DevOps Integration for CI/CD Workflows**
- **Labels**: enhancement, feature: devops, priority: low, needs-discussion, size: XL
- **Status**: DEFER to v2.0 (needs discussion)
- **Rationale**:
  - Template generation, not CLI execution
  - Large effort (XL)
  - AI assistants can already generate Dockerfiles/CI configs
  - Not core to .NET CLI functionality
  - Better handled by specialized CI/CD tools
- **Recommendation**: **CLOSE or defer to v2.0** - Outside core focus
- **Alternative**: Document best practices for using existing templates

#### **Issue #52: Add Performance Profiling and Analysis Tools**
- **Labels**: enhancement, feature: performance, priority: low, size: XL
- **Status**: DEFER to v2.0
- **Rationale**:
  - Wraps existing diagnostic tools (`dotnet-trace`, `dotnet-counters`)
  - Low priority (labeled as such)
  - Large effort (XL)
  - Advanced use case
- **Recommendation**: **Defer to v2.0 milestone** - Advanced feature
- **Estimated Effort**: XL (> 1 week)

#### **Issue #53: Add .NET Migration Assistant Tools**
- **Labels**: enhancement, feature: migration, priority: low, needs-discussion, size: XL
- **Status**: DEFER to v2.0 (needs discussion)
- **Rationale**:
  - Requires .NET Upgrade Assistant APIs (major dependency)
  - Large effort (XL)
  - Specialized use case
  - Low priority and needs discussion
- **Recommendation**: **Defer to v2.0 milestone** - Specialized feature
- **Estimated Effort**: XL (> 1 week)

---

## Recommended Milestones

### **v1.0 - Core MCP & Essential CLI Tools** (Target: Q1 2026)
**Focus**: MCP spec compliance, core .NET CLI completeness, quality

| Issue | Title | Priority | Effort | Dependencies |
|-------|-------|----------|--------|--------------|
| #114 | MCP conformance checks to CI | HIGH | M (1-3d) | None |
| #45 | .NET Workload Management Tools | HIGH | M (1-3d) | None |
| #46 | Project File Analysis & Introspection | HIGH | L (3-7d) | None |
| #47 | Enhanced Error Diagnostics | HIGH | M (1-3d) | #46 |
| #61 | CapabilityNotAvailable Error Strategy | MEDIUM | S (<1d) | None |

**Total Estimated Effort**: 2-3 weeks
**Outcome**: Production-ready MCP server with complete core .NET CLI coverage

### **v1.1 - Enhanced Intelligence** (Target: Q2 2026)
**Focus**: AI assistance improvements, solution-level features

| Issue | Title | Priority | Effort |
|-------|-------|----------|--------|
| #50 | Solution-Wide Analysis Tools | MEDIUM | L (3-7d) |
| #62 | Prompt Definitions & Discovery Optimization | MEDIUM | M (1-3d) |
| #63 | Telemetry & Metrics Tool | MEDIUM | S (<1d) |

**Total Estimated Effort**: 1-2 weeks

### **v2.0 - Advanced Features** (Target: Q3-Q4 2026)
**Focus**: Advanced scenarios, specialized tools

| Issue | Title | Priority | Effort |
|-------|-------|----------|--------|
| #52 | Performance Profiling and Analysis | LOW | XL (>1w) |
| #53 | .NET Migration Assistant Tools | LOW | XL (>1w) |

### **Closed / Not Planned**

| Issue | Title | Recommendation | Reason |
|-------|-------|----------------|--------|
| #48 | NuGet API Integration | CLOSE | Overlaps with official NuGet MCP server |
| #49 | Code Generation & Scaffolding | CLOSE | Feature creep - beyond CLI scope |
| #51 | DevOps Integration for CI/CD | CLOSE | Template generation, not CLI execution |

---

## Missing Issues - Recommendations for New Issues

Based on the project focus, here are **recommended new issues** to add:

### **1. Comprehensive Test Coverage for All Tools**
- **Priority**: HIGH (v1.0)
- **Rationale**: Quality focus requires solid test coverage
- **Scope**: 
  - Unit tests for all 44+ tools
  - Integration tests with real .NET CLI
  - Mock external dependencies appropriately
- **Estimated Effort**: L (3-7 days)

### **2. Tool Parameter Validation Enhancement**
- **Priority**: MEDIUM (v1.0)
- **Rationale**: Better error messages before CLI execution
- **Scope**:
  - Validate framework TFMs before passing to CLI
  - Validate template names against installed templates
  - Check file/path parameters exist
- **Estimated Effort**: S (<1 day)

### **3. Performance Benchmarking and Optimization**
- **Priority**: MEDIUM (v1.0)
- **Rationale**: Performance is a stated priority
- **Scope**:
  - Benchmark tool execution times
  - Optimize template cache (already implemented)
  - Set performance budgets (e.g., <100ms overhead)
- **Estimated Effort**: M (1-3 days)

### **4. Documentation: AI Assistant Best Practices**
- **Priority**: MEDIUM (v1.0)
- **Rationale**: Help users get the most from MCP integration
- **Scope**:
  - Document common workflows
  - Example prompts for complex scenarios
  - Integration with official MS MCP servers (NuGet, Aspire)
- **Estimated Effort**: S (<1 day)

### **5. MCP Resource Performance Optimization**
- **Priority**: LOW (v1.1)
- **Rationale**: Resources should be faster than tool calls
- **Scope**:
  - Cache SDK/runtime info (changes rarely)
  - Lazy-load template catalog
  - Implement stale-while-revalidate pattern
- **Estimated Effort**: S (<1 day)

---

## Summary of Recommendations

### **Actions for v1.0 Milestone**
1. ✅ **Move to v1.0**: Issues #114, #45, #46, #47, #61
2. ✅ **Create new issues**:
   - Comprehensive test coverage
   - Parameter validation enhancement
   - Performance benchmarking
   - AI assistant best practices documentation

### **Actions for Future Milestones**
3. ⏰ **Defer to v1.1**: Issues #50, #62, #63
4. ⏰ **Defer to v2.0**: Issues #52, #53

### **Actions to Close**
5. ❌ **Close as out of scope**: Issues #48, #49, #51
   - #48: Overlaps with official NuGet MCP
   - #49: Feature creep (code generation beyond CLI)
   - #51: Template generation, not CLI execution

### **Milestone Distribution**

| Milestone | Issue Count | Total Effort | Focus |
|-----------|-------------|--------------|-------|
| v1.0 | 5 existing + 4 new | 3-4 weeks | MCP compliance, core CLI, quality |
| v1.1 | 3 | 1-2 weeks | AI enhancements, solution-level |
| v2.0 | 2 | 2+ weeks | Advanced scenarios |
| Closed | 3 | N/A | Out of scope |

---

## Conclusion

The current issue list shows enthusiasm for expanding features, but many issues represent **feature creep** that would dilute the project's core value proposition. 

By focusing on:
1. **MCP spec compliance** (#114, #61)
2. **Core .NET CLI completeness** (#45)
3. **Intelligent assistance** (#46, #47)
4. **Quality and performance** (new issues)

The v1.0 release will deliver a **production-ready, focused MCP server** that excels at .NET CLI integration rather than a bloated tool trying to do everything.

Advanced features (#52, #53) and AI enhancements (#50, #62, #63) should wait until after v1.0 is solid and adopted. Features that overlap with official Microsoft MCPs (#48) or fall outside CLI scope (#49, #51) should be closed to maintain focus.
