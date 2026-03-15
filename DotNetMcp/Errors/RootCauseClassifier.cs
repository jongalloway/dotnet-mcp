namespace DotNetMcp;

/// <summary>
/// Classifies .NET CLI errors into structured root causes and recommends next actions.
/// Focused on errors produced by dotnet SDK, MSBuild, and NuGet.
/// </summary>
internal static class RootCauseClassifier
{
    /// <summary>
    /// Classify an error based on its code, message, and context.
    /// Returns <see cref="RootCauseKind.Unknown"/> with no action when confidence is low.
    /// </summary>
    public static (RootCauseKind Kind, RecommendedAction? Action) Classify(
        string? errorCode, string? message, string? stderr, int? exitCode)
    {
        // 1. Try classification by error code prefix (highest confidence)
        if (!string.IsNullOrEmpty(errorCode))
        {
            var byCode = ClassifyByErrorCode(errorCode, message);
            if (byCode.Kind != RootCauseKind.Unknown)
                return byCode;
        }

        // 2. Try classification by stderr patterns
        var combinedText = CombineText(message, stderr);
        if (!string.IsNullOrEmpty(combinedText))
        {
            var byPattern = ClassifyByPattern(combinedText);
            if (byPattern.Kind != RootCauseKind.Unknown)
                return byPattern;
        }

        return (RootCauseKind.Unknown, null);
    }

    private static (RootCauseKind Kind, RecommendedAction? Action) ClassifyByErrorCode(
        string errorCode, string? message)
    {
        var upper = errorCode.ToUpperInvariant();

        // CS#### - C# compilation errors
        if (upper.StartsWith("CS", StringComparison.Ordinal) && upper.Length > 2 && char.IsDigit(upper[2]))
        {
            return (RootCauseKind.CompilationError, new RecommendedAction
            {
                ActionKind = ActionKind.ManualStep,
                Description = "Fix the compilation errors in source code, then rebuild."
            });
        }

        // MSB#### - MSBuild errors
        if (upper.StartsWith("MSB", StringComparison.Ordinal) && upper.Length > 3 && char.IsDigit(upper[3]))
        {
            // MSB1003 = project/solution not found
            if (upper == "MSB1003")
            {
                return (RootCauseKind.ProjectNotFound, new RecommendedAction
                {
                    ActionKind = ActionKind.ManualStep,
                    Description = "Verify the project or solution file path exists and is correct."
                });
            }

            // MSB4236 = SDK not found
            if (upper == "MSB4236")
            {
                return (RootCauseKind.MissingSdk, new RecommendedAction
                {
                    ActionKind = ActionKind.RunCommand,
                    Command = "dotnet --list-sdks",
                    Description = "List installed SDKs to verify the required SDK is available."
                });
            }

            return (RootCauseKind.BuildError, new RecommendedAction
            {
                ActionKind = ActionKind.ManualStep,
                Description = "Review the MSBuild error and fix the project configuration."
            });
        }

        // NU#### - NuGet errors
        if (upper.StartsWith("NU", StringComparison.Ordinal) && upper.Length > 2 && char.IsDigit(upper[2]))
        {
            // NU1101, NU1102, NU1103 = package not found / version not found
            if (upper is "NU1101" or "NU1102" or "NU1103")
            {
                return (RootCauseKind.PackageError, new RecommendedAction
                {
                    ActionKind = ActionKind.CallTool,
                    ToolName = "DotnetPackage",
                    ToolArgs = new Dictionary<string, string> { ["action"] = "Search" },
                    Description = "Search for the correct package name or available versions."
                });
            }

            // NU1301, NU1303 = restore / source failures
            if (upper is "NU1301" or "NU1303")
            {
                return (RootCauseKind.RestoreFailure, new RecommendedAction
                {
                    ActionKind = ActionKind.CallTool,
                    ToolName = "DotnetProject",
                    ToolArgs = new Dictionary<string, string> { ["action"] = "Restore" },
                    Description = "Retry the NuGet restore operation."
                });
            }

            return (RootCauseKind.PackageError, new RecommendedAction
            {
                ActionKind = ActionKind.CallTool,
                ToolName = "DotnetPackage",
                ToolArgs = new Dictionary<string, string> { ["action"] = "Search" },
                Description = "Check package name, version constraints, and NuGet sources."
            });
        }

        // NETSDK#### - .NET SDK errors
        if (upper.StartsWith("NETSDK", StringComparison.Ordinal) && upper.Length > 6 && char.IsDigit(upper[6]))
        {
            // NETSDK1004 = assets file not found -> restore needed
            if (upper == "NETSDK1004")
            {
                return (RootCauseKind.RestoreFailure, new RecommendedAction
                {
                    ActionKind = ActionKind.CallTool,
                    ToolName = "DotnetProject",
                    ToolArgs = new Dictionary<string, string> { ["action"] = "Restore" },
                    Description = "Run dotnet restore to generate the assets file."
                });
            }

            return (RootCauseKind.SdkError, new RecommendedAction
            {
                ActionKind = ActionKind.RunCommand,
                Command = "dotnet --list-sdks",
                Description = "Verify the installed SDK version supports the target framework."
            });
        }

        return (RootCauseKind.Unknown, null);
    }

    private static (RootCauseKind Kind, RecommendedAction? Action) ClassifyByPattern(string text)
    {
        // SDK not found / not installed
        if (ContainsAll(text, "sdk", "not") && ContainsAny(text, "found", "installed"))
        {
            return (RootCauseKind.MissingSdk, new RecommendedAction
            {
                ActionKind = ActionKind.RunCommand,
                Command = "dotnet --list-sdks",
                Description = "List installed SDKs to verify the required SDK is available."
            });
        }

        // Workload not installed
        if (Contains(text, "workload") && ContainsAny(text, "not installed", "is required", "missing"))
        {
            return (RootCauseKind.MissingWorkload, new RecommendedAction
            {
                ActionKind = ActionKind.CallTool,
                ToolName = "DotnetWorkload",
                ToolArgs = new Dictionary<string, string> { ["action"] = "Install" },
                Description = "Install the required workload."
            });
        }

        // Project / solution file not found
        if (ContainsAny(text, "could not find", "does not contain") &&
            ContainsAny(text, ".csproj", ".sln", ".slnx", "project", "solution file"))
        {
            return (RootCauseKind.ProjectNotFound, new RecommendedAction
            {
                ActionKind = ActionKind.ManualStep,
                Description = "Verify the project or solution file path exists and is correct."
            });
        }

        // Test failures
        if (ContainsAny(text, "test run failed", "tests failed", "no test available", "no test matches"))
        {
            return (RootCauseKind.TestFailure, new RecommendedAction
            {
                ActionKind = ActionKind.ManualStep,
                Description = "Review test failures and fix failing tests."
            });
        }

        // Restore failures
        if (ContainsAny(text, "unable to restore", "failed to restore", "restore failed"))
        {
            return (RootCauseKind.RestoreFailure, new RecommendedAction
            {
                ActionKind = ActionKind.CallTool,
                ToolName = "DotnetProject",
                ToolArgs = new Dictionary<string, string> { ["action"] = "Restore" },
                Description = "Retry the NuGet restore operation."
            });
        }

        // Template not found
        if (Contains(text, "template") && ContainsAny(text, "not found", "no templates", "could not find"))
        {
            return (RootCauseKind.TemplateNotFound, new RecommendedAction
            {
                ActionKind = ActionKind.CallTool,
                ToolName = "DotnetSdk",
                ToolArgs = new Dictionary<string, string> { ["action"] = "TemplateSearch" },
                Description = "Search for available templates."
            });
        }

        // Access denied / permission errors
        if (ContainsAny(text, "access denied", "access is denied", "permission denied", "unauthorized access"))
        {
            return (RootCauseKind.AccessDenied, new RecommendedAction
            {
                ActionKind = ActionKind.ManualStep,
                Description = "Check file and directory permissions, then retry the operation."
            });
        }

        // Timeout
        if (ContainsAny(text, "timed out", "operation timed out", "timeout expired"))
        {
            return (RootCauseKind.Timeout, new RecommendedAction
            {
                ActionKind = ActionKind.ManualStep,
                Description = "Retry the operation. If timeouts persist, check network connectivity."
            });
        }

        return (RootCauseKind.Unknown, null);
    }

    private static string? CombineText(string? message, string? stderr)
    {
        if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(stderr))
            return null;

        if (string.IsNullOrEmpty(stderr))
            return message;

        if (string.IsNullOrEmpty(message))
            return stderr;

        return $"{message}\n{stderr}";
    }

    private static bool Contains(string text, string value) =>
        text.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(string text, params string[] values)
    {
        foreach (var value in values)
        {
            if (text.Contains(value, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool ContainsAll(string text, params string[] values)
    {
        foreach (var value in values)
        {
            if (!text.Contains(value, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
