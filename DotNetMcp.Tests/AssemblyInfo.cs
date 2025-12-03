using Xunit;

// Configure xUnit test parallelization behavior
// Tests in different collections can run in parallel, but tests within the same class run sequentially.
// This is the default xUnit behavior, but we explicitly configure it here for clarity.
// 
// MaxParallelThreads = 4 limits parallel execution to avoid overwhelming system resources
// while still allowing parallel execution for better performance.
// 
// Tests that share state (like TemplateEngineHelper cache) should use [Collection] attributes
// to ensure they run sequentially. See TestCollections.cs for collection definitions.
[assembly: CollectionBehavior(MaxParallelThreads = 4)]
