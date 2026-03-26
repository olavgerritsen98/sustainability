using System.Text.Json;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks.Constants;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

/// <summary>
/// Implementation of the fossil-free living in one generation topic-specific requirement.
/// </summary>
public class FossilFreeLivingRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
{
    /// <inheritdoc />
    public override RequirementCode AssociatedRequirementCode => RequirementCode.FossilFreeLivingInOneGeneration;

    /// <inheritdoc />
    public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.FossilFreeLivingInOneGeneration;


    /// <summary>
    /// Checks the sustainable production requirement.
    /// </summary>
    /// <param name="claim"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected override async Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent = "", CancellationToken cancellationToken = default)
    {
        string FossilFreeLivingInOneGenerationRequirementCheckPrompt = GetFossilFreeLivingInOneGenerationRequirementCheckPrompt(claim);

        string result = await ChatHelpers.ExecutePromptAsync(
            kernel,
            FossilFreeLivingInOneGenerationRequirementCheckPrompt,
            typeof(TopicComplianceEvaluationResponse),
            cancellationToken: cancellationToken
        );

        TopicComplianceEvaluationResponse response =
            JsonSerializer.Deserialize<TopicComplianceEvaluationResponse>(result) ??
            throw new InvalidOperationException($"Failed to parse fossil free living requirement check response for claim: {claim.ClaimText}");

        return [response];
    }

    private static string GetFossilFreeLivingInOneGenerationRequirementCheckPrompt(SustainabilityClaim claim)
    {
        return $"""
            System instruction:
            You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.

            This claim relates to statements using the slogan "Fossil-Free Living in One Generation."
            Evaluate whether the claim complies with the following requirements derived from Vattenfall’s handbook and ACM rules.
            
            [GENERAL COMPLIANCE RULES]
            {SharedPromptConstants.CopyHandboekRules}

            [BEGIN REQUIREMENTS]
            1. Ambition framing (FFL-1)
            • The phrase “Fossil-Free Living in One Generation” must be presented as an ambition, vision, or long-term goal—not as a statement of current achievement.
            • Wording like “we already deliver fossil-free energy” is misleading.

            2. Tone and phrasing (FFL-5)
            • Tone must be forward-looking and transparent (use “ambition,” “goal,” “working toward”).
            • Avoid exaggeration or superiority: no “already fossil-free” or “more fossil-free than others.”
            
        3. Concrete evidence for progress claims (FFL-3)
        • If the claim implies that progress toward fossil-free living is being made NOW (not just as a long-term vision), it must reference a concrete Vattenfall Action Plan or Roadmap.
        • General references to external pacts or agreements are not sufficient.
        • Mark as non-compliant if progress is claimed without linking to a Vattenfall-specific roadmap/plan.
            [END REQUIREMENTS]

            [CLAIM TO EVALUATE]
            "{claim.ClaimText}"

            [CLAIM RELATED URLS]
            "{string.Join(", ", claim.RelatedUrls)}"
            """;
    }
}
