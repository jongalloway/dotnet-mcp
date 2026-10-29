using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DotNetMcp.Tests;

// SDK 2.0's client removes logLevel during metadata injection. Add it at the wire
// boundary so these tests exercise real per-request logging, not legacy setLevel.
internal sealed class PerRequestLoggingTransport(IClientTransport inner) : IClientTransport
{
    public string Name => inner.Name;

    public async Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
        => new LoggingTransport(await inner.ConnectAsync(cancellationToken));

    private sealed class LoggingTransport(ITransport inner) : ITransport
    {
        public string? SessionId => inner.SessionId;
        public ChannelReader<JsonRpcMessage> MessageReader => inner.MessageReader;

#pragma warning disable MCP9005 // Exercise the retained per-request logging feature.
        public Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
        {
            if (message is JsonRpcRequest { Context.LogLevel: { } level, Params: JsonObject parameters } &&
                parameters["_meta"] is JsonObject meta)
            {
                meta[MetaKeys.LogLevel] = JsonSerializer.SerializeToNode(level, McpJsonUtilities.DefaultOptions);
            }
            return inner.SendMessageAsync(message, cancellationToken);
        }
#pragma warning restore MCP9005

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
