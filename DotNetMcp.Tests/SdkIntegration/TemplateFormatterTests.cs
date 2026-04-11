namespace DotNetMcp.Tests;

/// <summary>
/// Deterministic unit tests for <see cref="TemplateFormatter"/> and the
/// <see cref="TemplateDisplayInfo"/> / <see cref="TemplateParameterDisplayInfo"/> DTOs.
///
/// These tests require no Template Engine state, no I/O, and no async operations —
/// all inputs are plain records constructed inline.
/// </summary>
public class TemplateFormatterTests
{
    // ---------------------------------------------------------------------------
    // FormatInstalledTemplates
    // ---------------------------------------------------------------------------

    [Fact]
    public void FormatInstalledTemplates_WithSingleTemplate_ContainsExpectedColumns()
    {
        var templates = new[]
        {
            MakeTemplate("console", "C#", "project", "A console application")
        };

        var result = TemplateFormatter.FormatInstalledTemplates(templates);

        Assert.Contains("Installed .NET Templates:", result);
        Assert.Contains("Short Name", result);
        Assert.Contains("Language", result);
        Assert.Contains("Type", result);
        Assert.Contains("Description", result);
        Assert.Contains("console", result);
        Assert.Contains("C#", result);
        Assert.Contains("project", result);
        Assert.Contains("A console application", result);
    }

    [Fact]
    public void FormatInstalledTemplates_TemplateCountLine_MatchesNumberOfTemplates()
    {
        var templates = new[]
        {
            MakeTemplate("console", "C#", "project", "Console application"),
            MakeTemplate("classlib", "C#", "library", "Class library"),
            MakeTemplate("webapi",   "C#", "project", "Web API"),
        };

        var result = TemplateFormatter.FormatInstalledTemplates(templates);

        Assert.Contains("Total templates: 3", result);
    }

    [Fact]
    public void FormatInstalledTemplates_TemplatesAreSortedByShortName()
    {
        var templates = new[]
        {
            MakeTemplate("webapi",  "C#", "project", "Web API"),
            MakeTemplate("console", "C#", "project", "Console application"),
            MakeTemplate("classlib","C#", "library", "Class library"),
        };

        var result = TemplateFormatter.FormatInstalledTemplates(templates);

        var classLibPos = result.IndexOf("classlib", StringComparison.Ordinal);
        var consolePos  = result.IndexOf("console",  StringComparison.Ordinal);
        var webapiPos   = result.IndexOf("webapi",   StringComparison.Ordinal);

        Assert.True(classLibPos < consolePos, "classlib should appear before console");
        Assert.True(consolePos  < webapiPos,  "console should appear before webapi");
    }

    [Fact]
    public void FormatInstalledTemplates_LongDescription_IsTruncatedToFortyChars()
    {
        var longDescription = new string('A', 50); // 50 chars, exceeds 40-char limit
        var templates = new[]
        {
            MakeTemplate("console", "C#", "project", longDescription)
        };

        var result = TemplateFormatter.FormatInstalledTemplates(templates);

        // Description should be truncated at 37 chars + "..."
        var truncated = new string('A', 37) + "...";
        Assert.Contains(truncated, result);
        // Full 50-char description should not appear
        Assert.DoesNotContain(longDescription, result);
    }

    [Fact]
    public void FormatInstalledTemplates_ExactlyFortyCharDescription_IsNotTruncated()
    {
        var description = new string('B', 40); // exactly 40 chars
        var templates = new[]
        {
            MakeTemplate("console", "C#", "project", description)
        };

        var result = TemplateFormatter.FormatInstalledTemplates(templates);

        Assert.Contains(description, result);
        Assert.DoesNotContain("...", result);
    }

    [Fact]
    public void FormatInstalledTemplates_NullDescription_ShowsEmptyCell()
    {
        var templates = new[]
        {
            MakeTemplate("console", "C#", "project", null)
        };

        var result = TemplateFormatter.FormatInstalledTemplates(templates);

        // Should not throw and should contain the template entry
        Assert.Contains("console", result);
    }

    [Fact]
    public void FormatInstalledTemplates_EmptyCollection_ShowsHeaderAndZeroCount()
    {
        var result = TemplateFormatter.FormatInstalledTemplates([]);

        Assert.Contains("Installed .NET Templates:", result);
        Assert.Contains("Total templates: 0", result);
    }

    // ---------------------------------------------------------------------------
    // FormatTemplateDetails
    // ---------------------------------------------------------------------------

    [Fact]
    public void FormatTemplateDetails_BasicFields_AreAllPresent()
    {
        var template = MakeTemplateInfo(
            name: "Console App",
            shortNames: ["console"],
            language: "C#",
            type: "project",
            description: "A simple console application",
            author: "Microsoft");

        var result = TemplateFormatter.FormatTemplateDetails(template);

        Assert.Contains("Template: Console App", result);
        Assert.Contains("Short Name(s): console", result);
        Assert.Contains("Author: Microsoft", result);
        Assert.Contains("Language: C#", result);
        Assert.Contains("Type: project", result);
        Assert.Contains("Description: A simple console application", result);
    }

    [Fact]
    public void FormatTemplateDetails_MultipleShortNames_AreJoinedWithComma()
    {
        var template = MakeTemplateInfo(
            name: "Console App",
            shortNames: ["console", "con"],
            language: "C#",
            type: "project",
            description: "Console app",
            author: "Microsoft");

        var result = TemplateFormatter.FormatTemplateDetails(template);

        Assert.Contains("Short Name(s): console, con", result);
    }

    [Fact]
    public void FormatTemplateDetails_NullAuthor_ShowsNotAvailable()
    {
        var template = MakeTemplateInfo(
            name: "Console App",
            shortNames: ["console"],
            language: "C#",
            type: "project",
            description: "Console app",
            author: null);

        var result = TemplateFormatter.FormatTemplateDetails(template);

        Assert.Contains("Author: N/A", result);
    }

    [Fact]
    public void FormatTemplateDetails_NullDescription_ShowsNotAvailable()
    {
        var template = MakeTemplateInfo(
            name: "Console App",
            shortNames: ["console"],
            language: "C#",
            type: "project",
            description: null,
            author: "Microsoft");

        var result = TemplateFormatter.FormatTemplateDetails(template);

        Assert.Contains("Description: N/A", result);
    }

    [Fact]
    public void FormatTemplateDetails_NoParameters_OmitsParametersSection()
    {
        var template = MakeTemplateInfo(
            name: "Console App",
            shortNames: ["console"],
            language: "C#",
            type: "project",
            description: "Console app",
            author: "Microsoft");

        var result = TemplateFormatter.FormatTemplateDetails(template);

        Assert.DoesNotContain("Parameters:", result);
    }

    [Fact]
    public void FormatTemplateDetails_WithParameters_ShowsParameterBlock()
    {
        var parameters = new[]
        {
            new TemplateParameterDisplayInfo("Framework", "Target framework", "string", "net10.0"),
            new TemplateParameterDisplayInfo("NoRestore", "Skip restore", "bool", null),
        };

        var template = new TemplateDisplayInfo(
            ShortName: "console",
            ShortNames: ["console"],
            Language: "C#",
            Type: "project",
            Description: "Console app",
            Name: "Console App",
            Author: "Microsoft",
            Parameters: parameters);

        var result = TemplateFormatter.FormatTemplateDetails(template);

        Assert.Contains("Parameters:", result);
        Assert.Contains("--Framework", result);
        Assert.Contains("Target framework", result);
        Assert.Contains("Default: net10.0", result);
        Assert.Contains("--NoRestore", result);
        // No default for NoRestore - should not appear
        Assert.DoesNotContain("Default: \n", result);
    }

    [Fact]
    public void FormatTemplateDetails_ParametersAreSortedByName()
    {
        var parameters = new[]
        {
            new TemplateParameterDisplayInfo("ZParam", null, "bool", null),
            new TemplateParameterDisplayInfo("AParam", null, "bool", null),
        };

        var template = new TemplateDisplayInfo(
            ShortName: "console",
            ShortNames: ["console"],
            Language: "C#",
            Type: "project",
            Description: "Console app",
            Name: "Console App",
            Author: "Microsoft",
            Parameters: parameters);

        var result = TemplateFormatter.FormatTemplateDetails(template);

        var aPos = result.IndexOf("--AParam", StringComparison.Ordinal);
        var zPos = result.IndexOf("--ZParam", StringComparison.Ordinal);

        Assert.True(aPos < zPos, "AParam should appear before ZParam");
    }

    // ---------------------------------------------------------------------------
    // FormatSearchResults
    // ---------------------------------------------------------------------------

    [Fact]
    public void FormatSearchResults_MatchingTemplates_ContainsExpectedContent()
    {
        var matches = new[]
        {
            MakeTemplate("console", "C#", "project", "A console application"),
        };

        var result = TemplateFormatter.FormatSearchResults("console", matches);

        Assert.Contains("Templates matching 'console':", result);
        Assert.Contains("console", result);
        Assert.Contains("C#", result);
        Assert.Contains("A console application", result);
        Assert.Contains("Found 1 matching template(s).", result);
    }

    [Fact]
    public void FormatSearchResults_MultipleMatches_CountIsCorrect()
    {
        var matches = new[]
        {
            MakeTemplate("console",    "C#", "project", "Console app"),
            MakeTemplate("consolefs",  "F#", "project", "Console app F#"),
        };

        var result = TemplateFormatter.FormatSearchResults("console", matches);

        Assert.Contains("Found 2 matching template(s).", result);
    }

    [Fact]
    public void FormatSearchResults_MatchesAreSortedByShortName()
    {
        var matches = new[]
        {
            MakeTemplate("webapi",  "C#", "project", "Web API"),
            MakeTemplate("console", "C#", "project", "Console app"),
        };

        var result = TemplateFormatter.FormatSearchResults("c", matches);

        var consolePos = result.IndexOf("console", StringComparison.Ordinal);
        var webapiPos  = result.IndexOf("webapi",  StringComparison.Ordinal);

        Assert.True(consolePos < webapiPos, "console should appear before webapi");
    }

    [Fact]
    public void FormatSearchResults_LongDescription_IsTruncatedToThirtyFiveChars()
    {
        var longDescription = new string('X', 40); // exceeds 35-char limit
        var matches = new[]
        {
            MakeTemplate("console", "C#", "project", longDescription)
        };

        var result = TemplateFormatter.FormatSearchResults("console", matches);

        var truncated = new string('X', 32) + "...";
        Assert.Contains(truncated, result);
        Assert.DoesNotContain(longDescription, result);
    }

    [Fact]
    public void FormatSearchResults_ExactlyThirtyFiveCharDescription_IsNotTruncated()
    {
        var description = new string('Y', 35); // exactly 35 chars
        var matches = new[]
        {
            MakeTemplate("console", "C#", "project", description)
        };

        var result = TemplateFormatter.FormatSearchResults("console", matches);

        Assert.Contains(description, result);
        Assert.DoesNotContain("...", result);
    }

    [Fact]
    public void FormatSearchResults_EmptyCollection_ShowsZeroCount()
    {
        var result = TemplateFormatter.FormatSearchResults("nomatch", []);

        Assert.Contains("Templates matching 'nomatch':", result);
        Assert.Contains("Found 0 matching template(s).", result);
    }

    // ---------------------------------------------------------------------------
    // TemplateDisplayInfo.FromTemplateInfo (mapping layer)
    // ---------------------------------------------------------------------------

    [Fact]
    public void FromTemplateInfo_LanguageTag_IsMappedCorrectly()
    {
        var helper = new FakeTemplateInfo
        {
            ShortNameList = ["console"],
            TagsCollection = new Dictionary<string, string> { ["language"] = "F#" },
        };

        var dto = TemplateDisplayInfo.FromTemplateInfo(helper);

        Assert.Equal("F#", dto.Language);
    }

    [Fact]
    public void FromTemplateInfo_MissingLanguageTag_DefaultsToMultiple()
    {
        var helper = new FakeTemplateInfo
        {
            ShortNameList = ["console"],
            TagsCollection = new Dictionary<string, string>(),
        };

        var dto = TemplateDisplayInfo.FromTemplateInfo(helper);

        Assert.Equal("Multiple", dto.Language);
    }

    [Fact]
    public void FromTemplateInfo_TypeTag_IsMappedCorrectly()
    {
        var helper = new FakeTemplateInfo
        {
            ShortNameList = ["classlib"],
            TagsCollection = new Dictionary<string, string> { ["type"] = "library" },
        };

        var dto = TemplateDisplayInfo.FromTemplateInfo(helper);

        Assert.Equal("library", dto.Type);
    }

    [Fact]
    public void FromTemplateInfo_EmptyShortNameList_ShortNameFallsBackToNA()
    {
        var helper = new FakeTemplateInfo
        {
            ShortNameList = [],
            TagsCollection = new Dictionary<string, string>(),
        };

        var dto = TemplateDisplayInfo.FromTemplateInfo(helper);

        Assert.Equal("N/A", dto.ShortName);
    }

    [Fact]
    public void FromTemplateInfo_ParametersAreSortedByName()
    {
        var helper = new FakeTemplateInfo
        {
            ShortNameList = ["console"],
            TagsCollection = new Dictionary<string, string>(),
            ParameterDefinitions = [
                new FakeTemplateParameter { Name = "ZFirst", DataType = "bool" },
                new FakeTemplateParameter { Name = "AFirst", DataType = "bool" },
            ],
        };

        var dto = TemplateDisplayInfo.FromTemplateInfo(helper);

        Assert.Equal("AFirst", dto.Parameters[0].Name);
        Assert.Equal("ZFirst", dto.Parameters[1].Name);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="TemplateDisplayInfo"/> with commonly-used defaults.
    /// </summary>
    private static TemplateDisplayInfo MakeTemplate(
        string shortName,
        string language,
        string type,
        string? description) =>
        new(
            ShortName: shortName,
            ShortNames: [shortName],
            Language: language,
            Type: type,
            Description: description,
            Name: shortName,
            Author: "Test",
            Parameters: []);

    /// <summary>
    /// Creates a <see cref="TemplateDisplayInfo"/> with full metadata.
    /// </summary>
    private static TemplateDisplayInfo MakeTemplateInfo(
        string name,
        string[] shortNames,
        string language,
        string type,
        string? description,
        string? author) =>
        new(
            ShortName: shortNames.FirstOrDefault() ?? "N/A",
            ShortNames: shortNames,
            Language: language,
            Type: type,
            Description: description,
            Name: name,
            Author: author,
            Parameters: []);

    // -----------------------------------------------------------------------
    // Minimal fakes for FromTemplateInfo mapping tests
    // -----------------------------------------------------------------------

    private sealed class FakeTemplateInfo : Microsoft.TemplateEngine.Abstractions.ITemplateInfo
    {
        // Fields used by TemplateDisplayInfo.FromTemplateInfo
        public IReadOnlyList<string> ShortNameList { get; init; } = [];
        public string? Author { get; init; }
        public string? Description { get; init; }
        public string Name { get; init; } = "";
        public IReadOnlyDictionary<string, string> TagsCollection { get; init; } =
            new Dictionary<string, string>();
        public IReadOnlyList<FakeTemplateParameter> ParameterDefinitions { get; init; } = [];

        Microsoft.TemplateEngine.Abstractions.Parameters.IParameterDefinitionSet
            Microsoft.TemplateEngine.Abstractions.ITemplateMetadata.ParameterDefinitions =>
                new FakeParameterDefinitionSet(ParameterDefinitions);

        // Stub implementations for the rest of ITemplateInfo / ITemplateMetadata
        public string ShortName => ShortNameList.FirstOrDefault() ?? "";
#pragma warning disable CS0618 // ICacheTag / ICacheParameter obsolete — required by ITemplateInfo
        public IReadOnlyDictionary<string, Microsoft.TemplateEngine.Abstractions.ICacheTag> Tags =>
            new Dictionary<string, Microsoft.TemplateEngine.Abstractions.ICacheTag>();
        public IReadOnlyDictionary<string, Microsoft.TemplateEngine.Abstractions.ICacheParameter> CacheParameters =>
            new Dictionary<string, Microsoft.TemplateEngine.Abstractions.ICacheParameter>();
#pragma warning restore CS0618
        public IReadOnlyList<Microsoft.TemplateEngine.Abstractions.ITemplateParameter> Parameters =>
            new List<Microsoft.TemplateEngine.Abstractions.ITemplateParameter>();
        public bool HasScriptRunningPostActions { get; set; }

        // ITemplateMetadata
        public IReadOnlyList<string> Classifications => [];
        public string? DefaultName => null;
        public string Identity => "";
        public string? GroupIdentity => null;
        int Microsoft.TemplateEngine.Abstractions.ITemplateMetadata.Precedence => 0;
        public string? ThirdPartyNotices => null;
        public IReadOnlyDictionary<string, Microsoft.TemplateEngine.Abstractions.IBaselineInfo> BaselineInfo =>
            new Dictionary<string, Microsoft.TemplateEngine.Abstractions.IBaselineInfo>();
        public IReadOnlyList<Guid> PostActions => [];
        public IReadOnlyList<Microsoft.TemplateEngine.Abstractions.Constraints.TemplateConstraintInfo> Constraints => [];
        public bool PreferDefaultName => false;

        // IExtendedTemplateLocator
        public string? LocaleConfigPlace => null;
        public string? HostConfigPlace => null;

        // ITemplateLocator
        public Guid GeneratorId => Guid.Empty;
        public string MountPointUri => "";
        public string ConfigPlace => "";
    }

    private sealed class FakeParameterDefinitionSet(
        IReadOnlyList<FakeTemplateParameter> parameters)
        : Microsoft.TemplateEngine.Abstractions.Parameters.IParameterDefinitionSet
    {
        private readonly IReadOnlyList<Microsoft.TemplateEngine.Abstractions.ITemplateParameter> _params =
            parameters.Cast<Microsoft.TemplateEngine.Abstractions.ITemplateParameter>().ToList();

        public Microsoft.TemplateEngine.Abstractions.ITemplateParameter this[string key] =>
            _params.First(p => p.Name == key);
        public IEnumerable<string> Keys => _params.Select(p => p.Name);
        public IEnumerable<Microsoft.TemplateEngine.Abstractions.ITemplateParameter> Values => _params;
        public bool ContainsKey(string key) => _params.Any(p => p.Name == key);
        public bool TryGetValue(string key, out Microsoft.TemplateEngine.Abstractions.ITemplateParameter value)
        {
            value = _params.FirstOrDefault(p => p.Name == key)!;
            return value != null;
        }
        public IReadOnlyDictionary<string, Microsoft.TemplateEngine.Abstractions.ITemplateParameter> AsReadonlyDictionary() =>
            _params.ToDictionary(p => p.Name);

        // IReadOnlyList<ITemplateParameter>
        public int Count => _params.Count;
        public Microsoft.TemplateEngine.Abstractions.ITemplateParameter this[int index] => _params[index];
        public IEnumerator<Microsoft.TemplateEngine.Abstractions.ITemplateParameter> GetEnumerator() => _params.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _params.GetEnumerator();
    }

    private sealed class FakeTemplateParameter : Microsoft.TemplateEngine.Abstractions.ITemplateParameter
    {
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public string DataType { get; init; } = "string";
        public string? DefaultValue { get; init; }

        // Stub remaining members
        public string? Documentation => null;
#pragma warning disable CS0618 // TemplateParameterPriority obsolete — required by ITemplateParameter
        public Microsoft.TemplateEngine.Abstractions.TemplateParameterPriority Priority =>
            Microsoft.TemplateEngine.Abstractions.TemplateParameterPriority.Optional;
#pragma warning restore CS0618
        public Microsoft.TemplateEngine.Abstractions.TemplateParameterPrecedence Precedence =>
            new(Microsoft.TemplateEngine.Abstractions.PrecedenceDefinition.Optional, null, null, false);
        public string Type => DataType;
        public bool IsName => false;
        public IReadOnlyDictionary<string, Microsoft.TemplateEngine.Abstractions.ParameterChoice>? Choices => null;
        public string? DisplayName => null;
        public bool AllowMultipleValues => false;
        public string? DefaultIfOptionWithoutValue => null;

        // IEquatable<ITemplateParameter>
        public bool Equals(Microsoft.TemplateEngine.Abstractions.ITemplateParameter? other) =>
            other is not null && other.Name == Name;
    }
}
