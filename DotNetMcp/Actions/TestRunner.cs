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
