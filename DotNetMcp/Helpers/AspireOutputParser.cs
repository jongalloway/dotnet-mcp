using System.Text.RegularExpressions;

namespace DotNetMcp;

/// <summary>
/// Parser for extracting .NET Aspire dashboard URLs and endpoints from process output.
/// Handles common Aspire output patterns including dashboard login URLs and resource service URLs.
/// </summary>
public static partial class AspireOutputParser
{
    // Aspire dashboard login URL pattern - matches lines like:
    // "Dashboard: https://localhost:17213/login?t=2b4a2ebc362b7fef9b5ccf73e702647b"
    // "Login to the dashboard at https://localhost:17213/login?t=2b4a2ebc362b7fef9b5ccf73e702647b"
    [GeneratedRegex(@"(?:Dashboard:\s*|Login\s+to\s+the\s+dashboard\s+at\s+)(https?://[^\s]+/login\?t=[a-f0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DashboardLoginUrlRegex();

    // Resource service endpoint URL pattern - matches lines like:
    // "Now listening on: https://localhost:22057"
    // "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL: https://localhost:22057"
    [GeneratedRegex(@"(?:ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL:\s*|DOTNET_RESOURCE_SERVICE_ENDPOINT_URL:\s*|Resource\s+service\s+endpoint:\s*)(https?://[^\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ResourceServiceUrlRegex();

    // OTLP endpoint URL pattern - matches lines like:
    // "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL: https://localhost:21030"
    // "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL: https://localhost:21030"
    [GeneratedRegex(@"(?:ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL:\s*|DOTNET_DASHBOARD_OTLP_ENDPOINT_URL:\s*|OTLP\s+endpoint:\s*)(https?://[^\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex OtlpEndpointUrlRegex();

    /// <summary>
    /// Parse Aspire-related URLs from command output.
    /// Extracts dashboard login URLs, resource service URLs, and OTLP endpoints.
    /// </summary>
    /// <param name="output">The stdout/stderr output from dotnet run or similar command</param>
    /// <returns>Dictionary of found URLs with keys like "dashboardLoginUrl", "resourceServiceUrl", "otlpEndpointUrl"</returns>
    public static Dictionary<string, string> ParseAspireUrls(string output)
    {
        var urls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(output))
        {
            return urls;
        }

        // Parse dashboard login URL
        var dashboardMatch = DashboardLoginUrlRegex().Match(output);
        if (dashboardMatch.Success)
        {
            var url = dashboardMatch.Groups[1].Value;
            urls["dashboardLoginUrl"] = url;
            
            // Also add a simplified dashboard URL (without the token)
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var baseUrl = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath.TrimEnd('/')}";
                // Remove /login if present
                if (baseUrl.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = baseUrl[..^6]; // Remove "/login"
                }
                urls["dashboardUrl"] = baseUrl;
            }
        }

        // Parse resource service URL
        var resourceMatch = ResourceServiceUrlRegex().Match(output);
        if (resourceMatch.Success)
        {
            urls["resourceServiceUrl"] = resourceMatch.Groups[1].Value;
        }

        // Parse OTLP endpoint URL
        var otlpMatch = OtlpEndpointUrlRegex().Match(output);
        if (otlpMatch.Success)
        {
            urls["otlpEndpointUrl"] = otlpMatch.Groups[1].Value;
        }

        return urls;
    }

    /// <summary>
    /// Determines if the output appears to be from an Aspire application.
    /// Checks for common Aspire-specific patterns to avoid false positives.
    /// </summary>
    /// <param name="output">The stdout/stderr output to check</param>
    /// <returns>True if the output contains Aspire-specific indicators</returns>
    public static bool IsAspireOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        // Look for Aspire-specific indicators
        return output.Contains("Dashboard:", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("Login to the dashboard at", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("Aspire.Hosting", StringComparison.OrdinalIgnoreCase);
    }
}
