using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Query SDK/runtime information, framework descriptions, or existing cache statistics without changing configuration.
    /// Template discovery, installation, cache reload, and global.json changes remain on dotnet_sdk.
    /// </summary>
    /// <param name="action">The read-only SDK operation to perform.</param>
    /// <param name="framework">Optional framework to describe for FrameworkInfo (e.g., 'net10.0').</param>
    /// <param name="workingDirectory">Existing directory used for SDK version resolution.</param>
    [McpServerTool(Title = ".NET SDK Queries", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Version","Info","ListSdks","ListRuntimes","FrameworkInfo","CacheMetrics"]""")]
    public async partial Task<CallToolResult> DotnetSdkQuery(
        DotnetSdkQueryAction action,
        string? framework = null,
        string? workingDirectory = null)
    {
        if (!ParameterValidator.ValidateAction<DotnetSdkQueryAction>(action, out var error))
            return QueryError($"Error: {error}");

        var sdkAction = action switch
        {
            DotnetSdkQueryAction.Version => DotnetSdkAction.Version,
            DotnetSdkQueryAction.Info => DotnetSdkAction.Info,
            DotnetSdkQueryAction.ListSdks => DotnetSdkAction.ListSdks,
            DotnetSdkQueryAction.ListRuntimes => DotnetSdkAction.ListRuntimes,
            DotnetSdkQueryAction.FrameworkInfo => DotnetSdkAction.FrameworkInfo,
            DotnetSdkQueryAction.CacheMetrics => DotnetSdkAction.CacheMetrics,
            _ => throw new InvalidOperationException($"Unsupported query action '{action}'.")
        };
        return MarkQueryFailure(await DotnetSdk(sdkAction, framework: framework, workingDirectory: workingDirectory));
    }

    /// <summary>
    /// Read already captured background-process logs without executing or evaluating a project.
    /// Project analysis, builds, runs, and process management remain on dotnet_project.
    /// </summary>
    /// <param name="action">The read-only project operation: Logs.</param>
    /// <param name="sessionId">ID of an existing background process session.</param>
    /// <param name="tailLines">Optional positive number of most recent lines to return.</param>
    /// <param name="since">Optional ISO 8601 timestamp limiting the returned lines.</param>
    [McpServerTool(Title = ".NET Project Queries", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/File%20Folder/Flat/file_folder_flat.svg")]
    [McpMeta("category", "project")]
    [McpMeta("priority", 8.0)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Logs"]""")]
    public async partial Task<CallToolResult> DotnetProjectQuery(
        DotnetProjectQueryAction action,
        string sessionId,
        int? tailLines = null,
        string? since = null)
    {
        if (!ParameterValidator.ValidateAction<DotnetProjectQueryAction>(action, out var error))
            return QueryError($"Error: {error}");

        return MarkQueryFailure(StructuredContentHelper.ToCallToolResult(await HandleLogsAction(sessionId, tailLines, since)));
    }

    /// <summary>
    /// Read in-memory invocation metrics or token savings snapshots without resetting counters.
    /// Reset operations remain on dotnet_server_metrics. Snapshots can change as other calls are recorded.
    /// </summary>
    /// <param name="action">The snapshot to retrieve: Get or TokenSavingsGet.</param>
    [McpServerTool(Title = "Server Metrics Queries", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Bar%20Chart/Flat/bar_chart_flat.svg")]
    [McpMeta("category", "telemetry")]
    [McpMeta("priority", 6.0)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Get","TokenSavingsGet"]""")]
    public async partial Task<CallToolResult> DotnetServerMetricsQuery(DotnetServerMetricsQueryAction action)
    {
        if (!ParameterValidator.ValidateAction<DotnetServerMetricsQueryAction>(action, out var error))
            return QueryError($"Error: {error}");

        var metricsAction = action switch
        {
            DotnetServerMetricsQueryAction.Get => DotnetServerMetricsAction.Get,
            DotnetServerMetricsQueryAction.TokenSavingsGet => DotnetServerMetricsAction.TokenSavingsGet,
            _ => throw new InvalidOperationException($"Unsupported query action '{action}'.")
        };
        return await DotnetServerMetrics(metricsAction);
    }

    private static CallToolResult QueryError(string message)
    {
        var result = StructuredContentHelper.ToCallToolResult(message);
        result.IsError = true;
        return result;
    }

    private static CallToolResult MarkQueryFailure(CallToolResult result)
    {
        var text = result.GetText();
        if (text.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) || IsCommandFailure(text))
            result.IsError = true;
        return result;
    }
}
