using System.Text;
using DotNetMcp.Actions;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;

namespace DotNetMcp;

/// <summary>
/// Consolidated .NET developer certificates and user secrets management tool.
/// Provides unified interface for certificate and secrets operations.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Manage developer certificates and user secrets for secure local development.
    /// Provides a consolidated interface for certificate trust/management and user secrets storage.
    /// </summary>
    /// <param name="action">The operation to perform</param>
    /// <param name="project">Path to project file (for secrets operations)</param>
    /// <param name="path">Export file path (for cert_export action)</param>
    /// <param name="password">Certificate password for protection (for cert_export action)</param>
    /// <param name="format">Export format: Pfx or Pem (for cert_export action)</param>
    /// <param name="trust">Also check if certificate is trusted (for cert_check action)</param>
    /// <param name="key">Secret key for set/remove operations (supports hierarchical keys like 'ConnectionStrings:Default')</param>
    /// <param name="value">Secret value for set operation</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    [McpServerTool(Title = "Dev Certs & User Secrets", Destructive = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Locked/Flat/locked_flat.svg")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["CertificateTrust","CertificateCheck","CertificateClean","CertificateExport","SecretsInit","SecretsSet","SecretsList","SecretsRemove","SecretsClear"]""")]
    [McpMeta("tags", JsonValue = """["security","certificates","secrets","dev-certs","user-secrets","consolidated"]""")]
    public async partial Task<CallToolResult> DotnetDevCerts(
        DotnetDevCertsAction action,
        string? project = null,
        string? path = null,
        string? password = null,
        string? format = null,
        bool trust = false,
        string? key = null,
        string? value = null,
        string? workingDirectory = null)
    {
        var textResult = await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action enum
            if (!ParameterValidator.ValidateAction<DotnetDevCertsAction>(action, out var actionError))
            {
                return $"Error: {actionError}";
            }

            // Route to appropriate action handler
            return action switch
            {
                DotnetDevCertsAction.CertificateTrust => await HandleCertificateTrustAction(),
                DotnetDevCertsAction.CertificateCheck => await HandleCertificateCheckAction(trust),
                DotnetDevCertsAction.CertificateClean => await HandleCertificateCleanAction(),
                DotnetDevCertsAction.CertificateExport => await HandleCertificateExportAction(path, password, format),
                DotnetDevCertsAction.SecretsInit => await HandleSecretsInitAction(project),
                DotnetDevCertsAction.SecretsSet => await HandleSecretsSetAction(key, value, project),
                DotnetDevCertsAction.SecretsList => await HandleSecretsListAction(project),
                DotnetDevCertsAction.SecretsRemove => await HandleSecretsRemoveAction(key, project),
                DotnetDevCertsAction.SecretsClear => await HandleSecretsClearAction(project),
                _ => $"Error: Unsupported action '{action}'"
            };
        });

        return StructuredContentHelper.ToCallToolResult(textResult);
    }

    private async Task<string> HandleCertificateTrustAction()
    {
        return await ExecuteDotNetCommand("dev-certs https --trust");
    }

    private async Task<string> HandleCertificateCheckAction(bool trust)
    {
        var args = "dev-certs https --check";
        if (trust)
        {
            args += " --trust";
        }
        return await ExecuteDotNetCommand(args);
    }

    private async Task<string> HandleCertificateCleanAction()
    {
        return await ExecuteDotNetCommand("dev-certs https --clean");
    }

    private async Task<string> HandleCertificateExportAction(
        string? path,
        string? password,
        string? format)
    {
        // Validate required path parameter
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Error: path parameter is required for cert_export action.";
        }

        // Validate and normalize format if provided
        string? normalizedFormat = null;
        if (!string.IsNullOrEmpty(format))
        {
            normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat != "pfx" && normalizedFormat != "pem")
            {
                return "Error: format must be either 'pfx' or 'pem' (case-insensitive).";
            }
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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, unsafeOutput: false);
    }

    private async Task<string> HandleSecretsInitAction(string? project)
    {
        var args = new StringBuilder("user-secrets init");
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }

    private async Task<string> HandleSecretsSetAction(
        string? key,
        string? value,
        string? project)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(key))
        {
            return "Error: key parameter is required for secrets_set action.";
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return "Error: value parameter is required for secrets_set action.";
        }

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
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");

        // Pass logger: null to prevent DotNetCommandExecutor from logging the secret value
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, unsafeOutput: false);
    }

    private async Task<string> HandleSecretsListAction(string? project)
    {
        var args = new StringBuilder("user-secrets list");
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }

    private async Task<string> HandleSecretsRemoveAction(
        string? key,
        string? project)
    {
        // Validate required key parameter
        if (string.IsNullOrWhiteSpace(key))
        {
            return "Error: key parameter is required for secrets_remove action.";
        }

        var args = new StringBuilder($"user-secrets remove \"{key}\"");
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }

    private async Task<string> HandleSecretsClearAction(string? project)
    {
        var args = new StringBuilder("user-secrets clear");
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }

    // ===== Security helper methods (moved from DotNetCliTools.Security.cs) =====
    // Certificate Management

    /// <summary>
    /// Trust the HTTPS development certificate. Installs the certificate to the trusted root store.
    /// May require elevation on Windows/macOS. Essential for local ASP.NET Core HTTPS development.
    /// </summary>
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    [McpMeta("requiresElevation", true)]
    internal async Task<string> DotnetCertificateTrust()
        => await ExecuteDotNetCommand("dev-certs https --trust");

    /// <summary>
    /// Check if the HTTPS development certificate exists and is trusted.
    /// Returns certificate status and validity information.
    /// </summary>
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    internal async Task<string> DotnetCertificateCheck()
        => await ExecuteDotNetCommand("dev-certs https --check");

    /// <summary>
    /// Remove all HTTPS development certificates.
    /// Use this to clean up old or invalid certificates before creating new ones.
    /// </summary>
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetCertificateClean()
        => await ExecuteDotNetCommand("dev-certs https --clean");

    /// <summary>
    /// Export the HTTPS development certificate to a file.
    /// Useful for Docker containers or sharing certificates across environments. Supports PFX and PEM formats with optional password protection.
    /// </summary>
    /// <param name="path">Path to export the certificate file</param>
    /// <param name="password">Certificate password for protection (optional, but recommended for PFX format)</param>
    /// <param name="format">Export format: Pfx or Pem (defaults to Pfx if not specified)</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetCertificateExport(
        string path,
        string? password = null,
        string? format = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Error: path parameter is required.";
        }

        // Validate and normalize format if provided
        string? normalizedFormat = null;
        if (!string.IsNullOrEmpty(format))
        {
            normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat != "pfx" && normalizedFormat != "pem")
            {
                return "Error: format must be either 'pfx' or 'pem' (case-insensitive).";
            }
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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, unsafeOutput: false);
    }

    // User Secrets Management

    /// <summary>
    /// Initialize user secrets for a project.
    /// Creates a unique secrets ID and enables secret storage. This is the first step to using user secrets in your project.
    /// </summary>
    /// <param name="project">Project file to initialize secrets for (optional; uses current directory if not specified)</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 8.0)]
    internal async Task<string> DotnetSecretsInit(
        string? project = null)
    {
        var args = new StringBuilder("user-secrets init");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Set a user secret value.
    /// Stores sensitive configuration outside of the project. Supports hierarchical keys (e.g., 'ConnectionStrings:DefaultConnection').
    /// DEVELOPMENT ONLY - not for production deployment.
    /// </summary>
    /// <param name="key">Secret key (supports hierarchical keys like 'ConnectionStrings:DefaultConnection')</param>
    /// <param name="value">Secret value (will not be logged for security)</param>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    internal async Task<string> DotnetSecretsSet(
        string key,
        string value,
        string? project = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "Error: key parameter is required.";
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return "Error: value parameter is required.";
        }

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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, unsafeOutput: false);
    }

    /// <summary>
    /// List all user secrets for a project. Displays secret keys and values.
    /// Useful for debugging configuration.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    internal async Task<string> DotnetSecretsList(
        string? project = null)
    {
        var args = new StringBuilder("user-secrets list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Remove a specific user secret by key. Deletes the secret from local storage.
    /// </summary>
    /// <param name="key">Secret key to remove</param>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetSecretsRemove(
        string key,
        string? project = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "Error: key parameter is required.";
        }

        var args = new StringBuilder($"user-secrets remove \"{key}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Clear all user secrets for a project. Removes all stored secrets.
    /// Use this for a fresh start when debugging configuration issues.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 5.0)]
    internal async Task<string> DotnetSecretsClear(
        string? project = null)
    {
        var args = new StringBuilder("user-secrets clear");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString());
    }
}
