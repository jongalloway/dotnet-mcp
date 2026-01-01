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
        Assert.Contains("[REDACTED]", successResult.Output);
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
        Assert.Contains("[REDACTED]", genericError.RawOutput);
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

    [Fact]
    public void CreateResult_WithResourceNotFoundError_IncludesMcpErrorCode()
    {
        // Arrange - Package not found error
        var output = "";
        var error = "error NU1101: Unable to find package NonExistentPackage. No packages exist with this id in source(s): nuget.org";
        var exitCode = 1;
        var command = "dotnet add package NonExistentPackage";

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode, command);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("NU1101", parsedError.Code);
        Assert.NotNull(parsedError.McpErrorCode);
        Assert.Equal(-32002, parsedError.McpErrorCode); // ResourceNotFound
        
        // Verify Data is populated
        Assert.NotNull(parsedError.Data);
        Assert.Equal(command, parsedError.Data.Command);
        Assert.Equal(exitCode, parsedError.Data.ExitCode);
        Assert.NotNull(parsedError.Data.Stderr);
    }

    [Fact]
    public void CreateResult_WithProjectNotFoundError_IncludesMcpErrorCode()
    {
        // Arrange - Project file not found
        var output = "";
        var error = "error MSB1003: Specify a project or solution file. The current working directory does not contain a project or solution file.";
        var exitCode = 1;
        var command = "dotnet build";

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode, command);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("MSB1003", parsedError.Code);
        Assert.NotNull(parsedError.McpErrorCode);
        Assert.Equal(-32002, parsedError.McpErrorCode); // ResourceNotFound
        
        // Verify Data is populated
        Assert.NotNull(parsedError.Data);
        Assert.Equal(command, parsedError.Data.Command);
        Assert.Equal(exitCode, parsedError.Data.ExitCode);
        Assert.NotNull(parsedError.Data.Stderr);
    }

    [Fact]
    public void CreateResult_WithInvalidParamsError_IncludesMcpErrorCode()
    {
        // Arrange - Invalid parameters (unsupported framework)
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
        Assert.NotNull(parsedError.McpErrorCode);
        Assert.Equal(-32602, parsedError.McpErrorCode); // InvalidParams
    }

    [Fact]
    public void CreateResult_WithGenericError_IncludesMcpErrorCodeAndData()
    {
        // Arrange - Generic error with no specific code
        var output = "";
        var error = "Command failed with unknown error";
        var exitCode = 1;
        var command = "dotnet build MyProject.csproj";

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode, command);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("EXIT_1", parsedError.Code);
        Assert.NotNull(parsedError.McpErrorCode);
        Assert.Equal(-32603, parsedError.McpErrorCode); // InternalError
        
        // Check data payload
        Assert.NotNull(parsedError.Data);
        Assert.Equal(command, parsedError.Data.Command);
        Assert.Equal(exitCode, parsedError.Data.ExitCode);
        Assert.NotNull(parsedError.Data.Stderr);
    }

    [Fact]
    public void CreateResult_WithDataPayload_RedactsSensitiveInformation()
    {
        // Arrange - Error with sensitive data in command and stderr
        var output = "";
        var error = "Authentication failed: token=ghp_secret123456";
        var exitCode = 1;
        var command = "dotnet nuget push api_key=secret_key_123456";

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode, command);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.NotNull(parsedError.Data);
        
        // Verify secrets are redacted in command (api_key=... pattern is recognized)
        Assert.Contains("[REDACTED]", parsedError.Data.Command);
        Assert.DoesNotContain("secret_key_123456", parsedError.Data.Command);
        
        // Verify secrets are redacted in stderr (token=... pattern is recognized)
        Assert.Contains("[REDACTED]", parsedError.Data.Stderr);
        Assert.DoesNotContain("ghp_secret123456", parsedError.Data.Stderr);
    }

    [Fact]
    public void CreateResult_WithCompilerError_DoesNotIncludeMcpErrorCode()
    {
        // Arrange - Standard compiler error that doesn't map to MCP error
        var output = "";
        var error = "Program.cs(10,5): error CS0103: The name 'foo' does not exist in the current context";
        var exitCode = 1;
        var command = "dotnet build test.csproj";

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode, command);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("CS0103", parsedError.Code);
        Assert.Null(parsedError.McpErrorCode); // No MCP error code for regular compiler errors
        
        // ErrorData is still provided even without MCP error code (useful for debugging)
        Assert.NotNull(parsedError.Data);
        Assert.Equal(command, parsedError.Data.Command);
        Assert.Equal(exitCode, parsedError.Data.ExitCode);
    }

    [Fact]
    public void CreateConcurrencyConflict_IncludesMcpErrorCodeAndStructuredData()
    {
        // Act
        var result = ErrorResultFactory.CreateConcurrencyConflict("build", "MyProject.csproj", "test operation");

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        
        var error = result.Errors[0];
        Assert.Equal("CONCURRENCY_CONFLICT", error.Code);
        Assert.NotNull(error.McpErrorCode);
        Assert.Equal(-32603, error.McpErrorCode); // InternalError
        
        // Check structured data
        Assert.NotNull(error.Data);
        Assert.Equal(-1, error.Data.ExitCode);
        Assert.NotNull(error.Data.AdditionalData);
        Assert.Equal("build", error.Data.AdditionalData["operationType"]);
        Assert.Equal("MyProject.csproj", error.Data.AdditionalData["target"]);
        Assert.Equal("test operation", error.Data.AdditionalData["conflictingOperation"]);
    }

    [Fact]
    public void ToJson_WithMcpErrorCode_ProducesValidJson()
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
                    Code = "NU1101",
                    Message = "Package not found",
                    Category = "Package",
                    McpErrorCode = -32002,
                    Data = new ErrorData
                    {
                        Command = "dotnet add package NonExistent",
                        ExitCode = 1,
                        Stderr = "Package not found"
                    }
                }
            }
        };

        // Act
        var json = ErrorResultFactory.ToJson(errorResponse);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("\"mcpErrorCode\": -32002", json);
        Assert.Contains("\"data\"", json);
        Assert.Contains("\"command\"", json);
        Assert.Contains("\"stderr\"", json);
    }

    [Fact]
    public void CreateResult_WithLongStderr_TruncatesCorrectly()
    {
        // Arrange - Create stderr longer than 1000 characters
        var longStderr = new string('x', 1200);
        var output = "";
        var exitCode = 1;
        var command = "dotnet test";

        // Act
        var result = ErrorResultFactory.CreateResult(output, longStderr, exitCode, command);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.NotNull(parsedError.Data);
        Assert.NotNull(parsedError.Data.Stderr);
        
        // Verify truncation: should be <= 1000 characters
        Assert.True(parsedError.Data.Stderr.Length <= 1000);
        
        // Verify truncation suffix is present
        Assert.EndsWith("... (truncated)", parsedError.Data.Stderr);
    }

    [Fact]
    public void CreateResult_WithNoCommandNoStderrAndExitCode0_ReturnsNullErrorData()
    {
        // Arrange - Success case with no command or stderr
        var output = "Build succeeded.";
        var error = "";
        var exitCode = 0;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode, null);

        // Assert
        Assert.IsType<SuccessResult>(result);
        var successResult = (SuccessResult)result;
        Assert.True(successResult.Success);
        Assert.Equal(0, successResult.ExitCode);
    }

    [Fact]
    public void CreateResult_WithNoCommandNoStderrButNonZeroExitCode_CreatesErrorData()
    {
        // Arrange - Failure with no command or stderr but non-zero exit code
        var output = "";
        var error = "";
        var exitCode = 1;
        var command = null as string;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode, command);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        // ErrorData should be created even without command/stderr because exitCode != 0
        Assert.NotNull(parsedError.Data);
        Assert.Null(parsedError.Data.Command);
        Assert.Null(parsedError.Data.Stderr);
        Assert.Equal(exitCode, parsedError.Data.ExitCode);
    }

    [Fact]
    public void CreateResult_WithCS0103_IncludesEnhancedErrorInfo()
    {
        // Arrange
        var output = "";
        var error = "Program.cs(10,5): error CS0103: The name 'foo' does not exist in the current context";
        var exitCode = 1;
        var command = "dotnet build";

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode, command);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("CS0103", parsedError.Code);
        Assert.Equal("Compilation", parsedError.Category);
        
        // Check enhanced error information from dictionary
        Assert.NotNull(parsedError.Explanation);
        Assert.Contains("identifier", parsedError.Explanation, StringComparison.OrdinalIgnoreCase);
        
        Assert.NotNull(parsedError.DocumentationUrl);
        Assert.StartsWith("https://", parsedError.DocumentationUrl);
        Assert.Contains("microsoft.com", parsedError.DocumentationUrl);
        
        Assert.NotNull(parsedError.SuggestedFixes);
        Assert.NotEmpty(parsedError.SuggestedFixes);
        Assert.True(parsedError.SuggestedFixes.Count >= 3);
    }

    [Fact]
    public void CreateResult_WithMSB3644_IncludesSDKInstallationGuidance()
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
        
        // Check enhanced error information
        Assert.NotNull(parsedError.Explanation);
        Assert.Contains("reference assemblies", parsedError.Explanation, StringComparison.OrdinalIgnoreCase);
        
        Assert.NotNull(parsedError.SuggestedFixes);
        Assert.Contains(parsedError.SuggestedFixes, fix => 
            fix.Contains("Install", StringComparison.OrdinalIgnoreCase) &&
            fix.Contains("SDK", StringComparison.OrdinalIgnoreCase));
        
        Assert.NotNull(parsedError.DocumentationUrl);
        Assert.Contains("dotnet", parsedError.DocumentationUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateResult_WithNU1101_IncludesPackageSearchSuggestions()
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
        
        // Check enhanced error information
        Assert.NotNull(parsedError.Explanation);
        Assert.Contains("package", parsedError.Explanation, StringComparison.OrdinalIgnoreCase);
        
        Assert.NotNull(parsedError.SuggestedFixes);
        Assert.Contains(parsedError.SuggestedFixes, fix => 
            fix.Contains("search", StringComparison.OrdinalIgnoreCase));
        
        Assert.NotNull(parsedError.DocumentationUrl);
        Assert.StartsWith("https://", parsedError.DocumentationUrl);
    }

    [Fact]
    public void CreateResult_WithNETSDK1045_IncludesFrameworkVersionGuidance()
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
        
        // Check enhanced error information
        Assert.NotNull(parsedError.Explanation);
        Assert.Contains("SDK", parsedError.Explanation, StringComparison.OrdinalIgnoreCase);
        
        Assert.NotNull(parsedError.SuggestedFixes);
        Assert.Contains(parsedError.SuggestedFixes, fix => 
            fix.Contains("SDK", StringComparison.OrdinalIgnoreCase) ||
            fix.Contains("framework", StringComparison.OrdinalIgnoreCase));
        
        Assert.NotNull(parsedError.DocumentationUrl);
        Assert.Contains("netsdk", parsedError.DocumentationUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateResult_WithNETSDK1004_IncludesRestoreGuidance()
    {
        // Arrange
        var output = "";
        var error = "error NETSDK1004: Assets file 'project.assets.json' not found. Run a NuGet package restore to generate this file.";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("NETSDK1004", parsedError.Code);
        
        // Check enhanced error information
        Assert.NotNull(parsedError.SuggestedFixes);
        Assert.Contains(parsedError.SuggestedFixes, fix => 
            fix.Contains("dotnet restore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateResult_WithUnknownError_DoesNotIncludeEnhancedInfo()
    {
        // Arrange
        var output = "";
        var error = "Some unknown error occurred";
        var exitCode = 1;

        // Act
        var result = ErrorResultFactory.CreateResult(output, error, exitCode);

        // Assert
        Assert.IsType<ErrorResponse>(result);
        var errorResponse = (ErrorResponse)result;
        Assert.Single(errorResponse.Errors);
        
        var parsedError = errorResponse.Errors[0];
        Assert.Equal("EXIT_1", parsedError.Code);
        
        // Unknown errors should not have enhanced info
        Assert.Null(parsedError.Explanation);
        Assert.Null(parsedError.DocumentationUrl);
        Assert.Null(parsedError.SuggestedFixes);
    }

    [Fact]
    public void ToJson_WithEnhancedErrorInfo_IncludesAllFields()
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
                    Message = "The name 'foo' does not exist",
                    Category = "Compilation",
                    Hint = "Check for typos",
                    Explanation = "The compiler cannot find the specified identifier",
                    DocumentationUrl = "https://learn.microsoft.com/dotnet/csharp/cs0103",
                    SuggestedFixes = new List<string> 
                    { 
                        "Check for typos",
                        "Add using directive",
                        "Add package reference"
                    }
                }
            }
        };

        // Act
        var json = ErrorResultFactory.ToJson(errorResponse);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"explanation\"", json);
        Assert.Contains("\"documentationUrl\"", json);
        Assert.Contains("\"suggestedFixes\"", json);
        Assert.Contains("compiler cannot find", json);
        Assert.Contains("learn.microsoft.com", json);
    }
}
