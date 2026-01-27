using Xunit;

namespace DotNetMcp.Tests;

public class AspireOutputParserTests
{
    [Fact]
    public void ParseAspireUrls_WithDashboardUrl_ExtractsBothUrls()
    {
        // Arrange
        var output = @"
Building...
Dashboard: https://localhost:17213/login?t=2b4a2ebc362b7fef9b5ccf73e702647b
Application started. Press Ctrl+C to shut down.
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("https://localhost:17213/login?t=2b4a2ebc362b7fef9b5ccf73e702647b", result["dashboardLoginUrl"]);
        Assert.Equal("https://localhost:17213", result["dashboardUrl"]);
    }

    [Fact]
    public void ParseAspireUrls_WithLoginToDashboardAt_ExtractsUrls()
    {
        // Arrange
        var output = @"
info: Aspire.Hosting.DistributedApplication[0]
      Login to the dashboard at https://localhost:15000/login?t=abc123def4567890
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("https://localhost:15000/login?t=abc123def4567890", result["dashboardLoginUrl"]);
        Assert.Equal("https://localhost:15000", result["dashboardUrl"]);
    }

    [Fact]
    public void ParseAspireUrls_WithResourceServiceUrl_ExtractsUrl()
    {
        // Arrange
        var output = @"
ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL: https://localhost:22057
Dashboard: https://localhost:17213/login?t=abc123def4567890
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("https://localhost:22057", result["resourceServiceUrl"]);
        Assert.Contains("dashboardLoginUrl", result.Keys);
    }

    [Fact]
    public void ParseAspireUrls_WithOtlpEndpointUrl_ExtractsUrl()
    {
        // Arrange
        var output = @"
ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL: https://localhost:21030
Dashboard: https://localhost:17213/login?t=abc123def4567890
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("https://localhost:21030", result["otlpEndpointUrl"]);
        Assert.Contains("dashboardLoginUrl", result.Keys);
    }

    [Fact]
    public void ParseAspireUrls_WithAllUrls_ExtractsAll()
    {
        // Arrange
        var output = @"
info: Aspire.Hosting.DistributedApplication[0]
      Dashboard: https://localhost:17213/login?t=2b4a2ebc362b7fef9b5ccf73e702647b
ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL: https://localhost:22057
ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL: https://localhost:21030
Application started.
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("https://localhost:17213/login?t=2b4a2ebc362b7fef9b5ccf73e702647b", result["dashboardLoginUrl"]);
        Assert.Equal("https://localhost:17213", result["dashboardUrl"]);
        Assert.Equal("https://localhost:22057", result["resourceServiceUrl"]);
        Assert.Equal("https://localhost:21030", result["otlpEndpointUrl"]);
    }

    [Fact]
    public void ParseAspireUrls_WithDotnetPrefix_ExtractsUrl()
    {
        // Arrange
        var output = @"
DOTNET_RESOURCE_SERVICE_ENDPOINT_URL: https://localhost:22057
DOTNET_DASHBOARD_OTLP_ENDPOINT_URL: https://localhost:21030
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("https://localhost:22057", result["resourceServiceUrl"]);
        Assert.Equal("https://localhost:21030", result["otlpEndpointUrl"]);
    }

    [Fact]
    public void ParseAspireUrls_WithNoAspireOutput_ReturnsEmpty()
    {
        // Arrange
        var output = @"
Building...
Build succeeded.
Application is running on https://localhost:5001
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAspireUrls_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = AspireOutputParser.ParseAspireUrls("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAspireUrls_WithNull_ReturnsEmpty()
    {
        // Act
        var result = AspireOutputParser.ParseAspireUrls(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void IsAspireOutput_WithDashboardUrl_ReturnsTrue()
    {
        // Arrange
        var output = "Dashboard: https://localhost:17213/login?t=abc123def4567890";

        // Act
        var result = AspireOutputParser.IsAspireOutput(output);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspireOutput_WithLoginToDashboardAt_ReturnsTrue()
    {
        // Arrange
        var output = "Login to the dashboard at https://localhost:17213/login?t=abc123def4567890";

        // Act
        var result = AspireOutputParser.IsAspireOutput(output);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspireOutput_WithAspireResourceServiceUrl_ReturnsTrue()
    {
        // Arrange
        var output = "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL: https://localhost:22057";

        // Act
        var result = AspireOutputParser.IsAspireOutput(output);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspireOutput_WithAspireHostingReference_ReturnsTrue()
    {
        // Arrange
        var output = "info: Aspire.Hosting.DistributedApplication[0]";

        // Act
        var result = AspireOutputParser.IsAspireOutput(output);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspireOutput_WithNonAspireOutput_ReturnsFalse()
    {
        // Arrange
        var output = @"
Building...
Build succeeded.
Application is running on https://localhost:5001
";

        // Act
        var result = AspireOutputParser.IsAspireOutput(output);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAspireOutput_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = AspireOutputParser.IsAspireOutput("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAspireOutput_WithNull_ReturnsFalse()
    {
        // Act
        var result = AspireOutputParser.IsAspireOutput(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ParseAspireUrls_WithMixedCaseKeywords_ExtractsUrls()
    {
        // Arrange
        var output = @"
DASHBOARD: https://localhost:17213/login?t=abc123def4567890
aspire_resource_service_endpoint_url: https://localhost:22057
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(3, result.Count); // dashboardLoginUrl, dashboardUrl, resourceServiceUrl
    }

    [Fact]
    public void ParseAspireUrls_WithHttpUrls_ExtractsUrls()
    {
        // Arrange - testing with http (not https) which is sometimes used in dev
        var output = @"
Dashboard: http://localhost:17213/login?t=abc123def4567890
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("http://localhost:17213/login?t=abc123def4567890", result["dashboardLoginUrl"]);
        Assert.Equal("http://localhost:17213", result["dashboardUrl"]);
    }

    [Fact]
    public void ParseAspireUrls_WithUppercaseToken_ExtractsUrls()
    {
        // Arrange - testing with uppercase hexadecimal characters in token
        var output = @"
Dashboard: https://localhost:17213/login?t=ABC123DEF4567890
";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("https://localhost:17213/login?t=ABC123DEF4567890", result["dashboardLoginUrl"]);
        Assert.Equal("https://localhost:17213", result["dashboardUrl"]);
    }

    [Fact]
    public void ParseAspireUrls_KeysAreCaseInsensitive()
    {
        // Arrange
        var output = "Dashboard: https://localhost:17213/login?t=abc123def4567890";
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Act & Assert - verify case-insensitive access
        Assert.True(result.ContainsKey("dashboardLoginUrl"));
        Assert.True(result.ContainsKey("DASHBOARDLOGINURL"));
        Assert.True(result.ContainsKey("DashboardLoginUrl"));
    }

    [Fact]
    public void ParseAspireUrls_WithShortToken_DoesNotMatch()
    {
        // Arrange - token less than 16 characters should not match (prevents false positives)
        var output = "Dashboard: https://localhost:17213/login?t=abc123";

        // Act
        var result = AspireOutputParser.ParseAspireUrls(output);

        // Assert - should not parse due to token being too short
        Assert.Empty(result);
    }
}
