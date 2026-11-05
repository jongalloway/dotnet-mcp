namespace DotNetMcp.Tests;

internal static class TestServerCommand
{
    internal const string ExecutablePathEnvironmentVariable = "DOTNET_MCP_TEST_SERVER_PATH";

    internal static (string command, string[] arguments) Get() =>
        Resolve(Environment.GetEnvironmentVariable(ExecutablePathEnvironmentVariable),
            AppContext.BaseDirectory, OperatingSystem.IsWindows());

    internal static (string command, string[] arguments) Resolve(
        string? executablePath, string testBinaryDirectory, bool isWindows)
    {
        if (executablePath is not null)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !Path.IsPathFullyQualified(executablePath))
            {
                throw new InvalidOperationException(
                    $"{ExecutablePathEnvironmentVariable} must be an absolute path to the server executable.");
            }

            if (!File.Exists(executablePath))
                throw new FileNotFoundException("Explicit test server executable not found.", executablePath);

            return (executablePath, []);
        }

        var binaryDirectory = testBinaryDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var configurationDirectory = Path.GetDirectoryName(binaryDirectory);
        if (string.IsNullOrEmpty(configurationDirectory))
            throw new InvalidOperationException($"Unexpected test binary directory: {testBinaryDirectory}");

        var configuration = Path.GetFileName(configurationDirectory);
        var framework = Path.GetFileName(binaryDirectory);
        var serverDirectory = Path.GetFullPath(Path.Join(testBinaryDirectory,
            "..", "..", "..", "..", "DotNetMcp", "bin", configuration, framework));

        if (!Directory.Exists(serverDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Server binary directory not found at: {serverDirectory}. Build DotNetMcp before running tests.");
        }

        var serverPath = Path.Join(serverDirectory, isWindows ? "DotNetMcp.exe" : "DotNetMcp.dll");
        if (!File.Exists(serverPath))
            throw new FileNotFoundException("Test server binary not found.", serverPath);

        return isWindows ? (serverPath, []) : ("dotnet", [serverPath]);
    }
}
