using DotNetMcp;
using FluentAssertions;
using Xunit;

namespace DotNetMcp.Tests;

public class ErrorResultFactoryTests
{
    [Fact]
    public void CreateResult_WithExitCode0_ReturnsSuccessResult()
    {
        // Arrange
        var output = "Build succeeded.";
        var error = "";
        var exitCode = 0;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<SuccessResult>();
        var successResult = (SuccessResult)result;
        successResult.Success.Should().BeTrue();
        successResult.ExitCode.Should().Be(0);
        successResult.Output.Should().Contain("Build succeeded");
    }

    [Fact]
    public void CreateResult_WithNonZeroExitCodeAndNoErrors_ReturnsGenericError()
    {
        // Arrange
        var output = "";
        var error = "Command failed";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<ErrorResponse>();
        var errorResponse = (ErrorResponse)result;
        errorResponse.Success.Should().BeFalse();
        errorResponse.ExitCode.Should().Be(1);
        errorResponse.Errors.Should().HaveCount(1);
        errorResponse.Errors[0].Code.Should().Be("EXIT_1");
        errorResponse.Errors[0].Category.Should().Be("Unknown");
    }

    [Fact]
    public void CreateResult_WithCompilerError_ParsesCorrectly()
    {
        // Arrange
        var output = "";
        var error = "Program.cs(10,5): error CS0103: The name 'foo' does not exist in the current context";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<ErrorResponse>();
        var errorResponse = (ErrorResponse)result;
        errorResponse.Success.Should().BeFalse();
        errorResponse.Errors.Should().HaveCount(1);
        
        var parsedError = errorResponse.Errors[0];
        parsedError.Code.Should().Be("CS0103");
        parsedError.Category.Should().Be("Compilation");
        parsedError.Message.Should().Contain("does not exist in the current context");
        parsedError.Hint.Should().Contain("Check for typos or missing using directives");
    }

    [Fact]
    public void CreateResult_WithMSBuildError_ParsesCorrectly()
    {
        // Arrange
        var output = "";
        var error = "error MSB3644: The reference assemblies for .NETFramework,Version=v5.0 were not found";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<ErrorResponse>();
        var errorResponse = (ErrorResponse)result;
        errorResponse.Errors.Should().HaveCount(1);
        
        var parsedError = errorResponse.Errors[0];
        parsedError.Code.Should().Be("MSB3644");
        parsedError.Category.Should().Be("Build");
        parsedError.Message.Should().Contain("reference assemblies");
        parsedError.Hint.Should().Contain("Install the .NET SDK");
    }

    [Fact]
    public void CreateResult_WithNuGetError_ParsesCorrectly()
    {
        // Arrange
        var output = "";
        var error = "error NU1101: Unable to find package NonExistentPackage. No packages exist with this id in source(s): nuget.org";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<ErrorResponse>();
        var errorResponse = (ErrorResponse)result;
        errorResponse.Errors.Should().HaveCount(1);
        
        var parsedError = errorResponse.Errors[0];
        parsedError.Code.Should().Be("NU1101");
        parsedError.Category.Should().Be("Package");
        parsedError.Message.Should().Contain("Unable to find package");
        parsedError.Hint.Should().Contain("Check package name and source");
    }

    [Fact]
    public void CreateResult_WithMultipleErrors_ParsesAll()
    {
        // Arrange
        var output = "";
        var error = @"Program.cs(10,5): error CS0103: The name 'foo' does not exist
Program.cs(15,10): error CS1001: Identifier expected";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<ErrorResponse>();
        var errorResponse = (ErrorResponse)result;
        errorResponse.Errors.Should().HaveCount(2);
        errorResponse.Errors[0].Code.Should().Be("CS0103");
        errorResponse.Errors[1].Code.Should().Be("CS1001");
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        // Arrange
        var successResult = new SuccessResult
        {
            Success = true,
            Output = "Test output",
            ExitCode = 0
        };

        // Act
        var json = ErrorResultFactory.ToJson(successResult);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"success\": true");
        json.Should().Contain("\"output\"");
        json.Should().Contain("\"exitCode\": 0");
    }

    [Fact]
    public void ToJson_WithError_ProducesValidJson()
    {
        // Arrange
        var errorResponse = new ErrorResponse
        {
            Success = false,
            ExitCode = 1,
            Errors = new List<ErrorResult>
            {
                new ErrorResult
                {
                    Code = "CS0103",
                    Message = "Test error",
                    Category = "Compilation",
                    Hint = "Test hint",
                    RawOutput = "Raw output"
                }
            }
        };

        // Act
        var json = ErrorResultFactory.ToJson(errorResponse);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"success\": false");
        json.Should().Contain("\"code\": \"CS0103\"");
        json.Should().Contain("\"category\": \"Compilation\"");
        json.Should().Contain("\"hint\": \"Test hint\"");
    }

    [Fact]
    public void CreateResult_SanitizesPasswordInOutput()
    {
        // Arrange
        var output = "Connection successful with password=MySecretPass123";
        var error = "";
        var exitCode = 0;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<SuccessResult>();
        var successResult = (SuccessResult)result;
        successResult.Output.Should().NotContain("MySecretPass123");
        successResult.Output.Should().Contain("***REDACTED***");
    }

    [Fact]
    public void CreateResult_SanitizesTokenInError()
    {
        // Arrange
        var output = "";
        var error = "Authentication failed: token=ghp_abc123def456";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<ErrorResponse>();
        var errorResponse = (ErrorResponse)result;
        var genericError = errorResponse.Errors.Should().ContainSingle().Subject;
        genericError.RawOutput.Should().NotContain("ghp_abc123def456");
        genericError.RawOutput.Should().Contain("***REDACTED***");
    }

    [Fact]
    public void CreateResult_WithNetSdkError_ParsesCorrectly()
    {
        // Arrange
        var output = "";
        var error = "error NETSDK1045: The current .NET SDK does not support targeting .NET 6.0";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        result.Should().BeOfType<ErrorResponse>();
        var errorResponse = (ErrorResponse)result;
        errorResponse.Errors.Should().HaveCount(1);
        
        var parsedError = errorResponse.Errors[0];
        parsedError.Code.Should().Be("NETSDK1045");
        parsedError.Category.Should().Be("SDK");
        parsedError.Hint.Should().Contain("Update the SDK");
    }
}
