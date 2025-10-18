using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static ModelContextProtocol.Protocol.ElicitRequestParams;

[McpServerToolType]
public sealed class DotNetCliTools
{
    [McpServerTool, Description("List all installed .NET templates with their metadata using the Template Engine. Provides structured information about available project templates.")]
    public async Task<string> DotnetTemplateList()
    {
        return await DotNetMcp.TemplateEngineHelper.GetInstalledTemplatesAsync();
    }

    [McpServerTool, Description("Search for .NET templates by name or description. Returns matching templates with their details.")]
    public async Task<string> DotnetTemplateSearch(
        [Description("Search term to find templates (searches in name, short name, and description)")]
        string searchTerm)
    {
        return await DotNetMcp.TemplateEngineHelper.SearchTemplatesAsync(searchTerm);
    }

    [McpServerTool, Description("Get detailed information about a specific template including available parameters and options.")]
    public async Task<string> DotnetTemplateInfo(
        [Description("The template short name (e.g., 'console', 'webapi', 'classlib')")]
        string templateShortName)
    {
        return await DotNetMcp.TemplateEngineHelper.GetTemplateDetailsAsync(templateShortName);
    }

    [McpServerTool, Description("Get information about .NET framework versions, including which are LTS releases. Useful for understanding framework compatibility.")]
    public Task<string> DotnetFrameworkInfo(
        [Description("Optional: specific framework to get info about (e.g., 'net8.0', 'net6.0')")]
        string? framework = null)
    {
        var result = new StringBuilder();
        
        if (!string.IsNullOrEmpty(framework))
        {
            // Info about specific framework
            result.AppendLine($"Framework: {framework}");
            result.AppendLine($"Description: {DotNetMcp.FrameworkHelper.GetFrameworkDescription(framework)}");
            result.AppendLine($"Is LTS: {DotNetMcp.FrameworkHelper.IsLtsFramework(framework)}");
            result.AppendLine($"Is Modern .NET: {DotNetMcp.FrameworkHelper.IsModernNet(framework)}");
            result.AppendLine($"Is .NET Core: {DotNetMcp.FrameworkHelper.IsNetCore(framework)}");
            result.AppendLine($"Is .NET Framework: {DotNetMcp.FrameworkHelper.IsNetFramework(framework)}");
            result.AppendLine($"Is .NET Standard: {DotNetMcp.FrameworkHelper.IsNetStandard(framework)}");
        }
        else
        {
            // General info
            result.AppendLine("Modern .NET Frameworks (5.0+):");
            foreach (var fw in DotNetMcp.FrameworkHelper.GetSupportedModernFrameworks())
            {
                var ltsMarker = DotNetMcp.FrameworkHelper.IsLtsFramework(fw) ? " (LTS)" : "";
                result.AppendLine($"  {fw}{ltsMarker} - {DotNetMcp.FrameworkHelper.GetFrameworkDescription(fw)}");
            }
            
            result.AppendLine();
            result.AppendLine(".NET Core Frameworks:");
            foreach (var fw in DotNetMcp.FrameworkHelper.GetSupportedNetCoreFrameworks())
            {
                var ltsMarker = DotNetMcp.FrameworkHelper.IsLtsFramework(fw) ? " (LTS)" : "";
                result.AppendLine($"  {fw}{ltsMarker} - {DotNetMcp.FrameworkHelper.GetFrameworkDescription(fw)}");
            }
            
            result.AppendLine();
            result.AppendLine($"Latest Recommended: {DotNetMcp.FrameworkHelper.GetLatestRecommendedFramework()}");
            result.AppendLine($"Latest LTS: {DotNetMcp.FrameworkHelper.GetLatestLtsFramework()}");
        }
        
        return Task.FromResult(result.ToString());
    }

    [McpServerTool, Description("Create a new .NET project or file from a template. Common templates: console, classlib, web, webapi, mvc, blazor, xunit, nunit, mstest. If template is not provided, will prompt the user interactively.")]
    public async Task<string> DotnetProjectNew(
        McpServer server,
        CancellationToken cancellationToken,
        [Description("The template to use (e.g., 'console', 'classlib', 'webapi'). If not provided, you will be prompted.")]
        string? template = null,
        [Description("The name for the project")]
        string? name = null,
        [Description("The output directory")]
        string? output = null,
        [Description("The target framework (e.g., 'net9.0', 'net8.0')")]
        string? framework = null)
    {
        // If template is not provided and client supports elicitation, ask for it
        if (string.IsNullOrEmpty(template) && server.ClientCapabilities?.Elicitation != null)
        {
            var templateSchema = new RequestSchema
            {
                Properties =
                {
                    ["template"] = new StringSchema()
                    {
                        Description = "The template to use (e.g., 'console', 'classlib', 'webapi', 'mvc', 'blazor', 'xunit', 'nunit', 'mstest')"
                    }
                }
            };

            var response = await server.ElicitAsync(new ElicitRequestParams
            {
                Message = "Which template would you like to use? Common templates: console, classlib, web, webapi, mvc, blazor, xunit, nunit, mstest",
                RequestedSchema = templateSchema
            }, cancellationToken);

            if (response.Action != "accept" || response.Content == null)
            {
                return "Template creation cancelled by user.";
            }

            // Safely extract the template value
            if (!response.Content.TryGetValue("template", out var templateValue))
            {
                return "No template provided. Template creation cancelled.";
            }

            template = templateValue.ValueKind == JsonValueKind.String ? templateValue.GetString() : null;
            
            if (string.IsNullOrEmpty(template))
            {
                return "No template provided. Template creation cancelled.";
            }
        }
        else if (string.IsNullOrEmpty(template))
        {
            // Client doesn't support elicitation, return error
            return "Error: Template parameter is required. Use dotnet_help with command='new' to see available templates.";
        }

        var args = new StringBuilder($"new {template}");
        
        if (!string.IsNullOrEmpty(name))
            args.Append($" -n \"{name}\"");
            
        if (!string.IsNullOrEmpty(output))
            args.Append($" -o \"{output}\"");
            
        if (!string.IsNullOrEmpty(framework))
            args.Append($" -f {framework}");
        
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Restore the dependencies and tools of a .NET project")]
    public async Task<string> DotnetProjectRestore(
        [Description("The project file or solution file to restore")]
        string? project = null)
    {
        var args = "restore";
        if (!string.IsNullOrEmpty(project))
            args += $" \"{project}\"";
            
        return await ExecuteDotNetCommand(args);
    }

    [McpServerTool, Description("Build a .NET project and its dependencies")]
    public async Task<string> DotnetProjectBuild(
        [Description("The project file or solution file to build")]
        string? project = null,
        [Description("The configuration to build (Debug or Release)")]
        string? configuration = null,
        [Description("Build for a specific framework")]
        string? framework = null)
    {
        var args = new StringBuilder("build");
        
        if (!string.IsNullOrEmpty(project))
            args.Append($" \"{project}\"");
            
        if (!string.IsNullOrEmpty(configuration))
            args.Append($" -c {configuration}");
            
        if (!string.IsNullOrEmpty(framework))
            args.Append($" -f {framework}");
        
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Build and run a .NET project")]
    public async Task<string> DotnetProjectRun(
        [Description("The project file to run")]
        string? project = null,
        [Description("The configuration to use (Debug or Release)")]
        string? configuration = null,
        [Description("Arguments to pass to the application")]
        string? appArgs = null)
    {
        var args = new StringBuilder("run");
        
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
            
        if (!string.IsNullOrEmpty(configuration))
            args.Append($" -c {configuration}");
            
        if (!string.IsNullOrEmpty(appArgs))
            args.Append($" -- {appArgs}");
        
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Run unit tests in a .NET project")]
    public async Task<string> DotnetProjectTest(
        [Description("The project file or solution file to test")]
        string? project = null,
        [Description("The configuration to test (Debug or Release)")]
        string? configuration = null,
        [Description("Filter to run specific tests")]
        string? filter = null)
    {
        var args = new StringBuilder("test");
        
        if (!string.IsNullOrEmpty(project))
            args.Append($" \"{project}\"");
            
        if (!string.IsNullOrEmpty(configuration))
            args.Append($" -c {configuration}");
            
        if (!string.IsNullOrEmpty(filter))
            args.Append($" --filter \"{filter}\"");
        
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Publish a .NET project for deployment")]
    public async Task<string> DotnetProjectPublish(
        [Description("The project file to publish")]
        string? project = null,
        [Description("The configuration to publish (Debug or Release)")]
        string? configuration = null,
        [Description("The output directory for published files")]
        string? output = null,
        [Description("The target runtime identifier (e.g., 'linux-x64', 'win-x64')")]
        string? runtime = null)
    {
        var args = new StringBuilder("publish");
        
        if (!string.IsNullOrEmpty(project))
            args.Append($" \"{project}\"");
            
        if (!string.IsNullOrEmpty(configuration))
            args.Append($" -c {configuration}");
            
        if (!string.IsNullOrEmpty(output))
            args.Append($" -o \"{output}\"");
            
        if (!string.IsNullOrEmpty(runtime))
            args.Append($" -r {runtime}");
        
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Clean the output of a .NET project")]
    public async Task<string> DotnetProjectClean(
        [Description("The project file or solution file to clean")]
        string? project = null,
        [Description("The configuration to clean (Debug or Release)")]
        string? configuration = null)
    {
        var args = new StringBuilder("clean");
        
        if (!string.IsNullOrEmpty(project))
            args.Append($" \"{project}\"");
            
        if (!string.IsNullOrEmpty(configuration))
            args.Append($" -c {configuration}");
        
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Add a NuGet package reference to a .NET project")]
    public async Task<string> DotnetPackageAdd(
        [Description("The name of the NuGet package to add")]
        string packageName,
        [Description("The project file to add the package to")]
        string? project = null,
        [Description("The version of the package")]
        string? version = null,
        [Description("Include prerelease packages")]
        bool prerelease = false)
    {
        var args = new StringBuilder("add");
        
        if (!string.IsNullOrEmpty(project))
            args.Append($" \"{project}\"");
            
        args.Append($" package {packageName}");
            
        if (!string.IsNullOrEmpty(version))
            args.Append($" --version {version}");
        else if (prerelease)
            args.Append(" --prerelease");
        
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("Add a project-to-project reference")]
    public async Task<string> DotnetReferenceAdd(
        [Description("The project file to add the reference from")]
        string project,
        [Description("The project file to reference")]
        string reference)
    {
        var args = $"add \"{project}\" reference \"{reference}\"";
        return await ExecuteDotNetCommand(args);
    }

    [McpServerTool, Description("List package references for a .NET project")]
    public async Task<string> DotnetPackageList(
        [Description("The project file or solution file")]
        string? project = null,
        [Description("Show outdated packages")]
        bool outdated = false,
        [Description("Show deprecated packages")]
        bool deprecated = false)
    {
        var args = new StringBuilder("list");
        
        if (!string.IsNullOrEmpty(project))
            args.Append($" \"{project}\"");
            
        args.Append(" package");
        
        if (outdated)
            args.Append(" --outdated");
            
        if (deprecated)
            args.Append(" --deprecated");
        
        return await ExecuteDotNetCommand(args.ToString());
    }

    [McpServerTool, Description("List project references")]
    public async Task<string> DotnetReferenceList(
        [Description("The project file")]
        string? project = null)
    {
        var args = "list";
        
        if (!string.IsNullOrEmpty(project))
            args += $" \"{project}\"";
            
        args += " reference";
        
        return await ExecuteDotNetCommand(args);
    }

    [McpServerTool, Description("Get information about installed .NET SDKs and runtimes")]
    public async Task<string> DotnetSdkInfo()
    {
        return await ExecuteDotNetCommand("--info");
    }

    [McpServerTool, Description("Get the version of the .NET SDK")]
    public async Task<string> DotnetSdkVersion()
    {
        return await ExecuteDotNetCommand("--version");
    }

    [McpServerTool, Description("List installed .NET SDKs")]
    public async Task<string> DotnetSdkList()
    {
        return await ExecuteDotNetCommand("--list-sdks");
    }

    [McpServerTool, Description("List installed .NET runtimes")]
    public async Task<string> DotnetRuntimeList()
    {
        return await ExecuteDotNetCommand("--list-runtimes");
    }

    [McpServerTool, Description("Get help for a specific dotnet command. Use this to discover available options for any dotnet command.")]
    public async Task<string> DotnetHelp(
        [Description("The dotnet command to get help for (e.g., 'build', 'new', 'run'). If not specified, shows general dotnet help.")]
        string? command = null)
    {
        var args = command != null ? $"{command} --help" : "--help";
        return await ExecuteDotNetCommand(args);
    }

    private async Task<string> ExecuteDotNetCommand(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        await process.WaitForExitAsync();

        var result = new StringBuilder();
        
        if (output.Length > 0)
            result.AppendLine(output.ToString());
            
        if (error.Length > 0)
        {
            result.AppendLine("Errors:");
            result.AppendLine(error.ToString());
        }

        result.AppendLine($"Exit Code: {process.ExitCode}");
        
        return result.ToString();
    }
}
