namespace DotNetMcp;

/// <summary>
/// Classifies the root cause of a .NET CLI error for programmatic handling.
/// Only includes causes directly related to dotnet SDK, MSBuild, and NuGet operations.
/// </summary>
public enum RootCauseKind
{
    /// <summary>Root cause could not be determined</summary>
    Unknown,

    /// <summary>C# compilation error (CS#### codes)</summary>
    CompilationError,

    /// <summary>MSBuild build error (MSB#### codes)</summary>
    BuildError,

    /// <summary>NuGet package not found or version conflict (NU#### codes)</summary>
    PackageError,

    /// <summary>Target framework not installed or invalid (NETSDK#### codes)</summary>
    SdkError,

    /// <summary>Required .NET SDK not installed</summary>
    MissingSdk,

    /// <summary>Required workload not installed</summary>
    MissingWorkload,

    /// <summary>Project or solution file not found</summary>
    ProjectNotFound,

    /// <summary>Invalid or missing command parameter</summary>
    InvalidParameter,

    /// <summary>Test execution failed (one or more tests failed)</summary>
    TestFailure,

    /// <summary>NuGet restore failed (network, auth, source issues)</summary>
    RestoreFailure,

    /// <summary>Template not found or not installed</summary>
    TemplateNotFound,

    /// <summary>Permission or access error during operation</summary>
    AccessDenied,

    /// <summary>Operation timed out</summary>
    Timeout
}
