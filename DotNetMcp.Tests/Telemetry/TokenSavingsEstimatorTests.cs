using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class TokenSavingsEstimatorTests
{
    [Fact]
    public void EstimateWorkflow_RollsUpTotalsAndSteps()
    {
        var estimator = new TokenSavingsEstimator();
        var steps = new[]
        {
            new TokenSavingsStepInput
            {
                StepId = "step-1",
                StepLabel = "Create project",
                ToolName = "dotnet_project",
                StepKind = "discovery",
                PromptText = "Create a console app",
                ToolArgumentsJson = "{\"template\":\"console\"}",
                ToolResponseText = "Project created"
            }
        };

        var estimate = estimator.EstimateWorkflow("create-project-package-build", steps);

        Assert.Equal("create-project-package-build", estimate.WorkflowId);
        Assert.Single(estimate.Steps);
        Assert.Equal("v1", estimate.AssumptionsProfile.AssumptionsVersion);
        Assert.True(estimate.McpEstimatedTokens > 0);
        Assert.True(estimate.BaselineEstimatedTokens > estimate.McpEstimatedTokens);
        Assert.Equal(estimate.BaselineEstimatedTokens - estimate.McpEstimatedTokens, estimate.EstimatedSavingsTokens);
        Assert.InRange(estimate.EstimatedSavingsPercent, 0, 100);
    }

    [Fact]
    public void EstimateWorkflow_UsesFallbackVersionWhenRequested()
    {
        var estimator = new TokenSavingsEstimator();
        var estimate = estimator.EstimateWorkflow(
            "run-tests-summarize-failures",
            [new TokenSavingsStepInput { StepId = "step-1", StepKind = "outputParsing", PromptText = "Run tests" }],
            assumptionsVersion: "v2");

        Assert.Equal("v2", estimate.AssumptionsProfile.AssumptionsVersion);
        Assert.Contains("Fallback profile", estimate.AssumptionsProfile.AssumptionsNotes ?? string.Empty);
    }

    [Fact]
    public void EstimateStep_UsesMeasuredTokensForHighConfidence()
    {
        var estimator = new TokenSavingsEstimator();
        var step = estimator.EstimateStep(
            "scaffold-webapi-ef-setup",
            new TokenSavingsStepInput
            {
                StepId = "step-1",
                StepKind = "manualDiscovery",
                MeasuredMcpTokens = 120,
                MeasuredBaselineTokens = 420
            });

        Assert.Equal("high", step.Confidence);
        Assert.Equal(420, step.BaselineEstimatedTokens);
        Assert.Equal(120, step.McpEstimatedTokens);
        Assert.Equal(300, step.EstimatedSavingsTokens);
    }

    [Fact]
    public void TokenSavingsAccumulator_StoresAndResetsSnapshots()
    {
        var accumulator = new TokenSavingsAccumulator();
        var profile = TokenSavingsAssumptionsProfile.CreateDefault();
        accumulator.RecordWorkflow(new TokenSavingsWorkflowEstimate
        {
            WorkflowId = "create-project-package-build",
            McpEstimatedTokens = 10,
            BaselineEstimatedTokens = 40,
            EstimatedSavingsTokens = 30,
            EstimatedSavingsPercent = 75.0,
            Confidence = "low",
            AssumptionsProfile = profile,
            Steps = []
        });

        var snapshot = accumulator.GetSnapshot();

        Assert.Single(snapshot);
        Assert.True(snapshot.ContainsKey("create-project-package-build"));

        accumulator.Reset();

        Assert.Empty(accumulator.GetSnapshot());
    }

    // -------------------------------------------------------------------------
    // Model-family tests
    // -------------------------------------------------------------------------

    [Fact]
    public void EstimateWorkflow_ModelFamily_SurfacedOnEstimate()
    {
        var estimator = new TokenSavingsEstimator(defaultModelFamily: ModelFamily.ClaudeSonnet);
        var steps = new[]
        {
            new TokenSavingsStepInput { StepId = "s1", StepKind = "normal", PromptText = "Do something" }
        };

        var estimate = estimator.EstimateWorkflow("create-project-package-build", steps);

        Assert.Equal(ModelFamily.ClaudeSonnet, estimate.ModelFamilyUsed);
        Assert.Equal(ModelFamily.ClaudeSonnet, estimate.Steps[0].ModelFamilyUsed);
    }

    [Fact]
    public void EstimateWorkflow_PerStepModelOverrideWins()
    {
        var estimator = new TokenSavingsEstimator(defaultModelFamily: ModelFamily.OpenAiGpt4O);
        var steps = new[]
        {
            new TokenSavingsStepInput
            {
                StepId = "s1",
                StepKind = "normal",
                PromptText = "Do something",
                ModelFamily = ModelFamily.ClaudeHaiku
            }
        };

        var estimate = estimator.EstimateWorkflow("create-project-package-build", steps);

        Assert.Equal(ModelFamily.ClaudeHaiku, estimate.Steps[0].ModelFamilyUsed);
    }

    [Theory]
    [InlineData(ModelFamily.OpenAiO1)]
    [InlineData(ModelFamily.ClaudeHaiku)]
    public void EstimateWorkflow_DifferentModels_ProduceDifferentBaselineTokens(ModelFamily family)
    {
        // o1 has a higher BaselineScaleFactor (1.8), Haiku a lower one (0.85).
        // Both should differ from the Unknown baseline (scale factor 1.0).
        static TokenSavingsWorkflowEstimate Run(ModelFamily f) =>
            new TokenSavingsEstimator(defaultModelFamily: f)
                .EstimateWorkflow("scaffold-webapi-ef-setup",
                [
                    new TokenSavingsStepInput
                    {
                        StepId = "s1", StepKind = "discovery",
                        PromptText = "Create a web API with EF Core",
                        ToolArgumentsJson = "{\"template\":\"webapi\"}"
                    }
                ]);

        Assert.NotEqual(Run(ModelFamily.Unknown).BaselineEstimatedTokens, Run(family).BaselineEstimatedTokens);
    }

    // -------------------------------------------------------------------------
    // Content-kind detection tests
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("{\"key\":\"value\",\"n\":42}", ContentKind.Json)]
    [InlineData("[\"a\",\"b\",\"c\"]", ContentKind.Json)]
    [InlineData("public void Foo() { return; }", ContentKind.Code)]
    [InlineData("if (x > 0) { DoSomething(); }", ContentKind.Code)]
    [InlineData("Create a console application that prints Hello World.", ContentKind.Prose)]
    [InlineData("Run tests and summarize failures for the test suite.", ContentKind.Prose)]
    public void DetectContentKind_ReturnsExpectedKind(string text, ContentKind expected)
    {
        Assert.Equal(expected, TokenSavingsEstimator.DetectContentKind(text));
    }

    // -------------------------------------------------------------------------
    // ParseModelId tests
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseModelId_MapsKnownModelIds()
    {
        Assert.Equal(ModelFamily.OpenAiGpt4O,  TokenizerApproximation.ParseModelId("gpt-4o-2024-08-06"));
        Assert.Equal(ModelFamily.OpenAiGpt4,   TokenizerApproximation.ParseModelId("gpt-4-turbo"));
        Assert.Equal(ModelFamily.OpenAiO1,     TokenizerApproximation.ParseModelId("o1-preview"));
        Assert.Equal(ModelFamily.OpenAiO1,     TokenizerApproximation.ParseModelId("o3-mini"));
        Assert.Equal(ModelFamily.ClaudeSonnet, TokenizerApproximation.ParseModelId("claude-3-5-sonnet-20241022"));
        Assert.Equal(ModelFamily.ClaudeSonnet, TokenizerApproximation.ParseModelId("claude-sonnet-4-5"));
        Assert.Equal(ModelFamily.ClaudeOpus,   TokenizerApproximation.ParseModelId("claude-3-opus-20240229"));
        Assert.Equal(ModelFamily.ClaudeHaiku,  TokenizerApproximation.ParseModelId("claude-3-haiku-20240307"));
        Assert.Equal(ModelFamily.GeminiFlash,  TokenizerApproximation.ParseModelId("gemini-1.5-flash"));
        Assert.Equal(ModelFamily.GeminiPro,    TokenizerApproximation.ParseModelId("gemini-2.0-pro"));
        Assert.Equal(ModelFamily.Llama3,       TokenizerApproximation.ParseModelId("llama-3.1-70b"));
        Assert.Equal(ModelFamily.Mistral,      TokenizerApproximation.ParseModelId("mistral-large-latest"));
        Assert.Equal(ModelFamily.Mistral,      TokenizerApproximation.ParseModelId("mixtral-8x7b"));
        Assert.Equal(ModelFamily.Unknown,      TokenizerApproximation.ParseModelId(null));
        Assert.Equal(ModelFamily.Unknown,      TokenizerApproximation.ParseModelId("some-future-model-xyz"));
    }
}
