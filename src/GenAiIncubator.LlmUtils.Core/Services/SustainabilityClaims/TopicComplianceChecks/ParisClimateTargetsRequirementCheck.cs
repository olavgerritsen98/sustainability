using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks.Constants;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

/// <summary>
/// Implementation of the green electricity topic-specific requirement.
/// </summary>
public class ParisClimateTargetsRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
{
    private const string SubCheckWordingClarity = "WordingClarity";

    /// <inheritdoc />
    public override RequirementCode AssociatedRequirementCode => RequirementCode.ParisClimateTargets;

    /// <inheritdoc />
    public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.ParisClimateTargets;


    /// <summary>
    /// Checks the Paris climate targets requirement.
    /// </summary>
    /// <param name="claim"></param>
    /// <param name="sourceContent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected override async Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(
        SustainabilityClaim claim,
        string sourceContent = "",
        CancellationToken cancellationToken = default)
    {
        return
        [
            await RunWordingClarityCheckAsync(claim, sourceContent, cancellationToken),
        ];
    }

    private async Task<TopicComplianceEvaluationResponse> RunWordingClarityCheckAsync(
        SustainabilityClaim claim,
        string sourceContent,
        CancellationToken cancellationToken)
    {
        return await kernel.ExecuteTopicSubCheckAsync(
            SubCheckWordingClarity,
            GetWordingClarityPrompt(claim, sourceContent),
            cancellationToken);
    }

    private static string GetWordingClarityPrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.
        You're evaluating a sustainability claim in the topic: Paris Climate Targets.

        Requirement:
        - If the claim uses potentially unclear jargon, shorthand, or marketing terms related to the Paris Agreement / climate targets
          (e.g., "Paris proof", "parijsproof", "Paris-aligned", "Paris-compatible", "1.5°C-proof", "in lijn met Parijs"),
          the wording must be understandable to an average reader.
        - Unclear terms must be explained in plain language OR be immediately clarified via a short explanation nearby.
        - If the claim already uses clear language (e.g., explicitly explains what the term means), it is compliant.
        - If the claim does not use such jargon at all, it is compliant with this requirement.

        Guidance for evaluation:
        - Treat it as NON-compliant if a reader could reasonably ask: "What does Paris proof mean here?" and no explanation is provided.
        - An explanation can be brief (one sentence) and does not need to include extensive proof; this check is only about clarity of wording.

        For example, the claim "Our building is Paris-proof" without further explanation is NON-compliant.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}
        
        [BEGIN CLAIM]
        ClaimText: "{claim.ClaimText}"
        RelatedText: "{claim.RelatedText}"
        [END CLAIM]

        [CLAIM RELATED URLS]
        "{string.Join(", ", claim.RelatedUrls)}"
        [END CLAIM RELATED URLS]

        [BEGIN BROADER CONTEXT]
        "{sourceContent}"
        [END BROADER CONTEXT]

        Task:
        - Return a TopicComplianceEvaluationResponse JSON object with fields:
          - IsCompliant (boolean)
          - Reasoning (string)
          - Warning (string, optional)
          - SuggestedAlternative (string, if non-compliant)
        - If non-compliant, provide a suggested alternative wording that explains the unclear term(s) succinctly.
        """;
    }
}