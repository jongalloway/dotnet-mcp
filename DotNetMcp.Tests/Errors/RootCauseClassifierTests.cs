using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for <see cref="RootCauseClassifier"/> covering error code classification,
/// stderr pattern matching, recommended actions, and edge cases.
/// </summary>
public class RootCauseClassifierTests
{
    #region Error Code Classification

    [Theory]
    [InlineData("CS0103", RootCauseKind.CompilationError)]
    [InlineData("CS1001", RootCauseKind.CompilationError)]
    [InlineData("CS0246", RootCauseKind.CompilationError)]
    [InlineData("CS1513", RootCauseKind.CompilationError)]
    public void Classify_CsErrorCode_ReturnsCompilationError(string code, RootCauseKind expected)
    {
        var (kind, action) = RootCauseClassifier.Classify(code, "some message", null, 1);

        Assert.Equal(expected, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.ManualStep, action.ActionKind);
        Assert.Contains("compilation", action.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("MSB3644", RootCauseKind.BuildError)]
    [InlineData("MSB4019", RootCauseKind.BuildError)]
    public void Classify_MsbErrorCode_ReturnsBuildError(string code, RootCauseKind expected)
    {
        var (kind, action) = RootCauseClassifier.Classify(code, "build error", null, 1);

        Assert.Equal(expected, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.ManualStep, action.ActionKind);
    }

    [Fact]
    public void Classify_MSB1003_ReturnsProjectNotFound()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "MSB1003", "Specify a project or solution file.", null, 1);

        Assert.Equal(RootCauseKind.ProjectNotFound, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.ManualStep, action.ActionKind);
        Assert.Contains("project", action.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Classify_MSB4236_ReturnsMissingSdk()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "MSB4236", "The SDK could not be found.", null, 1);

        Assert.Equal(RootCauseKind.MissingSdk, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.RunCommand, action.ActionKind);
        Assert.Equal("dotnet --list-sdks", action.Command);
    }

    [Theory]
    [InlineData("NU1101", RootCauseKind.PackageError)]
    [InlineData("NU1102", RootCauseKind.PackageError)]
    [InlineData("NU1103", RootCauseKind.PackageError)]
    [InlineData("NU1605", RootCauseKind.PackageError)]
    public void Classify_NuErrorCode_ReturnsPackageError(string code, RootCauseKind expected)
    {
        var (kind, action) = RootCauseClassifier.Classify(code, "package error", null, 1);

        Assert.Equal(expected, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.CallTool, action.ActionKind);
        Assert.Equal("DotnetPackage", action.ToolName);
    }

    [Theory]
    [InlineData("NU1301", RootCauseKind.RestoreFailure)]
    [InlineData("NU1303", RootCauseKind.RestoreFailure)]
    public void Classify_NuRestoreErrorCode_ReturnsRestoreFailure(string code, RootCauseKind expected)
    {
        var (kind, action) = RootCauseClassifier.Classify(code, "restore failed", null, 1);

        Assert.Equal(expected, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.CallTool, action.ActionKind);
        Assert.Equal("DotnetProject", action.ToolName);
    }

    [Theory]
    [InlineData("NETSDK1045", RootCauseKind.SdkError)]
    [InlineData("NETSDK1005", RootCauseKind.SdkError)]
    public void Classify_NetSdkErrorCode_ReturnsSdkError(string code, RootCauseKind expected)
    {
        var (kind, action) = RootCauseClassifier.Classify(code, "sdk error", null, 1);

        Assert.Equal(expected, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.RunCommand, action.ActionKind);
        Assert.Equal("dotnet --list-sdks", action.Command);
    }

    [Fact]
    public void Classify_NETSDK1004_ReturnsRestoreFailure()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "NETSDK1004", "Assets file not found.", null, 1);

        Assert.Equal(RootCauseKind.RestoreFailure, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.CallTool, action.ActionKind);
        Assert.Equal("DotnetProject", action.ToolName);
        Assert.NotNull(action.ToolArgs);
        Assert.Equal("Restore", action.ToolArgs["action"]);
    }

    #endregion

    #region Stderr Pattern Classification

    [Fact]
    public void Classify_SdkNotFoundInStderr_ReturnsMissingSdk()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "The SDK 'Microsoft.NET.Sdk' specified was not found.", 1);

        Assert.Equal(RootCauseKind.MissingSdk, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.RunCommand, action.ActionKind);
        Assert.Equal("dotnet --list-sdks", action.Command);
    }

    [Fact]
    public void Classify_SdkNotInstalled_ReturnsMissingSdk()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, ".NET SDK is not installed. Install it from https://dot.net", 1);

        Assert.Equal(RootCauseKind.MissingSdk, kind);
    }

    [Fact]
    public void Classify_WorkloadNotInstalled_ReturnsMissingWorkload()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "The workload 'maui' is not installed. Run dotnet workload install.", 1);

        Assert.Equal(RootCauseKind.MissingWorkload, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.CallTool, action.ActionKind);
        Assert.Equal("DotnetWorkload", action.ToolName);
    }

    [Fact]
    public void Classify_ProjectNotFoundInStderr_ReturnsProjectNotFound()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "The current directory does not contain a project or solution file.", 1);

        // The pattern "does not contain" + "project" matches ProjectNotFound
        Assert.Equal(RootCauseKind.ProjectNotFound, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.ManualStep, action.ActionKind);
    }

    [Fact]
    public void Classify_CouldNotFindCsproj_ReturnsProjectNotFound()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "Could not find a .csproj file in the specified directory.", 1);

        Assert.Equal(RootCauseKind.ProjectNotFound, kind);
    }

    [Fact]
    public void Classify_TestRunFailed_ReturnsTestFailure()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "Test run failed. 3 tests failed.", 1);

        Assert.Equal(RootCauseKind.TestFailure, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.ManualStep, action.ActionKind);
        Assert.Contains("test", action.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Classify_RestoreFailedInStderr_ReturnsRestoreFailure()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "Failed to restore packages for project MyApp.csproj", 1);

        Assert.Equal(RootCauseKind.RestoreFailure, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.CallTool, action.ActionKind);
        Assert.Equal("DotnetProject", action.ToolName);
    }

    [Fact]
    public void Classify_TemplateNotFound_ReturnsTemplateNotFound()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "No templates found matching: 'mytemplate'.", 1);

        Assert.Equal(RootCauseKind.TemplateNotFound, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.CallTool, action.ActionKind);
        Assert.Equal("DotnetSdk", action.ToolName);
    }

    [Fact]
    public void Classify_AccessDenied_ReturnsAccessDenied()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "Access is denied to the file 'output.dll'.", 1);

        Assert.Equal(RootCauseKind.AccessDenied, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.ManualStep, action.ActionKind);
        Assert.Contains("permission", action.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Classify_Timeout_ReturnsTimeout()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "EXIT_1", null, "The operation timed out waiting for NuGet restore.", 1);

        Assert.Equal(RootCauseKind.Timeout, kind);
        Assert.NotNull(action);
        Assert.Equal(ActionKind.ManualStep, action.ActionKind);
        Assert.Contains("retry", action.Description, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Unknown / Edge Cases

    [Fact]
    public void Classify_UnrecognizedCode_ReturnsUnknown()
    {
        var (kind, action) = RootCauseClassifier.Classify(
            "CUSTOM123", "some unrelated error", null, 1);

        Assert.Equal(RootCauseKind.Unknown, kind);
        Assert.Null(action);
    }

    [Fact]
    public void Classify_NullInputs_ReturnsUnknown()
    {
        var (kind, action) = RootCauseClassifier.Classify(null, null, null, null);

        Assert.Equal(RootCauseKind.Unknown, kind);
        Assert.Null(action);
    }

    [Fact]
    public void Classify_EmptyStrings_ReturnsUnknown()
    {
        var (kind, action) = RootCauseClassifier.Classify("", "", "", 0);

        Assert.Equal(RootCauseKind.Unknown, kind);
        Assert.Null(action);
    }

    [Fact]
    public void Classify_OnlyMessage_ClassifiesFromMessage()
    {
        // Message alone should still trigger pattern matching
        var (kind, _) = RootCauseClassifier.Classify(
            "EXIT_1", "Failed to restore packages", null, 1);

        Assert.Equal(RootCauseKind.RestoreFailure, kind);
    }

    [Fact]
    public void Classify_ErrorCodeTakesPrecedenceOverPattern()
    {
        // CS0103 should classify as CompilationError even if stderr mentions "restore"
        var (kind, _) = RootCauseClassifier.Classify(
            "CS0103", "The name 'foo' does not exist", "Failed to restore packages", 1);

        Assert.Equal(RootCauseKind.CompilationError, kind);
    }

    #endregion

    #region Integration with ErrorResultFactory

    [Fact]
    public void CreateResult_WithCompilerError_IncludesRootCause()
    {
        var result = ErrorResultFactory.CreateResult(
            "", "Program.cs(10,5): error CS0103: The name 'foo' does not exist in the current context", 1);

        var errorResponse = Assert.IsType<ErrorResponse>(result);
        var error = Assert.Single(errorResponse.Errors);
        Assert.Equal(RootCauseKind.CompilationError, error.RootCauseKind);
        Assert.NotNull(error.RecommendedAction);
        Assert.Equal(ActionKind.ManualStep, error.RecommendedAction.ActionKind);
    }

    [Fact]
    public void CreateResult_WithNuGetError_IncludesRootCause()
    {
        var result = ErrorResultFactory.CreateResult(
            "", "error NU1101: Unable to find package NonExistent", 1);

        var errorResponse = Assert.IsType<ErrorResponse>(result);
        var error = Assert.Single(errorResponse.Errors);
        Assert.Equal(RootCauseKind.PackageError, error.RootCauseKind);
        Assert.NotNull(error.RecommendedAction);
        Assert.Equal(ActionKind.CallTool, error.RecommendedAction.ActionKind);
        Assert.Equal("DotnetPackage", error.RecommendedAction.ToolName);
    }

    [Fact]
    public void CreateResult_WithNetSdkError_IncludesRootCause()
    {
        var result = ErrorResultFactory.CreateResult(
            "", "error NETSDK1045: The current .NET SDK does not support targeting .NET 6.0", 1);

        var errorResponse = Assert.IsType<ErrorResponse>(result);
        var error = Assert.Single(errorResponse.Errors);
        Assert.Equal(RootCauseKind.SdkError, error.RootCauseKind);
        Assert.NotNull(error.RecommendedAction);
        Assert.Equal(ActionKind.RunCommand, error.RecommendedAction.ActionKind);
    }

    [Fact]
    public void CreateResult_WithGenericError_ClassifiesByStderrPattern()
    {
        var result = ErrorResultFactory.CreateResult(
            "", "Failed to restore packages for MyApp.csproj", 1);

        var errorResponse = Assert.IsType<ErrorResponse>(result);
        var error = Assert.Single(errorResponse.Errors);
        Assert.Equal(RootCauseKind.RestoreFailure, error.RootCauseKind);
    }

    [Fact]
    public void CreateResult_WithUnknownError_OmitsRootCauseKind()
    {
        var result = ErrorResultFactory.CreateResult(
            "", "Something went totally wrong", 1);

        var errorResponse = Assert.IsType<ErrorResponse>(result);
        var error = Assert.Single(errorResponse.Errors);
        Assert.Null(error.RootCauseKind);
        Assert.Null(error.RecommendedAction);
    }

    [Fact]
    public void CreateResult_SuccessResult_HasNoRootCause()
    {
        var result = ErrorResultFactory.CreateResult("Build succeeded.", "", 0);

        Assert.IsType<SuccessResult>(result);
    }

    [Fact]
    public void ToJson_WithRootCause_SerializesEnumAsString()
    {
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
                    RootCauseKind = RootCauseKind.CompilationError,
                    RecommendedAction = new RecommendedAction
                    {
                        ActionKind = ActionKind.ManualStep,
                        Description = "Fix compilation errors"
                    },
                    RawOutput = ""
                }
            }
        };

        var json = ErrorResultFactory.ToJson(errorResponse);

        Assert.Contains("\"rootCauseKind\": \"CompilationError\"", json);
        Assert.Contains("\"actionKind\": \"ManualStep\"", json);
        Assert.Contains("\"description\": \"Fix compilation errors\"", json);
    }

    [Fact]
    public void ToJson_WithNullRootCause_OmitsField()
    {
        var errorResponse = new ErrorResponse
        {
            Success = false,
            ExitCode = 1,
            Errors = new List<ErrorResult>
            {
                new ErrorResult
                {
                    Code = "EXIT_1",
                    Message = "Unknown error",
                    Category = "Unknown",
                    RawOutput = ""
                }
            }
        };

        var json = ErrorResultFactory.ToJson(errorResponse);

        Assert.DoesNotContain("rootCauseKind", json);
        Assert.DoesNotContain("recommendedAction", json);
    }

    #endregion
}
