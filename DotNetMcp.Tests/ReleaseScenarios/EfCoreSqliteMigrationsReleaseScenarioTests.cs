using DotNetMcp.Tests.Scenarios;
using System.Text.Json;
using Xunit;
using Xunit.Sdk;

namespace DotNetMcp.Tests.ReleaseScenarios;

[Collection("ProcessWideStateTests")]
public class EfCoreSqliteMigrationsReleaseScenarioTests
{
    private static void AssertMachineReadableSuccessOrThrow(string jsonText, string stepName)
    {
        using var json = ScenarioHelpers.ParseJson(jsonText);

        if (!json.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
        {
            throw new XunitException($"Release scenario step failed: {stepName}\nResponse JSON:\n{jsonText}");
        }
    }

    [ReleaseScenarioFact]
    public async Task ReleaseScenario_EfCoreSqlite_MigrationsAdd_And_DatabaseUpdate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(ReleaseScenario_EfCoreSqlite_MigrationsAdd_And_DatabaseUpdate));

        var nugetPackagesDir = Path.Combine(tempRoot.Path, ".nuget", "packages");
        var dotnetCliHomeDir = Path.Combine(tempRoot.Path, ".dotnet", "home");
        Directory.CreateDirectory(nugetPackagesDir);
        Directory.CreateDirectory(dotnetCliHomeDir);

        var previousNugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        var previousDotnetCliHome = Environment.GetEnvironmentVariable("DOTNET_CLI_HOME");

        try
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", nugetPackagesDir);
            Environment.SetEnvironmentVariable("DOTNET_CLI_HOME", dotnetCliHomeDir);

            // Create a console project via CLI.
            var (exitCode, _, stderr) = await ScenarioHelpers.RunDotNetAsync(
                $"new console -n EfApp -o \"{tempRoot.Path}\"",
                workingDirectory: tempRoot.Path,
                cancellationToken);

            Assert.True(exitCode == 0, $"dotnet new console failed: {stderr}");

            var projectPath = Path.Combine(tempRoot.Path, "EfApp.csproj");
            Assert.True(File.Exists(projectPath), $"Expected EfApp.csproj to exist at {projectPath}");

            // Add EF Core packages via MCP.
            await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

            var addSqliteText = await client.CallToolTextAsync(
                toolName: "dotnet_package",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Add",
                    ["project"] = projectPath,
                    ["packageId"] = "Microsoft.EntityFrameworkCore.Sqlite",
                    ["source"] = "https://api.nuget.org/v3/index.json",
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(addSqliteText, "dotnet_package Add Microsoft.EntityFrameworkCore.Sqlite");

            var addDesignText = await client.CallToolTextAsync(
                toolName: "dotnet_package",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Add",
                    ["project"] = projectPath,
                    ["packageId"] = "Microsoft.EntityFrameworkCore.Design",
                    ["source"] = "https://api.nuget.org/v3/index.json",
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(addDesignText, "dotnet_package Add Microsoft.EntityFrameworkCore.Design");

            // Add a minimal DbContext and a design-time factory.
            var dbContextSource = string.Join(Environment.NewLine, new[]
            {
                "using Microsoft.EntityFrameworkCore;",
                "",
                "namespace EfApp;",
                "",
                "public sealed class AppDbContext : DbContext",
                "{",
                "    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }",
                "",
                "    public DbSet<TodoItem> TodoItems => Set<TodoItem>();",
                "",
                "    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)",
                "    {",
                "        if (!optionsBuilder.IsConfigured)",
                "        {",
                "            optionsBuilder.UseSqlite(\"Data Source=app.db\");",
                "        }",
                "    }",
                "}",
                "",
                "public sealed class TodoItem",
                "{",
                "    public int Id { get; set; }",
                "    public string Title { get; set; } = string.Empty;",
                "}",
                "",
            });

            File.WriteAllText(Path.Combine(tempRoot.Path, "AppDbContext.cs"), dbContextSource);

            var dbContextFactorySource = string.Join(Environment.NewLine, new[]
            {
                "using Microsoft.EntityFrameworkCore;",
                "using Microsoft.EntityFrameworkCore.Design;",
                "",
                "namespace EfApp;",
                "",
                "public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>",
                "{",
                "    public AppDbContext CreateDbContext(string[] args)",
                "    {",
                "        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();",
                "        optionsBuilder.UseSqlite(\"Data Source=app.db\");",
                "        return new AppDbContext(optionsBuilder.Options);",
                "    }",
                "}",
                "",
            });

            File.WriteAllText(Path.Combine(tempRoot.Path, "AppDbContextFactory.cs"), dbContextFactorySource);

            // Ensure dotnet-ef is available as a local tool.
            var createManifestText = await client.CallToolTextAsync(
                toolName: "dotnet_tool",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "CreateManifest",
                    ["output"] = tempRoot.Path,
                    ["workingDirectory"] = tempRoot.Path,
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(createManifestText, "dotnet_tool CreateManifest");

            var installEfToolText = await client.CallToolTextAsync(
                toolName: "dotnet_tool",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Install",
                    ["packageId"] = "dotnet-ef",
                    ["global"] = false,
                    ["workingDirectory"] = tempRoot.Path,
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(installEfToolText, "dotnet_tool Install dotnet-ef (local)");

            var restoreToolsText = await client.CallToolTextAsync(
                toolName: "dotnet_tool",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Restore",
                    ["workingDirectory"] = tempRoot.Path,
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(restoreToolsText, "dotnet_tool Restore");

            // Restore project (now that EF packages are present).
            var restoreProjectText = await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Restore",
                    ["project"] = projectPath,
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(restoreProjectText, "dotnet_project Restore");

            // Build once up-front so failures include full compiler diagnostics.
            var buildProjectText = await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Build",
                    ["project"] = projectPath,
                    ["configuration"] = "Release",
                    ["noRestore"] = true,
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(buildProjectText, "dotnet_project Build");

            // Add a migration and apply it.
            var migrationsAddText = await client.CallToolTextAsync(
                toolName: "dotnet_ef",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "MigrationsAdd",
                    ["name"] = "Init",
                    ["project"] = projectPath,
                    ["startupProject"] = projectPath,
                    ["noBuild"] = true,
                    ["workingDirectory"] = tempRoot.Path,
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(migrationsAddText, "dotnet_ef MigrationsAdd");

            var databaseUpdateText = await client.CallToolTextAsync(
                toolName: "dotnet_ef",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "DatabaseUpdate",
                    ["project"] = projectPath,
                    ["startupProject"] = projectPath,
                    ["noBuild"] = true,
                    ["workingDirectory"] = tempRoot.Path,
                    ["machineReadable"] = true
                },
                cancellationToken);

            AssertMachineReadableSuccessOrThrow(databaseUpdateText, "dotnet_ef DatabaseUpdate");

            Assert.True(Directory.Exists(Path.Combine(tempRoot.Path, "Migrations")), "Expected Migrations folder to be created.");
            Assert.True(File.Exists(Path.Combine(tempRoot.Path, "app.db")), "Expected SQLite database file 'app.db' to exist.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", previousNugetPackages);
            Environment.SetEnvironmentVariable("DOTNET_CLI_HOME", previousDotnetCliHome);
        }
    }
}
