using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

/// <summary>
/// Result of a parameter validation operation.
/// </summary>
public readonly record struct ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Helper class for validating parameters before executing CLI commands.
/// Provides pre-CLI validation to catch errors early with better error messages.
/// </summary>
public static partial class ParameterValidator
{
    /// <summary>
    /// Source-generated regex for validating Runtime Identifiers (RIDs).
    /// Matches patterns like: win-x64, linux-x64, osx-arm64, win10-x64, linux-musl-x64
    /// </summary>
    [GeneratedRegex(@"^(win|linux|osx|android|ios|iossimulator)(10|11)?(-musl)?-(x64|x86|arm|arm64)$", RegexOptions.IgnoreCase)]
    private static partial Regex RuntimeIdentifierRegex();

    /// <summary>
    /// Validate a framework parameter against known Target Framework Monikers.
    /// </summary>
    /// <param name="framework">The framework string to validate</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid or null/empty, false otherwise</returns>
    public static bool ValidateFramework(string? framework, out string? errorMessage)
    {
        errorMessage = null;

        // Null or empty is valid (means use default)
        if (string.IsNullOrWhiteSpace(framework))
            return true;

        // Use FrameworkHelper to validate
        if (!FrameworkHelper.IsValidFramework(framework))
        {
            errorMessage = $"Invalid framework '{framework}'. Framework must start with 'net', 'netcoreapp', or 'netstandard'. " +
                          $"Examples: net10.0, net8.0, netcoreapp3.1, netstandard2.0. " +
                          $"Use DotnetFrameworkInfo to see available frameworks.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate a template parameter against installed templates.
    /// </summary>
    /// <param name="template">The template short name to validate</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>ValidationResult indicating success or failure with error message</returns>
    public static async Task<ValidationResult> ValidateTemplateAsync(string? template, ILogger? logger = null)
    {
        // Template is typically required for 'dotnet new' commands
        if (string.IsNullOrWhiteSpace(template))
        {
            return ValidationResult.Failure("Template parameter is required.");
        }

        // Use TemplateEngineHelper to validate if template exists
        if (!await TemplateEngineHelper.ValidateTemplateExistsAsync(template, forceReload: false, logger))
        {
            return ValidationResult.Failure($"Template '{template}' not found. Use DotnetTemplateList to see all available templates, " +
                          $"or DotnetTemplateSearch to search for templates.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validate a template package reference used by <c>dotnet new install</c>/<c>dotnet new uninstall</c>.
    /// Accepts either a NuGet package ID or a path (folder or .nupkg).
    /// </summary>
    public static bool ValidateTemplatePackage(string? templatePackage, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(templatePackage))
        {
            errorMessage = "templatePackage is required.";
            return false;
        }

        // Disallow control characters which can break argument parsing/logging.
        if (templatePackage.Any(char.IsControl))
        {
            errorMessage = "templatePackage contains control characters, which is not allowed.";
            return false;
        }

        // If the string looks like a path, validate that it exists.
        var looksLikePath = templatePackage.Contains('\\')
            || templatePackage.Contains('/')
            || templatePackage.StartsWith(".", StringComparison.Ordinal)
            || templatePackage.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase);

        if (looksLikePath)
        {
            if (File.Exists(templatePackage) || Directory.Exists(templatePackage))
            {
                return true;
            }

            errorMessage = $"Path not found: {templatePackage}. Provide an existing folder/.nupkg path, or a NuGet package ID.";
            return false;
        }

        // Otherwise treat as package ID (optionally with @version or ::version already appended).
        // Allow letters/digits plus '.', '-', '_', '@', and ':' (for <id>@<version> or legacy <id>::<version> syntax).
        if (templatePackage.Any(c => !(char.IsLetterOrDigit(c) || c is '.' or '-' or '_' or '@' or ':')))
        {
            errorMessage = $"Invalid templatePackage '{templatePackage}'. Package IDs may contain only letters, digits, '.', '-', '_' (and optionally '@' or '::' for version).";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate a template package version string (used as &lt;id&gt;@&lt;version&gt;).
    /// </summary>
    public static bool ValidateTemplatePackageVersion(string? templateVersion, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(templateVersion))
            return true;

        if (templateVersion.Any(char.IsControl))
        {
            errorMessage = "templateVersion contains control characters, which is not allowed.";
            return false;
        }

        // Keep this permissive (supports prerelease/build metadata) but disallow whitespace.
        if (templateVersion.Any(char.IsWhiteSpace))
        {
            errorMessage = "templateVersion must not contain whitespace.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate an optional NuGet source string for template installation.
    /// </summary>
    public static bool ValidateTemplateNugetSource(string? nugetSource, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(nugetSource))
            return true;

        if (nugetSource.Any(char.IsControl))
        {
            errorMessage = "nugetSource contains control characters, which is not allowed.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate a configuration parameter (Debug or Release).
    /// </summary>
    /// <param name="configuration">The configuration string to validate</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid or null/empty, false otherwise</returns>
    public static bool ValidateConfiguration(string? configuration, out string? errorMessage)
    {
        errorMessage = null;

        // Null or empty is valid (means use default)
        if (string.IsNullOrWhiteSpace(configuration))
            return true;

        // Case-insensitive check for Debug or Release
        if (!configuration.Equals(DotNetSdkConstants.Configurations.Debug, StringComparison.OrdinalIgnoreCase) &&
            !configuration.Equals(DotNetSdkConstants.Configurations.Release, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = $"Invalid configuration '{configuration}'. Configuration must be 'Debug' or 'Release'.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate that a file path exists.
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <param name="parameterName">The name of the parameter (for error message)</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid or null/empty, false otherwise</returns>
    public static bool ValidateFilePath(string? filePath, string parameterName, out string? errorMessage)
    {
        errorMessage = null;

        // Null or empty is valid (means use default/current directory)
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        // Check if file exists
        if (!File.Exists(filePath))
        {
            errorMessage = $"File not found: {filePath}. The {parameterName} parameter must point to an existing file.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate that a directory path exists.
    /// </summary>
    /// <param name="directoryPath">The directory path to validate</param>
    /// <param name="parameterName">The name of the parameter (for error message)</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid or null/empty, false otherwise</returns>
    public static bool ValidateDirectoryPath(string? directoryPath, string parameterName, out string? errorMessage)
    {
        errorMessage = null;

        // Null or empty is valid (means use default/current directory)
        if (string.IsNullOrWhiteSpace(directoryPath))
            return true;

        // Check if directory exists
        if (!Directory.Exists(directoryPath))
        {
            errorMessage = $"Directory not found: {directoryPath}. The {parameterName} parameter must point to an existing directory.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate a project file path (must be .csproj, .fsproj, .vbproj, or .sln if extension is provided).
    /// Does not check if file exists - the CLI will handle that.
    /// </summary>
    /// <param name="projectPath">The project file path to validate</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid or null/empty, false otherwise</returns>
    public static bool ValidateProjectPath(string? projectPath, out string? errorMessage)
    {
        errorMessage = null;

        // Null or empty is valid (means use default in current directory)
        if (string.IsNullOrWhiteSpace(projectPath))
            return true;

        // Check extension if the path has one
        var extension = Path.GetExtension(projectPath).ToLowerInvariant();
        if (!string.IsNullOrEmpty(extension) &&
            extension != ".csproj" && extension != ".fsproj" && extension != ".vbproj" && extension != ".sln" && extension != ".slnx")
        {
            errorMessage = $"Invalid project file extension: {projectPath}. Project files must have .csproj, .fsproj, .vbproj, .sln, or .slnx extension.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate a verbosity parameter.
    /// </summary>
    /// <param name="verbosity">The verbosity string to validate</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid or null/empty, false otherwise</returns>
    public static bool ValidateVerbosity(string? verbosity, out string? errorMessage)
    {
        errorMessage = null;

        // Null or empty is valid (means use default)
        if (string.IsNullOrWhiteSpace(verbosity))
            return true;

        // Valid verbosity levels
        var validLevels = new[] { "q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic" };

        if (!validLevels.Contains(verbosity.ToLowerInvariant()))
        {
            errorMessage = $"Invalid verbosity '{verbosity}'. Valid values are: q[uiet], m[inimal], n[ormal], d[etailed], diag[nostic].";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate a runtime identifier (RID).
    /// </summary>
    /// <param name="runtime">The runtime identifier to validate</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid or null/empty, false otherwise</returns>
    public static bool ValidateRuntimeIdentifier(string? runtime, out string? errorMessage)
    {
        errorMessage = null;

        // Null or empty is valid (means use default/framework-dependent)
        if (string.IsNullOrWhiteSpace(runtime))
            return true;

        // Check if it matches common RID patterns using source-generated regex
        // RIDs follow pattern: <os>-<arch> or <os>.<version>-<arch>
        // Examples: win-x64, linux-x64, osx-arm64, win10-x64
        if (!RuntimeIdentifierRegex().IsMatch(runtime))
        {
            errorMessage = $"Invalid runtime identifier '{runtime}'. " +
                          $"Runtime identifiers follow the pattern <os>-<arch>. " +
                          $"Examples: win-x64, linux-x64, osx-arm64, linux-musl-x64. " +
                          $"See https://learn.microsoft.com/en-us/dotnet/core/rid-catalog for more information.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate a workload ID.
    /// Workload IDs should only contain alphanumeric characters, hyphens, and underscores.
    /// </summary>
    /// <param name="workloadId">The workload ID to validate</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateWorkloadId(string? workloadId, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(workloadId))
        {
            errorMessage = "Workload ID cannot be null or empty.";
            return false;
        }

        // Use explicit Where to filter invalid characters
        var invalidChar = workloadId.Where(c => !(char.IsLetterOrDigit(c) || c is '-' or '_')).FirstOrDefault();
        if (invalidChar != default(char))
        {
            errorMessage = $"Invalid workload ID '{workloadId}'. Workload IDs must contain only alphanumeric characters, hyphens, and underscores.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parse and validate a comma-separated list of workload IDs.
    /// </summary>
    /// <param name="workloadIds">Comma-separated workload IDs</param>
    /// <param name="parsedIds">Output array of parsed and trimmed IDs</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if all IDs are valid, false otherwise</returns>
    public static bool ParseWorkloadIds(string workloadIds, out string[] parsedIds, out string? errorMessage)
    {
        errorMessage = null;
        parsedIds = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(workloadIds))
        {
            errorMessage = "At least one workload ID must be provided.";
            return false;
        }

        // Split and validate workload IDs
        var ids = workloadIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (ids.Length == 0)
        {
            errorMessage = "At least one workload ID must be provided.";
            return false;
        }

        // Use explicit Where to find first invalid ID
        var invalidId = ids.Where(id => !ValidateWorkloadId(id, out _)).FirstOrDefault();
        if (invalidId != null)
        {
            ValidateWorkloadId(invalidId, out errorMessage);
            return false;
        }

        parsedIds = ids;
        return true;
    }

    /// <summary>
    /// Validate that an action enum value is valid.
    /// </summary>
    /// <typeparam name="TAction">The enum type representing valid actions</typeparam>
    /// <param name="action">The action value to validate</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateAction<TAction>(TAction? action, out string? errorMessage) where TAction : struct, Enum
    {
        errorMessage = null;

        // Null is not valid for action enums
        if (action == null)
        {
            var validActions = string.Join(", ", Enum.GetNames(typeof(TAction)));
            errorMessage = $"Action parameter is required. Valid actions: {validActions}";
            return false;
        }

        // Check if the enum value is defined
        if (!Enum.IsDefined(typeof(TAction), action.Value))
        {
            var validActions = string.Join(", ", Enum.GetNames(typeof(TAction)));
            errorMessage = $"Invalid action '{action}' is not supported. Valid actions: {validActions}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate that a required parameter is present and not null/empty/whitespace.
    /// </summary>
    /// <param name="value">The parameter value to validate</param>
    /// <param name="parameterName">The name of the parameter (for error message)</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateRequiredParameter(string? value, string parameterName, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = $"The '{parameterName}' parameter is required and cannot be null or empty.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validate that a required parameter of any type is present and not null.
    /// </summary>
    /// <typeparam name="T">The type of the parameter</typeparam>
    /// <param name="value">The parameter value to validate</param>
    /// <param name="parameterName">The name of the parameter (for error message)</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateRequiredParameter<T>(T? value, string parameterName, out string? errorMessage) where T : class
    {
        errorMessage = null;

        if (value == null)
        {
            errorMessage = $"The '{parameterName}' parameter is required and cannot be null.";
            return false;
        }

        return true;
    }
}
