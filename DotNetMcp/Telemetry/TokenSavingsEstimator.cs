namespace DotNetMcp;

/// <summary>
/// Estimates token usage and token savings for a workflow.
/// </summary>
public sealed class TokenSavingsEstimator
{
    private readonly TokenSavingsAssumptionsProfile _defaultProfile;
    private readonly ModelFamily _defaultModelFamily;

    /// <summary>
    /// Initialises a new <see cref="TokenSavingsEstimator"/> with an optional assumptions profile and default model family.
    /// </summary>
    /// <param name="defaultProfile">Heuristic assumptions profile. Defaults to the v1 built-in profile.</param>
    /// <param name="defaultModelFamily">
    /// Default model family used for tokenizer approximation when not supplied per-step.
    /// Defaults to <see cref="ModelFamily.Unknown"/> (universal 4 chars/token fallback).
    /// </param>
    public TokenSavingsEstimator(
        TokenSavingsAssumptionsProfile? defaultProfile = null,
        ModelFamily defaultModelFamily = ModelFamily.Unknown)
    {
        _defaultProfile = defaultProfile ?? TokenSavingsAssumptionsProfile.CreateDefault();
        _defaultModelFamily = defaultModelFamily;
    }

    /// <summary>
    /// Estimates token savings for a complete workflow by aggregating per-step estimates.
    /// </summary>
    /// <param name="modelFamily">
    /// Optional model family override for this workflow. Falls back to the constructor default.
    /// Can be further overridden per-step via <see cref="TokenSavingsStepInput.ModelFamily"/>.
    /// </param>
    public TokenSavingsWorkflowEstimate EstimateWorkflow(
        string workflowId,
        IReadOnlyCollection<TokenSavingsStepInput> steps,
        string? assumptionsVersion = null,
        ModelFamily? modelFamily = null)
    {
        var profile = ResolveProfile(assumptionsVersion);
        var workflowFamily = modelFamily ?? _defaultModelFamily;
        var estimatedSteps = steps.Select(step => EstimateStep(workflowId, step, profile, workflowFamily)).ToArray();

        var mcpEstimatedTokens = estimatedSteps.Sum(step => step.McpEstimatedTokens);
        var baselineEstimatedTokens = estimatedSteps.Sum(step => step.BaselineEstimatedTokens);
        var savingsTokens = baselineEstimatedTokens - mcpEstimatedTokens;
        var savingsPercent = baselineEstimatedTokens > 0
            ? Math.Round((double)savingsTokens / baselineEstimatedTokens * 100.0, 1)
            : 0.0;

        // Surface the effective model family: unanimous if all steps agree, Unknown if mixed.
        var effectiveFamily = estimatedSteps.Length > 0 && estimatedSteps.All(s => s.ModelFamilyUsed == estimatedSteps[0].ModelFamilyUsed)
            ? estimatedSteps[0].ModelFamilyUsed
            : ModelFamily.Unknown;

        return new TokenSavingsWorkflowEstimate
        {
            WorkflowId = workflowId,
            ModelFamilyUsed = effectiveFamily,
            McpEstimatedTokens = mcpEstimatedTokens,
            BaselineEstimatedTokens = baselineEstimatedTokens,
            EstimatedSavingsTokens = savingsTokens,
            EstimatedSavingsPercent = savingsPercent,
            Confidence = CombineConfidence(estimatedSteps.Select(step => step.Confidence)),
            AssumptionsProfile = profile,
            Steps = estimatedSteps
        };
    }

    public TokenSavingsStepEstimate EstimateStep(
        string workflowId,
        TokenSavingsStepInput input,
        TokenSavingsAssumptionsProfile? profileOverride = null,
        ModelFamily? workflowFamilyOverride = null)
    {
        var profile = profileOverride ?? ResolveProfile(null);
        var family = input.ModelFamily ?? workflowFamilyOverride ?? _defaultModelFamily;
        var tokenizer = TokenizerApproximation.For(family);

        var mcpEstimatedTokens = input.MeasuredMcpTokens
            ?? EstimateTokens(tokenizer, input.PromptText, input.ToolArgumentsJson, input.ToolResponseText, input.StructuredContentJson);
        var heuristics = ResolveHeuristics(profile, workflowId);
        var baselineEstimatedTokens = input.MeasuredBaselineTokens
            ?? (mcpEstimatedTokens + EstimateBaselineOverhead(heuristics, input.StepKind, tokenizer));
        var savingsTokens = baselineEstimatedTokens - mcpEstimatedTokens;
        var savingsPercent = baselineEstimatedTokens > 0
            ? Math.Round((double)savingsTokens / baselineEstimatedTokens * 100.0, 1)
            : 0.0;

        return new TokenSavingsStepEstimate
        {
            StepId = input.StepId,
            StepLabel = input.StepLabel,
            ToolName = input.ToolName,
            ModelFamilyUsed = family,
            McpEstimatedTokens = mcpEstimatedTokens,
            BaselineEstimatedTokens = baselineEstimatedTokens,
            EstimatedSavingsTokens = savingsTokens,
            EstimatedSavingsPercent = savingsPercent,
            Confidence = ResolveConfidence(input),
            AssumptionsProfile = profile
        };
    }

    private TokenSavingsAssumptionsProfile ResolveProfile(string? assumptionsVersion)
    {
        if (string.IsNullOrWhiteSpace(assumptionsVersion) || string.Equals(assumptionsVersion, _defaultProfile.AssumptionsVersion, StringComparison.OrdinalIgnoreCase))
            return _defaultProfile;

        return new TokenSavingsAssumptionsProfile
        {
            AssumptionsVersion = assumptionsVersion,
            AssumptionsNotes = $"Fallback profile for unknown version '{assumptionsVersion}'.",
            Workflows = _defaultProfile.Workflows
        };
    }

    private static TokenSavingsWorkflowHeuristics ResolveHeuristics(TokenSavingsAssumptionsProfile profile, string workflowId)
        => profile.Workflows.TryGetValue(workflowId, out var heuristics)
            ? heuristics
            : new TokenSavingsWorkflowHeuristics
            {
                DiscoveryTurns = 1,
                RetryTurns = 1,
                OutputParsingTurns = 1,
                ManualDiscoveryTurns = 1,
                TokensPerTurn = 300,
                Notes = "Default heuristic fallback"
            };

    /// <summary>
    /// Estimates tokens for a set of text parts using the provided tokenizer approximation.
    /// Each part is sniffed for content kind (JSON, code, or prose) and the appropriate
    /// chars-per-token ratio is applied independently.
    /// </summary>
    internal static long EstimateTokens(TokenizerApproximation tokenizer, params string?[] parts)
    {
        long total = 0;
        foreach (var part in parts.OfType<string>().Where(p => p.Length > 0))
        {
            var charsPerToken = DetectContentKind(part) switch
            {
                ContentKind.Json => tokenizer.JsonCharsPerToken,
                ContentKind.Code => tokenizer.CodeCharsPerToken,
                _                => tokenizer.ProseCharsPerToken
            };
            total += (long)Math.Ceiling(part.Length / charsPerToken);
        }
        return total;
    }

    /// <summary>
    /// Heuristically detects whether a text payload looks like JSON, source code, or prose.
    /// Used to select the correct chars-per-token ratio for that content type.
    /// </summary>
    internal static ContentKind DetectContentKind(string? text)
    {
        if (string.IsNullOrEmpty(text)) return ContentKind.Prose;

        var trimmed = text.TrimStart();

        // JSON objects or arrays
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return ContentKind.Json;

        // Code heuristic: high density of code-punctuation relative to length
        var codePunct = text.Count(c => c is '{' or '}' or '(' or ')' or ';' or '<' or '>');
        if (codePunct > text.Length * 0.04)
            return ContentKind.Code;

        return ContentKind.Prose;
    }

    private static long EstimateBaselineOverhead(
        TokenSavingsWorkflowHeuristics heuristics,
        string stepKind,
        TokenizerApproximation tokenizer)
    {
        // Apply the model-family baseline scale factor to the per-turn token estimate.
        var tokensPerTurn = (long)Math.Ceiling(heuristics.TokensPerTurn * tokenizer.BaselineScaleFactor);
        return stepKind.Trim().ToLowerInvariant() switch
        {
            "discovery"       => heuristics.DiscoveryTurns * tokensPerTurn,
            "retry"           => heuristics.RetryTurns * tokensPerTurn,
            "outputparsing"   => heuristics.OutputParsingTurns * tokensPerTurn,
            "manualdiscovery" => heuristics.ManualDiscoveryTurns * tokensPerTurn,
            _                 => tokensPerTurn
        };
    }

    private static string ResolveConfidence(TokenSavingsStepInput input)
    {
        if (input.MeasuredMcpTokens.HasValue && input.MeasuredBaselineTokens.HasValue)
            return "high";

        if (input.MeasuredMcpTokens.HasValue || input.MeasuredBaselineTokens.HasValue)
            return "medium";

        return "low";
    }

    private static string CombineConfidence(IEnumerable<string> confidences)
    {
        var list = confidences.ToArray();
        if (list.Length == 0)
            return "low";

        if (list.All(value => value.Equals("high", StringComparison.OrdinalIgnoreCase)))
            return "high";

        if (list.Any(value => value.Equals("medium", StringComparison.OrdinalIgnoreCase)))
            return "medium";

        return "low";
    }
}