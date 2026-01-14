namespace DotNetMcp.Actions;

/// <summary>
/// Test runner selection for dotnet test command.
/// Determines whether to use --project flag (MTP) or positional argument (VSTest).
/// </summary>
public enum TestRunner
{
    /// <summary>
    /// Auto-detect test runner based on global.json configuration.
    /// If global.json contains { "test": { "runner": "Microsoft.Testing.Platform" } }, uses MTP.
    /// Otherwise defaults to VSTest for safety with legacy projects.
    /// </summary>
    Auto,

    /// <summary>
    /// Use Microsoft Testing Platform (MTP) mode with --project flag.
    /// Requires .NET SDK 8+ with MTP enabled or SDK 10+.
    /// </summary>
    MicrosoftTestingPlatform,

    /// <summary>
    /// Use legacy VSTest mode with positional project argument.
    /// Compatible with all SDK versions.
    /// </summary>
    VSTest
}

/// <summary>
/// Start mode for long-running dotnet commands like run.
/// Determines whether the command blocks until completion or returns immediately with a session ID.
/// </summary>
public enum StartMode
{
    /// <summary>
    /// Run in foreground mode - blocks until the process exits (default behavior).
    /// </summary>
    Foreground,

    /// <summary>
    /// Run in background mode - starts the process and returns immediately with a session ID.
    /// Allows the process to be stopped later using dotnet_project Stop action.
    /// </summary>
    Background
}
