using System.Linq;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks.Constants;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

/// <summary>
/// Dedicated check for superlative claims ("most", "best", "largest", etc.).
/// Determines compliance only when independent, up-to-date, verifiable evidence is present.
/// </summary>
public class SuperlativesRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
{
    /// <inheritdoc/>
    public override RequirementCode AssociatedRequirementCode => RequirementCode.Superlatives;

    /// <inheritdoc/>
    public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.SuperlativesStatement;

    /// <inheritdoc/>
    protected override async Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(
        SustainabilityClaim claim,
        string sourceContent = "",
        CancellationToken cancellationToken = default)
    {
        // Detect superlatives; if none, return compliant (not applicable)
        bool contains = ContainsSuperlative(claim.ClaimText);
        if (!contains)
        {
            return [new TopicComplianceEvaluationResponse
            {
                IsCompliant = true,
                Reasoning = "[Superlatives] No superlative wording detected; check not applicable.",
                SuggestedAlternative = string.Empty,
                Warning = string.Empty
            }];
        }

        var response = await kernel.ExecuteTopicSubCheckAsync(
            "SuperlativesEvidence",
            BuildSuperlativesPrompt(claim),
            cancellationToken);

        return [response];
    }

    private static string BuildSuperlativesPrompt(SustainabilityClaim claim) => $"""
System instruction:
You are an expert compliance analyst specializing in sustainability communications.

Sub-check: Superlatives Evidence Validation
Goal: Judge whether each superlative in the claim is backed by independent, up-to-date, verifiable evidence.

[GENERAL COMPLIANCE RULES]
{SharedPromptConstants.CopyHandboekRules}

[SUPERLATIVE GUIDELINES]
Terms like "the best", "the most sustainable", "real green", "100% sustainable", "largest", "leading" are superlatives.
They imply superiority over all competitors and require:
• Independent source or audit (e.g. public ranking, index, capacity statistics).
• Up-to-date and verifiable data (ideally <= 24 months).
• Clear description of what dimension the superiority refers to (capacity, perception, sustainability metric).

Without such evidence they are misleading and must not be used.

[EXAMPLES]
Incorrect: "We are the largest wind power producer in the Netherlands." (Needs current comparative capacity data.)
Correct: "We are one of the largest wind power producers in the Netherlands." (Softens absolute claim.)
Incorrect: "We are the most sustainable energy supplier." (Needs independent ranking; perception ≠ objective.)
Correct: "According to the Sustainable Brand Index 2022, consumers perceive Vattenfall as one of the more sustainable energy brands." (Clarifies it's perception.)
Incorrect: "We provide real green electricity, unlike others." (Unfair, vague.)
Correct: "With the product Groen uit Nederland, you receive 100% renewable electricity from Dutch wind, water, and sun." (Defines product features.)

[BEGIN CLAIM]
{claim.ClaimText}
[END CLAIM]

Tasks:
1. List each superlative term found.
2. For each, state if adequate independent, current evidence is present in the claim text (do not invent sources).
3. Overall compliance: true only if ALL detected superlatives have adequate evidence.
4. If non-compliant, provide an alternative wording that softens or cites a plausible evidence placeholder (without fabricating data).
5. Add a warning if the wording is likely to mislead readers.
""";

    private static bool ContainsSuperlative(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string[] markers = ["most", "least", "lowest", "highest", "best", "greenest", "largest", "leading", "number one", "#1", "top", "real green", "100% sustainable"];
        return markers.Any(m => text.Contains(m, StringComparison.OrdinalIgnoreCase));
    }
}
