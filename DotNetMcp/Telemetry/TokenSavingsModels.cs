using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace DotNetMcp;

/// <summary>
/// The broad family of LLM a caller is using. Controls which tokenizer approximation
/// and baseline scale factor are applied during token savings estimation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModelFamily
{
    /// <summary>No model specified; uses the universal 4 chars/token fallback.</summary>
    Unknown = 0,
    /// <summary>GPT-4 (non-omni) — cl100k_base tokenizer.</summary>
    OpenAiGpt4,
    /// <summary>GPT-4o and GPT-4o-mini — cl100k_base tokenizer, similar ratios to GPT-4.</summary>
    OpenAiGpt4O,
    /// <summary>OpenAI o1 / o3 reasoning models — same tokenizer but much higher baseline cost due to thinking tokens.</summary>
    OpenAiO1,
    /// <summary>Claude 3 / 3.5 / 4 Sonnet — Anthropic BPE tokenizer.</summary>
    ClaudeSonnet,
    /// <summary>Claude 3 / 3.5 Opus — Anthropic BPE tokenizer.</summary>
    ClaudeOpus,
    /// <summary>Claude 3 / 3.5 Haiku — Anthropic BPE tokenizer, more compact responses.</summary>
    ClaudeHaiku,
    /// <summary>Gemini Pro family (1.5 Pro, 2.0 Pro) — SentencePiece tokenizer.</summary>
    GeminiPro,
    /// <summary>Gemini Flash family — SentencePiece tokenizer, more compact responses.</summary>
    GeminiFlash,
    /// <summary>Llama 3 / 3.1 / 3.2 — tiktoken-style tokenizer.</summary>
    Llama3,
    /// <summary>Mistral family — SentencePiece tokenizer.</summary>
    Mistral
}

/// <summary>
/// Detected content kind used to pick the right chars-per-token ratio.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentKind
{
    /// <summary>Plain English prose.</summary>
    Prose,
    /// <summary>JSON payload (keys and punctuation split into many tokens).</summary>
    Json,
    /// <summary>Source code (brackets, operators, identifiers tokenize densely).</summary>
    Code
}

/// <summary>
/// Per-model-family tokenizer approximation.
/// Chars-per-token ratios are empirically derived from representative English + developer content.
/// BaselineScaleFactor adjusts the heuristic TokensPerTurn from the assumptions profile to reflect
/// how verbose a model family tends to be in a non-MCP (manual) workflow.
/// </summary>
public sealed class TokenizerApproximation
{
    /// <summary>Average characters per token for plain prose.</summary>
    public double ProseCharsPerToken { get; init; } = 4.0;

    /// <summary>Average characters per token for JSON payloads.</summary>
    public double JsonCharsPerToken { get; init; } = 3.5;

    /// <summary>Average characters per token for source code.</summary>
    public double CodeCharsPerToken { get; init; } = 3.2;

    /// <summary>
    /// Multiplier applied to the heuristic TokensPerTurn value to account for model verbosity.
    /// 1.0 = matches the profile baseline exactly. Values above 1.0 mean the model tends to
    /// produce larger non-MCP turns (e.g., reasoning tokens in o1/o3); below 1.0 means
    /// more concise models (e.g., Haiku, Flash).
    /// </summary>
    public double BaselineScaleFactor { get; init; } = 1.0;

    /// <summary>Well-known approximation profiles keyed by model family.</summary>
    public static IReadOnlyDictionary<ModelFamily, TokenizerApproximation> WellKnown { get; } =
        new Dictionary<ModelFamily, TokenizerApproximation>
        {
            // cl100k_base: well-studied; ~4 chars/token English prose
            [ModelFamily.Unknown]       = new() { ProseCharsPerToken = 4.0, JsonCharsPerToken = 3.5, CodeCharsPerToken = 3.2, BaselineScaleFactor = 1.00 },
            [ModelFamily.OpenAiGpt4]    = new() { ProseCharsPerToken = 4.0, JsonCharsPerToken = 3.5, CodeCharsPerToken = 3.2, BaselineScaleFactor = 1.10 },
            [ModelFamily.OpenAiGpt4O]   = new() { ProseCharsPerToken = 4.0, JsonCharsPerToken = 3.5, CodeCharsPerToken = 3.2, BaselineScaleFactor = 1.00 },
            // o1/o3: same external tokenizer, but thinking tokens inflate baseline dramatically
            [ModelFamily.OpenAiO1]      = new() { ProseCharsPerToken = 4.0, JsonCharsPerToken = 3.5, CodeCharsPerToken = 3.2, BaselineScaleFactor = 1.80 },
            // Anthropic BPE: produces ~3.7 chars/token for English prose
            [ModelFamily.ClaudeSonnet]  = new() { ProseCharsPerToken = 3.7, JsonCharsPerToken = 3.3, CodeCharsPerToken = 3.0, BaselineScaleFactor = 0.95 },
            [ModelFamily.ClaudeOpus]    = new() { ProseCharsPerToken = 3.7, JsonCharsPerToken = 3.3, CodeCharsPerToken = 3.0, BaselineScaleFactor = 1.05 },
            [ModelFamily.ClaudeHaiku]   = new() { ProseCharsPerToken = 3.7, JsonCharsPerToken = 3.3, CodeCharsPerToken = 3.0, BaselineScaleFactor = 0.85 },
            // SentencePiece (Gemini): slightly denser for code than cl100k
            [ModelFamily.GeminiPro]     = new() { ProseCharsPerToken = 3.8, JsonCharsPerToken = 3.4, CodeCharsPerToken = 3.1, BaselineScaleFactor = 1.00 },
            [ModelFamily.GeminiFlash]   = new() { ProseCharsPerToken = 3.8, JsonCharsPerToken = 3.4, CodeCharsPerToken = 3.1, BaselineScaleFactor = 0.90 },
            // Llama 3 tiktoken-variant
            [ModelFamily.Llama3]        = new() { ProseCharsPerToken = 3.5, JsonCharsPerToken = 3.2, CodeCharsPerToken = 3.0, BaselineScaleFactor = 0.95 },
            // Mistral SentencePiece
            [ModelFamily.Mistral]       = new() { ProseCharsPerToken = 3.6, JsonCharsPerToken = 3.3, CodeCharsPerToken = 3.1, BaselineScaleFactor = 0.95 },
        };

    /// <summary>
    /// Returns the approximation for the given model family,
    /// falling back to <see cref="ModelFamily.Unknown"/> if not found.
    /// </summary>
    public static TokenizerApproximation For(ModelFamily family)
        => WellKnown.TryGetValue(family, out var profile) ? profile : WellKnown[ModelFamily.Unknown];

    /// <summary>
    /// Parses a raw model-ID string (e.g. <c>"gpt-4o"</c>, <c>"claude-sonnet-4-5"</c>) into a
    /// <see cref="ModelFamily"/>. Comparison is case-insensitive; unknown strings return
    /// <see cref="ModelFamily.Unknown"/>.
    /// </summary>
    public static ModelFamily ParseModelId(string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            return ModelFamily.Unknown;

        var id = modelId.ToLowerInvariant();

        if (id.Contains("o1") || id.Contains("o3") || id.Contains("o4"))
            return ModelFamily.OpenAiO1;
        if (id.Contains("gpt-4o") || id.Contains("gpt4o"))
            return ModelFamily.OpenAiGpt4O;
        if (id.Contains("gpt-4") || id.Contains("gpt4"))
            return ModelFamily.OpenAiGpt4;
        if (id.Contains("claude") && id.Contains("opus"))
            return ModelFamily.ClaudeOpus;
        if (id.Contains("claude") && id.Contains("haiku"))
            return ModelFamily.ClaudeHaiku;
        if (id.Contains("claude") && (id.Contains("sonnet") || id.Contains("3") || id.Contains("4")))
            return ModelFamily.ClaudeSonnet;
        if (id.Contains("gemini") && id.Contains("flash"))
            return ModelFamily.GeminiFlash;
        if (id.Contains("gemini"))
            return ModelFamily.GeminiPro;
        if (id.Contains("llama"))
            return ModelFamily.Llama3;
        if (id.Contains("mistral") || id.Contains("mixtral"))
            return ModelFamily.Mistral;

        return ModelFamily.Unknown;
    }
}

/// <summary>
/// Versioned assumption profile metadata for token savings estimation.
/// </summary>
public sealed class TokenSavingsAssumptionsProfile
{
    /// <summary>Version identifier for the assumptions profile.</summary>
    [JsonPropertyName("assumptionsVersion")]
    public string AssumptionsVersion { get; init; } = "v1";

    /// <summary>Optional human-readable notes about the assumptions used.</summary>
    [JsonPropertyName("assumptionsNotes")]
    public string? AssumptionsNotes { get; init; }

    /// <summary>Workflow-specific heuristic profiles keyed by workflow id.</summary>
    [JsonPropertyName("workflows")]
    public Dictionary<string, TokenSavingsWorkflowHeuristics> Workflows { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates the default v1 assumptions profile, loading it from the embedded JSON resource.
    /// Falls back to hard-coded defaults if the resource cannot be read.
    /// </summary>
    public static TokenSavingsAssumptionsProfile CreateDefault()
    {
        try
        {
            var assembly = typeof(TokenSavingsAssumptionsProfile).Assembly;
            const string resourceName = "DotNetMcp.Telemetry.Profiles.TokenSavingsProfile.Default.v1.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var loaded = JsonSerializer.Deserialize<TokenSavingsAssumptionsProfile>(json, options);
                if (loaded is not null)
                    return loaded;
            }
        }
        catch (Exception)
        {
            // Fall through to hard-coded defaults
        }

        return CreateHardCodedDefault();
    }

    private static TokenSavingsAssumptionsProfile CreateHardCodedDefault() => new()
    {
        AssumptionsVersion = "v1",
        AssumptionsNotes = "Heuristic baseline profiles with a 4-char-per-token approximation for MCP payloads.",
        Workflows = new Dictionary<string, TokenSavingsWorkflowHeuristics>(StringComparer.OrdinalIgnoreCase)
        {
            ["create-project-package-build"] = new TokenSavingsWorkflowHeuristics
            {
                DiscoveryTurns = 2,
                RetryTurns = 1,
                OutputParsingTurns = 1,
                ManualDiscoveryTurns = 1,
                TokensPerTurn = 350,
                Notes = "Project creation + package add + build typically requires a small amount of discovery and one retry when package or template parameters are missing."
            },
            ["run-tests-summarize-failures"] = new TokenSavingsWorkflowHeuristics
            {
                DiscoveryTurns = 1,
                RetryTurns = 1,
                OutputParsingTurns = 2,
                ManualDiscoveryTurns = 1,
                TokensPerTurn = 300,
                Notes = "Test failure workflows pay more parsing cost because raw output is verbose and failure triage is iterative."
            },
            ["scaffold-webapi-ef-setup"] = new TokenSavingsWorkflowHeuristics
            {
                DiscoveryTurns = 2,
                RetryTurns = 1,
                OutputParsingTurns = 1,
                ManualDiscoveryTurns = 2,
                TokensPerTurn = 375,
                Notes = "Web API plus EF setup often includes template discovery, package selection, and user-secrets guidance."
            }
        }
    };
}

/// <summary>
/// Workflow-specific heuristic assumptions used to derive baseline token estimates.
/// </summary>
public sealed class TokenSavingsWorkflowHeuristics
{
    [JsonPropertyName("discoveryTurns")]
    public int DiscoveryTurns { get; init; }

    [JsonPropertyName("retryTurns")]
    public int RetryTurns { get; init; }

    [JsonPropertyName("outputParsingTurns")]
    public int OutputParsingTurns { get; init; }

    [JsonPropertyName("manualDiscoveryTurns")]
    public int ManualDiscoveryTurns { get; init; }

    [JsonPropertyName("tokensPerTurn")]
    public int TokensPerTurn { get; init; } = 300;

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

/// <summary>
/// One workflow step used as input to the estimator.
/// </summary>
public sealed class TokenSavingsStepInput
{
    /// <summary>
    /// Optional model family hint. When set, overrides the estimator's default and selects
    /// the appropriate tokenizer approximation for MCP-side token counting and baseline scaling.
    /// </summary>
    [JsonPropertyName("modelFamily")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ModelFamily? ModelFamily { get; init; }
    [JsonPropertyName("stepId")]
    public string StepId { get; init; } = string.Empty;

    [JsonPropertyName("stepLabel")]
    public string? StepLabel { get; init; }

    [JsonPropertyName("toolName")]
    public string? ToolName { get; init; }

    [JsonPropertyName("stepKind")]
    public string StepKind { get; init; } = "normal";

    [JsonPropertyName("promptText")]
    public string? PromptText { get; init; }

    [JsonPropertyName("toolArgumentsJson")]
    public string? ToolArgumentsJson { get; init; }

    [JsonPropertyName("toolResponseText")]
    public string? ToolResponseText { get; init; }

    [JsonPropertyName("structuredContentJson")]
    public string? StructuredContentJson { get; init; }

    [JsonPropertyName("measuredMcpTokens")]
    public long? MeasuredMcpTokens { get; init; }

    [JsonPropertyName("measuredBaselineTokens")]
    public long? MeasuredBaselineTokens { get; init; }
}

/// <summary>
/// Per-step token savings estimate.
/// </summary>
public sealed class TokenSavingsStepEstimate
{
    /// <summary>Model family that was used for tokenizer approximation in this step.</summary>
    [JsonPropertyName("modelFamilyUsed")]
    public ModelFamily ModelFamilyUsed { get; init; } = ModelFamily.Unknown;
    [JsonPropertyName("stepId")]
    public string StepId { get; init; } = string.Empty;

    [JsonPropertyName("stepLabel")]
    public string? StepLabel { get; init; }

    [JsonPropertyName("toolName")]
    public string? ToolName { get; init; }

    [JsonPropertyName("mcpEstimatedTokens")]
    public long McpEstimatedTokens { get; init; }

    [JsonPropertyName("baselineEstimatedTokens")]
    public long BaselineEstimatedTokens { get; init; }

    [JsonPropertyName("estimatedSavingsTokens")]
    public long EstimatedSavingsTokens { get; init; }

    [JsonPropertyName("estimatedSavingsPercent")]
    public double EstimatedSavingsPercent { get; init; }

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = "low";

    [JsonPropertyName("assumptionsProfile")]
    public TokenSavingsAssumptionsProfile AssumptionsProfile { get; init; } = new();
}

/// <summary>
/// Workflow-level token savings estimate with per-step detail.
/// </summary>
public sealed class TokenSavingsWorkflowEstimate
{
    /// <summary>Effective model family used for estimation (taken from steps; Unknown if mixed or unset).</summary>
    [JsonPropertyName("modelFamilyUsed")]
    public ModelFamily ModelFamilyUsed { get; init; } = ModelFamily.Unknown;
    [JsonPropertyName("workflowId")]
    public string WorkflowId { get; init; } = string.Empty;

    [JsonPropertyName("mcpEstimatedTokens")]
    public long McpEstimatedTokens { get; init; }

    [JsonPropertyName("baselineEstimatedTokens")]
    public long BaselineEstimatedTokens { get; init; }

    [JsonPropertyName("estimatedSavingsTokens")]
    public long EstimatedSavingsTokens { get; init; }

    [JsonPropertyName("estimatedSavingsPercent")]
    public double EstimatedSavingsPercent { get; init; }

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = "low";

    [JsonPropertyName("assumptionsProfile")]
    public TokenSavingsAssumptionsProfile AssumptionsProfile { get; init; } = new();

    [JsonPropertyName("steps")]
    public TokenSavingsStepEstimate[] Steps { get; init; } = [];
}