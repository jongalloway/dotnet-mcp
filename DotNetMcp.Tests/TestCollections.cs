using Xunit;

namespace DotNetMcp.Tests;

// Collection definitions for xUnit test parallelization.
//
// Tests in the same collection run sequentially (one at a time) to avoid race conditions
// when they share state. Tests in different collections can run in parallel.
//
// See: https://xunit.net/docs/running-tests-in-parallel

/// <summary>
/// Collection for tests that use TemplateEngineHelper, which has static shared state
/// including template cache and cache metrics. Tests in this collection run sequentially
/// to avoid interference with cache state.
/// </summary>
/// <remarks>
/// Used by:
/// - CachingIntegrationTests
/// - TemplateEngineHelperTests
/// 
/// These tests verify cache behavior and must run without interference from other tests
/// that might modify the static cache or metrics.
/// </remarks>
[CollectionDefinition("CachingIntegrationTests", DisableParallelization = true)]
public class CachingIntegrationTestsCollection
{
}
