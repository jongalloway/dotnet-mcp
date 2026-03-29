using DotNetMcp;
using DotNetMcp.Actions;

namespace DotNetMcp.Tests;

/// <summary>
/// A temporary directory that is automatically unregistered from the .NET template engine
/// and deleted when disposed.
/// </summary>
/// <remarks>
/// Use this helper in tests that call
/// <see cref="DotNetCliTools.DotnetSdk"/> with <see cref="DotnetSdkAction.InstallTemplatePack"/>
/// so that the template-engine registration is cleaned up after the test.
/// Without an explicit uninstall step the template engine retains the path even after the
/// directory is deleted, which causes "Failed to scan &lt;path&gt;" warnings on every subsequent
/// <c>dotnet new</c> invocation.
/// </remarks>
internal sealed class TempTemplatePackDirectory : IAsyncDisposable
{
    private readonly DotNetCliTools _tools;

    private TempTemplatePackDirectory(string path, DotNetCliTools tools)
    {
        Path = path;
        _tools = tools;
    }

    /// <summary>Gets the path of the temporary directory.</summary>
    public string Path { get; }

    /// <summary>Implicitly converts the instance to its directory path.</summary>
    public static implicit operator string(TempTemplatePackDirectory d) => d.Path;

    /// <summary>
    /// Creates a temporary directory (without installing it as a template pack).
    /// The caller is responsible for calling install; this object handles uninstall and
    /// directory deletion on <see cref="DisposeAsync"/>.
    /// </summary>
    /// <param name="tools">The <see cref="DotNetCliTools"/> instance used for uninstall.</param>
    /// <param name="prefix">Optional directory-name prefix used to distinguish test contexts.</param>
    public static TempTemplatePackDirectory Create(DotNetCliTools tools, string prefix = "dotnet-mcp-template-pack-test")
    {
        var dir = System.IO.Path.Join(System.IO.Path.GetTempPath(), prefix, Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(dir);
        return new TempTemplatePackDirectory(dir, tools);
    }

    /// <summary>
    /// Uninstalls the template pack registration, then deletes the directory.
    /// Both operations are best-effort: failures are silently ignored.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _tools.DotnetSdk(
                action: DotnetSdkAction.UninstallTemplatePack,
                templatePackage: Path);
        }
        catch (Exception) { /* best-effort uninstall */ }

        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException) { /* best-effort cleanup */ }
        catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
    }
}
