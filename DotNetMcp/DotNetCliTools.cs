namespace DotNetMcp;

/// <summary>
/// Tool methods for .NET CLI operations. This is a partial class split across multiple files.
/// All tool methods are implemented in the Tools/ directory partial class files.
/// See Tools/Cli/DotNetCliTools.Core.cs for class infrastructure.
/// </summary>
public sealed partial class DotNetCliTools
{
    // This file serves as the main entry point for the DotNetCliTools class.
    // All functionality is implemented in partial class files in the Tools/ directory:
    //
    // Core infrastructure:
    //   - Tools/Cli/DotNetCliTools.Core.cs - Fields, constructor, helper methods
    //
    // Consolidated tool entrypoints are implemented under Tools/Cli and Tools/Sdk.
    // Helper methods used by consolidated tools live alongside them.
    //
    // Tools/Cli:
    //   - DotNetCliTools.Project.Consolidated.cs
    //   - DotNetCliTools.Package.Consolidated.cs
    //   - DotNetCliTools.Solution.cs
    //   - DotNetCliTools.Tool.Consolidated.cs
    //   - DotNetCliTools.Workload.Consolidated.cs
    //   - DotNetCliTools.DevCerts.Consolidated.cs
    //   - DotNetCliTools.EntityFramework.Consolidated.cs
    //   - DotNetCliTools.Misc.cs
    //
    // Tools/Sdk:
    //   - DotNetCliTools.Sdk.Consolidated.cs
}

