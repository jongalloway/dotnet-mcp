using System.Diagnostics;

namespace DotNetMcp.Tests;

public sealed class OwnedProcessPolicyTests
{
    [Fact]
    public void OwnedProcessPolicy_OverridesOnlyChildBuildServerSettings()
    {
        var parent = Environment.GetEnvironmentVariable("DOTNET_CLI_USE_MSBUILD_SERVER");
        var startInfo = new ProcessStartInfo("dotnet");
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "true";
        startInfo.Environment["MSBUILDUSESERVER"] = "1";
        startInfo.Environment["KEEP_EXISTING_SETTING"] = "unchanged";

        DotNetCommandExecutor.ConfigureOwnedProcess(startInfo);

        Assert.Equal("false", startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"]);
        Assert.Equal("0", startInfo.Environment["MSBUILDUSESERVER"]);
        Assert.Equal("1", startInfo.Environment["MSBUILDDISABLENODEREUSE"]);
        Assert.Equal("0", startInfo.Environment["MSBUILDNOINPROCNODE"]);
        Assert.Equal("0", startInfo.Environment["DOTNET_CLI_USE_MSBUILDNOINPROCNODE"]);
        Assert.Equal("unchanged", startInfo.Environment["KEEP_EXISTING_SETTING"]);
        Assert.Equal(parent, Environment.GetEnvironmentVariable("DOTNET_CLI_USE_MSBUILD_SERVER"));
    }
}
