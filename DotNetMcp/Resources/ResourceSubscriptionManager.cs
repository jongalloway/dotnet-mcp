using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Manages resource subscriptions and sends <see cref="NotificationMethods.ResourceUpdatedNotification"/>
/// notifications to subscribed clients when resources change.
/// </summary>
public class ResourceSubscriptionManager
{
    /// <summary>
    /// The set of resource URIs this server actually serves.
    /// Subscribe/unsubscribe requests for URIs outside this set are rejected with an error.
    /// </summary>
    public static readonly IReadOnlySet<string> KnownResourceUris = new HashSet<string>(StringComparer.Ordinal)
    {
        "dotnet://sdk-info",
        "dotnet://runtime-info",
        "dotnet://templates",
        "dotnet://frameworks",
        "dotnet://telemetry-data",
    };

    private readonly ConcurrentDictionary<string, byte> _subscriptions =
        new(StringComparer.Ordinal);
    private readonly ILogger<ResourceSubscriptionManager>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ResourceSubscriptionManager"/>.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public ResourceSubscriptionManager(ILogger<ResourceSubscriptionManager>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a subscription for the specified resource URI.
    /// </summary>
    /// <param name="uri">The resource URI to subscribe to. Must be one of <see cref="KnownResourceUris"/>.</param>
    /// <exception cref="McpException">Thrown when <paramref name="uri"/> is not a known resource URI.</exception>
    public void Subscribe(string uri)
    {
        if (!KnownResourceUris.Contains(uri))
            throw new McpException($"Unknown resource URI '{uri}'. Known URIs: {string.Join(", ", KnownResourceUris)}");

        _subscriptions.TryAdd(uri, 0);
        _logger?.LogDebug("Client subscribed to resource: {Uri}", uri);
    }

    /// <summary>
    /// Removes the subscription for the specified resource URI.
    /// </summary>
    /// <param name="uri">The resource URI to unsubscribe from. Must be one of <see cref="KnownResourceUris"/>.</param>
    /// <exception cref="McpException">Thrown when <paramref name="uri"/> is not a known resource URI.</exception>
    public void Unsubscribe(string uri)
    {
        if (!KnownResourceUris.Contains(uri))
            throw new McpException($"Unknown resource URI '{uri}'. Known URIs: {string.Join(", ", KnownResourceUris)}");

        _subscriptions.TryRemove(uri, out _);
        _logger?.LogDebug("Client unsubscribed from resource: {Uri}", uri);
    }

    /// <summary>
    /// Returns whether the given URI has an active subscription.
    /// </summary>
    /// <param name="uri">The resource URI to check.</param>
    public bool IsSubscribed(string uri) => _subscriptions.ContainsKey(uri);

    /// <summary>
    /// Gets the collection of currently subscribed resource URIs.
    /// </summary>
    public IEnumerable<string> SubscribedUris => _subscriptions.Keys;

    /// <summary>
    /// Sends a <see cref="NotificationMethods.ResourceUpdatedNotification"/> for the given
    /// resource URI if there is an active subscription and a server connection available.
    /// Errors are logged and swallowed so they do not interrupt the caller.
    /// </summary>
    /// <param name="server">The MCP server connection used to send the notification.</param>
    /// <param name="uri">The URI of the resource that was updated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendResourceUpdatedAsync(
        McpServer? server,
        string uri,
        CancellationToken cancellationToken = default)
    {
        if (server == null || !IsSubscribed(uri))
            return;

        try
        {
            await server.SendNotificationAsync(
                NotificationMethods.ResourceUpdatedNotification,
                new ResourceUpdatedNotificationParams { Uri = uri },
                serializerOptions: null, // use MCP default serializer options
                cancellationToken);

            _logger?.LogDebug("Sent resource updated notification for: {Uri}", uri);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send resource updated notification for {Uri}", uri);
        }
    }
}
