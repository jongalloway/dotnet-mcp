using DotNetMcp;
using FluentAssertions;
using Xunit;

namespace DotNetMcp.Tests;

public class DotNetSdkConstantsTests
{
    [Fact]
    public void TargetFrameworks_ContainsModernNetVersions()
    {
        // Assert
        DotNetSdkConstants.TargetFrameworks.Net90.Should().Be("net9.0");
        DotNetSdkConstants.TargetFrameworks.Net80.Should().Be("net8.0");
        DotNetSdkConstants.TargetFrameworks.Net70.Should().Be("net7.0");
        DotNetSdkConstants.TargetFrameworks.Net60.Should().Be("net6.0");
        DotNetSdkConstants.TargetFrameworks.Net50.Should().Be("net5.0");
    }

    [Fact]
    public void TargetFrameworks_ContainsNetCoreVersions()
    {
        // Assert
        DotNetSdkConstants.TargetFrameworks.NetCoreApp31.Should().Be("netcoreapp3.1");
        DotNetSdkConstants.TargetFrameworks.NetCoreApp21.Should().Be("netcoreapp2.1");
    }

    [Fact]
    public void TargetFrameworks_ContainsNetStandardVersions()
    {
        // Assert
        DotNetSdkConstants.TargetFrameworks.NetStandard21.Should().Be("netstandard2.1");
        DotNetSdkConstants.TargetFrameworks.NetStandard20.Should().Be("netstandard2.0");
    }

    [Fact]
    public void Configurations_ContainsDebugAndRelease()
    {
        // Assert
        DotNetSdkConstants.Configurations.Debug.Should().Be("Debug");
        DotNetSdkConstants.Configurations.Release.Should().Be("Release");
    }

    [Fact]
    public void RuntimeIdentifiers_ContainsCommonPlatforms()
    {
        // Assert
        DotNetSdkConstants.RuntimeIdentifiers.WinX64.Should().Be("win-x64");
        DotNetSdkConstants.RuntimeIdentifiers.LinuxX64.Should().Be("linux-x64");
        DotNetSdkConstants.RuntimeIdentifiers.OsxArm64.Should().Be("osx-arm64");
    }

    [Fact]
    public void Templates_ContainsCommonTemplates()
    {
        // Assert
        DotNetSdkConstants.Templates.Console.Should().Be("console");
        DotNetSdkConstants.Templates.WebApi.Should().Be("webapi");
        DotNetSdkConstants.Templates.Blazor.Should().Be("blazor");
        DotNetSdkConstants.Templates.XUnit.Should().Be("xunit");
    }

    [Fact]
    public void CommonPackages_ContainsWellKnownPackages()
    {
        // Assert
        DotNetSdkConstants.CommonPackages.NewtonsoftJson.Should().Be("Newtonsoft.Json");
        DotNetSdkConstants.CommonPackages.EFCore.Should().Be("Microsoft.EntityFrameworkCore");
        DotNetSdkConstants.CommonPackages.XUnitCore.Should().Be("xunit");
    }

    [Fact]
    public void VerbosityLevels_ContainsAllLevels()
    {
        // Assert
        DotNetSdkConstants.VerbosityLevels.Quiet.Should().Be("quiet");
        DotNetSdkConstants.VerbosityLevels.Minimal.Should().Be("minimal");
        DotNetSdkConstants.VerbosityLevels.Normal.Should().Be("normal");
        DotNetSdkConstants.VerbosityLevels.Detailed.Should().Be("detailed");
        DotNetSdkConstants.VerbosityLevels.Diagnostic.Should().Be("diagnostic");
    }
}
