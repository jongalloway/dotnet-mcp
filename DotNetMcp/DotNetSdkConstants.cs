namespace DotNetMcp;

/// <summary>
/// Well-known constants from the .NET SDK for use with CLI commands.
/// These constants provide type-safe access to common values used in dotnet commands.
/// </summary>
public static class DotNetSdkConstants
{
    /// <summary>
    /// Target Framework Monikers (TFMs) for .NET and .NET Core versions.
    /// </summary>
    public static class TargetFrameworks
    {
        // .NET (Modern)
        public const string Net90 = "net9.0";
        public const string Net80 = "net8.0";
        public const string Net70 = "net7.0";
        public const string Net60 = "net6.0";
        public const string Net50 = "net5.0";

        // .NET Core
        public const string NetCoreApp31 = "netcoreapp3.1";
        public const string NetCoreApp30 = "netcoreapp3.0";
        public const string NetCoreApp22 = "netcoreapp2.2";
        public const string NetCoreApp21 = "netcoreapp2.1";
        public const string NetCoreApp20 = "netcoreapp2.0";

        // .NET Standard
        public const string NetStandard21 = "netstandard2.1";
        public const string NetStandard20 = "netstandard2.0";
        public const string NetStandard16 = "netstandard1.6";
        public const string NetStandard15 = "netstandard1.5";
        public const string NetStandard14 = "netstandard1.4";
        public const string NetStandard13 = "netstandard1.3";
        public const string NetStandard12 = "netstandard1.2";
        public const string NetStandard11 = "netstandard1.1";
        public const string NetStandard10 = "netstandard1.0";

        // .NET Framework
        public const string Net481 = "net481";
        public const string Net48 = "net48";
        public const string Net472 = "net472";
        public const string Net471 = "net471";
        public const string Net47 = "net47";
        public const string Net462 = "net462";
        public const string Net461 = "net461";
        public const string Net46 = "net46";
        public const string Net452 = "net452";
        public const string Net451 = "net451";
        public const string Net45 = "net45";
        public const string Net40 = "net40";
        public const string Net35 = "net35";
        public const string Net20 = "net20";
    }

    /// <summary>
    /// Build configurations.
    /// </summary>
    public static class Configurations
    {
        public const string Debug = "Debug";
        public const string Release = "Release";
    }

    /// <summary>
    /// Runtime Identifiers (RIDs) for publishing self-contained applications.
    /// </summary>
    public static class RuntimeIdentifiers
    {
        // Windows
        public const string WinX64 = "win-x64";
        public const string WinX86 = "win-x86";
        public const string WinArm64 = "win-arm64";
        public const string Win10X64 = "win10-x64";
        public const string Win10X86 = "win10-x86";
        public const string Win10Arm64 = "win10-arm64";

        // Linux
        public const string LinuxX64 = "linux-x64";
        public const string LinuxArm = "linux-arm";
        public const string LinuxArm64 = "linux-arm64";
        public const string LinuxMuslX64 = "linux-musl-x64";
        public const string LinuxMuslArm64 = "linux-musl-arm64";

        // macOS
        public const string OsxX64 = "osx-x64";
        public const string OsxArm64 = "osx-arm64";

        // iOS
        public const string IosArm64 = "ios-arm64";
        public const string IosSimulatorX64 = "iossimulator-x64";
        public const string IosSimulatorArm64 = "iossimulator-arm64";

        // Android
        public const string AndroidArm64 = "android-arm64";
        public const string AndroidX64 = "android-x64";
    }

    /// <summary>
    /// Common template short names for dotnet new command.
    /// </summary>
    public static class Templates
    {
        // Console/Library
        public const string Console = "console";
        public const string ClassLib = "classlib";
        public const string Worker = "worker";

        // Web
        public const string Web = "web";
        public const string WebApi = "webapi";
        public const string Mvc = "mvc";
        public const string Webapp = "webapp";
        public const string Razor = "razor";
        public const string Angular = "angular";
        public const string React = "react";
        public const string Blazor = "blazor";
        public const string BlazorWasm = "blazorwasm";
        public const string BlazorServer = "blazorserver";

        // Testing
        public const string XUnit = "xunit";
        public const string NUnit = "nunit";
        public const string MsTest = "mstest";

        // Configuration
        public const string WebConfig = "webconfig";
        public const string GlobalJson = "globaljson";
        public const string NuGetConfig = "nugetconfig";
        public const string GitIgnore = "gitignore";
        public const string EditorConfig = "editorconfig";

        // gRPC
        public const string Grpc = "grpc";

        // Solution
        public const string Sln = "sln";
    }

    /// <summary>
    /// Common NuGet package names.
    /// </summary>
    public static class CommonPackages
    {
        // JSON
        public const string NewtonsoftJson = "Newtonsoft.Json";
        public const string SystemTextJson = "System.Text.Json";

        // HTTP
        public const string MicrosoftAspNetCoreHttp = "Microsoft.AspNetCore.Http";
        public const string SystemNetHttp = "System.Net.Http";

        // Entity Framework
        public const string EFCore = "Microsoft.EntityFrameworkCore";
        public const string EFCoreSqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        public const string EFCoreTools = "Microsoft.EntityFrameworkCore.Tools";

        // Testing
        public const string XUnitCore = "xunit";
        public const string XUnitRunner = "xunit.runner.visualstudio";
        public const string NUnit = "NUnit";
        public const string NUnitTestAdapter = "NUnit3TestAdapter";
        public const string MsTestFramework = "MSTest.TestFramework";
        public const string MsTestAdapter = "MSTest.TestAdapter";
        public const string Moq = "Moq";

        // Logging
        public const string Serilog = "Serilog";
        public const string SerilogAspNetCore = "Serilog.AspNetCore";
        public const string NLog = "NLog";

        // Configuration
        public const string MicrosoftExtensionsConfiguration = "Microsoft.Extensions.Configuration";
        public const string MicrosoftExtensionsConfigurationJson = "Microsoft.Extensions.Configuration.Json";
    }

    /// <summary>
    /// Language identifiers for multi-language templates.
    /// </summary>
    public static class Languages
    {
        public const string CSharp = "C#";
        public const string FSharp = "F#";
        public const string VisualBasic = "VB";
    }

    /// <summary>
    /// Verbosity levels for dotnet commands.
    /// </summary>
    public static class VerbosityLevels
    {
        public const string Quiet = "quiet";
        public const string Minimal = "minimal";
        public const string Normal = "normal";
        public const string Detailed = "detailed";
        public const string Diagnostic = "diagnostic";
    }
}
