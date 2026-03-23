using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Template Wizard tool — surfaces an interactive UI for browsing templates,
/// configuring options, and creating new .NET projects.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Open an interactive new-project wizard that shows all available .NET templates
    /// as a filterable grid. Select a template to see its parameters, configure options,
    /// and create a project — all from the visual UI.
    /// Use this tool when the user wants to browse templates or create a project interactively.
    /// </summary>
    /// <param name="searchTerm">Optional filter to pre-filter templates by name or keyword</param>
    /// <param name="language">Optional language filter (e.g., 'C#', 'F#', 'VB')</param>
    [McpServerTool(Title = "New Project Wizard", Destructive = false, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Sparkles/Flat/sparkles_flat.svg")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.5)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://dotnet-mcp/template-wizard"}""")]
    [McpMeta("ui/resourceUri", "ui://dotnet-mcp/template-wizard")]
    public async partial Task<CallToolResult> DotnetNewProjectWizard(
        string? searchTerm = null,
        string? language = null)
    {
        // Fetch template list so the LLM has context alongside the UI
        var templateText = await DotnetTemplateList(forceReload: false);

        // Build structured content with optional pre-filters for the wizard
        var structured = new
        {
            action = "templateWizard",
            templateWizardUri = "ui://dotnet-mcp/template-wizard",
            searchTerm,
            language,
            templatesText = templateText
        };

        var displayText = "[The Template Wizard UI is displayed. The user can browse templates, configure options, and create a project from the visual interface.]\n\n"
            + $"Loaded templates{(string.IsNullOrEmpty(searchTerm) ? "" : $" matching '{searchTerm}'")}. Use the wizard above to select a template and configure your new project.";

        return StructuredContentHelper.ToCallToolResult(displayText, structured);
    }
}
