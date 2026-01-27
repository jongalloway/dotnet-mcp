using DotNetMcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register ConcurrencyManager as a singleton
builder.Services.AddSingleton<ConcurrencyManager>();

// Register ProcessSessionManager as a singleton
builder.Services.AddSingleton<ProcessSessionManager>();

builder.Services.AddMcpServer(options =>
{
    // Configure server implementation with .NET-themed icon
    options.ServerInfo = new Implementation
    {
        Name = "dotnet-mcp",
        Version = "1.0.0",
        Title = ".NET MCP Server",
        Description = "MCP server providing AI assistants with direct access to the .NET SDK through both official NuGet packages and CLI execution",
        WebsiteUrl = "https://github.com/jongalloway/dotnet-mcp",
        Icons =
        [
            new Icon
            {
                Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg",
                MimeType = "image/svg+xml",
                Sizes = ["any"],
                Theme = "light"
            },
            new Icon
            {
                Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/3D/gear_3d.png",
                MimeType = "image/png",
                Sizes = ["256x256"]
            }
        ]
    };
})
    .WithStdioServerTransport()
    .WithTools<DotNetCliTools>()
    .WithResources<DotNetResources>();

await builder.Build().RunAsync();
