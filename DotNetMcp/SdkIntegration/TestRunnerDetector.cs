using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotNetMcp.SdkIntegration;

/// <summary>
/// Helper class for detecting the active test runner (MTP vs VSTest) from global.json configuration.
/// </summary>
public static class TestRunnerDetector
{
    /// <summary>
    /// Detect the test runner based on global.json configuration.
    /// </summary>
    /// <param name="workingDirectory">The working directory to start searching from (optional)</param>
    /// <param name="projectPath">The project path to search from if working directory is not provided (optional)</param>
    /// <param name="logger">Optional logger for diagnostic messages</param>
    /// <returns>A tuple containing the detected runner and the source of detection</returns>
    public static (Actions.TestRunner runner, string source) DetectTestRunner(
        string? workingDirectory = null,
        string? projectPath = null,
        ILogger? logger = null)
    {
        // Determine starting directory for global.json search
        string? searchDirectory = DetermineSearchDirectory(workingDirectory, projectPath);

        if (searchDirectory == null)
        {
            logger?.LogDebug("No search directory available for test runner detection, defaulting to VSTest");
            return (Actions.TestRunner.VSTest, "default");
        }

        // Walk up directory tree looking for global.json
        var globalJsonPath = FindGlobalJson(searchDirectory);

        if (globalJsonPath == null)
        {
            logger?.LogDebug("No global.json found, defaulting to VSTest for legacy compatibility");
            return (Actions.TestRunner.VSTest, "default");
        }

        logger?.LogDebug("Found global.json at: {Path}", globalJsonPath);

        // Parse global.json to check for test runner configuration
        try
        {
            var jsonText = File.ReadAllText(globalJsonPath);
            using var doc = JsonDocument.Parse(jsonText);
            
            if (doc.RootElement.TryGetProperty("test", out var testElement))
            {
                if (testElement.TryGetProperty("runner", out var runnerElement))
                {
                    var runnerValue = runnerElement.GetString();
                    if (runnerValue == "Microsoft.Testing.Platform")
                    {
                        logger?.LogDebug("Detected MTP runner from global.json");
                        return (Actions.TestRunner.MicrosoftTestingPlatform, "global.json");
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            logger?.LogWarning(ex, "Failed to read or parse global.json at {Path}, defaulting to VSTest", globalJsonPath);
            return (Actions.TestRunner.VSTest, "default");
        }

        logger?.LogDebug("global.json found but no MTP runner configured, defaulting to VSTest");
        return (Actions.TestRunner.VSTest, "default");
    }

    /// <summary>
    /// Determine the directory to start searching for global.json.
    /// Prioritizes workingDirectory, then projectPath's directory.
    /// </summary>
    private static string? DetermineSearchDirectory(string? workingDirectory, string? projectPath)
    {
        // Prefer working directory if provided
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            try
            {
                return Path.GetFullPath(workingDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // Invalid path, fall through to try project path
            }
        }

        // Try project path directory
        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            try
            {
                var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath));
                if (!string.IsNullOrEmpty(projectDir))
                {
                    return projectDir;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // Invalid path, return null
            }
        }

        return null;
    }

    /// <summary>
    /// Walk up the directory tree to find global.json.
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from</param>
    /// <returns>Full path to global.json if found, null otherwise</returns>
    private static string? FindGlobalJson(string startDirectory)
    {
        try
        {
            var current = new DirectoryInfo(startDirectory);

            while (current != null)
            {
                var globalJsonPath = Path.Join(current.FullName, "global.json");
                if (File.Exists(globalJsonPath))
                {
                    return globalJsonPath;
                }

                current = current.Parent;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // If we can't traverse the directory tree, return null
            return null;
        }

        return null;
    }
}
