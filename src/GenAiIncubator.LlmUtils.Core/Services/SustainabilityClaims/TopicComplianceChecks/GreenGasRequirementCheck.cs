using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks.Constants;
using GenAiIncubator.LlmUtils.Core.Services.WebContentNormalization;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

/// <summary>
/// Implementation of the green gas topic-specific requirement.
/// </summary>
public class GreenGasRequirementCheck(Kernel kernel, IWebContentNormalizationService webContentNormalizationService) : TopicSpecificRequirementCheck
{
    /// <inheritdoc />
    public override RequirementCode AssociatedRequirementCode => RequirementCode.GreenGas;

    /// <inheritdoc />
    public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.GreenGas;

    /// <summary>
    /// Checks the green gas topic-specific requirement.
    /// </summary>
    /// <param name="claim"></param>
    /// <param name="sourceContent">Broader page/context content that may provide required clarifications.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent = "", CancellationToken cancellationToken = default)
    {
        return [
            await RunEmissionsClarificationCheckAsync(claim, sourceContent, cancellationToken),
            await RunFossilFreeTerminologyCheckAsync(claim, sourceContent, cancellationToken),
            await RunCertificationVertiCerCheckAsync(claim, sourceContent, cancellationToken),
            await RunGreenCompositionCheckAsync(claim, sourceContent, cancellationToken)
        ];
    }


    private const string SubCheckEmissionsClarification = "EmissionsClarification";
    private const string SubCheckEmissionsClarificationPage = "EmissionsClarificationPage";
    private const string SubCheckFossilFreeTerminology = "FossilFreeTerminology";
    private const string SubCheckCertificationVertiCer = "CertificationVertiCer";
    private const string SubCheckCertificationVertiCerPage = "CertificationVertiCerPage";
    private const string SubCheckGreenComposition = "GreenComposition";

    private async Task<TopicComplianceEvaluationResponse> RunEmissionsClarificationCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        // Claim-level evaluation first
        TopicComplianceEvaluationResponse claimResult = await kernel.ExecuteTopicSubCheckAsync(
            SubCheckEmissionsClarification,
            GetEmissionsClarificationPrompt(claim),
            cancellationToken);
        claimResult.Reasoning = $"[{SubCheckEmissionsClarification}] " + claimResult.Reasoning;
        if (claimResult.IsCompliant)
            return claimResult;

        // Page-level fallback if claim text itself lacks clarification
        TopicComplianceEvaluationResponse pageResult = await RunEmissionsClarificationPageCheckAsync(claim, sourceContent, cancellationToken);
        if (pageResult.IsCompliant)
        {
            return new TopicComplianceEvaluationResponse
            {
                IsCompliant = true,
                Reasoning = $"[{SubCheckEmissionsClarificationPage}] Claim lacks explicit emissions clarification, but broader page context supplies it. {pageResult.Reasoning}",
                Warning = $"[{SubCheckEmissionsClarificationPage}] Emissions clarification absent in claim text; relying on broader page context.",
                SuggestedAlternative = string.Empty
            };
        }

        // Combine both reasonings for richer feedback when still non-compliant
        return new TopicComplianceEvaluationResponse
        {
            IsCompliant = false,
            Reasoning = claimResult.Reasoning + " " + pageResult.Reasoning,
            Warning = $"[{SubCheckEmissionsClarification}] Emissions clarification missing in claim and broader page context.",
            SuggestedAlternative = string.IsNullOrWhiteSpace(pageResult.SuggestedAlternative) ? claimResult.SuggestedAlternative : pageResult.SuggestedAlternative
        };
    }

    private static string GetEmissionsClarificationPrompt(SustainabilityClaim claim)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst for sustainability claims.
        Requirement: Communications about green gas must clarify that green gas still causes CO₂ emissions (is not CO₂-neutral) – it only reduces or avoids additional emissions compared to fossil natural gas. Over-claiming neutrality (e.g. "CO₂-neutral") is non-compliant.
        Treat as compliant if either the claim itself explicitly states continued emissions (e.g. "CO₂ is still released" / "there are still CO₂ emissions") OR avoids neutrality wording.
        Non-compliant if claim suggests or states CO₂-neutral / zero emissions without clarification.

        [BEGIN CLAIM]
        {claim.ClaimText}
        [END CLAIM]

        [BEGIN RELATED TEXT]
        {claim.RelatedText}
        [END RELATED TEXT]

        Task:
        - Determine compliance with emissions clarification requirement.
        - If non-compliant, include a SuggestedAlternative that rewrites the claim to add a concise clarification (e.g. "Er komt nog steeds CO₂ vrij, maar...").
        """;
    }

    private async Task<TopicComplianceEvaluationResponse> RunEmissionsClarificationPageCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        TopicComplianceEvaluationResponse pageResult = await kernel.ExecuteTopicSubCheckAsync(
            SubCheckEmissionsClarificationPage,
            GetEmissionsClarificationPagePrompt(claim, sourceContent),
            cancellationToken);
        pageResult.Reasoning = $"[{SubCheckEmissionsClarificationPage}] " + pageResult.Reasoning;
        return pageResult;
    }

    private static string GetEmissionsClarificationPagePrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst.
        The claim text did not clearly state that CO₂ emissions still occur when using/burning green gas. We now examine the broader page/context to see if this clarification appears elsewhere.

        Compliance if broader context contains:
          • Explicit statement that CO₂ (or greenhouse gases) are still emitted when using green gas; OR
          • Phrase clarifying reduction vs elimination (e.g. "vermindert de uitstoot", "er komt nog steeds CO₂ vrij"); OR
          • A sentence distinguishing green gas from being fully CO₂-neutral.
        Non-compliant if no such clarification appears anywhere.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}
        
        [BEGIN CLAIM]
        {claim.RelatedText}
        [END CLAIM]

        [BEGIN CONTEXT]
        {sourceContent}
        [END CONTEXT]

        Task:
        - Determine if broader context supplies the missing emissions clarification.
        - If still non-compliant, craft SuggestedAlternative that adds a concise clarification sentence.
        """;
    }

    private async Task<TopicComplianceEvaluationResponse> RunFossilFreeTerminologyCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        TopicComplianceEvaluationResponse result = await kernel.ExecuteTopicSubCheckAsync(
            SubCheckFossilFreeTerminology,
            GetFossilFreeTerminologyPrompt(claim, sourceContent),
            cancellationToken);
        result.Reasoning = $"[{SubCheckFossilFreeTerminology}] " + result.Reasoning;
        result.Warning = $"[{SubCheckFossilFreeTerminology}] " + result.Warning;
        return result;
    }

    private static string GetFossilFreeTerminologyPrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst.
        Requirement: Usage of the term "fossil-free" (or Dutch equivalents like "fossielvrij") in relation to green gas must be immediately accompanied by an explanation that CO₂ emissions still occur when burning green gas. If the term is used without clarification nearby (claim text itself or immediate surrounding context), it is non-compliant.
        Treat as compliant if:
          • Claim does not use fossil-free terminology at all (then no obligation).
          • Claim uses fossil-free AND explicitly states emissions still occur (e.g. "Er komt nog steeds CO₂ vrij" / "CO₂ wordt nog uitgestoten" / "There are still CO₂ emissions").
        Non-compliant if fossil-free wording is present with no clarification in claim.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM]
        {claim.ClaimText}
        [END CLAIM]

        [BEGIN RELATED TEXT]
        {claim.RelatedText}
        [END RELATED TEXT]

        Task:
        - Assess compliance for fossil-free terminology usage.
        - Return TopicComplianceEvaluationResponse JSON. If non-compliant, suggest alternative by appending a concise clarification (e.g. "Er komt nog steeds CO₂ vrij, maar...").
        """;
    }

    private async Task<TopicComplianceEvaluationResponse> RunCertificationVertiCerCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        // If VertiCer URL present in related URLs, auto-compliant.
        if (claim.RelatedUrls.Any(u => u.Contains("verticer", StringComparison.OrdinalIgnoreCase)))
        {
            return new TopicComplianceEvaluationResponse
            {
                IsCompliant = true,
                Reasoning = $"[{SubCheckCertificationVertiCer}] VertiCer certification URL present in related URLs.",
                Warning = string.Empty,
                SuggestedAlternative = string.Empty
            };
        }

        TopicComplianceEvaluationResponse result = await kernel.ExecuteTopicSubCheckAsync(
            SubCheckCertificationVertiCer,
            GetCertificationVertiCerPrompt(claim, sourceContent),
            cancellationToken);
        if (result.IsCompliant)
            return result;

        TopicComplianceEvaluationResponse pageResult = await RunCertificationVertiCerPageCheckAsync(claim, sourceContent, cancellationToken);
        if (pageResult.IsCompliant)
        {
            return new TopicComplianceEvaluationResponse
            {
                IsCompliant = true,
                Reasoning = $"[{SubCheckCertificationVertiCerPage}] Claim itself lacks VertiCer/GvO certification explanation, but broader page context provides it. {pageResult.Reasoning}",
                Warning = $"[{SubCheckCertificationVertiCerPage}] Certification explanation missing in claim text, but broader page context provides it.",
                SuggestedAlternative = string.Empty
            };
        }

        bool relatedUrlsContentContainGvo = await CheckUrlsForGvoMentionAsync(claim.RelatedUrls, cancellationToken);
        if (relatedUrlsContentContainGvo)
        {
            return new TopicComplianceEvaluationResponse
            {
                IsCompliant = true,
                Reasoning = $"[{SubCheckCertificationVertiCer}] VertiCer/GvO certification explanation found in related URLs content.",
                Warning = $"[{SubCheckCertificationVertiCer}] Certification explanation not in claim or page context, but found in related URLs content.",
                SuggestedAlternative = string.Empty
            };
        }

        // Combine reasoning from both attempts for richer feedback.
        return new TopicComplianceEvaluationResponse
        {
            IsCompliant = false,
            Reasoning = result.Reasoning + " " + pageResult.Reasoning,
            Warning = $"[{SubCheckCertificationVertiCer}] Certification explanation not found in claim, broader page context nor related URLs content.",
            SuggestedAlternative = pageResult.SuggestedAlternative
        };
    }

    private async Task<bool> CheckUrlsForGvoMentionAsync(List<string> urls, CancellationToken cancellationToken)
    {
        var gvoTerms = new[] { "gvo", "garantie van oorsprong", "garanties van oorsprong", "guarantee of origin", "guarantees of origin", "verticer" };
        async Task<bool> check(string content, CancellationToken ct) => gvoTerms.Any(content.ToLowerInvariant().Contains);
        foreach (var url in urls)
            if (await CheckUrl(url, check, cancellationToken))
                return true;
        return false;
    }

    private async Task<bool> CheckUrl(string url, Func<string, CancellationToken, Task<bool>> requirementCheck, CancellationToken cancellationToken)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var absolute) ||
                (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps))
                return false;

            if (absolute == null)
                return false;

            var targetUrl = absolute.ToString();
            string content = await webContentNormalizationService.FetchAndNormalizeAsync(targetUrl, cancellationToken);

            if (string.IsNullOrEmpty(content)) 
                return false;

            if (await requirementCheck(content, cancellationToken))
                return true;
        }
        catch
        {
            // Ignore fetch/parse errors and continue to next URL
        }
        return false;
    }

    private static string GetCertificationVertiCerPrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst.
        Requirement: Communications about green gas must explain that it is certified via Guarantees of Origin (GvOs) from VertiCer.
        Treat as compliant if claim text OR provided context mentions VertiCer, VertiCer certification, GvO(s), Guarantees of Origin related to green gas.
        Non-compliant if no such certification explanation appears.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM]
        {claim.ClaimText}
        [END CLAIM]

        [BEGIN RELATED TEXT]
        {claim.RelatedText}
        [END RELATED TEXT]

        [BEGIN CONTEXT]
        {sourceContent}
        [END CONTEXT]

        Task:
        - Assess compliance with certification explanation requirement.
        - Return TopicComplianceEvaluationResponse JSON. If non-compliant, propose SuggestedAlternative that adds a concise certification sentence referencing VertiCer GvOs.
        """;
    }

    private async Task<TopicComplianceEvaluationResponse> RunCertificationVertiCerPageCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        TopicComplianceEvaluationResponse pageResult = await kernel.ExecuteTopicSubCheckAsync(
            SubCheckCertificationVertiCerPage,
            GetCertificationVertiCerPagePrompt(claim, sourceContent),
            cancellationToken);
        return pageResult;
    }

    private static string GetCertificationVertiCerPagePrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst.
        The claim text did not clearly mention VertiCer certification. We now examine the broader page/context to see if VertiCer or GvOs (Garanties van Oorsprong) are explained elsewhere.
        Compliance if the page context (sourceContent) contains:
          • Mention of VertiCer; OR
          • Explanation of Guarantees of Origin (GvO / GvOs) related to green gas certification; OR
          • A sentence that explicitly ties green gas to certified origin via VertiCer.
        Non-compliant if no such evidence appears anywhere in the provided context.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM]
        {claim.RelatedText}
        [END CLAIM]

        [BEGIN CONTEXT]
        {sourceContent}
        [END CONTEXT]

        Task:
        - Determine if broader context supplies the missing certification explanation.
        - Return TopicComplianceEvaluationResponse JSON. If non-compliant, craft SuggestedAlternative that adds a short sentence (e.g. "Ons groen gas is gecertificeerd met Garanties van Oorsprong (GvOs) van VertiCer.").
        """;
    }

    private async Task<TopicComplianceEvaluationResponse> RunGreenCompositionCheckAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        TopicComplianceEvaluationResponse result = await kernel.ExecuteTopicSubCheckAsync(
            SubCheckGreenComposition,
            GetGreenCompositionPrompt(claim, sourceContent),
            cancellationToken);
        result.Reasoning = $"[{SubCheckGreenComposition}] " + result.Reasoning;
        result.Warning = $"[{SubCheckGreenComposition}] " + result.Warning;
        return result;
    }

    private static string GetGreenCompositionPrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst.
        Requirement: When the term "green gas" (or "groen gas") is used, the composition or source of the gas must be specified (e.g. made from organic waste, biomass, manure, etc.).
        Treat as compliant if:
          • The claim or related text explains what green gas is made of (e.g. "gemaakt van organisch afval", "uit biomassa", "biogas").
          • The claim does not mention "green gas" or "groen gas" (then this specific requirement might not apply, or is trivially satisfied).
        Non-compliant if "green gas" is mentioned without any explanation of its composition/source.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}
        
        [BEGIN CLAIM]
        {claim.ClaimText}
        [END CLAIM]

        [BEGIN RELATED TEXT]
        {claim.RelatedText}
        [END RELATED TEXT]

        [BEGIN CONTEXT]
        {sourceContent}
        [END CONTEXT]

        Task:
        - Assess compliance with the green gas composition requirement.
        - Return TopicComplianceEvaluationResponse JSON. If non-compliant, suggest alternative by adding a brief explanation of composition (e.g. "Groen gas wordt gemaakt van organische reststromen...").
        """;
    }
}