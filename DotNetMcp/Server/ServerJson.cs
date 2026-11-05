using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ModelContextProtocol;

namespace DotNetMcp;

internal static class ServerJson
{
    internal static JsonSerializerOptions ApplicationOptions { get; } = new(ServerJsonContext.Default.Options)
    {
#if NATIVEAOT
        TypeInfoResolver = ServerJsonContext.Default
#else
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
            ? JsonTypeInfoResolver.Combine(ServerJsonContext.Default, new DefaultJsonTypeInfoResolver())
            : ServerJsonContext.Default
#endif
    };

    internal static JsonSerializerOptions McpOptions { get; } = new(McpJsonUtilities.DefaultOptions)
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(
            ServerJsonContext.Default, McpJsonUtilities.DefaultOptions.TypeInfoResolver!)
    };

    internal static JsonSerializerOptions IndentedOptions { get; } = new(ApplicationOptions)
    {
        WriteIndented = true
    };

    internal static JsonSerializerOptions ResourceOptions { get; } = new(ApplicationOptions)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    internal static string SerializeResource(object value) =>
        JsonSerializer.Serialize(value, ResourceOptions.GetTypeInfo(value.GetType()));

    internal static string ResourceFailure(Exception error) =>
        JsonSerializer.Serialize(new ResourceError(error.Message), ServerJsonContext.Default.ResourceError);

    internal static JsonTypeInfo<T> McpTypeInfo<T>() =>
        (JsonTypeInfo<T>)McpJsonUtilities.DefaultOptions.GetTypeInfo(typeof(T));
}
