// using System.Text.Json;
// using GenAiIncubator.LlmUtils.Core.Enums;
// using GenAiIncubator.LlmUtils.Core.Helpers;
// using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
// using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
// using Microsoft.SemanticKernel;

// namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

// /// <summary>
// /// Implementation of the sustainable production topic-specific requirement.
// /// </summary>
// public class SustainableProductionRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
// {
//     /// <inheritdoc />
//     public override RequirementCode AssociatedRequirementCode => RequirementCode.SustainableProduction;

//     /// <inheritdoc />
//     public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.SustainableProduction;


//     /// <summary>
//     /// Checks the sustainable production requirement.
//     /// </summary>
//     /// <param name="claim"></param>
//     /// <param name="cancellationToken"></param>
//     /// <returns></returns>
//     /// <exception cref="InvalidOperationException"></exception>
//     protected override async Task<TopicComplianceEvaluationResponse> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent = "", CancellationToken cancellationToken = default)
//     {
//         string SustainableProductionRequirementCheckPrompt = GetSustainableProductionRequirementCheckPrompt(claim);

//         string result = await ChatHelpers.ExecutePromptAsync(
//             kernel,
//             SustainableProductionRequirementCheckPrompt,
//             typeof(TopicComplianceEvaluationResponse),
//             cancellationToken: cancellationToken
//         );

//         TopicComplianceEvaluationResponse response =
//             JsonSerializer.Deserialize<TopicComplianceEvaluationResponse>(result) ??
//             throw new InvalidOperationException($"Failed to parse sustainable production requirement check response for claim: {claim.ClaimText}");

//         return response;
//     }

//     private static string GetSustainableProductionRequirementCheckPrompt(SustainabilityClaim claim)
//     {
//         return $"""
//             System instruction:
//             You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.

//             This claim relates to sustainable production of electricity.
//             Evaluate whether the claim complies with the following requirements derived from Vattenfall’s handbook and ACM rules.

//             [BEGIN REQUIREMENTS]
//             1. Accuracy and specificity (PROD-1)
//             • The claim must correctly identify the type(s) of renewable generation (solar, wind, hydro).  
//             • It must not imply that all Vattenfall production is renewable.  
//             • It must describe what “sustainable production” means (e.g., electricity from renewable sources such as wind, water, and sun).

//             2. Geographical and source transparency (PROD-3)
//             • The claim must specify where and how the electricity is produced 
//                 (e.g., “Dutch wind turbines,” “hydro plants in Sweden”).  
//             • It must clarify whether it refers to specific plants, the Netherlands, or Vattenfall’s overall portfolio.
//             • A link to the stroometiket or equivalent documentation should be provided for verification.
            

//             3. Absolute or unprovable claims (PROD-4)
//             • Avoid absolute terms such as “100% sustainable,” “completely green,” or “fully renewable,” unless independent certification proves this for all energy concerned (e.g. energy label).
//             • Avoid comparative wording suggesting other providers are less sustainable.
//             [END REQUIREMENTS]

//             [CLAIM TO EVALUATE]
//             "{claim.ClaimText}"

//             [CLAIM RELATED URLS]
//             "{string.Join(", ", claim.RelatedUrls)}"
//             """;
//     }
// }