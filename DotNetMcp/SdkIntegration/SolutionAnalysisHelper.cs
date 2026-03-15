using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

/// <summary>
/// Provides solution-level analysis by aggregating project analysis results.
/// Uses dotnet CLI commands and MSBuild APIs for data collection.
/// </summary>
internal static class SolutionAnalysisHelper
{
    /// <summary>
    /// Analyze a solution's structure including project types, frameworks, and dependencies.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file</param>
    /// <param name="executor">Function to execute dotnet CLI commands</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>Formatted text summary of the solution analysis</returns>
    public static async Task<string> AnalyzeSolutionAsync(string solutionPath, Func<string, Task<string>> executor, ILogger? logger = null)
    {
        var projectPaths = await GetProjectPathsAsync(solutionPath, executor, logger);
        if (projectPaths.Error != null)
            return projectPaths.Error;

        var solutionDir = Path.GetDirectoryName(solutionPath)!;
        var sb = new StringBuilder();
        sb.AppendLine($"Solution Analysis: {Path.GetFileName(solutionPath)}");
        sb.AppendLine($"Projects: {projectPaths.Paths.Length}");
        sb.AppendLine(new string('-', 50));

        var allFrameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allPackages = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var projectTypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in projectPaths.Paths)
        {
            var fullPath = Path.GetFullPath(Path.Join(solutionDir, relativePath));
            sb.AppendLine();
            sb.AppendLine($"Project: {Path.GetFileNameWithoutExtension(relativePath)}");
            sb.AppendLine($"  Path: {relativePath}");

            try
            {
                var analysisJson = await ProjectAnalysisHelper.AnalyzeProjectAsync(fullPath, logger);
                using var doc = JsonDocument.Parse(analysisJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    // Output type
                    if (root.TryGetProperty("outputType", out var outputType))
                    {
                        var ot = outputType.GetString() ?? "Library";
                        sb.AppendLine($"  Type: {(string.IsNullOrEmpty(ot) ? "Library" : ot)}");
                        var typeKey = string.IsNullOrEmpty(ot) ? "Library" : ot;
                        projectTypes[typeKey] = projectTypes.GetValueOrDefault(typeKey) + 1;
                    }

                    // Target frameworks
                    if (root.TryGetProperty("targetFrameworks", out var frameworks))
                    {
                        var tfms = frameworks.EnumerateArray()
                            .Select(fw => fw.GetString())
                            .OfType<string>()
                            .ToList();
                        allFrameworks.UnionWith(tfms);
                        sb.AppendLine($"  Frameworks: {string.Join(", ", tfms)}");
                    }

                    // Package references
                    if (root.TryGetProperty("packageReferences", out var packages))
                    {
                        var count = 0;
                        foreach (var pkg in packages.EnumerateArray())
                        {
                            count++;
                            if (pkg.TryGetProperty("name", out var nameEl))
                            {
                                var pkgName = nameEl.GetString() ?? "";
                                var version = pkg.TryGetProperty("version", out var versionEl) ? versionEl.GetString() ?? "" : "";
                                if (!allPackages.ContainsKey(pkgName))
                                    allPackages[pkgName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                if (!string.IsNullOrEmpty(version))
                                    allPackages[pkgName].Add(version);
                            }
                        }
                        sb.AppendLine($"  Packages: {count}");
                    }

                    // Project references
                    if (root.TryGetProperty("projectReferences", out var projRefs))
                    {
                        var refCount = 0;
                        foreach (var _ in projRefs.EnumerateArray())
                            refCount++;
                        if (refCount > 0)
                            sb.AppendLine($"  Project References: {refCount}");
                    }
                }
                else
                {
                    var error = root.TryGetProperty("error", out var errEl) ? errEl.GetString() : "Unknown error";
                    sb.AppendLine($"  Error: {error}");
                }
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to analyze project {ProjectPath}", fullPath);
                sb.AppendLine($"  Error: {ex.Message}");
            }
        }

        // Summary section
        sb.AppendLine();
        sb.AppendLine(new string('=', 50));
        sb.AppendLine("Summary");
        sb.AppendLine(new string('-', 50));
        sb.AppendLine($"Total Projects: {projectPaths.Paths.Length}");

        if (projectTypes.Count > 0)
        {
            sb.AppendLine($"Project Types: {string.Join(", ", projectTypes.Select(kv => $"{kv.Key} ({kv.Value})"))}");
        }

        if (allFrameworks.Count > 0)
        {
            sb.AppendLine($"Frameworks Used: {string.Join(", ", allFrameworks.OrderBy(f => f))}");
        }

        if (allPackages.Count > 0)
        {
            var sharedPackages = allPackages.Where(kv => kv.Value.Count > 0).OrderBy(kv => kv.Key).ToList();
            sb.AppendLine($"Unique Packages: {sharedPackages.Count}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Show the dependency graph between projects in the solution.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file</param>
    /// <param name="executor">Function to execute dotnet CLI commands</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>Text representation of project dependencies</returns>
    public static async Task<string> GetSolutionDependenciesAsync(string solutionPath, Func<string, Task<string>> executor, ILogger? logger = null)
    {
        var projectPaths = await GetProjectPathsAsync(solutionPath, executor, logger);
        if (projectPaths.Error != null)
            return projectPaths.Error;

        var solutionDir = Path.GetDirectoryName(solutionPath)!;
        var sb = new StringBuilder();
        sb.AppendLine($"Solution Dependencies: {Path.GetFileName(solutionPath)}");
        sb.AppendLine(new string('=', 50));

        // Collect all project-to-project references and package references
        var projectDeps = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var allPackages = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in projectPaths.Paths)
        {
            var fullPath = Path.GetFullPath(Path.Join(solutionDir, relativePath));
            var projectName = Path.GetFileNameWithoutExtension(relativePath);

            try
            {
                var depsJson = await ProjectAnalysisHelper.AnalyzeDependenciesAsync(fullPath, logger);
                using var doc = JsonDocument.Parse(depsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    // Project-to-project references
                    var refs = new List<string>();
                    if (root.TryGetProperty("directProjectDependencies", out var projDeps))
                    {
                        foreach (var dep in projDeps.EnumerateArray())
                        {
                            var name = dep.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                            if (!string.IsNullOrEmpty(name))
                                refs.Add(name);
                        }
                    }
                    projectDeps[projectName] = refs;

                    // Package references
                    if (root.TryGetProperty("directPackageDependencies", out var pkgDeps))
                    {
                        foreach (var pkg in pkgDeps.EnumerateArray())
                        {
                            var pkgName = pkg.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                            var version = pkg.TryGetProperty("version", out var versionEl) ? versionEl.GetString() ?? "" : "";
                            if (!string.IsNullOrEmpty(pkgName))
                            {
                                if (!allPackages.ContainsKey(pkgName))
                                    allPackages[pkgName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                allPackages[pkgName][projectName] = version;
                            }
                        }
                    }
                }
                else
                {
                    var error = root.TryGetProperty("error", out var errEl) ? errEl.GetString() : "Unknown error";
                    sb.AppendLine($"  {projectName}: Error - {error}");
                }
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to analyze dependencies for {ProjectPath}", fullPath);
                projectDeps[projectName] = new List<string>();
            }
        }

        // Project-to-project dependency graph
        sb.AppendLine();
        sb.AppendLine("Project References:");
        sb.AppendLine(new string('-', 50));

        var hasAnyProjectRefs = false;
        foreach (var (project, refs) in projectDeps.OrderBy(kv => kv.Key))
        {
            if (refs.Count > 0)
            {
                hasAnyProjectRefs = true;
                sb.AppendLine($"  {project} -> {string.Join(", ", refs)}");
            }
        }

        if (!hasAnyProjectRefs)
        {
            sb.AppendLine("  (no project-to-project references found)");
        }

        // Shared NuGet packages
        var sharedPackages = allPackages.Where(kv => kv.Value.Count > 1).OrderBy(kv => kv.Key).ToList();
        sb.AppendLine();
        sb.AppendLine("Shared NuGet Packages (used by multiple projects):");
        sb.AppendLine(new string('-', 50));

        if (sharedPackages.Count > 0)
        {
            foreach (var (pkgName, usages) in sharedPackages)
            {
                var versions = usages.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var versionInfo = versions.Count == 1 ? versions[0] : string.Join(", ", versions);
                sb.AppendLine($"  {pkgName} ({versionInfo})");
                sb.AppendLine($"    Used by: {string.Join(", ", usages.Keys.OrderBy(k => k))}");
            }
        }
        else
        {
            sb.AppendLine("  (no shared packages found)");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validate solution health and detect issues across projects.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file</param>
    /// <param name="executor">Function to execute dotnet CLI commands</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>Aggregated validation results with warnings</returns>
    public static async Task<string> ValidateSolutionAsync(string solutionPath, Func<string, Task<string>> executor, ILogger? logger = null)
    {
        var projectPaths = await GetProjectPathsAsync(solutionPath, executor, logger);
        if (projectPaths.Error != null)
            return projectPaths.Error;

        var solutionDir = Path.GetDirectoryName(solutionPath)!;
        var sb = new StringBuilder();
        sb.AppendLine($"Solution Validation: {Path.GetFileName(solutionPath)}");
        sb.AppendLine(new string('=', 50));

        var allErrors = new List<string>();
        var allWarnings = new List<string>();
        var allRecommendations = new List<string>();
        var projectFrameworks = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var packageVersions = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var projectsWithIssues = 0;

        foreach (var relativePath in projectPaths.Paths)
        {
            var fullPath = Path.GetFullPath(Path.Join(solutionDir, relativePath));
            var projectName = Path.GetFileNameWithoutExtension(relativePath);

            try
            {
                var validationJson = await ProjectAnalysisHelper.ValidateProjectAsync(fullPath, logger);
                using var doc = JsonDocument.Parse(validationJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    var hasIssues = false;

                    // Collect errors
                    if (root.TryGetProperty("errors", out var errors))
                    {
                        foreach (var err in errors.EnumerateArray())
                        {
                            hasIssues = true;
                            allErrors.Add($"[{projectName}] {err.GetString()}");
                        }
                    }

                    // Collect warnings
                    if (root.TryGetProperty("warnings", out var warnings))
                    {
                        foreach (var warn in warnings.EnumerateArray())
                        {
                            hasIssues = true;
                            allWarnings.Add($"[{projectName}] {warn.GetString()}");
                        }
                    }

                    // Collect recommendations
                    if (root.TryGetProperty("recommendations", out var recs))
                    {
                        foreach (var rec in recs.EnumerateArray())
                        {
                            allRecommendations.Add($"[{projectName}] {rec.GetString()}");
                        }
                    }

                    if (hasIssues) projectsWithIssues++;
                }
                else
                {
                    var error = root.TryGetProperty("error", out var errEl) ? errEl.GetString() : "Unknown error";
                    allErrors.Add($"[{projectName}] Failed to validate: {error}");
                    projectsWithIssues++;
                }

                // Collect frameworks for cross-project check
                var analysisJson = await ProjectAnalysisHelper.AnalyzeProjectAsync(fullPath, logger);
                using var analysisDoc = JsonDocument.Parse(analysisJson);
                var analysisRoot = analysisDoc.RootElement;
                if (analysisRoot.TryGetProperty("success", out var analyzeSuccess) && analyzeSuccess.GetBoolean())
                {
                    if (analysisRoot.TryGetProperty("targetFrameworks", out var frameworks))
                    {
                        projectFrameworks[projectName] = frameworks.EnumerateArray()
                            .Select(fw => fw.GetString())
                            .OfType<string>()
                            .ToArray();
                    }

                    if (analysisRoot.TryGetProperty("packageReferences", out var packages))
                    {
                        foreach (var pkg in packages.EnumerateArray())
                        {
                            var pkgName = pkg.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                            var version = pkg.TryGetProperty("version", out var versionEl) ? versionEl.GetString() ?? "" : "";
                            if (!string.IsNullOrEmpty(pkgName))
                            {
                                if (!packageVersions.ContainsKey(pkgName))
                                    packageVersions[pkgName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                packageVersions[pkgName][projectName] = version;
                            }
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to validate project {ProjectPath}", fullPath);
                allErrors.Add($"[{projectName}] Exception: {ex.Message}");
                projectsWithIssues++;
            }
        }

        // Cross-project checks: framework mismatches
        var uniqueFrameworks = projectFrameworks.Values
            .SelectMany(f => f)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (uniqueFrameworks.Count > 1)
        {
            allWarnings.Add($"Multiple target frameworks detected across solution: {string.Join(", ", uniqueFrameworks)}");
            foreach (var (project, frameworks) in projectFrameworks.OrderBy(kv => kv.Key))
            {
                if (frameworks.Length > 0)
                {
                    allWarnings.Add($"  {project}: {string.Join(", ", frameworks)}");
                }
            }
        }

        // Cross-project checks: package version conflicts
        var versionConflicts = packageVersions
            .Where(kv => kv.Value.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .OrderBy(kv => kv.Key)
            .ToList();

        if (versionConflicts.Count > 0)
        {
            allWarnings.Add("Package version conflicts detected:");
            foreach (var (pkgName, usages) in versionConflicts)
            {
                allWarnings.Add($"  {pkgName}:");
                foreach (var (project, version) in usages.OrderBy(kv => kv.Key))
                {
                    allWarnings.Add($"    {project}: {version}");
                }
            }
        }

        // Output results
        sb.AppendLine($"Projects Analyzed: {projectPaths.Paths.Length}");
        sb.AppendLine($"Projects with Issues: {projectsWithIssues}");
        sb.AppendLine();

        if (allErrors.Count > 0)
        {
            sb.AppendLine($"Errors ({allErrors.Count}):");
            foreach (var error in allErrors)
                sb.AppendLine($"  {error}");
            sb.AppendLine();
        }

        if (allWarnings.Count > 0)
        {
            sb.AppendLine($"Warnings ({allWarnings.Count}):");
            foreach (var warning in allWarnings)
                sb.AppendLine($"  {warning}");
            sb.AppendLine();
        }

        if (allRecommendations.Count > 0)
        {
            sb.AppendLine($"Recommendations ({allRecommendations.Count}):");
            foreach (var rec in allRecommendations)
                sb.AppendLine($"  {rec}");
            sb.AppendLine();
        }

        if (allErrors.Count == 0 && allWarnings.Count == 0)
        {
            sb.AppendLine("No issues found. Solution is healthy.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parse project paths from "dotnet sln list" output.
    /// </summary>
    private static async Task<(string[] Paths, string? Error)> GetProjectPathsAsync(
        string solutionPath, Func<string, Task<string>> executor, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return (Array.Empty<string>(), "Error: solution path is required for this action.");

        string listOutput = await executor($"solution \"{solutionPath}\" list");

        var paths = listOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l =>
                !l.StartsWith("Project(s)", StringComparison.OrdinalIgnoreCase) &&
                !l.StartsWith("---", StringComparison.OrdinalIgnoreCase) &&
                !l.StartsWith("Exit Code:", StringComparison.OrdinalIgnoreCase) &&
                !l.StartsWith("Error", StringComparison.OrdinalIgnoreCase) &&
                (l.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                 l.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase) ||
                 l.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (paths.Length == 0)
        {
            return (Array.Empty<string>(), $"No projects found in solution '{solutionPath}'.\nRaw output:\n{listOutput}");
        }

        return (paths, null);
    }
}