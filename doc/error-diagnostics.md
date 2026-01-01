# Enhanced Error Diagnostics

The .NET MCP Server provides enhanced error diagnostics that go beyond simple error messages. When build or test failures occur, the server parses error codes and provides:

- **Plain English explanations** of what the error means
- **Documentation links** to official Microsoft docs
- **Common causes** of the error
- **Suggested fixes** with specific commands to resolve the issue

## Supported Error Categories

The server recognizes and provides enhanced diagnostics for errors from:

- **C# Compiler** (CS#### codes) - 23 error codes
- **MSBuild** (MSB#### codes) - 12 error codes
- **NuGet** (NU#### codes) - 10 error codes
- **NET SDK** (NETSDK#### codes) - 7 error codes

Total: **52 common error codes** with detailed explanations.

## How It Works

When you use `machineReadable=true` with build or test commands, errors are automatically enriched with diagnostic information:

```bash
# Example: Build with machine-readable output
dotnet_project_build("MyProject.csproj", machineReadable=true)
```

### Example Output

#### Before Enhancement (Plain Error):
```json
{
  "success": false,
  "errors": [
    {
      "code": "CS0103",
      "message": "The name 'foo' does not exist in the current context",
      "category": "Compilation"
    }
  ]
}
```

#### After Enhancement (Rich Diagnostics):
```json
{
  "success": false,
  "errors": [
    {
      "code": "CS0103",
      "message": "The name 'foo' does not exist in the current context",
      "category": "Compilation",
      "explanation": "The compiler cannot find the specified identifier. This usually means you're trying to use a variable, method, or type that hasn't been defined or isn't accessible in the current scope.",
      "documentationUrl": "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103",
      "suggestedFixes": [
        "Check for typos in the identifier name",
        "Add the required 'using' directive at the top of the file",
        "Add a package reference using 'dotnet add package'",
        "Verify the identifier is declared in the current scope",
        "Check capitalization - C# is case-sensitive"
      ],
      "hint": "The name does not exist in the current context. Check for typos or missing using directives."
    }
  ]
}
```

## Common Error Code Examples

### CS0246: Type or namespace not found

**What it means:** The compiler can't find a type or namespace you're trying to use.

**Common causes:**
- Missing NuGet package reference
- Missing using directive
- Typo in type or namespace name

**Suggested fixes:**
```bash
# Add the missing package
dotnet add package <PackageName>

# Or restore packages
dotnet restore
```

**Documentation:** [CS0246 on Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0246)

---

### MSB3644: Reference assemblies not found

**What it means:** The .NET reference assemblies for the target framework aren't installed.

**Common causes:**
- Target framework not installed
- Missing .NET SDK or targeting pack
- Incorrect target framework specified

**Suggested fixes:**
```bash
# Install the .NET SDK for the target framework
# Download from: https://dotnet.microsoft.com/download

# Verify SDK installation
dotnet --info

# Or change target framework to an installed version
# Edit .csproj and change <TargetFramework>
```

**Documentation:** [MSB3644 on Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/msb3644)

---

### NU1101: Unable to find package

**What it means:** NuGet can't locate the specified package in any configured package source.

**Common causes:**
- Package name is misspelled
- Package doesn't exist on NuGet.org
- Network connectivity problem

**Suggested fixes:**
```bash
# Search for the package
dotnet package search <PackageName>

# Verify package exists on NuGet.org
# Check: https://www.nuget.org/packages/<PackageName>

# Check network and firewall settings
```

**Documentation:** [NU1101 on Microsoft Learn](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1101)

---

### NETSDK1045: Current SDK does not support target framework

**What it means:** The installed .NET SDK version is too old to support the target framework.

**Common causes:**
- SDK version is older than target framework
- Project targets newer framework than SDK supports

**Suggested fixes:**
```bash
# Install newer SDK
# Download from: https://dotnet.microsoft.com/download

# Or change target framework to supported version
# Edit .csproj <TargetFramework> to match installed SDK

# Check global.json for SDK version constraints
```

**Documentation:** [NETSDK1045 on Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1045)

---

### NETSDK1004: Assets file not found

**What it means:** The project.assets.json file is missing (needed for builds).

**Common causes:**
- Project not restored
- obj folder deleted
- Restore command failed

**Suggested fixes:**
```bash
# Restore NuGet packages
dotnet restore

# Or clean and restore
dotnet clean
dotnet restore
dotnet build
```

**Documentation:** [NETSDK1004 on Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1004)

## Pattern Detection (Future Enhancement)

The system currently parses individual error codes. Future enhancements will include pattern detection for:

- **Missing package references** - Detect when import statements have no corresponding package
- **Wrong target framework** - Identify framework compatibility issues
- **Missing SDK workload** - Detect MAUI, iOS, Android workload requirements
- **Version conflicts** - Identify package version conflicts
- **Platform-specific issues** - Detect platform-specific API usage

## Benefits for AI Assistants

AI assistants using the .NET MCP Server benefit from:

1. **Contextual Understanding** - Explanations help the AI understand what went wrong
2. **Actionable Suggestions** - Specific fix commands the AI can recommend or execute
3. **Learning** - Documentation links provide deeper context
4. **Better Troubleshooting** - Multiple suggested fixes increase problem-solving success
5. **Reduced Guesswork** - No need to hallucinate solutions - real fixes are provided

## API Usage

### From MCP Tools

All build and test tools support `machineReadable` parameter:

```python
# Python example using MCP
result = await mcp_client.call_tool(
    "dotnet_project_build",
    {
        "project": "MyApp.csproj",
        "machineReadable": True
    }
)

# Parse JSON response
import json
response = json.loads(result)

if not response["success"]:
    for error in response["errors"]:
        print(f"Error {error['code']}: {error['message']}")
        print(f"Explanation: {error['explanation']}")
        print(f"Suggested fixes:")
        for fix in error['suggestedFixes']:
            print(f"  - {fix}")
        print(f"Documentation: {error['documentationUrl']}")
```

### From Code

The `ErrorCodeDictionary` class can be used directly:

```csharp
using DotNetMcp;

// Look up error information
var errorInfo = ErrorCodeDictionary.GetErrorInfo("CS0103");
if (errorInfo != null)
{
    Console.WriteLine($"Title: {errorInfo.Title}");
    Console.WriteLine($"Explanation: {errorInfo.Explanation}");
    Console.WriteLine($"Documentation: {errorInfo.DocumentationUrl}");
    
    Console.WriteLine("\nSuggested fixes:");
    foreach (var fix in errorInfo.SuggestedFixes)
    {
        Console.WriteLine($"  - {fix}");
    }
}

// Check if error has detailed information
bool hasInfo = ErrorCodeDictionary.HasErrorInfo("MSB3644");

// Get total number of error codes in dictionary
int count = ErrorCodeDictionary.Count;
```

## Error Code Dictionary

The error code dictionary is loaded from an embedded JSON resource and cached for performance. It includes:

- **Title** - Short, clear description of the error
- **Explanation** - Detailed explanation of what causes this error
- **Category** - Error type (Compilation, Build, Package, SDK, Runtime)
- **Common Causes** - List of typical reasons this error occurs
- **Suggested Fixes** - Actionable steps to resolve the error
- **Documentation URL** - Link to official Microsoft documentation

## Customization

The error dictionary can be extended by:

1. **Adding new error codes** to `ErrorCodes.json`
2. **Updating explanations** to match your team's conventions
3. **Adding project-specific suggestions** for common patterns

## Testing

The error diagnostics system includes comprehensive tests:

- **26 tests** for error dictionary functionality
- **8 tests** for enhanced error parsing
- **100% test coverage** of error code lookup and parsing

Run tests with:
```bash
dotnet test --filter-class "*ErrorCodeDictionary*"
dotnet test --filter-class "*ErrorResultFactory*"
```

## Performance

- **Lazy loading** - Error dictionary loaded on first access
- **In-memory caching** - Dictionary cached for lifetime of application
- **Fast lookup** - O(1) dictionary lookups by error code
- **Minimal overhead** - Error enhancement adds <1ms to error processing
