using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetMcp;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;

try
{
    var input = await SdkHelperProtocol.ReadMessageAsync(Console.OpenStandardInput(), SdkHelperProtocol.MaximumRequestBytes);
    var request = JsonSerializer.Deserialize(input, HelperJsonContext.Default.SdkHelperRequest)
        ?? throw new InvalidDataException("An SDK helper request is required.");
    if (!Enum.IsDefined(request.Operation))
        throw new InvalidDataException("Unknown SDK helper operation.");
    SdkHelperResponse response;
    if (request.Operation == SdkHelperOperation.ReadTemplates)
    {
        using var settings = new EngineEnvironmentSettings(
            new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0"), virtualizeSettings: true);
        using var manager = new TemplatePackageManager(settings);
        var templates = await manager.GetTemplatesAsync(default);
        response = new(Templates: templates.Select(TemplateResourceMetadata.FromTemplateInfo).ToArray());
    }
    else
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectPath);
        if (request.Operation is SdkHelperOperation.SetProperty or SdkHelperOperation.GetProperty or SdkHelperOperation.RemoveProperty)
            ArgumentException.ThrowIfNullOrWhiteSpace(request.PropertyName);
        if (request.Operation is SdkHelperOperation.AddItem or SdkHelperOperation.RemoveItem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.ItemType);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Include);
        }
        if (request.Operation == SdkHelperOperation.SetProperty)
            ArgumentNullException.ThrowIfNull(request.PropertyValue);
        var json = request.Operation switch
        {
            SdkHelperOperation.AnalyzeProject => await ProjectAnalysisHelper.AnalyzeProjectAsync(request.ProjectPath),
            SdkHelperOperation.AnalyzeDependencies => await ProjectAnalysisHelper.AnalyzeDependenciesAsync(request.ProjectPath),
            SdkHelperOperation.ValidateProject => await ProjectAnalysisHelper.ValidateProjectAsync(request.ProjectPath),
            SdkHelperOperation.SetProperty => await ProjectAnalysisHelper.SetPropertyAsync(request.ProjectPath, request.PropertyName!, request.PropertyValue!),
            SdkHelperOperation.GetProperty => await ProjectAnalysisHelper.GetPropertyAsync(request.ProjectPath, request.PropertyName!),
            SdkHelperOperation.RemoveProperty => await ProjectAnalysisHelper.RemovePropertyAsync(request.ProjectPath, request.PropertyName!),
            SdkHelperOperation.AddItem => await ProjectAnalysisHelper.AddItemAsync(request.ProjectPath, request.ItemType!, request.Include!, request.Metadata),
            SdkHelperOperation.RemoveItem => await ProjectAnalysisHelper.RemoveItemAsync(request.ProjectPath, request.ItemType!, request.Include!),
            SdkHelperOperation.ListItems => await ProjectAnalysisHelper.ListItemsAsync(request.ProjectPath, request.ItemType),
            _ => throw new InvalidDataException("Unsupported SDK helper operation.")
        };
        response = new(Json: json);
    }
    Console.WriteLine(JsonSerializer.Serialize(response, HelperJsonContext.Default.SdkHelperResponse));
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"SDK metadata helper: {error.Message}");
    return 1;
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(SdkHelperRequest))]
[JsonSerializable(typeof(SdkHelperResponse))]
internal partial class HelperJsonContext : JsonSerializerContext;
