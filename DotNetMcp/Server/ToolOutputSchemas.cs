using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace DotNetMcp;

internal static class ToolOutputSchemas
{
    private static readonly JsonElement SdkSchema = Compose(
        (typeof(SdkActionResult), ["version"]),
        (typeof(SdkListResult), ["sdks"]),
        (typeof(RuntimeListResult), ["runtimes"]));

    private static readonly JsonElement MetricsSchema = Compose(
        (typeof(ServerMetricsResponse), []),
        (typeof(ServerMetricsResetResponse), []),
        (typeof(ServerMetricsTokenSavingsResponse), []));

    internal static IMcpServerBuilder WithDotNetCliTools(this IMcpServerBuilder builder)
    {
        builder.Services.AddTransient<DotNetCliTools>();
        return builder.WithTools(typeof(DotNetCliTools).GetMethods()
            .Where(method => method.IsDefined(typeof(McpServerToolAttribute)))
            .Select(CreateTool));
    }

    internal static McpServerTool CreateTool(MethodInfo method) =>
        McpServerTool.Create(method,
            context => context.Services!.GetRequiredService<DotNetCliTools>(),
            new McpServerToolCreateOptions
            {
                OutputSchema = method.Name switch
                {
                    nameof(DotNetCliTools.DotnetSdk) => SdkSchema,
                    nameof(DotNetCliTools.DotnetServerMetrics) => MetricsSchema,
                    _ => null
                }
            });

    private static JsonElement Compose(params (Type Type, string[] Required)[] alternatives)
    {
        var definitions = new JsonObject();
        var branches = new JsonArray();
        foreach (var (type, required) in alternatives)
        {
            var schema = JsonNode.Parse(AIJsonUtilities.CreateJsonSchema(type).GetRawText())!.AsObject();
            schema.Remove("$schema");
            var properties = schema["properties"]!.AsObject();
            var requiredNames = required.Length == 0 ? properties.Select(property => property.Key).ToArray() : required;
            schema["required"] = new JsonArray(requiredNames.Select(name => (JsonNode?)JsonValue.Create(name)).ToArray());
            foreach (var name in requiredNames)
            {
                if (properties[name]?["type"] is JsonArray types)
                    properties[name]!["type"] = new JsonArray(types.Where(item => item!.GetValue<string>() != "null")
                        .Select(item => item!.DeepClone()).ToArray());
            }

            // Exporter references are rooted in each model; rebase them into the composed document.
            RebaseReferences(schema, $"#/$defs/{type.Name}");
            definitions[type.Name] = schema;
            branches.Add(new JsonObject { ["$ref"] = $"#/$defs/{type.Name}" });
        }

        return JsonSerializer.SerializeToElement(new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["type"] = "object",
            ["anyOf"] = branches,
            ["$defs"] = definitions
        });
    }

    private static void RebaseReferences(JsonNode node, string prefix)
    {
        if (node is JsonObject obj)
        {
            if (obj["$ref"] is JsonValue reference && reference.TryGetValue<string>(out var value) && value.StartsWith('#'))
                obj["$ref"] = prefix + value[1..];
            foreach (var property in obj)
                if (property.Value is not null)
                    RebaseReferences(property.Value, prefix);
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
                if (item is not null)
                    RebaseReferences(item, prefix);
        }
    }
}
