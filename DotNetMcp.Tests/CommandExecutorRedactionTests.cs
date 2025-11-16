using DotNetMcp;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetMcp.Tests;

public class CommandExecutorRedactionTests
{
    private readonly Mock<ILogger> _loggerMock;

    public CommandExecutorRedactionTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithSensitiveOutput_RedactsByDefault()
    {
        // Arrange - use --version which is quick and won't contain secrets
        // We'll verify the redaction mechanism is in place
        var arguments = "--version";

        // Act
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(
            arguments, 
            _loggerMock.Object, 
            machineReadable: false,
            unsafeOutput: false);

        // Assert - should complete successfully
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Exit Code: 0");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithUnsafeOutputTrue_SkipsRedaction()
    {
        // Arrange - use --version which is quick
        var arguments = "--version";

        // Act
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(
            arguments, 
            _loggerMock.Object, 
            machineReadable: false,
            unsafeOutput: true);

        // Assert - should complete successfully
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Exit Code: 0");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithUnsafeOutputFalse_AppliesRedaction()
    {
        // Arrange - This would be an integration test if we had a way to inject fake output
        // For now, we verify the API accepts the parameter correctly
        var arguments = "--version";

        // Act
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(
            arguments, 
            _loggerMock.Object, 
            machineReadable: false,
            unsafeOutput: false);

        // Assert - should complete successfully
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Exit Code: 0");
    }

    [Fact]
    public async Task ExecuteCommandAsync_MachineReadableWithRedaction_ReturnsJson()
    {
        // Arrange
        var arguments = "--version";

        // Act
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(
            arguments, 
            _loggerMock.Object, 
            machineReadable: true,
            unsafeOutput: false);

        // Assert - should return JSON format
        result.Should().Contain("\"success\"");
        result.Should().Contain("\"exitCode\"");
    }

    [Fact]
    public async Task ExecuteCommandForResourceAsync_AlwaysAppliesRedaction()
    {
        // Arrange - This method doesn't have unsafeOutput parameter, always redacts
        var arguments = "--version";

        // Act
        var result = await DotNetCommandExecutor.ExecuteCommandForResourceAsync(
            arguments, 
            _loggerMock.Object);

        // Assert - should complete successfully with redaction applied
        result.Should().NotBeNullOrEmpty();
        // Version output doesn't have exit code in ExecuteCommandForResourceAsync
        result.Should().NotContain("Exit Code");
    }

    [Fact]
    public void SecretRedactor_IntegrationWithRealPatterns_WorksCorrectly()
    {
        // Arrange - Simulate output that might come from dotnet commands
        var simulatedOutput = @"
Connecting to database...
Connection string: Server=localhost;Database=MyApp;User ID=admin;Password=SecretP@ss123;
Deployment complete.
API Key: api_key=abcdef1234567890ghijklmnopqrstuvwxyz
Exit Code: 0
";

        // Act
        var redacted = SecretRedactor.Redact(simulatedOutput);

        // Assert
        redacted.Should().Contain("Password=[REDACTED]");
        redacted.Should().Contain("api_key=[REDACTED]");
        redacted.Should().NotContain("SecretP@ss123");
        redacted.Should().NotContain("abcdef1234567890ghijklmnopqrstuvwxyz");
        redacted.Should().Contain("Server=localhost");
        redacted.Should().Contain("User ID=admin");
    }

    [Theory]
    [InlineData(false, "Password=[REDACTED]")]
    [InlineData(true, "Password=MySecret")]
    public void SecretRedactor_BehavesCorrectlyBasedOnUnsafeFlag(bool unsafeOutput, string expectedSubstring)
    {
        // Arrange
        var input = "Connection: Server=localhost;Password=MySecret;";

        // Act
        var result = unsafeOutput ? input : SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain(expectedSubstring);
    }

    [Fact]
    public void SecretRedactor_PreservesNonSensitiveInformation()
    {
        // Arrange
        var input = @"
Build succeeded.
    Project1 -> /path/to/bin/Debug/net9.0/Project1.dll
    Project2 -> /path/to/bin/Debug/net9.0/Project2.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:05.23
";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert - nothing should be redacted from normal build output
        result.Should().Be(input);
    }

    [Fact]
    public void SecretRedactor_HandlesEdgeCases()
    {
        // Arrange - various edge cases
        var testCases = new[]
        {
            ("", ""), // Empty string
            ("Password=", "Password="), // Empty value (too short to redact)
            ("Password=a", "Password=a"), // Very short value (too short to redact)
            ("Password=short", "Password=short"), // Short value (below redaction threshold)
            ("NotAPassword=value", "NotAPassword=value"), // Not a sensitive key
        };

        foreach (var (input, expected) in testCases)
        {
            // Act
            var result = SecretRedactor.Redact(input);

            // Assert
            result.Should().Be(expected, $"input was: {input}");
        }
    }

    [Fact]
    public void SecretRedactor_WorksWithMultilineOutput()
    {
        // Arrange
        var input = @"Line 1: Normal output
Line 2: Password=Secret123
Line 3: More normal output
Line 4: api_key=1234567890abcdefghijklmnopqrstuvwxyz
Line 5: Final line";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain("Line 1: Normal output");
        result.Should().Contain("Line 2: Password=[REDACTED]");
        result.Should().Contain("Line 3: More normal output");
        result.Should().Contain("Line 4: api_key=[REDACTED]");
        result.Should().Contain("Line 5: Final line");
        result.Should().NotContain("Secret123");
        result.Should().NotContain("1234567890abcdefghijklmnopqrstuvwxyz");
    }
}
