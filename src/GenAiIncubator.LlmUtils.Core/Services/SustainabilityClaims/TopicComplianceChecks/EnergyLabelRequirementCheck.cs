using System.Text.Json;
using DocumentFormat.OpenXml.Office2016.Excel;
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
public class EnergyLabelRequirementCheck(Kernel kernel) : TopicSpecificRequirementCheck
{
    /// <inheritdoc />
    public override RequirementCode AssociatedRequirementCode => RequirementCode.EnergyLabel;

    /// <inheritdoc />
    public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.EnergyLabel;

    private const string IsCallToActionName = "IsCallToAction";
    private const string EnergyLabelYearExplanationClaimName = "EnergyLabelYearExplanationClaim";
    private const string EnergyLabelFactCheckName = "EnergyLabelFactCheck";
    
    private static string EscapeForPrompt(string value) => value?.Replace("\"", "\\\"") ?? string.Empty;

    /// <summary>
    /// Check requirements related to energy labels (electricity and heat). 
    /// </summary>
    /// <param name="claim"></param>
    /// <param name="sourceContent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected override async Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent = "", CancellationToken cancellationToken = default)
    {
        return [
            await CheckIfIsCallToActionAsync(claim, cancellationToken),
            await CheckIfHasYearAndExplanationAsync(claim, sourceContent, cancellationToken),
            await CheckEnergyLabelFacts(claim, cancellationToken),
        ];
    }

    private async Task<TopicComplianceEvaluationResponse> CheckIfHasYearAndExplanationOnPageAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        string prompt = GetYearAndExplanationPagePrompt(claim, sourceContent);
        return await kernel.ExecuteTopicSubCheckAsync(EnergyLabelYearExplanationClaimName, prompt, cancellationToken);
    }

    private async Task<TopicComplianceEvaluationResponse> CheckIfHasYearAndExplanationAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        var claimLevel = await kernel.ExecuteTopicSubCheckAsync(EnergyLabelYearExplanationClaimName, GetYearAndExplanationClaimPrompt(claim), cancellationToken);
        if (claimLevel.IsCompliant)
            return claimLevel;

        var pageLevel = await CheckIfHasYearAndExplanationOnPageAsync(claim, sourceContent, cancellationToken);
        if (pageLevel.IsCompliant)
        {
            return new TopicComplianceEvaluationResponse
            {
                IsCompliant = true,
                Reasoning = $"[{EnergyLabelYearExplanationClaimName}] Year and/or explanation missing in claim snippet but clearly present elsewhere on the page/context.",
                Warning = $"[{EnergyLabelYearExplanationClaimName}] Year and/or explanation missing in claim snippet but clearly present elsewhere on the page/context.",
                SuggestedAlternative = string.Empty
            };
        }

        return claimLevel; 
    }

    private async Task<TopicComplianceEvaluationResponse> CheckIfIsCallToActionAsync(SustainabilityClaim claim, CancellationToken cancellationToken)
    {
        return await kernel.ExecuteTopicSubCheckAsync(IsCallToActionName, GetIsCallToActionPrompt(claim), cancellationToken);
    }

    private async Task<TopicComplianceEvaluationResponse> CheckEnergyLabelFacts(SustainabilityClaim claim, CancellationToken cancellationToken)
    {
        List<string> possibleTypesList = ["electricity", "heat", ""];
        string labelType = await ChatHelpers.ExecutePromptAsync(
            kernel,
            GetEnergyLabelTypePrompt(claim, possibleTypesList),
            cancellationToken: cancellationToken);

        return labelType switch
        {
            "electricity" => await kernel.ExecuteTopicSubCheckAsync(EnergyLabelFactCheckName, GetElectricityLabelFactsPrompt(claim), cancellationToken),
            "heat" => await kernel.ExecuteTopicSubCheckAsync(EnergyLabelFactCheckName, GetHeatLabelFactsPrompt(claim), cancellationToken),
            _ => new TopicComplianceEvaluationResponse
            {
                IsCompliant = false,
                Reasoning = $"[{EnergyLabelFactCheckName}] Unrecognized energy label type referenced in the claim.",
                Warning = string.Empty,
                SuggestedAlternative = "Please specify a recognized energy label type (electricity or heat)."
            },
        };
    }

    private static string GetHeatLabelFactsPrompt(SustainabilityClaim claim)
    {
                return $"""
                [Instructions]
                You are an expert compliance analyst. Evaluate the RELATED TEXT section of the claim for factual accuracy regarding district heat network labels (stadswarmte-etiket / warmtenet label) for 2024.
                Verify that statements about: (a) energy source composition per geographic area, (b) environmental impact metrics (renewable share, waste heat share, CO₂ reductions, CO₂ emissions, losses, primary and renewable energy factors), or (c) methodology references are correct based on the facts in the [HEAT LABEL FACTS] section below.
                If there are no factual inaccuracies, return a compliant evaluation.
                If there are inaccuracies, return a non-compliant evaluation with reasoning and suggest a corrected alternative.
                
                [GENERAL COMPLIANCE RULES]
                {SharedPromptConstants.CopyHandboekRules}
                
                [HEAT LABEL FACTS]
                Stadswarmte-etiket 2024

                Energiebronnen per gebied:
                - Amsterdam Noord en West:
                    - 68% Afvalverbranding
                    - 31% Industrie restwarmte
                    - 1% Elektriciteit uit openbare net

                - Amsterdam Zuid en Oost:
                    - 6% Afvalverbranding
                    - 77% Industrie restwarmte
                    - 4% Gasmotoren (WKK)
                    - 13% Elektriciteit uit openbare net

                - Koudenetten Amsterdam:
                    - 85% Koude uit oppervlaktewater
                    - 15% Elektriciteit uit openbare net

                - Almere:
                    - 5% Zon
                    - 14% Biomassa
                    - 81% Afvalverbranding

                - Arnhem, Duiven en Westervoort:
                    - 96% Industrie restwarmte
                    - 2% Gasmotoren (WKK)
                    - 2% Elektriciteit uit openbare net

                - Nijmegen Waalsprong:
                    - 94% Industrie restwarmte
                    - 6% Gasmotoren (WKK)

                - Leidse regio:
                    - 54% Biomassa
                    - 46% Industrie restwarmte

                - Rotterdam:
                    - 23% Zon
                    - 60% Industrie restwarmte
                    - 16% Gasmotoren (WKK)
                    - 1% Elektriciteit uit openbare net

                - Lelystad:
                    - 89% Afvalverbranding
                    - 11% Elektriciteit uit openbare net

                - Ede:
                    - 83% Biomassa
                    - 17% Elektriciteit uit openbare net

                Opmerking:
                Berekeningen zijn conform "Rapportageformat Duurzaamheidsrapportage voor leveranciers in het kader van de Warmtewet".

                [END HEAT LABEL FACTS]
                [BEGIN CLAIM TEXT]
                """ + EscapeForPrompt(claim.RelatedText) + """
                [END CLAIM TEXT]
                """;
    }

    private static string GetElectricityLabelFactsPrompt(SustainabilityClaim claim)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst. Evaluate the RELATED TEXT section of the claim for factual accuracy regarding electricity labels (stroometiket).
        Verify that any statements about the electricity label's content, purpose, or implications are correct based on the facts given in the [ELECTRICITY LABEL FACTS] section below.
        If there are no factual inaccuracies, return a compliant evaluation.
        If there are inaccuracies, return a non-compliant evaluation with reasoning and suggest a corrected alternative.

        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [ELECTRICITY LABEL FACTS]
        Stroometiket 2024

        Energiebronnen per product:
        - Groen uit Nederland (100% duurzaam): 94.1% Wind NL,  5.9% Zon NL
        - Stroom Hollandse Kust (100% duurzaam): 100% Wind NL
        - Stroom & Zakelijk Stroom (100% duurzaam): 64.1% Wind NL, 12% Water NL, 34.7% Zon NL
        - VastePrijsStroom & Zakelijk VastePrijsStroom (100% duurzaam): 32.1% Wind NL, 3.9% Water NL, 64% Zon NL

        Energiebronnen per organisatieonderdeel:
        - Vattenfall NL Consumenten & MKB (100% duurzaam): 77.6% Wind NL, 21.7% Zon NL, 0.7% Water NL
        - Vattenfall Groep NL (76.5% duurzaam): 40.0% Wind NL, 26.8% Wind EU, 0.2% Water NL, 5.4% Zon NL, 3.8% Biomassa NL, 23.5% Aardgas NL

        Milieugevolgen per product / organisatieonderdeel:
        - Groen uit NL: CO2-uitstoot g/kWh CO₂, 0 g/kWh radioactief afval
        - Stroom & Zakelijk Stroom: CO2-uitstoot g/kWh CO₂, 0 g/kWh radioactief afval
        - VastePrijsStroom & Zakelijk VastePrijsStroom: CO2-uitstoot g/kWh CO₂, 0 g/kWh radioactief afval
        - Vattenfall NL Consumenten & MKB: 0 g/kWh CO₂, CO2-uitstoot g/kWh radioactief afval
        - Vattenfall Groep NL: 89 g/kWh CO₂, CO2-uitstoot g/kWh radioactief afval

        Opmerking: Stroometiketten voor Modelcontract, TijdPrijsStroom en FlexPrijs zijn gelijk aan Groen uit Nederland.
        [END ELECTRICITY LABEL FACTS]

        [BEGIN CLAIM TEXT]
        """ + EscapeForPrompt(claim.RelatedText) + """
        [END CLAIM TEXT]

        Now compare the claim text against the facts above and evaluate its accuracy. We're not looking for minor wording issues, only significant factual inaccuracies. 
        For example, if only giving example of sources but not the full list, that's acceptable. 
        But if the claim states incorrect percentages, or wrong sources, or misrepresents the label's purpose, that counts as a factual inaccuracy.
        """;
    }

    private static string GetEnergyLabelTypePrompt(SustainabilityClaim claim, List<string> possibleTypesList)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst. Determine which type of energy label is being referenced in the RELATED TEXT section of the claim.
        Possible types are: {string.Join(", ", possibleTypesList)}.
        If no recognizable type is referenced, return an empty string.
        
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TEXT]
        """ + EscapeForPrompt(claim.RelatedText) + """
        [END CLAIM TEXT]
        """;
    }

    private static string GetIsCallToActionPrompt(SustainabilityClaim claim)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst. Evaluate the RELATED TEXT for whether any reference to an energy label
        (keywords: stroometiket, warmte-etiket, heat label, electricity label, energy label) is sufficiently actionable.

        Goal:
        When an energy label is presented as a link/button/standalone reference, it must read like a clear "Call To Action" (CTA).
        When the label is referenced inside normal explanatory text as a source/locator (e.g., “on our label you can see…”),
        it is acceptable even without an imperative verb.

        --------------------------------------
        STEP 1 — Detect label references
        --------------------------------------
        If there is NO reference to any energy label keyword → mark COMPLIANT (no violations).

        --------------------------------------
        STEP 2 — Determine how the label is used
        --------------------------------------
        A) Standalone / anchor-like label reference (CTA REQUIRED)
        Treat as standalone/anchor-like if the label appears as:
        - the whole phrase or near-whole phrase of a link/button (often short, noun-only)
        - preceded by symbols like "→", ">", "|", or appears on its own line
        - a menu item / footer link style mention (very short, noun-only)

        In this case, it must be an EXPLICIT CTA.

        B) Embedded in explanatory text (CTA NOT strictly required)
        Treat as embedded if the label keyword is part of a sentence that functions as a locator/source, e.g.:
        - “Op/in ons stroometiket zie je…”
        - “Het stroometiket laat zien…”
        - “In het warmte-etiket staat…”
        - “You can find/see this on the energy label…”
        - “The energy label shows/indicates…”
        - "More information is available on the electricity label…”

        This counts as an IMPLICIT CTA and is COMPLIANT.

        --------------------------------------
        What counts as a compliant CTA?
        --------------------------------------
        1) EXPLICIT CTA (imperative/action phrasing adjacent to label reference), e.g.:
        - “Bekijk ons stroometiket”
        - “Bekijk het warmte-etiket”
        - “View/See/Read the electricity/energy label”
        - “Lees het stroometiket”

        2) IMPLICIT CTA (directional phrasing in a sentence), e.g.:
        - “Op ons stroometiket zie je dat…”
        - “Het stroometiket laat zien…”
        - “In het stroometiket vind je…”
        - “The energy label shows/indicates…”
        - “You can see/find this on the energy label…”

        Non-compliant:
        - Standalone noun-only label references used as link/button text, e.g.:
        “stroometiket”, “energy label”, “electricity label” (with no action/directional phrasing)

        --------------------------------------
        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TEXT]
        """ + EscapeForPrompt(claim.RelatedText) + """
        [END CLAIM TEXT]

        Task:
        Return a compliance evaluation:
        - If COMPLIANT: no violations.
        - If NOT compliant: list violations and suggest a compliant alternative anchor text (e.g., “Bekijk het stroometiket”).
        """;
    }

    private static string GetYearAndExplanationClaimPrompt(SustainabilityClaim claim)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst. We're currently checking if a claim that references an energy label is compliant againt a requirement.
        The requirement states that the energy label reference must also contain 
            (1) an indication of the time period / year coverage (e.g. a specific year like 2024 OR relative wording like "last year's", "this year's", "2023", "voor 2024"). Works even if the time coverage is implied somehow.
            (2) a brief explanation of what the label represents (e.g. that it shows origin/mix of energy sources, composition, or sustainability attributes).

        If the claim does NOT reference an energy label at all, treat as compliant (requirement only applies when label is referenced).

        - Here is a valid explanation of the energy label:
        "The electricity label shows which energy sources our electricity was generated from in 2021. You can also see the share of each energy source that was used. At Vattenfall NL Consumers & SMEs, an increasingly larger part of the electricity is generated sustainably, from sun, wind and water. View our electricity label. In April 2023 we will publish the electricity label for 2022."

        - Whereas here is an explanation that is a bit too brief:
        "In 2024 ontvingen al onze klanten thuis en in het mkb 100% groene stroom uit Nederland. Bekijk ons stroometiket (https://www.vattenfall.nl/stroom/stroometiket/) om te zien uit welke bronnen onze stroom is opgewekt."

        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TEXT]
        """ + EscapeForPrompt(claim.RelatedText) + """
        [END CLAIM TEXT]

        Your response should be in english.

        Task:
        - Evaluate the claim against the requirements (presence of coverage year and explanation).
        - If the claim is compliant, return a compliance evaluation with no violations.
        - If the claim is not compliant (label referenced but missing year or explanation), return a compliance evaluation with the violations and suggest a compliant alternative.
        """;
    }

    private static string GetYearAndExplanationPagePrompt(SustainabilityClaim claim, string sourceContent)
    {
        return $"""
        [Instructions]
        You are an expert compliance analyst. We're currently checking if a claim that references an energy label is compliant againt a requirement.
        The requirement states that the energy label reference must also contain 
            (1) an indication of the time period / year coverage (e.g. a specific year like 2024 OR relative wording like "last year's", "this year's", "2023", "voor 2024"). Works even if the time coverage is implied somehow.
            (2) a brief explanation of what the label represents (e.g. that it shows origin/mix of energy sources, composition, or sustainability attributes).

        We already determined that the claim snippet itself is missing one or both of these elements.
        Now, check the broader page context (provided in the BROADER CONTEXT section) to see if these missing elements are present there.
        If both elements are found in the broader context, in a place where users can presumably easily access it, we can consider the claim compliant. 
        However it's important that both elements are clearly present in the broader context, assuming a typical user would be able to find and understand them.
        If the elements are still missing in the broader context, or are not easily discoverable, the claim remains non-compliant.

        - Here is a valid explanation of the energy label:
        "The electricity label shows which energy sources our electricity was generated from in 2021. You can also see the share of each energy source that was used. At Vattenfall NL Consumers & SMEs, an increasingly larger part of the electricity is generated sustainably, from sun, wind and water. In 2021 we were already above 70%. Our product Green from the Netherlands is generated 100% from sustainable energy sources. View our electricity label. In April 2023 we will publish the electricity label for 2022."

        - Whereas here is an explanation that is a bit too brief:
        "In 2024 ontvingen al onze klanten thuis en in het mkb 100% groene stroom uit Nederland. Bekijk ons stroometiket (https://www.vattenfall.nl/stroom/stroometiket/) om te zien uit welke bronnen onze stroom is opgewekt."

        [GENERAL COMPLIANCE RULES]
        {SharedPromptConstants.CopyHandboekRules}

        [BEGIN CLAIM TEXT]
        """ + EscapeForPrompt(claim.RelatedText) + """
        [END CLAIM TEXT]

        [BEGIN BROADER CONTEXT]
        """ + EscapeForPrompt(sourceContent) + """
        [END BROADER CONTEXT]

        Your response should be in english.

        Task:
        - Evaluate the claim against the requirements (presence of coverage year and explanation).
        - If the claim is compliant, return a compliance evaluation with no violations.
        - If the claim is not compliant (label referenced but missing year or explanation), return a compliance evaluation with the violations and suggest a compliant alternative.
        """;
    }
}