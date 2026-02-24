using System.Text.Json;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

/// <summary>
/// Helper class for analyzing .NET project files (.csproj) using MSBuild APIs.
/// Provides programmatic access to project metadata, dependencies, and configuration.
/// </summary>
public static class ProjectAnalysisHelper
{
    /// <summary>
    /// Analyze a .csproj file and return comprehensive project information.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string containing project analysis</returns>
    public static async Task<string> AnalyzeProjectAsync(string projectPath, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Analyzing project: {ProjectPath}", projectPath);

                // Use a new ProjectCollection to avoid conflicts
                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                var analysis = new
                {
                    success = true,
                    projectPath = projectPath,
                    projectName = Path.GetFileNameWithoutExtension(projectPath),
                    sdk = project.GetPropertyValue("UsingMicrosoftNETSdk") == "true" ? "Microsoft.NET.Sdk" : GetSdkFromProject(project),
                    targetFrameworks = GetTargetFrameworks(project),
                    outputType = project.GetPropertyValue("OutputType"),
                    packageReferences = GetPackageReferences(project),
                    projectReferences = GetProjectReferences(project),
                    buildProperties = GetBuildProperties(project),
                    analyzers = GetAnalyzers(project),
                    implicitUsings = project.GetPropertyValue("ImplicitUsings"),
                    nullable = project.GetPropertyValue("Nullable"),
                    langVersion = project.GetPropertyValue("LangVersion")
                };

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error analyzing project {ProjectPath}", projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error analyzing project: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    /// <summary>
    /// Analyze project dependencies and build a dependency graph.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string containing dependency analysis</returns>
    public static async Task<string> AnalyzeDependenciesAsync(string projectPath, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Analyzing dependencies for: {ProjectPath}", projectPath);

                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                var packageRefs = GetPackageReferences(project);
                var projectRefs = GetProjectReferences(project);
                var frameworks = GetTargetFrameworks(project);

                var analysis = new
                {
                    success = true,
                    projectPath = projectPath,
                    targetFrameworks = frameworks,
                    directPackageDependencies = packageRefs,
                    directProjectDependencies = projectRefs,
                    totalDirectDependencies = packageRefs.Length + projectRefs.Length,
                    notes = new[]
                    {
                        "This shows direct dependencies only.",
                        "For transitive dependencies, use 'dotnet list package --include-transitive'",
                        "For version conflicts, use 'dotnet list package --vulnerable' or '--deprecated'"
                    }
                };

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error analyzing dependencies for {ProjectPath}", projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error analyzing dependencies: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    /// <summary>
    /// Validate project health and detect common issues.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string containing validation results</returns>
    public static async Task<string> ValidateProjectAsync(string projectPath, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Validating project: {ProjectPath}", projectPath);

                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                var warnings = new List<string>();
                var errors = new List<string>();
                var recommendations = new List<string>();

                // Check for SDK
                var sdk = GetSdkFromProject(project);
                if (string.IsNullOrEmpty(sdk))
                {
                    warnings.Add("Project does not specify an SDK (not SDK-style project)");
                }

                // Check target frameworks
                var frameworks = GetTargetFrameworks(project);
                if (frameworks.Length == 0)
                {
                    errors.Add("No target framework specified");
                }
                else
                {
                    foreach (var framework in frameworks)
                    {
                        if (!FrameworkHelper.IsValidFramework(framework))
                        {
                            warnings.Add($"Target framework '{framework}' may not be valid");
                        }
                        else if (!FrameworkHelper.IsLtsFramework(framework) && FrameworkHelper.IsModernNet(framework))
                        {
                            recommendations.Add($"Consider using LTS framework instead of '{framework}'. Latest LTS: {FrameworkHelper.GetLatestLtsFramework()}");
                        }
                    }
                }

                // Check output type
                var outputType = project.GetPropertyValue("OutputType");
                if (string.IsNullOrEmpty(outputType))
                {
                    warnings.Add("OutputType not specified");
                }

                // Check for nullable reference types
                var nullable = project.GetPropertyValue("Nullable");
                if (string.IsNullOrEmpty(nullable) || nullable.Equals("disable", StringComparison.OrdinalIgnoreCase))
                {
                    recommendations.Add("Consider enabling nullable reference types (<Nullable>enable</Nullable>) for better null safety");
                }

                // Check for deprecated packages (this is a placeholder - actual detection requires NuGet API)
                var packageRefs = GetPackageReferences(project);
                recommendations.Add($"Found {packageRefs.Length} package references. Use 'dotnet list package --deprecated' to check for deprecated packages.");
                recommendations.Add("Use 'dotnet list package --vulnerable' to check for security vulnerabilities.");

                var validation = new
                {
                    success = true,
                    projectPath = projectPath,
                    isValid = errors.Count == 0,
                    errors = errors.ToArray(),
                    warnings = warnings.ToArray(),
                    recommendations = recommendations.ToArray()
                };

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(validation, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error validating project {ProjectPath}", projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error validating project: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    /// <summary>
    /// Set (or add) an MSBuild property in the project file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="propertyName">MSBuild property name (e.g., 'OutputType')</param>
    /// <param name="propertyValue">Value to set</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string indicating success or failure</returns>
    public static async Task<string> SetPropertyAsync(string projectPath, string propertyName, string propertyValue, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Setting property {PropertyName}={PropertyValue} in {ProjectPath}", propertyName, propertyValue, projectPath);

                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                project.SetProperty(propertyName, propertyValue);
                project.Save();

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    projectPath,
                    propertyName,
                    propertyValue
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error setting property {PropertyName} in {ProjectPath}", propertyName, projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error setting property: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    /// <summary>
    /// Get the current value of an MSBuild property from the project file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="propertyName">MSBuild property name (e.g., 'OutputType')</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string containing the property value or an error</returns>
    public static async Task<string> GetPropertyAsync(string projectPath, string propertyName, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Getting property {PropertyName} from {ProjectPath}", propertyName, projectPath);

                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                var value = project.GetPropertyValue(propertyName);

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    projectPath,
                    propertyName,
                    propertyValue = value,
                    isSet = !string.IsNullOrEmpty(value)
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting property {PropertyName} from {ProjectPath}", propertyName, projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error getting property: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    /// <summary>
    /// Remove an MSBuild property from the project file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="propertyName">MSBuild property name to remove</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string indicating success or failure</returns>
    public static async Task<string> RemovePropertyAsync(string projectPath, string propertyName, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Removing property {PropertyName} from {ProjectPath}", propertyName, projectPath);

                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                var prop = project.GetProperty(propertyName);
                bool removed;
                if (prop != null)
                {
                    project.RemoveProperty(prop);
                    project.Save();
                    removed = true;
                }
                else
                {
                    removed = false;
                }

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    projectPath,
                    propertyName,
                    removed
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error removing property {PropertyName} from {ProjectPath}", propertyName, projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error removing property: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    /// <summary>
    /// Add an MSBuild item to the project file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="itemType">Item type (e.g., 'Using', 'Content', 'None')</param>
    /// <param name="include">The Include attribute value</param>
    /// <param name="metadata">Optional metadata key/value pairs</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string indicating success or failure</returns>
    public static async Task<string> AddItemAsync(string projectPath, string itemType, string include, IEnumerable<KeyValuePair<string, string>>? metadata = null, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Adding item {ItemType}={Include} to {ProjectPath}", itemType, include, projectPath);

                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                var metadataList = metadata?.ToList() ?? [];
                project.AddItem(itemType, include, metadataList);
                project.Save();

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    projectPath,
                    itemType,
                    include,
                    metadata = metadataList.Count > 0
                        ? metadataList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        : (Dictionary<string, string>?)null
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error adding item {ItemType} to {ProjectPath}", itemType, projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error adding item: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    /// <summary>
    /// Remove an MSBuild item from the project file by matching its type and Include value.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="itemType">Item type (e.g., 'Using', 'Content', 'None')</param>
    /// <param name="include">The Include attribute value to match for removal</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string indicating success or failure</returns>
    public static async Task<string> RemoveItemAsync(string projectPath, string itemType, string include, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Removing item {ItemType}={Include} from {ProjectPath}", itemType, include, projectPath);

                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                var itemToRemove = project.GetItems(itemType)
                    .FirstOrDefault(i => string.Equals(i.EvaluatedInclude, include, StringComparison.OrdinalIgnoreCase));

                bool removed;
                if (itemToRemove != null)
                {
                    project.RemoveItem(itemToRemove);
                    project.Save();
                    removed = true;
                }
                else
                {
                    removed = false;
                }

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    projectPath,
                    itemType,
                    include,
                    removed
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error removing item {ItemType} from {ProjectPath}", itemType, projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error removing item: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    /// <summary>
    /// List MSBuild items of a given type (or all item types) from the project file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="itemType">Optional item type to filter (e.g., 'Using'). If null or empty, returns all items.</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>JSON string containing the list of items</returns>
    public static async Task<string> ListItemsAsync(string projectPath, string? itemType = null, ILogger? logger = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Project file not found: {projectPath}"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                logger?.LogDebug("Listing items (type={ItemType}) from {ProjectPath}", itemType ?? "(all)", projectPath);

                using var projectCollection = new ProjectCollection();
                var project = new Project(projectPath, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports);

                IEnumerable<ProjectItem> items;
                if (!string.IsNullOrWhiteSpace(itemType))
                {
                    items = project.GetItems(itemType);
                }
                else
                {
                    items = project.Items;
                }

                var itemList = items.Select(i => new
                {
                    itemType = i.ItemType,
                    include = i.EvaluatedInclude,
                    metadata = i.DirectMetadata
                        .Where(m => !m.Name.StartsWith("_", StringComparison.Ordinal))
                        .ToDictionary(m => m.Name, m => m.EvaluatedValue)
                }).ToArray();

                projectCollection.UnloadProject(project);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    projectPath,
                    itemType = itemType ?? "(all)",
                    count = itemList.Length,
                    items = itemList
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error listing items from {ProjectPath}", projectPath);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Error listing items: {ex.Message}"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });
    }

    private static string GetSdkFromProject(Project project)
    {
        // Try to get SDK from the project root element
        var sdk = project.Xml.Sdk;
        if (!string.IsNullOrEmpty(sdk))
            return sdk;

        // Check if it's using Microsoft.NET.Sdk
        if (project.GetPropertyValue("UsingMicrosoftNETSdk") == "true")
            return "Microsoft.NET.Sdk";

        return string.Empty;
    }

    private static string[] GetTargetFrameworks(Project project)
    {
        var targetFramework = project.GetPropertyValue("TargetFramework");
        if (!string.IsNullOrEmpty(targetFramework))
            return new[] { targetFramework };

        var targetFrameworks = project.GetPropertyValue("TargetFrameworks");
        if (!string.IsNullOrEmpty(targetFrameworks))
            return targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Array.Empty<string>();
    }

    private static object[] GetPackageReferences(Project project)
    {
        var packages = new List<object>();

        foreach (var item in project.GetItems("PackageReference"))
        {
            var packageName = item.EvaluatedInclude;
            var version = item.GetMetadataValue("Version");
            var privateAssets = item.GetMetadataValue("PrivateAssets");
            var includeAssets = item.GetMetadataValue("IncludeAssets");

            packages.Add(new
            {
                name = packageName,
                version = string.IsNullOrEmpty(version) ? "Not specified" : version,
                privateAssets = string.IsNullOrEmpty(privateAssets) ? null : privateAssets,
                includeAssets = string.IsNullOrEmpty(includeAssets) ? null : includeAssets
            });
        }

        return packages.ToArray();
    }

    private static object[] GetProjectReferences(Project project)
    {
        return project.GetItems("ProjectReference")
            .Select(item => new
            {
                name = Path.GetFileNameWithoutExtension(item.EvaluatedInclude),
                path = item.EvaluatedInclude
            })
            .ToArray<object>();
    }

    private static object GetBuildProperties(Project project)
    {
        return new
        {
            publishAot = project.GetPropertyValue("PublishAot"),
            invariantGlobalization = project.GetPropertyValue("InvariantGlobalization"),
            publishTrimmed = project.GetPropertyValue("PublishTrimmed"),
            publishSingleFile = project.GetPropertyValue("PublishSingleFile"),
            publishReadyToRun = project.GetPropertyValue("PublishReadyToRun"),
            selfContained = project.GetPropertyValue("SelfContained"),
            runtimeIdentifier = project.GetPropertyValue("RuntimeIdentifier"),
            runtimeIdentifiers = project.GetPropertyValue("RuntimeIdentifiers"),
            configuration = project.GetPropertyValue("Configuration"),
            platform = project.GetPropertyValue("Platform"),
            treatWarningsAsErrors = project.GetPropertyValue("TreatWarningsAsErrors"),
            warningLevel = project.GetPropertyValue("WarningLevel")
        };
    }

    private static object[] GetAnalyzers(Project project)
    {
        var analyzers = new List<object>();

        foreach (var item in project.GetItems("Analyzer"))
        {
            analyzers.Add(new
            {
                path = item.EvaluatedInclude
            });
        }

        return analyzers.ToArray();
    }
}
