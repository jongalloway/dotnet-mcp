using DotNetMcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register ConcurrencyManager as a singleton
builder.Services.AddSingleton<ConcurrencyManager>();

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<DotNetCliTools>()
    .WithResources<DotNetResources>();

await builder.Build().RunAsync();
