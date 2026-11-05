namespace DotNetMcp.Tests;

public sealed class TestServerCommandTests : IDisposable
{
    private readonly string _directory = Path.Join(Path.GetTempPath(), $"dotnet-mcp-launch-{Guid.NewGuid():N}");

    public TestServerCommandTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Resolve_ExplicitExecutable_DoesNotRequireManagedBuildOutput()
    {
        var executable = Path.Join(_directory, "published server.exe");
        File.WriteAllBytes(executable, []);

        var result = TestServerCommand.Resolve(executable, Path.Join(_directory, "missing"), isWindows: true);

        Assert.Equal(executable, result.command);
        Assert.Empty(result.arguments);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-server.exe")]
    public void Resolve_InvalidExplicitPath_DoesNotFallBack(string path)
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => TestServerCommand.Resolve(path, AppContext.BaseDirectory, isWindows: true));

        Assert.Contains(TestServerCommand.ExecutablePathEnvironmentVariable, error.Message);
    }

    [Fact]
    public void Resolve_MissingExplicitExecutable_DoesNotFallBack()
    {
        var path = Path.Join(_directory, "missing.exe");

        var error = Assert.Throws<FileNotFoundException>(
            () => TestServerCommand.Resolve(path, AppContext.BaseDirectory, isWindows: true));

        Assert.Equal(path, error.FileName);
    }

    [Theory]
    [InlineData(true, "net10.0")]
    [InlineData(true, "net11.0")]
    [InlineData(false, "net10.0")]
    [InlineData(false, "net11.0")]
    public void Resolve_Default_MatchesTestConfigurationAndFramework(bool isWindows, string framework)
    {
        var testDirectory = Path.Join(_directory, "DotNetMcp.Tests", "bin", "Release", framework);
        var serverDirectory = Path.Join(_directory, "DotNetMcp", "bin", "Release", framework);
        Directory.CreateDirectory(testDirectory);
        Directory.CreateDirectory(serverDirectory);
        var serverPath = Path.Join(serverDirectory, isWindows ? "DotNetMcp.exe" : "DotNetMcp.dll");
        File.WriteAllBytes(serverPath, []);

        var result = TestServerCommand.Resolve(null, testDirectory + Path.DirectorySeparatorChar, isWindows);

        Assert.Equal(isWindows ? serverPath : "dotnet", result.command);
        Assert.Equal(isWindows ? [] : new[] { serverPath }, result.arguments);
    }

    [Fact]
    public void Resolve_MissingManagedOutput_Fails()
    {
        var testDirectory = Path.Join(_directory, "DotNetMcp.Tests", "bin", "Release", "net11.0");

        Assert.Throws<DirectoryNotFoundException>(
            () => TestServerCommand.Resolve(null, testDirectory, isWindows: true));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
