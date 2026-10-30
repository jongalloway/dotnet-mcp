using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

internal static class DestructiveOperationConfirmation
{
    internal static async Task<bool> ConfirmAsync(
        McpServer? server,
        RequestContext<CallToolRequestParams>? context,
        string message,
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (server?.ClientCapabilities?.Elicitation is null)
            return true;

        const string inputKey = "confirmation";
        var request = new ElicitRequestParams
        {
            Message = message,
            RequestedSchema = new()
            {
                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                {
                    ["confirmed"] = new ElicitRequestParams.BooleanSchema
                    {
                        Title = title,
                        Description = description
                    }
                },
                Required = ["confirmed"]
            }
        };

        ElicitResult? result;
        // SDK 2.0 cannot compose explicit MRTR exceptions with Tasks. Its task-aware
        // ElicitAsync interceptor suspends the existing execution via input_required instead.
        var tasksRequested = server.ClientCapabilities?.Extensions?.ContainsKey(TasksProtocol.ExtensionId) == true;
        if (context is not null && server.IsMrtrSupported && !tasksRequested)
        {
            // Bind the continuation to the current tool and arguments, not client-echoed state.
            var arguments = JsonSerializer.Serialize(context.Params.Arguments?.OrderBy(pair => pair.Key, StringComparer.Ordinal));
            var state = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{context.Params.Name}\n{arguments}\n{message}")));
            if (context.Params.RequestState is null && context.Params.InputResponses is null)
            {
                throw new InputRequiredException(
                    new Dictionary<string, InputRequest> { [inputKey] = InputRequest.ForElicitation(request) },
                    state);
            }

            if (context.Params.RequestState != state ||
                context.Params.InputResponses is not { Count: 1 } responses ||
                !responses.TryGetValue(inputKey, out var response))
            {
                throw new McpProtocolException("Invalid destructive-operation confirmation continuation.", McpErrorCode.InvalidParams);
            }

            try
            {
                result = response.Deserialize(InputResponse.ElicitResultJsonTypeInfo);
            }
            catch (JsonException)
            {
                throw new McpProtocolException("Invalid destructive-operation confirmation response.", McpErrorCode.InvalidParams);
            }
        }
        else
        {
            result = await server.ElicitAsync(request, cancellationToken);
        }

        return result?.IsAccepted == true &&
            result.Content?.TryGetValue("confirmed", out var confirmed) == true &&
            confirmed.ValueKind == JsonValueKind.True;
    }
}
