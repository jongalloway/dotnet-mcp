namespace DotNetMcp.Actions;

/// <summary>Verified read-only actions for dotnet_sdk_query. Values retain the aggregate action numbering.</summary>
public enum DotnetSdkQueryAction
{
    /// <summary>Display the current SDK version.</summary>
    Version = (int)DotnetSdkAction.Version,
    /// <summary>Display SDK information without running CLI first-use setup.</summary>
    Info = (int)DotnetSdkAction.Info,
    /// <summary>List installed SDKs.</summary>
    ListSdks = (int)DotnetSdkAction.ListSdks,
    /// <summary>List installed runtimes.</summary>
    ListRuntimes = (int)DotnetSdkAction.ListRuntimes,
    /// <summary>Describe supported frameworks.</summary>
    FrameworkInfo = (int)DotnetSdkAction.FrameworkInfo,
    /// <summary>Read cache statistics without loading or clearing cached data.</summary>
    CacheMetrics = (int)DotnetSdkAction.CacheMetrics
}

/// <summary>Verified read-only actions for dotnet_project_query.</summary>
public enum DotnetProjectQueryAction
{
    /// <summary>Read already captured process output without starting or stopping a process.</summary>
    Logs = (int)DotnetProjectAction.Logs
}

/// <summary>Snapshot-only actions for dotnet_server_metrics_query.</summary>
public enum DotnetServerMetricsQueryAction
{
    /// <summary>Read invocation metrics.</summary>
    Get = (int)DotnetServerMetricsAction.Get,
    /// <summary>Read token savings estimates.</summary>
    TokenSavingsGet = (int)DotnetServerMetricsAction.TokenSavingsGet
}
