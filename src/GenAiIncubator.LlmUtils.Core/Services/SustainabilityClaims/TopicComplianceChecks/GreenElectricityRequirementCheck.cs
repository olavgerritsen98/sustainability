using System.Text.Json;
using DocumentFormat.OpenXml.Drawing;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks.Constants;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

/// <summary>
/// Implementation of the green electricity topic-specific requirement.
/// </summary>
public class GreenElectricityRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
{
    private const string SubCheckGroenUitNlProduct = "GroenUitNlProduct";
    private const string SubCheckGroenUitNlSources = "GroenUitNlSources";
    private const string SubCheckEnergyLabel = "EnergyLabel";
    private const string SubCheckBenefitScope = "BenefitScope";
    private const string SubCheckMixedElectricity = "MixedElectricity"; 

    /// <inheritdoc />
    public override RequirementCode AssociatedRequirementCode => RequirementCode.GreenElectricity;

    /// <inheritdoc />
    public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.GreenElectricity;


    /// <summary>
    /// Consumer stroometiket URL
    /// </summary>
    public static readonly string ElectricityConsumerLabelUrl = "www.vattenfall.nl/stroom/stroometiket/";

    /// <summary>
    /// Business stroometiket URL
    /// </summary>
    public static readonly string ElectricityBusinessLabelUrl = "www.vattenfall.nl/grootzakelijk/zakelijke-stroom-gas/stroometiket/";

    /// <summary>
    /// Checks the green electricity requirement.
    /// </summary>
    /// <param name="claim"></param>
    /// <param name="sourceContent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A list of compliancy evaluations </returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected override async Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken = default)
    {
        return
            [
                await RunGroenUitNlProductCheckAsync(claim, cancellationToken),
                await RunGroenUitNlSourcesCheckAsync(claim, sourceContent, cancellationToken),
                await RunEnergyLabelCheckAsync(claim, sourceContent, cancellationToken),
                await RunBenefitScopeCheckAsync(claim, sourceContent, cancellationToken),
                await RunMixedElectricityCheckAsync(claim, sourceContent, cancellationToken),
            ];
    }

    private async Task<TopicComplianceEvaluationResponse> RunEnergyLabelCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        if (claim.SourceUrl.Contains(ElectricityConsumerLabelUrl, StringComparison.OrdinalIgnoreCase) ||
            claim.SourceUrl.Contains(ElectricityBusinessLabelUrl, StringComparison.OrdinalIgnoreCase))
            return new TopicComplianceEvaluationResponse { IsCompliant = true, Reasoning = "The source URL includes the stroometiket." };

        if (claim.RelatedUrls.Any(url => url.Contains(ElectricityConsumerLabelUrl, StringComparison.OrdinalIgnoreCase)) ||
            claim.RelatedUrls.Any(url => url.Contains(ElectricityBusinessLabelUrl, StringComparison.OrdinalIgnoreCase)))
            return new TopicComplianceEvaluationResponse { IsCompliant = true, Reasoning = "The claim includes a link to the stroometiket." };

        TopicComplianceEvaluationResponse isLabelInPage = await kernel.ExecuteTopicSubCheckAsync(SubCheckEnergyLabel, GetHasEnergyLabelInPagePrompt(claim, sourceContent), cancellationToken);
        
        // If the LLM determines the rule doesn't apply (IsCompliant=true), we return no warning/link.
        if (isLabelInPage.IsCompliant)
            return new TopicComplianceEvaluationResponse
            {
                IsCompliant = true,
                Reasoning = $"[{SubCheckEnergyLabel}] " + isLabelInPage.Reasoning,
                Warning = string.Empty,
                SuggestedAlternative = string.Empty
            };

        return isLabelInPage;
    }

    private async Task<TopicComplianceEvaluationResponse> RunGroenUitNlProductCheckAsync(SustainabilityClaim claim, CancellationToken cancellationToken)
    {
        TopicComplianceEvaluationResponse result = await kernel.ExecuteTopicSubCheckAsync(SubCheckGroenUitNlProduct, GetGroenUitNlProductPrompt(claim), cancellationToken);
        result.Reasoning = $"[{SubCheckGroenUitNlProduct}] " + result.Reasoning;
        result.Warning = $"[{SubCheckGroenUitNlProduct}] " + result.Warning;
        return result;
    }

    private async Task<TopicComplianceEvaluationResponse> RunGroenUitNlSourcesCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        TopicComplianceEvaluationResponse claimLevel = await kernel.ExecuteTopicSubCheckAsync(SubCheckGroenUitNlSources, GetGreenElecSourcesPrompt(claim), cancellationToken);
        if (claimLevel.IsCompliant) return claimLevel;

        TopicComplianceEvaluationResponse pageLevel = await kernel.ExecuteTopicSubCheckAsync(SubCheckGroenUitNlSources, GetGroenUitNlSourcesPagePrompt(claim, sourceContent), cancellationToken);
        if (pageLevel.IsCompliant)
            return new TopicComplianceEvaluationResponse { IsCompliant = true, Reasoning = $"[{SubCheckGroenUitNlSources}] Sources indicated elsewhere on page." };

        pageLevel.Reasoning = $"[{SubCheckGroenUitNlSources}] Sources not clearly listed. {pageLevel.Reasoning}";
        return pageLevel;
    }

    private async Task<TopicComplianceEvaluationResponse> RunBenefitScopeCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
                var result = await kernel.ExecuteTopicSubCheckAsync(SubCheckBenefitScope, GetBenefitScopeRequirementCheckPrompt(claim, sourceContent), cancellationToken);
        result.Reasoning = $"[{SubCheckBenefitScope}] " + result.Reasoning;
        result.Warning = string.IsNullOrWhiteSpace(result.Warning) ? string.Empty : $"[{SubCheckBenefitScope}] " + result.Warning;
        return result;
    }

    private async Task<TopicComplianceEvaluationResponse> RunMixedElectricityCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
                var result = await kernel.ExecuteTopicSubCheckAsync(SubCheckMixedElectricity, GetMixedElectricityRequirementCheckPrompt(claim, sourceContent), cancellationToken);
        result.Reasoning = $"[{SubCheckMixedElectricity}] " + result.Reasoning;
        result.Warning = string.IsNullOrWhiteSpace(result.Warning) ? string.Empty : $"[{SubCheckMixedElectricity}] " + result.Warning;
        return result;
    }

    private static string GetGroenUitNlProductPrompt(SustainabilityClaim claim)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.
        Requirement: For the green electricity product GroenUitNL, the fact that it is a product should always be clear.
        If no mention of "Groen uit Nederland", no violation.
        Whenever Groen uit Nederland is mentioned, it must be clear this refers to a product. 
        The word “product” is preferred, but clear context (buying/subscribing) is acceptable.
                IMPORTANT: The acceptable example below uses 'vermeld als' which implies it IS a named product/tariff, which is sufficient. However, if 'Groen uit Nederland' is used as if it is simply a description (e.g., 'our electricity comes from the Netherlands and is green'), that is non-compliant. The product nature must be inferable from context (ordering, subscribing, named tariff).
                        If "Groen uit Nederland" is used as a geographic or origin description (not as a capitalized product name), this check does not apply. Only flag when the capitalized product name "Groen uit Nederland" is mentioned without clear product context (the word "product" or clear buying/subscribing context).
        
        Example: "Zakelijk FlexPrijsStroom is vermeld als ‘Groen uit Nederland’. Bekijk ons stroometiket." is acceptable.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TO EVALUATE]
        "{claim.RelatedText}"
        [END CLAIM TO EVALUATE]

        Task:
        - Evaluate against requirement.
        - If non-compliant, suggest a compliant alternative.
        """;
    }

    private static string GetGreenElecSourcesPrompt(SustainabilityClaim claim)
    {
        return $"""
        [Instructions]
        Requirement: For green electricity products, all sources (wind, water, sun) must be included in the claim.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TO EVALUATE]
        "{claim.RelatedText}"
        [END CLAIM TO EVALUATE]

        Task:
        - Evaluate against requirements. If non-compliant, suggest alternative.
        """;
    }

    private static string GetGroenUitNlSourcesPagePrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        Check broader context to determine if the page names the sources (wind, water, sun) elsewhere.
        If the broader context fulfill these criteria, treat as compliant.
       
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TO EVALUATE]
        "{claim.RelatedText}"
        [END CLAIM TO EVALUATE]

        [BEGIN BROADER CONTEXT]
        "{sourceContent}"
        [END BROADER CONTEXT]

        Task:
        - Determine if sources are named in context.
        """;
    }

    private static string GetHasEnergyLabelInPagePrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst.
        
        [STRICT LOGIC STEP: CATEGORIZE THE CLAIM]
        1. TYPE A (General/Educational): Claim is about technology (e-boilers), general electrification, or beliefs. 
           Example: "We believe electrification is essential" or "E-boilers are a sustainable way to make steam."
           -> Stroometiket rule DOES NOT apply. Return IsCompliant = true. Do NOT suggest a link.

        2. TYPE B (Product/Supply): Claim is about a specific contract, price model, or act of delivery.
           Example: "Our Supply & Steering model includes delivery of green electricity."
           -> Stroometiket rule applies. Proceed to check for link in context.

        [IMPORTANT]
        Even if the page context mentions that Vattenfall *can* supply electricity, if the SPECIFIC claim text is TYPE A (General), you MUST NOT suggest a stroometiket link in the alternative.

        [Example of a Type A Claim (No Link Needed)]
        Claim: "Wij geloven dat elektrificatie een essentieel onderdeel is van een fossielvrije industrie. Het plaatsen van een e-boiler is een slimme en duurzame manier om stoom of warmte te produceren met hernieuwbare elektriciteit in plaats van aardgas."
        Correct Action: Mark as compliant. Reasoning: "This is a general educational claim about technology, not a specific supply offer."
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TO EVALUATE]
        "{claim.RelatedText}"
        [END CLAIM TO EVALUATE]

        [BEGIN BROADER CONTEXT]
        "{sourceContent}"
        [END BROADER CONTEXT]

        Task:
        - Categorize the claim.
        - If Type A: Return IsCompliant=true.
        - If Type B: Check if {ElectricityConsumerLabelUrl} or {ElectricityBusinessLabelUrl} is in context.
        """;
    }

    private static string GetMixedElectricityRequirementCheckPrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        Requirement: Communications must clarify that the grid contains a mix of green and grey electricity unless the offering is exclusively green.

        [STRICT GUARDRAIL: NO LINK FOR GENERAL CLAIMS]
        If the claim is Categorized as General (Type A):
        - DO NOT suggest adding a stroometiket link.
        - DO suggest text-based clarification (e.g., "The grid is a mix of green and grey").

        Example for General Claim:
        Suggested alternative: "...De e-boiler maakt gebruik van elektriciteit uit het net, wat een mix is van groene en grijze stroom." (NO LINK).
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TO EVALUATE]
        {claim.RelatedText}
        [END CLAIM TO EVALUATE]

        [BEGIN BROADER CONTEXT]
        {sourceContent}
        [END BROADER CONTEXT]

        Task:
        - Evaluate clarity about coexistence.
        - If non-compliant, suggest an alternative following the guardrail above.
        """;
    }

    private static string GetBenefitScopeRequirementCheckPrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        Requirement: Clarify whether the sustainability benefit applies to the whole company or just a part (e.g. the specific e-boiler product).
                A claim is non-compliant if it implies a broad sustainability benefit (e.g., "all electricity is green", "our electricity is fossil-free") without making clear this only applies to the specific product or tariff being sold. For example, saying 'GroenUitNL electricity' is green implies the entire electricity supply is green, which is misleading unless explicitly scoped to only the specific GroenUitNL product/tariff. The claim should specify that the sustainability applies to the specific product chosen, not to all electricity.
                        Non-compliant examples: "Met GroenUitNL krijg je groene stroom" (implies all electricity is green without scoping to the product).
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}
        
        [STRICT GUARDRAIL]
        This check is strictly about the "Scope" of the claim.
        If the claim is General (Type A), do NOT suggest adding a stroometiket link. Focus only on clarifying that the benefit applies to the specific process/product mentioned.
                If the claim already contains an explicit scope limiter (e.g. a named organizational unit like "thuis en mkb", "Consumenten", a specific product name, or a customer segment), the scope requirement is satisfied. Only flag when the claim reads as a company-wide absolute with no qualifier at all.

        [BEGIN CLAIM TO EVALUATE]
        {claim.RelatedText}
        [END CLAIM TO EVALUATE]

        [BEGIN BROADER CONTEXT]
        {sourceContent}
        [END BROADER CONTEXT]

        Task:
        - Evaluate clarity about benefit scope.
        - If non-compliant, suggest a compliant alternative.
        """;
    }
}
