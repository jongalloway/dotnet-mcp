using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

internal static class CachePolicy
{
    internal const int DefaultTtlSeconds = 300;

    internal static McpRequestFilter<TParams, TResult> CreateListFilter<TParams, TResult>()
        where TParams : RequestParams
        where TResult : Result, ICacheableResult
        => next => async (context, ct) =>
        {
            var result = await next(context, ct);
            var protocolVersion = context.JsonRpcRequest.Context?.ProtocolVersion
                ?? context.Server.NegotiatedProtocolVersion;
            if (protocolVersion == "2026-07-28")
            {
                result.TimeToLive = TimeSpan.FromSeconds(DefaultTtlSeconds);
                result.CacheScope = CacheScope.Private;
            }

            return result;
        };
}
