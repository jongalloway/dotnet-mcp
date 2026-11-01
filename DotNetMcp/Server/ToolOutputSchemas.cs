using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace DotNetMcp;

internal static class ToolOutputSchemas
{
    private static readonly JsonElement SdkSchema = WithFallback(Compose(
        (typeof(SdkActionResult), ["version"]),
        (typeof(SdkListResult), ["sdks"]),
        (typeof(RuntimeListResult), ["runtimes"]),
        (typeof(TemplateListResult), []),
        (typeof(TemplateDetailsResult), []),
        (typeof(FrameworkListResult), []),
        (typeof(CacheMetricsResult), [])),
        Enum.GetNames<DotnetSdkAction>().Except(["Version", "ListSdks", "ListRuntimes", "FrameworkInfo", "CacheMetrics"]).ToArray());

    private static readonly JsonElement ProjectSchema = WithFallback(Compose(
        (typeof(ProjectActionResult), ["success", "errorCount", "warningCount", "summary"]),
        (typeof(ProjectActionResult), ["success", "passed", "failed", "skipped", "summary"]),
        (typeof(ProjectActionResult), ["lockInfo"]),
        (typeof(ProjectAnalysisResult), []),
        (typeof(ProjectDependenciesResult), []),
        (typeof(ProjectValidationResult), []),
        (typeof(ProjectPropertyResult), ["success", "projectPath", "propertyName", "propertyValue"]),
        (typeof(ProjectPropertyResult), ["success", "projectPath", "propertyName", "removed"]),
        (typeof(ProjectItemResult), ["success", "projectPath", "itemType", "include"]),
        (typeof(ProjectItemsResult), [])),
        Enum.GetNames<DotnetProjectAction>().Except(["Build", "Test", "Run", "Publish", "Analyze", "Dependencies", "Validate",
            "SetProperty", "GetProperty", "RemoveProperty", "AddItem", "RemoveItem", "ListItems"]).ToArray());

    private static readonly JsonElement PackageSchema = WithFallback(Compose((typeof(PackageListResult), [])),
        Enum.GetNames<DotnetPackageAction>().Except(["List"]).ToArray());
    private static readonly JsonElement SolutionSchema = WithFallback(Compose((typeof(SolutionListResult), [])),
        Enum.GetNames<DotnetSolutionAction>().Except(["List"]).ToArray());
    private static readonly JsonElement CapabilitiesSchema = WithFallback(Compose((typeof(ServerCapabilities), [])), []);

    private static readonly JsonElement MetricsSchema = WithFallback(Compose(
        (typeof(ServerMetricsResponse), []),
        (typeof(ServerMetricsResetResponse), []),
        (typeof(ServerMetricsTokenSavingsResponse), [])), []);

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
                    nameof(DotNetCliTools.DotnetProject) => ProjectSchema,
                    nameof(DotNetCliTools.DotnetPackage) => PackageSchema,
                    nameof(DotNetCliTools.DotnetSolution) => SolutionSchema,
                    nameof(DotNetCliTools.DotnetServerCapabilities) => CapabilitiesSchema,
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
            AllowOmittedNulls(schema);
            var properties = schema["properties"]!.AsObject();
            schema["additionalProperties"] = false;
            if (type == typeof(ProjectActionResult))
            {
                string[] allowed = required.Contains("errorCount")
                    ? ["success", "project", "configuration", "errorCount", "warningCount", "summary", "errors", "warnings", "lockInfo"]
                    : required.Contains("passed")
                        ? ["success", "passed", "failed", "skipped", "durationMs", "summary", "firstFailures", "lockInfo"]
                        : ["lockInfo"];
                foreach (var key in properties.Select(p => p.Key).Except(allowed).ToArray())
                    properties.Remove(key);
            }
            var requiredNames = required.Length == 0 ? properties.Select(property => property.Key).ToArray() : required;
            schema["required"] = new JsonArray(requiredNames.Select(name => (JsonNode?)JsonValue.Create(name)).ToArray());
            foreach (var (name, types) in requiredNames.SelectMany(name =>
                properties[name]?["type"] is JsonArray types ? new[] { (name, types) } : []))
            {
                properties[name]!["type"] = new JsonArray(types.Where(item => item!.GetValue<string>() != "null")
                    .Select(item => item!.DeepClone()).ToArray());
            }

            // Exporter references are rooted in each model; rebase them into the composed document.
            var definitionName = definitions.ContainsKey(type.Name) ? $"{type.Name}{definitions.Count}" : type.Name;
            RebaseReferences(schema, $"#/$defs/{definitionName}");
            FixLockScope(schema);
            definitions[definitionName] = schema;
            branches.Add(new JsonObject { ["$ref"] = $"#/$defs/{definitionName}" });
        }

        return JsonSerializer.SerializeToElement(new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["type"] = "object",
            ["anyOf"] = branches,
            ["$defs"] = definitions
        });
    }

    private static JsonElement WithFallback(JsonElement schema, string[] actions)
    {
        var root = JsonNode.Parse(schema.GetRawText())!.AsObject();
        var errors = JsonNode.Parse(Compose((typeof(ApplicationErrorResult), []), (typeof(ErrorResponse), ["success", "errors"])).GetRawText())!;
        foreach (var (key, value) in errors["$defs"]!.AsObject())
            root["$defs"]![key] = value!.DeepClone();
        root["$defs"]!["ApplicationErrorResult"]!["properties"]!["success"]!["const"] = false;
        root["$defs"]!["ErrorResponse"]!["properties"]!["success"]!["const"] = false;
        foreach (var branch in errors["anyOf"]!.AsArray())
            root["anyOf"]!.AsArray().Add(branch!.DeepClone());
        if (actions.Length > 0)
        {
            var fallback = JsonNode.Parse(Compose((typeof(ActionTextResult), [])).GetRawText())!;
            var definition = fallback["$defs"]!["ActionTextResult"]!.DeepClone();
            definition["properties"]!["action"]!["enum"] = new JsonArray(actions.Select(a => (JsonNode?)JsonValue.Create(a)).ToArray());
            root["$defs"]!["ActionTextResult"] = definition;
            root["anyOf"]!.AsArray().Add(new JsonObject { ["$ref"] = "#/$defs/ActionTextResult" });
        }
        return JsonSerializer.SerializeToElement(root);
    }

    private static void FixLockScope(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            if (obj["properties"] is JsonObject properties && properties.ContainsKey("lockScope"))
            {
                properties["lockScope"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray("project", "solution", "workingDirectory", "global")
                };
                obj["required"] = new JsonArray("lockScope", "lockKey");
            }
            foreach (var child in obj.Select(p => p.Value).OfType<JsonNode>()) FixLockScope(child);
        }
        else if (node is JsonArray array)
            foreach (var child in array.OfType<JsonNode>()) FixLockScope(child);
    }

    private static void RebaseReferences(JsonNode node, string prefix)
    {
        if (node is JsonObject obj)
        {
            if (obj["$ref"] is JsonValue reference && reference.TryGetValue<string>(out var value) && value.StartsWith('#'))
                obj["$ref"] = prefix + value[1..];
            foreach (var valueNode in obj.Select(property => property.Value).OfType<JsonNode>())
                RebaseReferences(valueNode, prefix);
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array.OfType<JsonNode>())
                RebaseReferences(item, prefix);
        }
    }

    private static void AllowOmittedNulls(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            if (obj["properties"] is JsonObject properties && obj["required"] is JsonArray required)
            {
                foreach (var name in required.Select(n => n!.GetValue<string>()).ToArray())
                    if (properties[name]?["type"] is JsonArray types && types.Any(t => t!.GetValue<string>() == "null"))
                        required.Remove(required.First(n => n!.GetValue<string>() == name));
            }
            if (obj["properties"] is JsonObject identity)
            {
                if (identity.ContainsKey("version") && identity.ContainsKey("path") && identity.Count <= 3)
                    obj["required"] = identity.ContainsKey("name") ? new JsonArray("name", "version") : new JsonArray("version");
                if (identity.ContainsKey("requestedVersion") && identity.ContainsKey("name"))
                    obj["required"] = new JsonArray("name");
            }
            foreach (var child in obj.Select(p => p.Value).OfType<JsonNode>()) AllowOmittedNulls(child);
        }
        else if (node is JsonArray array)
            foreach (var child in array.OfType<JsonNode>()) AllowOmittedNulls(child);
    }
}
