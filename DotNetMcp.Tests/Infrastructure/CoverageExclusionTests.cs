using System.Text.RegularExpressions;
using Xunit;

namespace DotNetMcp.Tests.Infrastructure;

/// <summary>
/// Tests to validate that coverage exclusion patterns are correctly defined and enforced.
/// These tests help prevent regressions in the coverage policy.
/// </summary>
public class CoverageExclusionTests
{
    // These are the patterns we expect to be excluded based on our policy
    private static readonly string[] ExcludedPatterns = new[]
    {
        // Build artifacts
        "**/obj/**",
        "**/bin/**",
        
        // Auto-generated files
        "**/*.g.cs",
        "**/*.GlobalUsings.g.cs",
        "**/*.AssemblyInfo.cs",
        "**/*.AssemblyAttributes.cs",
        
        // Source generator outputs
        "**/generated/**",
        "**/*Generator*/**/*.cs",
        
        // Test projects
        "**/*.Tests/**",
        "**/Tests/**",
    };

    [Fact]
    public void CodecovYml_Exists()
    {
        var repoRoot = FindRepoRoot();
        var codecovPath = Path.Join(repoRoot, "codecov.yml");
        
        Assert.True(File.Exists(codecovPath), 
            $"codecov.yml should exist at {codecovPath}");
    }

    [Fact]
    public void CodecovYml_ContainsIgnoreSection()
    {
        var repoRoot = FindRepoRoot();
        var codecovPath = Path.Join(repoRoot, "codecov.yml");
        var content = File.ReadAllText(codecovPath);
        
        Assert.Contains("ignore:", content);
    }

    [Theory]
    [InlineData("**/obj/**")]
    [InlineData("**/bin/**")]
    [InlineData("**/*.g.cs")]
    [InlineData("**/*.GlobalUsings.g.cs")]
    [InlineData("**/*.AssemblyInfo.cs")]
    [InlineData("**/*.AssemblyAttributes.cs")]
    [InlineData("**/generated/**")]
    [InlineData("**/*Generator*/**/*.cs")]
    [InlineData("**/*.Tests/**")]
    public void CodecovYml_ContainsExpectedExclusionPattern(string pattern)
    {
        var repoRoot = FindRepoRoot();
        var codecovPath = Path.Join(repoRoot, "codecov.yml");
        var content = File.ReadAllText(codecovPath);
        
        Assert.True(content.Contains(pattern),
            $"codecov.yml should contain exclusion pattern: {pattern}");
    }

    [Fact]
    public void TestingDocumentation_DocumentsCoverageExclusionsPolicy()
    {
        var repoRoot = FindRepoRoot();
        var testingDocPath = Path.Join(repoRoot, "doc", "testing.md");
        
        Assert.True(File.Exists(testingDocPath), 
            $"testing.md should exist at {testingDocPath}");
        
        var content = File.ReadAllText(testingDocPath);
        
        // Verify the coverage exclusions section exists
        Assert.Contains("Coverage Exclusions Policy", content);
        
        // Verify key policy points are documented
        Assert.Contains("What We Measure", content);
        
        Assert.Contains("What We Exclude", content);
        
        Assert.Contains("Where Exclusions Are Enforced", content);
    }

    [Theory]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp/obj/Release/net10.0/DotNetMcp.GlobalUsings.g.cs")]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp/obj/Debug/net10.0/DotNetMcp.AssemblyInfo.cs")]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp/obj/Release/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs")]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp/obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs")]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp.Tests/obj/Release/net10.0/DotNetMcp.Tests.GlobalUsings.g.cs")]
    public void ExclusionPatterns_ShouldMatchKnownGeneratedFiles(string filePath)
    {
        var shouldBeExcluded = ShouldBeExcludedByCoveragePolicy(filePath);
        
        Assert.True(shouldBeExcluded,
            $"File should be excluded by coverage policy: {filePath}");
    }

    [Theory]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp/Program.cs")]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp/DotNetCliTools.cs")]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp/Helpers/FrameworkHelper.cs")]
    [InlineData("/home/runner/work/dotnet-mcp/dotnet-mcp/DotNetMcp/Server/ServerCapabilities.cs")]
    public void ExclusionPatterns_ShouldNotMatchProductionCode(string filePath)
    {
        var shouldBeExcluded = ShouldBeExcludedByCoveragePolicy(filePath);
        
        Assert.False(shouldBeExcluded,
            $"Production code should NOT be excluded: {filePath}");
    }

    /// <summary>
    /// Simulates how Codecov evaluates glob patterns.
    /// This is a simplified implementation - Codecov uses gitignore-style matching.
    /// </summary>
    private static bool ShouldBeExcludedByCoveragePolicy(string filePath)
    {
        // Normalize path separators
        var normalizedPath = filePath.Replace('\\', '/');
        
        foreach (var pattern in ExcludedPatterns)
        {
            if (MatchesGlobPattern(normalizedPath, pattern))
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Simple glob pattern matching for common patterns used in codecov.yml.
    /// This is a simplified implementation that handles the patterns we use.
    /// </summary>
    private static bool MatchesGlobPattern(string path, string pattern)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*\*/", ".*?/")  // **/ matches any path segments
            .Replace(@"\*\*", ".*")     // ** matches everything
            .Replace(@"\*", "[^/]*")    // * matches within a segment
            .Replace(@"\?", ".")        // ? matches single char
            + "$";
        
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var slnxPath = Path.Join(directory.FullName, "DotNetMcp.slnx");
            if (File.Exists(slnxPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Unable to locate repository root (DotNetMcp.slnx not found) starting from AppContext.BaseDirectory.");
    }
}
