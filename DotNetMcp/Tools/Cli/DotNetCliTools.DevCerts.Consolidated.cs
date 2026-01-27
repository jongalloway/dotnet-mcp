using System.Text;
using DotNetMcp.Actions;
using ModelContextProtocol.Server;

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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool(IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Locked/Flat/locked_flat.svg")]
    [McpMeta("category", "security")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["CertificateTrust","CertificateCheck","CertificateClean","CertificateExport","SecretsInit","SecretsSet","SecretsList","SecretsRemove","SecretsClear"]""")]
    [McpMeta("tags", JsonValue = """["security","certificates","secrets","dev-certs","user-secrets","consolidated"]""")]
    public async partial Task<string> DotnetDevCerts(
        DotnetDevCertsAction action,
        string? project = null,
        string? path = null,
        string? password = null,
        string? format = null,
        bool trust = false,
        string? key = null,
        string? value = null,
        string? workingDirectory = null,
        bool machineReadable = false)
    {
        return await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action enum
            if (!ParameterValidator.ValidateAction<DotnetDevCertsAction>(action, out var actionError))
            {
                if (machineReadable)
                {
                    var validActions = Enum.GetNames(typeof(DotnetDevCertsAction));
                    var error = ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        validActions,
                        toolName: "dotnet_dev_certs");
                    return ErrorResultFactory.ToJson(error);
                }
                return $"Error: {actionError}";
            }

            // Route to appropriate action handler
            return action switch
            {
                DotnetDevCertsAction.CertificateTrust => await HandleCertificateTrustAction(machineReadable),
                DotnetDevCertsAction.CertificateCheck => await HandleCertificateCheckAction(trust, machineReadable),
                DotnetDevCertsAction.CertificateClean => await HandleCertificateCleanAction(machineReadable),
                DotnetDevCertsAction.CertificateExport => await HandleCertificateExportAction(path, password, format, machineReadable),
                DotnetDevCertsAction.SecretsInit => await HandleSecretsInitAction(project, machineReadable),
                DotnetDevCertsAction.SecretsSet => await HandleSecretsSetAction(key, value, project, machineReadable),
                DotnetDevCertsAction.SecretsList => await HandleSecretsListAction(project, machineReadable),
                DotnetDevCertsAction.SecretsRemove => await HandleSecretsRemoveAction(key, project, machineReadable),
                DotnetDevCertsAction.SecretsClear => await HandleSecretsClearAction(project, machineReadable),
                _ => machineReadable
                    ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        Enum.GetNames(typeof(DotnetDevCertsAction)),
                        toolName: "dotnet_dev_certs"))
                    : $"Error: Unsupported action '{action}'"
            };
        });
    }

    private async Task<string> HandleCertificateTrustAction(bool machineReadable)
    {
        return await ExecuteDotNetCommand("dev-certs https --trust", machineReadable);
    }

    private async Task<string> HandleCertificateCheckAction(bool trust, bool machineReadable)
    {
        var args = "dev-certs https --check";
        if (trust)
        {
            args += " --trust";
        }
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    private async Task<string> HandleCertificateCleanAction(bool machineReadable)
    {
        return await ExecuteDotNetCommand("dev-certs https --clean", machineReadable);
    }

    private async Task<string> HandleCertificateExportAction(
        string? path,
        string? password,
        string? format,
        bool machineReadable)
    {
        // Validate required path parameter
        if (string.IsNullOrWhiteSpace(path))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "path parameter is required for cert_export action.",
                    parameterName: "path",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: path parameter is required for cert_export action.";
        }

        // Validate and normalize format if provided
        string? normalizedFormat = null;
        if (!string.IsNullOrEmpty(format))
        {
            normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat != "pfx" && normalizedFormat != "pem")
            {
                if (machineReadable)
                {
                    var error = ErrorResultFactory.CreateValidationError(
                        "format must be either 'pfx' or 'pem' (case-insensitive).",
                        parameterName: "format",
                        reason: "invalid value");
                    return ErrorResultFactory.ToJson(error);
                }
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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    private async Task<string> HandleSecretsInitAction(string? project, bool machineReadable)
    {
        var args = new StringBuilder("user-secrets init");
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    private async Task<string> HandleSecretsSetAction(
        string? key,
        string? value,
        string? project,
        bool machineReadable)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(key))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "key parameter is required for secrets_set action.",
                    parameterName: "key",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: key parameter is required for secrets_set action.";
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "value parameter is required for secrets_set action.",
                    parameterName: "value",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    private async Task<string> HandleSecretsListAction(string? project, bool machineReadable)
    {
        var args = new StringBuilder("user-secrets list");
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    private async Task<string> HandleSecretsRemoveAction(
        string? key,
        string? project,
        bool machineReadable)
    {
        // Validate required key parameter
        if (string.IsNullOrWhiteSpace(key))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "key parameter is required for secrets_remove action.",
                    parameterName: "key",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: key parameter is required for secrets_remove action.";
        }

        var args = new StringBuilder($"user-secrets remove \"{key}\"");
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    private async Task<string> HandleSecretsClearAction(string? project, bool machineReadable)
    {
        var args = new StringBuilder("user-secrets clear");
        if (!string.IsNullOrEmpty(project))
            args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    // ===== Security helper methods (moved from DotNetCliTools.Security.cs) =====
    // Certificate Management

    /// <summary>
    /// Trust the HTTPS development certificate. Installs the certificate to the trusted root store.
    /// May require elevation on Windows/macOS. Essential for local ASP.NET Core HTTPS development.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    [McpMeta("requiresElevation", true)]
    internal async Task<string> DotnetCertificateTrust(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --trust", machineReadable);

    /// <summary>
    /// Check if the HTTPS development certificate exists and is trusted.
    /// Returns certificate status and validity information.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    internal async Task<string> DotnetCertificateCheck(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --check", machineReadable);

    /// <summary>
    /// Remove all HTTPS development certificates.
    /// Use this to clean up old or invalid certificates before creating new ones.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetCertificateClean(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --clean", machineReadable);

    /// <summary>
    /// Export the HTTPS development certificate to a file.
    /// Useful for Docker containers or sharing certificates across environments. Supports PFX and PEM formats with optional password protection.
    /// </summary>
    /// <param name="path">Path to export the certificate file</param>
    /// <param name="password">Certificate password for protection (optional, but recommended for PFX format)</param>
    /// <param name="format">Export format: Pfx or Pem (defaults to Pfx if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetCertificateExport(
        string path,
        string? password = null,
        string? format = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "path parameter is required.",
                    parameterName: "path",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: path parameter is required.";
        }

        // Validate and normalize format if provided
        string? normalizedFormat = null;
        if (!string.IsNullOrEmpty(format))
        {
            normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat != "pfx" && normalizedFormat != "pem")
            {
                if (machineReadable)
                {
                    var error = ErrorResultFactory.CreateValidationError(
                        "format must be either 'pfx' or 'pem' (case-insensitive).",
                        parameterName: "format",
                        reason: "invalid value");
                    return ErrorResultFactory.ToJson(error);
                }
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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    // User Secrets Management

    /// <summary>
    /// Initialize user secrets for a project.
    /// Creates a unique secrets ID and enables secret storage. This is the first step to using user secrets in your project.
    /// </summary>
    /// <param name="project">Project file to initialize secrets for (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 8.0)]
    internal async Task<string> DotnetSecretsInit(
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
    [McpMeta("category", "security")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    internal async Task<string> DotnetSecretsSet(
        string key,
        string value,
        string? project = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "key parameter is required.",
                    parameterName: "key",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: key parameter is required.";
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "value parameter is required.",
                    parameterName: "value",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
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
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    /// <summary>
    /// List all user secrets for a project. Displays secret keys and values.
    /// Useful for debugging configuration.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    internal async Task<string> DotnetSecretsList(
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
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetSecretsRemove(
        string key,
        string? project = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "key parameter is required.",
                    parameterName: "key",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: key parameter is required.";
        }

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
    [McpMeta("category", "security")]
    [McpMeta("priority", 5.0)]
    internal async Task<string> DotnetSecretsClear(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets clear");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }
}
