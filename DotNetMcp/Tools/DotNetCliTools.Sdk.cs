using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// SDK and runtime information tools.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Get information about .NET framework versions, including which are LTS releases. 
    /// Useful for understanding framework compatibility.
    /// </summary>
    /// <param name="framework">Optional: specific framework to get info about (e.g., 'net8.0', 'net6.0')</param>
    [McpServerTool]
    [McpMeta("category", "framework")]
    [McpMeta("usesFrameworkHelper", true)]
    public async partial Task<string> DotnetFrameworkInfo(string? framework = null)
    {
        var result = new StringBuilder();

        if (!string.IsNullOrEmpty(framework))
        {
            result.AppendLine($"Framework: {framework}");
            result.AppendLine($"Description: {FrameworkHelper.GetFrameworkDescription(framework)}");
            result.AppendLine($"Is LTS: {FrameworkHelper.IsLtsFramework(framework)}");
            result.AppendLine($"Is Modern .NET: {FrameworkHelper.IsModernNet(framework)}");
            result.AppendLine($"Is .NET Core: {FrameworkHelper.IsNetCore(framework)}");
            result.AppendLine($"Is .NET Framework: {FrameworkHelper.IsNetFramework(framework)}");
            result.AppendLine($"Is .NET Standard: {FrameworkHelper.IsNetStandard(framework)}");
        }
        else
        {
            var supportedModernFrameworks = FrameworkHelper.GetSupportedModernFrameworks().ToList();

            // Only show preview TFMs when the SDK major version is installed.
            try
            {
                var sdkList = await DotNetCommandExecutor.ExecuteCommandForResourceAsync("--list-sdks", _logger);
                var hasNet11Sdk = sdkList
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Any(line => line.TrimStart().StartsWith("11.", StringComparison.Ordinal));
                if (hasNet11Sdk)
                {
                    supportedModernFrameworks.Insert(0, DotNetSdkConstants.TargetFrameworks.Net110);
                }
            }
            catch
            {
                // If SDK discovery fails, fall back to stable list.
            }

            result.AppendLine("Modern .NET Frameworks (5.0+):");
            foreach (var fw in supportedModernFrameworks)
            {
                var ltsMarker = FrameworkHelper.IsLtsFramework(fw) ? " (LTS)" : string.Empty;
                result.AppendLine($"  {fw}{ltsMarker} - {FrameworkHelper.GetFrameworkDescription(fw)}");
            }

            result.AppendLine();
            result.AppendLine(".NET Core Frameworks:");
            foreach (var fw in FrameworkHelper.GetSupportedNetCoreFrameworks())
            {
                var ltsMarker = FrameworkHelper.IsLtsFramework(fw) ? " (LTS)" : string.Empty;
                result.AppendLine($"  {fw}{ltsMarker} - {FrameworkHelper.GetFrameworkDescription(fw)}");
            }

            result.AppendLine();
            result.AppendLine($"Latest Recommended: {FrameworkHelper.GetLatestRecommendedFramework()}");
            result.AppendLine($"Latest LTS: {FrameworkHelper.GetLatestLtsFramework()}");
        }

        return result.ToString();
    }

    /// <summary>
    /// Get information about installed .NET SDKs and runtimes.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSdkInfo(bool machineReadable = false)
        => await ExecuteDotNetCommand("--info", machineReadable);

    /// <summary>
    /// Get the version of the .NET SDK.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSdkVersion(bool machineReadable = false)
        => await ExecuteDotNetCommand("--version", machineReadable);

    /// <summary>
    /// List installed .NET SDKs.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSdkList(bool machineReadable = false)
        => await ExecuteDotNetCommand("--list-sdks", machineReadable);

    /// <summary>
    /// List installed .NET runtimes.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetRuntimeList(bool machineReadable = false)
        => await ExecuteDotNetCommand("--list-runtimes", machineReadable);
}
