using System.Text;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Security tools for certificates and user secrets management.
/// </summary>
public sealed partial class DotNetCliTools
{
    // Certificate Management

    /// <summary>
    /// Trust the HTTPS development certificate. Installs the certificate to the trusted root store.
    /// May require elevation on Windows/macOS. Essential for local ASP.NET Core HTTPS development.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    [McpMeta("requiresElevation", true)]
    public async partial Task<string> DotnetCertificateTrust(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --trust", machineReadable);

    /// <summary>
    /// Check if the HTTPS development certificate exists and is trusted.
    /// Returns certificate status and validity information.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetCertificateCheck(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --check", machineReadable);

    /// <summary>
    /// Remove all HTTPS development certificates.
    /// Use this to clean up old or invalid certificates before creating new ones.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetCertificateClean(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --clean", machineReadable);

    /// <summary>
    /// Export the HTTPS development certificate to a file.
    /// Useful for Docker containers or sharing certificates across environments. Supports PFX and PEM formats with optional password protection.
    /// </summary>
    /// <param name="path">Path to export the certificate file</param>
    /// <param name="password">Certificate password for protection (optional, but recommended for PFX format)</param>
    /// <param name="format">Export format: Pfx or Pem (defaults to Pfx if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetCertificateExport(
        string path,
        string? password = null,
        string? format = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Error: path parameter is required.";

        // Validate and normalize format if provided
        string? normalizedFormat = null;
        if (!string.IsNullOrEmpty(format))
        {
            normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat != "pfx" && normalizedFormat != "pem")
                return "Error: format must be either 'pfx' or 'pem' (case-insensitive).";
        }

        // Security Note: The password must be passed as a command-line argument to dotnet dev-certs,
        // which is the standard .NET CLI behavior. While this stores the password temporarily in memory
        // (CodeQL alert cs/cleartext-storage-of-sensitive-information), this is:
        // 1. Required by the .NET CLI interface - there's no alternative secure input method
        // 2. Mitigated by passing logger: null below, which prevents logging of the password
        // 3. Not persisted to disk or stored long-term
        // 4. Consistent with how developers manually use the dotnet dev-certs command
        var args = new StringBuilder("dev-certs https");
        args.Append($" --export-path \"{path}\"");

        if (!string.IsNullOrEmpty(normalizedFormat))
            args.Append($" --format {normalizedFormat}");

        if (!string.IsNullOrEmpty(password))
            args.Append($" --password \"{password}\"");

        // Pass logger: null to prevent DotNetCommandExecutor from logging the password
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    // User Secrets Management

    /// <summary>
    /// Initialize user secrets for a project.
    /// Creates a unique secrets ID and enables secret storage. This is the first step to using user secrets in your project.
    /// </summary>
    /// <param name="project">Project file to initialize secrets for (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 8.0)]
    public async partial Task<string> DotnetSecretsInit(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets init");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Set a user secret value.
    /// Stores sensitive configuration outside of the project. Supports hierarchical keys (e.g., 'ConnectionStrings:DefaultConnection').
    /// DEVELOPMENT ONLY - not for production deployment.
    /// </summary>
    /// <param name="key">Secret key (supports hierarchical keys like 'ConnectionStrings:DefaultConnection')</param>
    /// <param name="value">Secret value (will not be logged for security)</param>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    public async partial Task<string> DotnetSecretsSet(
        string key,
        string value,
        string? project = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Error: key parameter is required.";

        if (string.IsNullOrWhiteSpace(value))
            return "Error: value parameter is required.";

        // Security Note: The secret value must be passed as a command-line argument to dotnet user-secrets,
        // which is the standard .NET CLI behavior. While this stores the value temporarily in memory
        // (similar to dev-certs password handling), this is:
        // 1. Required by the .NET CLI interface - there's no alternative secure input method
        // 2. Mitigated by passing logger: null below, which prevents logging of the secret value
        // 3. Not persisted to disk in logs or command history by our code
        // 4. Consistent with how developers manually use the dotnet user-secrets command
        // 5. User secrets are ONLY for development, never for production deployment
        var args = new StringBuilder("user-secrets set");
        args.Append($" \"{key}\" \"{value}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");

        // Pass logger: null to prevent DotNetCommandExecutor from logging the secret value
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    /// <summary>
    /// List all user secrets for a project. Displays secret keys and values.
    /// Useful for debugging configuration.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetSecretsList(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Remove a specific user secret by key. Deletes the secret from local storage.
    /// </summary>
    /// <param name="key">Secret key to remove</param>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSecretsRemove(
        string key,
        string? project = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Error: key parameter is required.";

        var args = new StringBuilder($"user-secrets remove \"{key}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Clear all user secrets for a project. Removes all stored secrets.
    /// Use this for a fresh start when debugging configuration issues.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetSecretsClear(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets clear");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }
}
