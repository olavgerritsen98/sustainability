// // UNUSED FOR NOW 



// using System.Text.Json;
// using GenAiIncubator.LlmUtils.Core.Enums;
// using GenAiIncubator.LlmUtils.Core.Helpers;
// using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
// using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
// using Microsoft.SemanticKernel;

// namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

// /// <summary>
// /// Implementation of the CO2 neutral topic-specific requirement.
// /// </summary>
// public class CO2neutralReductionCompensatedFreeRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
// {
//     /// <inheritdoc />
//     public override RequirementCode AssociatedRequirementCode => RequirementCode.CO2neutralReductionCompensatedFree;

//     /// <inheritdoc />
//     public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.CO2neutralReductionCompensatedFree;


//     /// <summary>
//     /// Checks the sustainable production requirement.
//     /// </summary>
//     /// <param name="claim"></param>
//     /// <param name="sourceContent">Initial source content</param>
//     /// <param name="cancellationToken"></param>
//     /// <returns></returns>
//     /// <exception cref="InvalidOperationException"></exception>
//     protected override async Task<TopicComplianceEvaluationResponse> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent = "", CancellationToken cancellationToken = default)
//     {
//         string CO2neutralReductionCompensatedFreeRequirementCheckPrompt = GetCO2neutralReductionCompensatedFreeRequirementCheckPrompt(claim);

//         string result = await ChatHelpers.ExecutePromptAsync(
//             kernel,
//             CO2neutralReductionCompensatedFreeRequirementCheckPrompt,
//             typeof(TopicComplianceEvaluationResponse),
//             cancellationToken: cancellationToken
//         );

//         TopicComplianceEvaluationResponse response =
//             JsonSerializer.Deserialize<TopicComplianceEvaluationResponse>(result) ??
//             throw new InvalidOperationException($"Failed to parse sustainable production requirement check response for claim: {claim.ClaimText}");

//         return response;
//     }

//     // TODO: remove substanciation and explain that stroometiket is enough 
//     private static string GetCO2neutralReductionCompensatedFreeRequirementCheckPrompt(SustainabilityClaim claim)
//     {
//         return $"""
//             System instruction:
//             You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.

//             This claim relates to the wording around CO₂ neutrality, reduction, and compensation.
//             Evaluate whether the claim complies with the following requirements derived from Vattenfall’s handbook and ACM rules.

//             [BEGIN REQUIREMENTS]
//             1. Define mechanism (CO2-1)
//             • The claim must explain how CO₂ neutrality or reduction is achieved
//                 (e.g., through emission reduction and/or compensation).
//             • It may not present neutrality as inherent or undefined (“CO₂-neutral electricity” without context).
//             • Example ❌ “Green gas is CO₂-neutral.” → misleading, because burning still emits CO₂.
//                 ✅ “The CO₂ emitted was previously absorbed by organic materials, so no net increase occurs.”

//             2. Quantitative accuracy (CO2-2)
//             • If numeric reduction data are stated, the claim must specify the baseline year,
//                 scope (e.g., scope 1–3), and calculation basis.
//             • Avoid vague wording like “much less CO₂” without figures or reference.
//             • Example ❌ “District heating is CO₂-free.” → misleading, as gas is still partly used.
//                 ✅ “On average, district heating in the Netherlands emits ≈ 60 % less greenhouse gases than individual gas boilers.” (Source: Milieu Centraal)

//             3. Compensation transparency (CO2-3)
//             • If offsets or compensation are used, the claim must:
//                 - Specify how much CO₂ is compensated.
//                 - Explain which projects are used and how they work.
//                 - Identify the certification standard (e.g., Gold Standard, VCS) and independent verification.
//                 - Provide proof (e.g., link to report or certificate).
//                 - Clarify that emissions still occur but are compensated afterward.
//             • Example ❌ “Electricity from wind turbines is 100 % CO₂-free.” → misleading, since construction/maintenance cause emissions.
//                 ✅ “Electricity from wind turbines emits significantly less CO₂ over its lifecycle than fossil-fuel electricity.”

//             4. No absolute “free” wording (CO2-4)
//             • Do not use absolute terms such as “CO₂-free”, “zero-emission”, or “fully climate-neutral”
//                 unless literally zero emissions across the lifecycle can be proven.
//             • Instead use “CO₂-neutral” or “CO₂-compensated” with explanation.

//             5. Public substantiation (CO2-5)
//             • The claim or its immediate context must include a link (within one click)
//                 to public, up-to-date proof—e.g., CO₂ calculation, Sustainability Report,
//                 or certified offset documentation.
//             • Missing or outdated evidence → FAIL.
//             [END REQUIREMENTS]

//             [CLAIM TO EVALUATE]
//             "{claim.ClaimText}"

//             [CLAIM RELATED URLS]
//             "{string.Join(", ", claim.RelatedUrls)}"
//             """;
//     }
// }