using System.Collections.Generic;
using System.Linq;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks.Constants;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

/// <summary>
/// Implementation of the heat topic-specific requirement.
/// </summary>
public class HeatRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
{
    /// <inheritdoc />
    public override RequirementCode AssociatedRequirementCode => RequirementCode.HeatAndCold;

    /// <inheritdoc />
    public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.HeatAndCold;

    public static readonly string HeatLabelUrl = "www.vattenfall.nl/stadsverwarming/warmte-etiket/";

    /// <summary>
    /// Checks the heat requirement.
    /// </summary>
    /// <param name="claim"></param>
    /// <param name="sourceContent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected override async Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent = "", CancellationToken cancellationToken = default)
    {
        // Sequential execution mirroring pattern used in GreenElectricityRequirementCheck.
        var results = new List<TopicComplianceEvaluationResponse>
        {
            await RunGeneralRequirementsCheckAsync(claim, cancellationToken),
            await RunHeatLabelWhenDataCheckAsync(claim, cancellationToken)
        };
        return results;
    }

    #region Sub-check execution helpers
    private const string SubCheckGeneralRequirements = "GeneralRequirements";
    private const string SubCheckHeatLabelWhenData = "HeatLabelWhenData";

    private async Task<TopicComplianceEvaluationResponse> RunGeneralRequirementsCheckAsync(SustainabilityClaim claim, CancellationToken cancellationToken)
    {
        var result = await kernel.ExecuteTopicSubCheckAsync(SubCheckGeneralRequirements, GetHeatRequirementCheckPrompt(claim), cancellationToken);
        result.Reasoning = $"[{SubCheckGeneralRequirements}] " + result.Reasoning;
        result.Warning = string.IsNullOrWhiteSpace(result.Warning) ? string.Empty : $"[{SubCheckGeneralRequirements}] " + result.Warning;
        return result;
    }

    private async Task<TopicComplianceEvaluationResponse> RunHeatLabelWhenDataCheckAsync(SustainabilityClaim claim, CancellationToken cancellationToken)
    {
        var result = await kernel.ExecuteTopicSubCheckAsync(SubCheckHeatLabelWhenData, GetHeatLabelIfDataRequirementCheckPrompt(claim), cancellationToken);
        result.Reasoning = $"[{SubCheckHeatLabelWhenData}] " + result.Reasoning;
        result.Warning = string.IsNullOrWhiteSpace(result.Warning) ? string.Empty : $"[{SubCheckHeatLabelWhenData}] " + result.Warning;
        return result;
    }
    #endregion

    private static string GetHeatRequirementCheckPrompt(SustainabilityClaim claim)
    {
        return $"""
            System instruction:
            You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.

            This claim relates to district heating (heat networks). 
            Evaluate whether the claim complies with the following requirements derived from Vattenfall’s handbook and ACM rules.
            
            [GENERAL COMPLIANCE RULES]
            {SharedPromptConstants.CopyHandboekRules}
            
            [BEGIN REQUIREMENTS]
            1. State current reality: Not all heat sources are yet sustainable; full sustainability is an ambition for 2040.
            2. Do not describe district heating as “natural gas-free” or imply all sources are sustainable.
            3. Ambitions including phrases such as “by 2040 only sustainable sources” must be clearly described as future goals, not current achievements.
            [END REQUIREMENTS]

            [CLAIM TO EVALUATE]
            "{claim.ClaimText}"

            [CLAIM RELATED TEXT]
            Broad context or additional information related to the claim:
            "{claim.RelatedText}"

            [CLAIM RELATED URLS]
            Here is a list of URLs which are related to the claim and live on the same page:
            "{string.Join(", ", claim.RelatedUrls)}"
            """;
    }

    private static string GetHeatLabelIfDataRequirementCheckPrompt(SustainabilityClaim claim)
    {
        return $"""
            System instruction:
                    You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.
        Your task is to evaluate a given claim, and check for non compliant uses of the heat label.
        You need to evaluate whether the claim requires the heat label to be present, depending on the kind of data presented in the claim.
        If the claim presents data about the sources of heat or cooling (e.g. percentages of heat from different sources), the heat label must be present in the RELATED URLS section.
        For info, the heat label can be found at: {HeatLabelUrl}

        STEP 1: Determine claim type:
        - Type A (generation process): Claims about how heat is generated or sourced (e.g. "biomassa", "industrie restwarmte", "afvalverbranding") in a technical/infrastructure context.
        - Type B (product delivery): Claims about heat delivered as a product to end customers.
        The warmte-etiket link requirement ONLY applies to Type B claims. If the claim is Type A (about generation/sourcing), mark as compliant.
            
            [CLAIM TO EVALUATE]
            "{claim.ClaimText}"

            [CLAIM RELATED TEXT]
            Broad context or additional information related to the claim:
            "{claim.RelatedText}"

            [CLAIM RELATED URLS]
            Here is a list of URLs which are related to the claim and live on the same page:
            "{string.Join(", ", claim.RelatedUrls)}"
            """;
    }
}
