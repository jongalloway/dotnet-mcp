# Performance Baseline Measurements (v1.0)

This document provides baseline performance measurements for dotnet-mcp v1.0 performance smoke tests.

## Purpose

These measurements establish a performance baseline for:
- Detecting obvious performance regressions in CI
- Understanding typical tool invocation overhead
- Comparing performance across different environments

**Important**: These are informational baselines, not hard performance budgets. Tests are non-blocking and do not fail CI builds.

## Future Work

For comprehensive performance testing and regression detection, see [Issue #151](https://github.com/jongalloway/dotnet-mcp/issues/151):
- BenchmarkDotNet integration
- Performance budgets with enforcement
- Regression gates
- More comprehensive tool coverage
- Memory profiling

## Test Methodology

### Configuration
- **Warmup iterations**: 3 (to stabilize JIT compilation and caching)
- **Measurement iterations**: 10
- **Statistics**: Min, Max, Mean, Median, StdDev, P95, P99
  - Note: P50 (50th percentile) is the same as the median, so we report median only

### Test Cases

#### 1. DotnetTemplateList
**Purpose**: Representative tool with SDK integration and caching

**Characteristics**:
- Uses Template Engine SDK for metadata
- Leverages response caching
- Returns structured data
- Typical of medium-complexity tools

**Expected Performance** (informational):
- Mean: ~500ms (first run after cache clear)
- P95: <1000ms
- Subsequent cached calls: <50ms

#### 2. DotnetSdkVersion
**Purpose**: Fast, simple tool for baseline overhead measurement

**Characteristics**:
- Simple CLI execution (`dotnet --version`)
- Minimal SDK overhead
- Small response size
- Typical of low-complexity tools

**Expected Performance** (informational):
- Mean: ~100ms
- P95: <200ms

## Baseline Measurements

### Local Development Environment

**Environment**:
- OS: [To be filled from local runs]
- .NET SDK: [To be filled from local runs]
- Hardware: [To be filled from local runs]

**Results**:
```
[Run locally and record results here]
```

### GitHub Actions CI (Ubuntu Latest)

**Environment**:
- OS: ubuntu-latest (GitHub Actions)
- .NET SDK: 10.0.x
- Hardware: Standard GitHub Actions runner (2-core, 7GB RAM)

**Results**:
```
[To be filled from CI runs]
```

## How to Run

### Locally
```bash
# Build in Release configuration
dotnet build --configuration Release

# Run performance smoke tests
dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj \
  --no-build --configuration Release \
  --filter-namespace "*Performance*"
```

### In CI
Performance smoke tests run as part of the standard CI build. Results are uploaded as artifacts:
- Artifact name: `performance-results`
- Format: Test output with detailed statistics

## Interpreting Results

### Normal Variance
Performance can vary based on:
- CPU load and thermal throttling
- Disk I/O contention
- Background processes
- Cloud/CI runner allocation

**Expected variance**: ±20-30% between runs is normal

### Potential Regressions
Investigate if you see:
- Mean >2x higher than baseline
- P95 >2x higher than baseline
- Significant increase in standard deviation (>50%)

### Performance Improvements
If results are consistently better:
- Update baseline values in this document
- Document the optimization in commit message

## Notes

- These tests measure **end-to-end overhead**, not micro-benchmarks
- Results include process startup, SDK calls, and response formatting
- Cache behavior significantly affects results (warmup is critical)
- Results are informational only - tests never fail CI builds
- For production performance budgets, use BenchmarkDotNet (Issue #151)
