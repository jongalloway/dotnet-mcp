using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class ParameterValidatorTests
{
    [Theory]
    [InlineData(null, true)] // null is valid (use default)
    [InlineData("", true)] // empty is valid (use default)
    [InlineData("   ", true)] // whitespace is valid (use default)
    [InlineData("net10.0", true)]
    [InlineData("net9.0", true)]
    [InlineData("net8.0", true)]
    [InlineData("netcoreapp3.1", true)]
    [InlineData("netstandard2.0", true)]
    [InlineData("netstandard2.1", true)]
    [InlineData("invalid-framework", false)]
    [InlineData("java11", false)]
    [InlineData("python3.9", false)]
    public void ValidateFramework_ValidatesCorrectly(string? framework, bool expectedValid)
    {
        // Act
        var isValid = ParameterValidator.ValidateFramework(framework, out var errorMessage);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.Null(errorMessage);
        }
        else
        {
            Assert.NotNull(errorMessage);
            Assert.Contains("Invalid framework", errorMessage);
        }
    }

    [Theory]
    [InlineData(null, true)] // null is valid (use default)
    [InlineData("", true)] // empty is valid (use default)
    [InlineData("   ", true)] // whitespace is valid (use default)
    [InlineData("Debug", true)]
    [InlineData("debug", true)] // case-insensitive
    [InlineData("DEBUG", true)] // case-insensitive
    [InlineData("Release", true)]
    [InlineData("release", true)] // case-insensitive
    [InlineData("RELEASE", true)] // case-insensitive
    [InlineData("Production", false)]
    [InlineData("Development", false)]
    [InlineData("Custom", false)]
    public void ValidateConfiguration_ValidatesCorrectly(string? configuration, bool expectedValid)
    {
        // Act
        var isValid = ParameterValidator.ValidateConfiguration(configuration, out var errorMessage);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.Null(errorMessage);
        }
        else
        {
            Assert.NotNull(errorMessage);
            Assert.Contains("Invalid configuration", errorMessage);
            Assert.Contains("Debug", errorMessage);
            Assert.Contains("Release", errorMessage);
        }
    }

    [Theory]
    [InlineData(null, true)] // null is valid (use default)
    [InlineData("", true)] // empty is valid (use default)
    [InlineData("   ", true)] // whitespace is valid (use default)
    [InlineData("q", true)]
    [InlineData("quiet", true)]
    [InlineData("m", true)]
    [InlineData("minimal", true)]
    [InlineData("n", true)]
    [InlineData("normal", true)]
    [InlineData("d", true)]
    [InlineData("detailed", true)]
    [InlineData("diag", true)]
    [InlineData("diagnostic", true)]
    [InlineData("QUIET", true)] // case-insensitive
    [InlineData("NORMAL", true)] // case-insensitive
    [InlineData("invalid", false)]
    [InlineData("verbose", false)]
    [InlineData("trace", false)]
    public void ValidateVerbosity_ValidatesCorrectly(string? verbosity, bool expectedValid)
    {
        // Act
        var isValid = ParameterValidator.ValidateVerbosity(verbosity, out var errorMessage);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.Null(errorMessage);
        }
        else
        {
            Assert.NotNull(errorMessage);
            Assert.Contains("Invalid verbosity", errorMessage);
        }
    }

    [Theory]
    [InlineData(null, true)] // null is valid (use default)
    [InlineData("", true)] // empty is valid (use default)
    [InlineData("   ", true)] // whitespace is valid (use default)
    [InlineData("win-x64", true)]
    [InlineData("win-x86", true)]
    [InlineData("win-arm64", true)]
    [InlineData("linux-x64", true)]
    [InlineData("linux-arm", true)]
    [InlineData("linux-arm64", true)]
    [InlineData("linux-musl-x64", true)]
    [InlineData("osx-x64", true)]
    [InlineData("osx-arm64", true)]
    [InlineData("win10-x64", true)]
    [InlineData("android-arm64", true)]
    [InlineData("ios-arm64", true)]
    [InlineData("iossimulator-x64", true)]
    [InlineData("WIN-X64", true)] // case-insensitive
    [InlineData("invalid-rid", false)]
    [InlineData("windows-x64", false)] // should be 'win-x64'
    [InlineData("macos-arm64", false)] // should be 'osx-arm64'
    public void ValidateRuntimeIdentifier_ValidatesCorrectly(string? runtime, bool expectedValid)
    {
        // Act
        var isValid = ParameterValidator.ValidateRuntimeIdentifier(runtime, out var errorMessage);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.Null(errorMessage);
        }
        else
        {
            Assert.NotNull(errorMessage);
            Assert.Contains("Invalid runtime identifier", errorMessage);
        }
    }

    [Fact]
    public void ValidateFilePath_WithNull_ReturnsTrue()
    {
        // Act
        var isValid = ParameterValidator.ValidateFilePath(null, "testParam", out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateFilePath_WithEmptyString_ReturnsTrue()
    {
        // Act
        var isValid = ParameterValidator.ValidateFilePath("", "testParam", out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateFilePath_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = "/path/to/nonexistent/file.txt";

        // Act
        var isValid = ParameterValidator.ValidateFilePath(nonExistentPath, "testParam", out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("File not found", errorMessage);
        Assert.Contains(nonExistentPath, errorMessage);
    }

    [Fact]
    public void ValidateDirectoryPath_WithNull_ReturnsTrue()
    {
        // Act
        var isValid = ParameterValidator.ValidateDirectoryPath(null, "testParam", out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateDirectoryPath_WithEmptyString_ReturnsTrue()
    {
        // Act
        var isValid = ParameterValidator.ValidateDirectoryPath("", "testParam", out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateDirectoryPath_WithNonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = "/path/to/nonexistent/directory";

        // Act
        var isValid = ParameterValidator.ValidateDirectoryPath(nonExistentPath, "testParam", out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Directory not found", errorMessage);
        Assert.Contains(nonExistentPath, errorMessage);
    }

    [Fact]
    public void ValidateProjectPath_WithNull_ReturnsTrue()
    {
        // Act
        var isValid = ParameterValidator.ValidateProjectPath(null, out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateProjectPath_WithEmptyString_ReturnsTrue()
    {
        // Act
        var isValid = ParameterValidator.ValidateProjectPath("", out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateProjectPath_WithNonExistentFile_ReturnsTrue()
    {
        // Arrange - Non-existent but valid extension
        var nonExistentPath = "/path/to/nonexistent/project.csproj";

        // Act
        var isValid = ParameterValidator.ValidateProjectPath(nonExistentPath, out var errorMessage);

        // Assert - Should pass validation (CLI will check existence)
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateProjectPath_WithInvalidExtension_ReturnsFalse()
    {
        // Arrange
        var invalidPath = "/path/to/project.txt";

        // Act
        var isValid = ParameterValidator.ValidateProjectPath(invalidPath, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Invalid project file extension", errorMessage);
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithNull_ReturnsFalse()
    {
        // Act
        var result = await ParameterValidator.ValidateTemplateAsync(null);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Template parameter is required", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = await ParameterValidator.ValidateTemplateAsync("");

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Template parameter is required", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithNonExistentTemplate_ReturnsFalse()
    {
        // Act
        var result = await ParameterValidator.ValidateTemplateAsync("nonexistent-template-xyz123");

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Template", result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.Contains("DotnetTemplateList", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFramework_ErrorMessage_ContainsExamples()
    {
        // Act
        ParameterValidator.ValidateFramework("invalid", out var errorMessage);

        // Assert
        Assert.NotNull(errorMessage);
        Assert.Contains("net10.0", errorMessage);
        Assert.Contains("net8.0", errorMessage);
        Assert.Contains("DotnetFrameworkInfo", errorMessage);
    }

    [Fact]
    public void ValidateRuntimeIdentifier_ErrorMessage_ContainsDocumentationLink()
    {
        // Act
        ParameterValidator.ValidateRuntimeIdentifier("invalid", out var errorMessage);

        // Assert
        Assert.NotNull(errorMessage);
        Assert.Contains("https://learn.microsoft.com", errorMessage);
        Assert.Contains("rid-catalog", errorMessage);
    }

    #region ValidateAction Tests

    // Test enum for action validation tests
    private enum TestAction
    {
        Create,
        Update,
        Delete,
        List
    }

    [Fact]
    public void ValidateAction_WithValidAction_ReturnsTrue()
    {
        // Arrange
        TestAction? action = TestAction.Create;

        // Act
        var isValid = ParameterValidator.ValidateAction(action, out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateAction_WithNullAction_ReturnsFalse()
    {
        // Arrange
        TestAction? action = null;

        // Act
        var isValid = ParameterValidator.ValidateAction(action, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Action parameter is required", errorMessage);
        Assert.Contains("Create", errorMessage);
        Assert.Contains("Update", errorMessage);
        Assert.Contains("Delete", errorMessage);
        Assert.Contains("List", errorMessage);
    }

    [Fact]
    public void ValidateAction_WithInvalidEnumValue_ReturnsFalse()
    {
        // Arrange - cast an invalid int to the enum
        TestAction? action = (TestAction)999;

        // Act
        var isValid = ParameterValidator.ValidateAction(action, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Invalid action", errorMessage);
        Assert.Contains("Valid actions:", errorMessage);
    }

    #endregion

    #region ValidateRequiredParameter Tests

    [Fact]
    public void ValidateRequiredParameter_String_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var value = "test-value";

        // Act
        var isValid = ParameterValidator.ValidateRequiredParameter(value, "testParam", out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequiredParameter_String_WithInvalidValue_ReturnsFalse(string? value)
    {
        // Act
        var isValid = ParameterValidator.ValidateRequiredParameter(value, "testParam", out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("testParam", errorMessage);
        Assert.Contains("required", errorMessage);
    }

    [Fact]
    public void ValidateRequiredParameter_Generic_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var value = new object();

        // Act
        var isValid = ParameterValidator.ValidateRequiredParameter(value, "testParam", out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateRequiredParameter_Generic_WithNull_ReturnsFalse()
    {
        // Arrange
        object? value = null;

        // Act
        var isValid = ParameterValidator.ValidateRequiredParameter(value, "testParam", out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("testParam", errorMessage);
        Assert.Contains("required", errorMessage);
    }

    #endregion
}
