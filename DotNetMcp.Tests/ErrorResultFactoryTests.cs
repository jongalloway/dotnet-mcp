using DotNetMcp;
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
        Assert.IsType<SuccessResult>(result);
        var successResult = (SuccessResult)result;
        Assert.True(successResult.Success);
        Assert.Equal(0, successResult.ExitCode);
        Assert.Contains("Build succeeded", successResult.Output);
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
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.False(errorResponse.Success);
        Assert.Equal(1, errorResponse.ExitCode);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("EXIT_1", errorResponse.Errors[0].Code);
        Assert.Equal("Unknown", errorResponse.Errors[0].Category);
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
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.False(errorResponse.Success);
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("CS0103", parsedError.Code);
        Assert.Equal("Compilation", parsedError.Category);
        Assert.Contains("does not exist in the current context", parsedError.Message);
        Assert.Contains("Check for typos or missing using directives", parsedError.Hint);
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
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("MSB3644", parsedError.Code);
        Assert.Equal("Build", parsedError.Category);
        Assert.Contains("reference assemblies", parsedError.Message);
        Assert.Contains("Install the .NET SDK", parsedError.Hint);
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
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("NU1101", parsedError.Code);
        Assert.Equal("Package", parsedError.Category);
        Assert.Contains("Unable to find package", parsedError.Message);
        Assert.Contains("Check package name and source", parsedError.Hint);
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
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Equal(2, errorResponse.Errors.Count);
        Assert.Equal("CS0103", errorResponse.Errors[0].Code);
        Assert.Equal("CS1001", errorResponse.Errors[1].Code);
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
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("\"success\": true", json);
        Assert.Contains("\"output\"", json);
        Assert.Contains("\"exitCode\": 0", json);
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
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("\"success\": false", json);
        Assert.Contains("\"code\": \"CS0103\"", json);
        Assert.Contains("\"category\": \"Compilation\"", json);
        Assert.Contains("\"hint\": \"Test hint\"", json);
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
        Assert.IsType<SuccessResult>(result);
        var successResult = (SuccessResult)result;
        Assert.DoesNotContain("MySecretPass123", successResult.Output);
        Assert.Contains("***REDACTED***", successResult.Output);
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
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        var genericError = errorResponse.Errors[0];
        Assert.DoesNotContain("ghp_abc123def456", genericError.RawOutput);
        Assert.Contains("***REDACTED***", genericError.RawOutput);
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
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("NETSDK1045", parsedError.Code);
        Assert.Equal("SDK", parsedError.Category);
        Assert.Contains("Update the SDK", parsedError.Hint);
    }
}
