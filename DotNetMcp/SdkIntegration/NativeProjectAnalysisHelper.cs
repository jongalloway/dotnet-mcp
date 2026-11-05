#if NATIVEAOT
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

public static class ProjectAnalysisHelper
{
    public static Task<string> AnalyzeProjectAsync(string projectPath, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.AnalyzeProject, projectPath), logger);

    public static Task<string> AnalyzeDependenciesAsync(string projectPath, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.AnalyzeDependencies, projectPath), logger);

    public static Task<string> ValidateProjectAsync(string projectPath, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.ValidateProject, projectPath), logger);

    public static Task<string> SetPropertyAsync(string projectPath, string propertyName, string propertyValue, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.SetProperty, projectPath, propertyName, propertyValue), logger);

    public static Task<string> GetPropertyAsync(string projectPath, string propertyName, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.GetProperty, projectPath, propertyName), logger);

    public static Task<string> RemovePropertyAsync(string projectPath, string propertyName, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.RemoveProperty, projectPath, propertyName), logger);

    public static Task<string> AddItemAsync(string projectPath, string itemType, string include,
        IEnumerable<KeyValuePair<string, string>>? metadata = null, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.AddItem, projectPath,
            ItemType: itemType, Include: include, Metadata: metadata?.ToArray()), logger);

    public static Task<string> RemoveItemAsync(string projectPath, string itemType, string include, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.RemoveItem, projectPath, ItemType: itemType, Include: include), logger);

    public static Task<string> ListItemsAsync(string projectPath, string? itemType = null, ILogger? logger = null) =>
        SdkHelperClient.ProjectAsync(new(SdkHelperOperation.ListItems, projectPath, ItemType: itemType), logger);
}
#endif
