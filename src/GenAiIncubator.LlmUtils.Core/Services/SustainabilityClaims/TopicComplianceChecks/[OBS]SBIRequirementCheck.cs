// using System.Text.Json;
// using GenAiIncubator.LlmUtils.Core.Enums;
// using GenAiIncubator.LlmUtils.Core.Helpers;
// using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
// using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
// using Microsoft.SemanticKernel;

// namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

// /// <summary>
// /// Implementation of the Sustainable Brand Index (SBI) topic-specific requirement.
// /// </summary>
// public class SBIRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
// {
//     /// <inheritdoc />
//     public override RequirementCode AssociatedRequirementCode => RequirementCode.SBI;

//     /// <inheritdoc />
//     public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.SBI;


//     /// <summary>
//     /// Checks the SBI requirement.
//     /// </summary>
//     /// <param name="claim"></param>
//     /// <param name="cancellationToken"></param>
//     /// <returns></returns>
//     /// <exception cref="InvalidOperationException"></exception>
//     protected override async Task<TopicComplianceEvaluationResponse> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent = "", CancellationToken cancellationToken = default)
//     {
//         string SBIRequirementCheckPrompt = GetSBIRequirementCheckPrompt(claim);

//         string result = await ChatHelpers.ExecutePromptAsync(
//             kernel,
//             SBIRequirementCheckPrompt,
//             typeof(TopicComplianceEvaluationResponse),
//             cancellationToken: cancellationToken
//         );

//         TopicComplianceEvaluationResponse response =
//             JsonSerializer.Deserialize<TopicComplianceEvaluationResponse>(result) ??
//             throw new InvalidOperationException($"Failed to parse sustainable production requirement check response for claim: {claim.ClaimText}");

//         return response;
//     }

//     private static string GetSBIRequirementCheckPrompt(SustainabilityClaim claim)
//     {
//         return $"""
//             System instruction:
//             You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.

//             This claim mentions a Sustainable Brand Index (SBI).
//             Evaluate whether the claim complies with the following requirements derived from Vattenfall’s handbook and ACM rules.

//             [BEGIN REQUIREMENTS]
//             1. Context and framing (SBI-1)
//             • When referencing the Sustainable Brand Index (SBI), the text must clearly state that:
//                 - SBI measures **consumer perception** of sustainability, not actual performance.
//                 - The reference includes **the year**, **country (Netherlands)**, and **sector (e.g., energy)**.
//             • Example ❌ “Vattenfall is the most sustainable energy company.” → misleading; presents SBI as fact.
//                 ✅ “According to the Sustainable Brand Index Netherlands 2024 – a survey on consumer perception – Vattenfall ranks among the most sustainable energy brands.”

//             2. No factual or superiority claims (SBI-2)
//             • The claim may not imply that SBI reflects factual sustainability performance, leadership, or environmental results.
//             • Avoid wording like “proves,” “shows we are the greenest,” or “most sustainable in reality.”
//             • Example ❌ “The Sustainable Brand Index shows that Vattenfall is the greenest energy provider.”
//                 ✅ “The Sustainable Brand Index reflects how consumers perceive Vattenfall’s sustainability efforts.”

//             3. Source and transparency (SBI-3)
//             • The claim must cite the **official SBI source** (full name: “Sustainable Brand Index Netherlands [year]”)
//                 and include a **direct link** (within one click) to the official report or website.
//             • Example ❌ “Recognized by SBI for sustainability.” → no link or proper naming.
//                 ✅ “See the Sustainable Brand Index Netherlands 2024 results (sustainablebrandindex.com/nl).”

//             4. Accuracy and recency (SBI-4)
//             • Only current and verifiable SBI results may be used.
//             • Ranking or position must match the official SBI data for the stated year.
//             • Example ❌ “Ranked #1 in the 2022 SBI” (in 2024 communication) → outdated and misleading.
//                 ✅ “In the 2024 Sustainable Brand Index Netherlands, Vattenfall ranked second in the energy sector.”
//             [END REQUIREMENTS]

//             [CLAIM TO EVALUATE]
//             "{claim.ClaimText}"

//             [CLAIM RELATED URLS]
//             "{string.Join(", ", claim.RelatedUrls)}"
//             """;
//     }
// }