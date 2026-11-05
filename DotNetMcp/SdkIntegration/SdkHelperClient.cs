#if NATIVEAOT
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

internal static class SdkHelperClient
{
    internal static async Task<SdkHelperResponse> InvokeAsync(SdkHelperRequest request, ILogger? logger = null)
    {
        var helper = Path.Join(AppContext.BaseDirectory, "sdk-helper", "DotNetMcp.SdkHelper.dll");
        if (!File.Exists(helper))
            throw new FileNotFoundException("The native server requires its published sdk-helper directory.", helper);
        var input = JsonSerializer.SerializeToUtf8Bytes(request, ServerJsonContext.Default.SdkHelperRequest);
        if (input.Length > SdkHelperProtocol.MaximumRequestBytes)
            throw new InvalidDataException("SDK helper requests must not exceed 1 MiB.");

        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Environment.CurrentDirectory
        };
        startInfo.ArgumentList.Add(helper);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            timeout.Token, DotNetCommandExecutor.RequestCancellationToken.Value);
        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException("Could not start the SDK metadata helper.");
        try
        {
            var stdout = SdkHelperProtocol.ReadMessageAsync(process.StandardOutput.BaseStream,
                SdkHelperProtocol.MaximumResponseBytes, cancellation.Token);
            var stderr = SdkHelperProtocol.ReadMessageAsync(process.StandardError.BaseStream,
                SdkHelperProtocol.MaximumRequestBytes, cancellation.Token);
            await process.StandardInput.BaseStream.WriteAsync(input, cancellation.Token);
            process.StandardInput.Close();
            await SdkHelperProtocol.WaitForProcessAndStreamsAsync(
                process.WaitForExitAsync(cancellation.Token), stdout, stderr);
            var output = await stdout;
            var errors = Encoding.UTF8.GetString(await stderr);
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"SDK metadata helper failed ({process.ExitCode}): {errors}");
            if (!string.IsNullOrWhiteSpace(errors))
                logger?.LogDebug("SDK metadata helper diagnostics: {Diagnostics}", errors);
            return JsonSerializer.Deserialize(output, ServerJsonContext.Default.SdkHelperResponse)
                ?? throw new InvalidDataException("SDK metadata helper returned an empty response.");
        }
        catch (OperationCanceledException error) when (timeout.IsCancellationRequested &&
            !DotNetCommandExecutor.RequestCancellationToken.Value.IsCancellationRequested)
        {
            throw new TimeoutException("SDK metadata helper exceeded its two-minute timeout.", error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    internal static async Task<string> ProjectAsync(SdkHelperRequest request, ILogger? logger) =>
        (await InvokeAsync(request, logger)).Json
        ?? throw new InvalidDataException("SDK metadata helper omitted its project result.");
}
#endif
