using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class DotNetSdkConstantsTests
{
    [Fact]
    public void TargetFrameworks_ContainsModernNetVersions()
    {
        // Assert
        Assert.Equal("net9.0", DotNetSdkConstants.TargetFrameworks.Net90);
        Assert.Equal("net8.0", DotNetSdkConstants.TargetFrameworks.Net80);
        Assert.Equal("net7.0", DotNetSdkConstants.TargetFrameworks.Net70);
        Assert.Equal("net6.0", DotNetSdkConstants.TargetFrameworks.Net60);
        Assert.Equal("net5.0", DotNetSdkConstants.TargetFrameworks.Net50);
    }

    [Fact]
    public void TargetFrameworks_ContainsNetCoreVersions()
    {
        // Assert
        Assert.Equal("netcoreapp3.1", DotNetSdkConstants.TargetFrameworks.NetCoreApp31);
        Assert.Equal("netcoreapp2.1", DotNetSdkConstants.TargetFrameworks.NetCoreApp21);
    }

    [Fact]
    public void TargetFrameworks_ContainsNetStandardVersions()
    {
        // Assert
        Assert.Equal("netstandard2.1", DotNetSdkConstants.TargetFrameworks.NetStandard21);
        Assert.Equal("netstandard2.0", DotNetSdkConstants.TargetFrameworks.NetStandard20);
    }

    [Fact]
    public void Configurations_ContainsDebugAndRelease()
    {
        // Assert
        Assert.Equal("Debug", DotNetSdkConstants.Configurations.Debug);
        Assert.Equal("Release", DotNetSdkConstants.Configurations.Release);
    }

    [Fact]
    public void RuntimeIdentifiers_ContainsCommonPlatforms()
    {
        // Assert
        Assert.Equal("win-x64", DotNetSdkConstants.RuntimeIdentifiers.WinX64);
        Assert.Equal("linux-x64", DotNetSdkConstants.RuntimeIdentifiers.LinuxX64);
        Assert.Equal("osx-arm64", DotNetSdkConstants.RuntimeIdentifiers.OsxArm64);
    }

    [Fact]
    public void Templates_ContainsCommonTemplates()
    {
        // Assert
        Assert.Equal("console", DotNetSdkConstants.Templates.Console);
        Assert.Equal("webapi", DotNetSdkConstants.Templates.WebApi);
        Assert.Equal("blazor", DotNetSdkConstants.Templates.Blazor);
        Assert.Equal("xunit", DotNetSdkConstants.Templates.XUnit);
    }

    [Fact]
    public void CommonPackages_ContainsWellKnownPackages()
    {
        // Assert
        Assert.Equal("Newtonsoft.Json", DotNetSdkConstants.CommonPackages.NewtonsoftJson);
        Assert.Equal("Microsoft.EntityFrameworkCore", DotNetSdkConstants.CommonPackages.EFCore);
        Assert.Equal("xunit", DotNetSdkConstants.CommonPackages.XUnitCore);
    }

    [Fact]
    public void VerbosityLevels_ContainsAllLevels()
    {
        // Assert
        Assert.Equal("quiet", DotNetSdkConstants.VerbosityLevels.Quiet);
        Assert.Equal("minimal", DotNetSdkConstants.VerbosityLevels.Minimal);
        Assert.Equal("normal", DotNetSdkConstants.VerbosityLevels.Normal);
        Assert.Equal("detailed", DotNetSdkConstants.VerbosityLevels.Detailed);
        Assert.Equal("diagnostic", DotNetSdkConstants.VerbosityLevels.Diagnostic);
    }
}
