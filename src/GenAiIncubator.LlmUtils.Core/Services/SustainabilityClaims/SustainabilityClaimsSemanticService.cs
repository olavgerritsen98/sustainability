using System.Text;
using System.Text.Json;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;


/// <summary>
/// Provides semantic (AI-based) operations for sustainability claims.
/// </summary>
/// <param name="kernel">The injected kernel object.</param>
public class SustainabilityClaimsSemanticService(Kernel kernel) : ISustainabilityClaimsSemanticService
{
    /// <summary>
    /// Extract all sustainability related sentences from the provided content without
    /// applying the stricter Vattenfall claim definition. This is a broad, recall-
    /// oriented pass that returns all potentially relevant sustainability statements.
    /// </summary>
    /// <param name="normalizedContent">Normalized content from the webpage.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of extracted sustainability-related statements.</returns>
    public async Task<List<SustainabilityClaim>> ExtractAllGenericSustainabilityClaimsAsync(
        string normalizedContent,
        CancellationToken cancellationToken = default)
    {
        string genericExtractionPrompt = GetGenericSustainabilitySentencesExtractionPrompt(normalizedContent);
        Type outputFormatType = typeof(SustainabilityClaimsExtractionResponse);

        string result = await ChatHelpers.ExecutePromptAsync(
            kernel,
            genericExtractionPrompt,
            outputFormatType,
            cancellationToken: cancellationToken
        );

        var claims = JsonSerializer.Deserialize<SustainabilityClaimsExtractionResponse>(result);
        return [.. claims?.Claims.Select(c => new SustainabilityClaim
        {
            ClaimText = c.ClaimText,
            ClaimType = c.ClaimType,
            Reasoning = c.Reasoning ?? string.Empty,
            RelatedText = c.RelatedText ?? string.Empty,
            RelatedUrls = c.RelatedUrls ?? [],
            RelatedTopics = c.RelatedTopics ?? [],
            PageSummary = claims.PageSummary
        }) ?? []];
    }

    /// <summary>
    /// Applies the Vattenfall-specific sustainability claim definition to a single
    /// broadly sustainability-related statement.
    /// </summary>
    /// <param name="genericClaim">A broadly extracted sustainability-related statement.</param>
    /// <param name="sourceContent">The full normalized content from which the statement was extracted.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A Vattenfall sustainability claim derived from the input statement, or null
    /// if the statement is not considered a Vattenfall sustainability claim.
    /// </returns>
    public async Task<SustainabilityClaim?> FilterVattenfallSustainabilityClaimAsync(
        SustainabilityClaim genericClaim,
        string sourceContent,
        CancellationToken cancellationToken = default)
    {
        if (genericClaim is null || string.IsNullOrWhiteSpace(genericClaim.RelatedText))
            return null;

        string filteringPrompt = GetVattenfallClaimFilteringPrompt(genericClaim);
        Type outputFormatType = typeof(FilteredSustainabilityClaim);

        string result = await ChatHelpers.ExecutePromptAsync(
            kernel,
            filteringPrompt,
            outputFormatType,
            cancellationToken: cancellationToken
        );

        FilteredSustainabilityClaim? filteredClaim;
        try
        {
            filteredClaim = JsonSerializer.Deserialize<FilteredSustainabilityClaim>(result);
        }
        catch (JsonException)
        {
            return null;
        }
        if (filteredClaim is null || !filteredClaim.IsVattenfallClaim)
            return null;

        return new SustainabilityClaim
        {
            ClaimText = genericClaim.ClaimText,
            ClaimType = genericClaim.ClaimType,
            Reasoning = filteredClaim.Reasoning ?? string.Empty,
            RelatedText = genericClaim.RelatedText,
            RelatedUrls = genericClaim.RelatedUrls,
            RelatedTopics = genericClaim.RelatedTopics,
            PageSummary = genericClaim.PageSummary
        };
    }

    /// <summary>
    /// Determines the most appropriate topic for a claim that requires evidence by attempting to match
    /// against existing topic descriptions; if no sufficient match is found, generates a new topic name
    /// and uses the claim text as the topic description.
    /// </summary>
    /// <param name="claim">The sustainability claim.</param>
    /// <param name="existingTopicsDescriptions">Existing topics mapped to their descriptions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of (topicDescription, topicName).</returns>
    public async Task<(string, string)> GetTopicForClaimRequiringEvidenceAsync(
        SustainabilityClaim claim,
        Dictionary<string, string> existingTopicsDescriptions,
        CancellationToken cancellationToken = default)
    {
        string topicSelectionPrompt = GetTopicSelectionPrompt(claim, existingTopicsDescriptions);
        Type outputFormatType = typeof(SustainabilityClaimTopicSelectionResponse);

        string result = await ChatHelpers.ExecutePromptAsync(
            kernel,
            topicSelectionPrompt,
            outputFormatType,
            cancellationToken: cancellationToken
        );

        var response = JsonSerializer.Deserialize<SustainabilityClaimTopicSelectionResponse>(result);
        return (response?.TopicName ?? "General", response?.TopicDescription ?? "General sustainability topic");
    }

    /// <summary>
    /// Evaluates whether a sustainability claim meets compliance requirements for its type.
    /// </summary>
    /// <param name="claim">The sustainability claim to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A compliance evaluation containing any violations and suggested alternatives.</returns>
    public async Task<SustainabilityClaimComplianceEvaluation> EvaluateSustainabilityClaimComplianceAsync(
        SustainabilityClaim claim,
        CancellationToken cancellationToken = default)
    {
        string sustainabilityClaimComplianceEvaluationPrompt = GetSustainabilityClaimComplianceEvaluationPrompt(claim);
        Type outputFormatType = typeof(SustainabilityClaimComplianceEvaluationResponse);

        string result = await ChatHelpers.ExecutePromptAsync(
            kernel,
            sustainabilityClaimComplianceEvaluationPrompt,
            outputFormatType,
            cancellationToken: cancellationToken
        );

        var response = JsonSerializer.Deserialize<SustainabilityClaimComplianceEvaluationResponse>(result);
        return new SustainabilityClaimComplianceEvaluation
        {
            Claim = claim,
            Violations = response?.Violations ?? [],
            SuggestedAlternative = response?.SuggestedAlternative ?? string.Empty
        };
    }

    private static string GetSustainabilityClaimComplianceEvaluationPrompt(SustainabilityClaim claim)
    {
        var applicableRequirements = SustainabilityClaimKnowledge.GetApplicableRequirements(claim.ClaimType);

        var requirementTexts = applicableRequirements
            .Select(code => $"- {code}: {SustainabilityClaimKnowledge.RequirementDescriptions[code]}")
            .ToArray();

        var claimTypeDescription = SustainabilityClaimKnowledge.ClaimTypeDescriptions[claim.ClaimType];

        var relatedUrls = (claim.RelatedUrls ?? [])
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct()
            .ToArray();
        var relatedUrlsText = relatedUrls.Length == 0
            ? "(none)"
            : string.Join(Environment.NewLine, relatedUrls.Select(u => $"- {u}"));

        return $"""
            System instruction:
            You are an expert compliance analyst specializing in sustainability communications and regulatory requirements. Evaluate whether a sustainability claim meets the required compliance standards.

            Claim to evaluate:
            "{claim.ClaimText}"
            
            Additional context (may be empty):
            "{claim.RelatedText}"

            Related URLs (may be empty):
            {relatedUrlsText}

            Claim type: {claim.ClaimType}
            Type description: {claimTypeDescription}

            Requirements to check:
            {string.Join(Environment.NewLine, requirementTexts)}

            Task:
            1) Evaluate the claim against EACH of the above requirements.
            2) For any requirement that is NOT met, create a violation with:
               - Code: the requirement code (e.g., "General_ClearAndUnambiguous", "Ambition_ClearlyLabeledAsAmbition")
               - Message: concise explanation in Dutch of why the requirement is violated (≤ 50 words)
            3) If the claim has violations, suggest a compliant alternative in Dutch that addresses the issues.
               - Keep the original intent where possible.
               - If the claim relies on substantiation, you may reference the Related URLs in the alternative (do not invent new URLs).
            4) If no violations are found, leave violations array empty and suggested alternative empty.

            Output:
            Return ONLY a JSON object matching the required schema with:
                - Violations: array of objects with properties Code and Message
            - SuggestedAlternative: string
            """;
    }

    /// <summary>
    /// Merges multiple suggested alternatives into a single cohesive suggestion, taking into account the violations that triggered them.
    /// </summary>
    /// <param name="claim">The original sustainability claim.</param>
    /// <param name="evaluations">List of compliance evaluations (containing violations and suggestions) to merge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A single merged suggested alternative.</returns>
    public async Task<string> MergeSuggestedAlternativesAsync(
        SustainabilityClaim claim,
        List<SustainabilityClaimComplianceEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        List<SustainabilityClaimComplianceEvaluation> relevantEvaluations = [.. evaluations
            .Where(e => !string.IsNullOrWhiteSpace(e.SuggestedAlternative) || e.Violations.Count > 0)];

        if (relevantEvaluations.Count == 0)
        {
            return string.Empty;
        }

        if (relevantEvaluations.Count == 1 && !string.IsNullOrWhiteSpace(relevantEvaluations[0].SuggestedAlternative))
        {
            return relevantEvaluations[0].SuggestedAlternative;
        }

        string mergePrompt = GetSuggestedAlternativesMergePrompt(claim, relevantEvaluations);
        Type outputFormatType = typeof(SuggestedAlternativeMergeResponse);

        string result = await ChatHelpers.ExecutePromptAsync(
            kernel,
            mergePrompt,
            outputFormatType,
            cancellationToken: cancellationToken
        );

        var response = JsonSerializer.Deserialize<SuggestedAlternativeMergeResponse>(result);
        return response?.SuggestedAlternative ?? string.Empty;
    }

    private static string GetGenericSustainabilitySentencesExtractionPrompt(string content)
    {
        var typeLines = SustainabilityClaimKnowledge.ClaimTypeDescriptions
            .Select(kvp => $"- {kvp.Key}: {kvp.Value}");
        var claimTypesWithDescriptions = string.Join("\n", typeLines);
        var allowedTypesCsv = string.Join(", ", SustainabilityClaimKnowledge.ClaimTypeDescriptions.Keys);
        var sustainabilityTopicsList = string.Join(
            "\n",
            Enum.GetValues<SustainabilityTopicsEnum>()
                .Select(e => $"- {e}: {e.GetDescription()}"));

        return $"""
            System instruction:
            You are an expert analyst specializing in sustainability-related communications for the energy provider Vattenfall. Extract all sentences or short text segments from the provided content that are related to sustainability in a broad sense.

            What should be included in this broad extraction?
            • Any statement related to environmental impact, climate, CO₂/greenhouse gases, fossil-free, renewable energy, recycling, circularity, biodiversity, animal welfare, social impact, working conditions, or broader ESG topics.
            • Statements about sustainability-related technologies, behaviors, public initiatives, regulations, or general trends.
            • Statements that may or may not be directly linked to Vattenfall.
            • Mentions of Vattenfall products, services, or operations that imply sustainability aspects, even if not explicitly framed as claims. For example, mentions of Green Electricity products (wind, sun...), renewable energy sources, or CO₂ reduction efforts.
            • Mentions of ambitions towards sustainability, even if not clearly labeled as ambitions. An example is the Paris Climate Agreement goals.
            • Statements related to electricity or gas usage.
            • Sentences redirecting readers to some of Vattenfall's products or services.

            At this stage, DO NOT exclude statements based on whether they are proper commercial "claims" or whether they refer explicitly to Vattenfall. The goal is to capture a rich set of sustainability-related content for later filtering.

            Classification types (use enumeration values exactly as listed):
            {claimTypesWithDescriptions}

            Related topics (use enumeration values exactly as listed):
            {sustainabilityTopicsList}

            Task:
            1) Read the content and extract all distinct sustainability-related statements. For each block of text that is conveying one main idea, output a single item. If a paragraph contains multiple closely related sentences, you may keep them together as one item.
            2) For each item, provide:
               - ClaimText: quote the exact text segment from the content (verbatim, trimmed). It's important that you also keep the URLs associated with extracted texts in the text with the formatting as in the original page. These URLs are crucial for later checks, here is an example format to keep in this field: "Look at our energy label (https://www.vattenfall.nl//stroom/stroometiket/)".
               - RelatedText: nearby context (before/after) that helps understand the statement. Should be the full sentences, no abbreviations or "...". Should contain the ClaimText, and related links if they're in the text.
               - ClaimType: one of [{allowedTypesCsv}]. Use Unspecified if the type is unclear or not directly applicable at this stage.
               - Reasoning: short justification (≤ 30 words) explaining why this text is sustainability-related.
               - RelatedUrls: up to 3 http(s) URLs that are directly associated with the statement, as they appear in the content, or just above/below it in the source. Important to include closeby URLs when they're connected to the sentence. 
               - RelatedTopics: a list of topics that the statement is related to.
            3) Also include a summary of the page. We'll later filter these statements further to identify actual Vattenfall sustainability claims, and this summary may help in that process. We'll give it instead of the full content, so try to summarize the context well, and make explicit how this statement relates to the overall page content. In particular, if there is contextual information that shed light on what the statment says, it should be included here. About 4-5 sentences.

            Notes:
            - The content preserves hyperlinks in the form "Link Text (URL)". Make sure to include those in the ClaimText, RelatedText and RelatedUrls fields. Urls close by a claim should be included in RelatedUrls, but also in the ClaimText and RelatedText if they are part of the sentence.
            - Only include fully qualified absolute URLs (http/https). Do not fabricate, normalize, or convert relative URLs.
            - If a block has a CTA/link immediately below it (like “Bekijk het warmte-etiket”), that URL is ALWAYS “directly associated” and must be included in RelatedUrls and kept in RelatedText.
            - If no sustainability-related statements are found, return an empty array [].
            - Sentences that are very close should be merged into a single claim to avoid redundancy. For example, unless there is a very good reason, a claim text should only be present in the claaim's RealtedText, and not also in the RealtedText of another claim.
                - This means that there shouldn't be multiple claims with very similar ClaimText. Instead, merge them into one claim with a more complete RelatedText.
            - Wording such as "Steeds meer" or "Vattenfall will invest ..." or "Vattenfall zal investen ...", usually expresses ambition, and should be classified accordingly in the claim type.

            [BEGIN CONTENT]
            {content}
            [END CONTENT]
            """;
    }

    private static string GetVattenfallClaimFilteringPrompt(SustainabilityClaim genericClaim)
    {
        return $"""
            [System instruction]
            You are an expert compliance analyst specializing in sustainability communications for Vattenfall. 
            You are given one broadly extracted sustainability-related statement. Your job is to determine whether it is a *Vattenfall sustainability claim* and, if so, explain why.

            --------------------------------------
            DEFINITION — What is a Vattenfall sustainability claim?
            --------------------------------------
            A sustainability claim is a statement made by Vattenfall **about one of its own products, services, operations, or activities** that suggests a positive or reduced negative impact on:
            - the environment
            - society
            - animal welfare
            - working conditions

            A statement counts as a Vattenfall sustainability claim if ANY of the following is true:
            • It asserts or implies that a Vattenfall product/service/activity is sustainable, greener, renewable, CO₂-reduced, recycled, circular, ethical, or otherwise socially/environmentally beneficial.  
            • It compares a Vattenfall offering to another product/company with respect to environmental or social benefit.  
            • It states that Vattenfall uses renewable or recycled sources/materials (if tied to a Vattenfall offering or activity).  
            • It links the sustainability aspect directly to a Vattenfall-provided energy label, tariff, district heating system, electricity product, heat network, etc.  
            • It promotes or references a Vattenfall product/service whose name itself implies sustainability OR renewable origin. This includes names containing sustainability keywords or renewable-source terms, even in CamelCase/compound form, e.g.: "green/groen", "eco", "bio", "CO₂-neutral", "renewable/duurzaam", and renewable sources like "wind", "zon/solar", "water/hydro", "biogas", "groen gas".  Examples: "GroenUitNederlands", "NederlandsGroenGas", "NederlandseWind".
            • It states a concrete ambition or goal related to sustainability for a Vattenfall product, service, or operation (e.g., "By 2030, all our electricity products will be 100% renewable"). 
            • It mentions Vattenfall's targets around the Paris Climate Agreement.
            • It explains/promotes sustainability related aspects 
            • It explains or describes the environmental or sustainability characteristics of a Vattenfall product or energy offering (including lifecycle explanations), even if written in an informational or educational tone rather than promotional language.
            • It links to a Vattenfall product/service page for Green Gas or Green Electiricity offerings.
            • Claims about Green Gas or Green Electricity products are considered Vattenfall sustainability claims by default, unless clearly contradicted by context.

            --------------------------------------
            WHAT IS NOT a Vattenfall sustainability claim?
            --------------------------------------
            A statement is NOT a sustainability claim if it is:
            • A neutral product/feature description with no sustainability angle. However, if the statement is a call to view a Vattenfall product for green gas or electricity (wind, solar, GroenUitNl...), it is a claim. 
            • A generic corporate aspiration or mission not tied to a Vattenfall offering.  
            • A sustainability-related statement **not about Vattenfall** (e.g., regional or societal trends, general renewable descriptions).  
                - Careful: If the statement is on a Vattenfall page and talks about green energy (gas, electricity) products, it is likely referring to a Vattenfall product. Make sure to consider the source URL to decide.
                - For example, on a page about Vattenfall's green gas products, a statement like "Green gas is produced from organic waste materials through anaerobic digestion" is likely describing Vattenfall's offering and thus counts as a claim.
            • General advice, tips, or educational content—even if written by Vattenfall experts—unless it explicitly references a Vattenfall product/service.  
            • Background statistics, facts, or definitions presented as context without linking the result to Vattenfall's own achievement.  
            • Administrative or descriptive text about labels, tools, calculators, or documentation that does **not** describe the sustainability of a Vattenfall product/service.  
            • Examples or supporting narratives that illustrate a claim but do not themselves describe sustainability benefits of a Vattenfall offering.  
            • Operational or project updates that do not express or imply sustainability benefits.
            • If the statement is framed as a question, invitation, or generic offer ("we can help you"), and does not assert a real sustainability outcome, treat it as promotional content — not a claim.
            • Products such as solar panels, heat pumps, or EV chargers sold by Vattenfall to customers for their own use. These are not Vattenfall sustainability claims since the sustainability effect depends on customer usage.
            • It mentions the Paris Climate Agreement in a generic way, without linking it to a Vattenfall product, service, operation, or ambition.

            Note:
            • The *strometiket* (energy label) explains sources of Vattenfall electricity products. It is **not** a product itself.
            • The source page where this statement was found is a Vattenfall page. Therefore mentions such as "Steeds meer elektriciteit opwekken met wind", or similar statements talking about green energy (gas, electricity) products on vattenfall pages are likely referring to a Vattenfall product. Make sure to consider the related URLs to decide.


            --------------------------------------
            EXAMPLES CLAIMS
            --------------------------------------
            Example 1:
            Statement: "Our district heating emits 60% less CO₂ than gas heating."
            Classification: Claim
            Reasoning: Direct comparison of Vattenfall's district heating product with environmental benefit.

            Example 2:
            Statement: "By 2024, 85% of the cooling supplied through Vattenfall's district cold systems came from renewable surface water."
            Classification: Claim
            Reasoning: States environmental benefit of a specific Vattenfall service (district cooling).

            Example 3:
            Statement: "Choose product GroenUitNederlands"
            Classification: Claim
            Reasoning: Promotes a Vattenfall Green Electricity product, which inherently implies sustainability benefits.

            Example 4:
            Statement: "Green gas is a more sustainable alternative to natural gas. It is made from natural raw materials, such as manure, sludge, and vegetable, fruit, and garden waste (VFG waste). During a fermentation process, bacteria convert this waste into biogas. This biogas is then purified, dried, and processed into green gas. By choosing green gas, you replace part of the natural gas in our gas network."
            Classification: Claim
            Reasoning: Talkes about green gas, which is a Vattenfall product, and its sustainability benefits.

            Example 5:
            Statement: "We are committed to a future in which we are less dependent on the use of fossil fuels for our energy. See what our climate ambition (https://www.vattenfall.nl/media/4.-over-vattenfall-corporate/2.-wat-we-doen/1.-ons-plan/onze-klimaatambitie-rapport-co2-reductie-2017-2040-mei-2025.pdf) is to reduce our dependence on these fuels as much as possible. "
            Classification: Claim
            Reasoning: Concrete ambition related to sustainability for a Vattenfall product, and talking about the company's fossil fuel dependence.

            --------------------------------------
            EXAMPLES NOT CLAIMS
            --------------------------------------
            Example 1:
            Statement: "We calculate the CO₂ emissions of our district heating network."
            Classification: Not a claim
            Reasoning: Describes a calculation process, not a sustainability benefit of the product.

            Example 2:   
            Statement: "Good progress has been made with six hybrid ATES projects…"
            Classification: Not a claim
            Reasoning: Operational update without stating a sustainability benefit.

            Example 3:
            Statement: "Concrete solutions for sustainability: from heat pumps and batteries to smart charging"
            Classification: Not a claim
            Reasoning: Products sold to customers for their own use; sustainability effect depends on customer usage.

            Example 4:
            Statement: "Looking to cut your building’s emissions while staying in control of costs and risks? We help with smart energy solutions like storage, efficient systems, renewables, and intelligent energy management."
            Classification: Not a claim
            Reasoning: General advice and advertisement framed as an invitation; does not assert a sustainability outcome of a Vattenfall offering.

            Example 5:
            Statement: "Sustainable electricity with your own solar roof"
            Classification: Not a claim
            Reasoning: Promotional content about solar panels sold to customers; sustainability depends on customer usage.

            Example 6:
            Statement: "We help you make your real estate more sustainable step by step. With data insights (via My Vattenfall Business (https://zakelijk.vattenfall.nl/) or an Energy Management System), tailored advice on subsidy and legislative opportunities, and proven technologies like heat pumps, batteries, and solar panels. This way, you'll take steps towards becoming Paris-proof (https://www.vattenfall.nl/grootzakelijk/sectoren/vastgoed/paris-proof/), save costs, and increase the value of your property."
            Classification: Not a claim
            Reasoning: General advice and promotional content; does not assert a specific sustainability outcome of a Vattenfall offering.

            --------------------------------------
            INPUT CONTEXT
            GenericStatement (JSON):
            Statement: {genericClaim.RelatedText}
            Source URL: {genericClaim.SourceUrl}
            Related Urls: {string.Join(", ", genericClaim.RelatedUrls)}

            --------------------------------------
            TASK
            Evaluate whether GenericStatement is a Vattenfall sustainability claim.
            Output a JSON object with fields:
            • "Claim": the original GenericStatement (keep URLs and formatting intact) 
            • "IsVattenfallClaim": "true" or "false"
            • "Reasoning": ≤ 50 words explaining the decision based on the given descriptions about what is and is not a claim.  
            - If it *is* a claim: identify the specific Vattenfall product/service/activity.  
            - If *not* a claim: reference the applicable exclusion rule or failed test.
            """;
    }

    /// <summary>
    /// Determines whether the provided content supplies sufficient, accessible substanciation supporting the claim.
    /// </summary>
    /// <param name="claim">The sustainability claim.</param>
    /// <param name="substanciationContent">Normalized content fetched from a related URL (one click away).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if content provides substanciation for the claim; otherwise false.</returns>
    public async Task<bool> DoesContentProvideSubstanciationForClaimAsync(
        SustainabilityClaim claim,
        string substanciationContent,
        CancellationToken cancellationToken = default)
    {
        string prompt = GetSubstanciationCheckPrompt(claim, substanciationContent);
        Type outputFormatType = typeof(SustainabilityClaimSubstanciationCheckResponse);

        string result = await ChatHelpers.ExecutePromptAsync(
            kernel,
            prompt,
            outputFormatType,
            cancellationToken: cancellationToken
        );

        try
        {

            var response = JsonSerializer.Deserialize<SustainabilityClaimSubstanciationCheckResponse>(result);
            return response?.ProvidesSubstanciation == true;
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"Failed to parse substanciation check response for claim: {claim.ClaimText}");
        }
    }

    private static string GetSubstanciationCheckPrompt(SustainabilityClaim claim, string substanciationContent)
    {
        return $"""
            System instruction:
            You are a compliance analyst. Determine if the provided substanciation content contains sufficient, clear, accessible support for the sustainability claim.

            Claim:
            "{claim.ClaimText}"

            Additional context (may be empty):
            "{claim.RelatedText}"

            Substanciation content to review (from a page one click away):
            [BEGIN CONTENT]
            {substanciationContent}
            [END CONTENT]

            Task: **Answer whether this content provides credible, specific, and sufficient substanciation for the claim.**
            - Consider presence of data, studies, third-party references, labels/certifications, or detailed explanations.
            - Substanciation can be evidence or plans and measures, or other statements that support the claim, in a way where the reader could potentially verify. Basically, it should be showing that the claim is backed up by something.
                - For example, a sentence explaining that there is a contract between Vattenfall some company, and this is stated as supporting some claim, is substanciation.
                        - IMPORTANT: The substantiation must be specifically relevant to the claim. A page that only describes a third-party supplier's general activities or services, without directly relating to or evidencing Vattenfall's specific claim, does NOT constitute valid substantiation. The content must actually back up what the claim states.

            Output JSON with fields:
            - ProvidesSubstanciation: boolean
            - Reason: short explanation (≤ 40 words)
            """;
    }

    /// <summary>
    /// Determines whether the provided content contains sufficient plans and measures supporting an ambition claim.
    /// </summary>
    /// <param name="claim">The sustainability ambition claim.</param>
    /// <param name="evidenceContent">Normalized content fetched from a related URL (one click away).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if content provides plans and measures for the ambition claim; otherwise false.</returns>
    public async Task<bool> DoesContentProvidePlansAndMeasuresForClaimAsync(
        SustainabilityClaim claim,
        string evidenceContent,
        CancellationToken cancellationToken = default)
    {
        string prompt = GetPlansAndMeasuresCheckPrompt(claim, evidenceContent);
        Type outputFormatType = typeof(SustainabilityClaimPlansAndMeasuresCheckResponse);

        string result = await ChatHelpers.ExecutePromptAsync(
            kernel,
            prompt,
            outputFormatType,
            cancellationToken: cancellationToken
        );

        var response = JsonSerializer.Deserialize<SustainabilityClaimPlansAndMeasuresCheckResponse>(result);
        return response?.ProvidesPlansAndMeasures == true;
    }

    private static string GetPlansAndMeasuresCheckPrompt(SustainabilityClaim claim, string evidenceContent)
    {
        return $"""
            System instruction:
            You are a compliance analyst specializing in sustainability communications. Determine if the provided content contains sufficient plans and measures that support the feasibility of an ambition claim.
            For example, if the claim states that money will be invested somewhere in the future, the content should provide evidence or a concrete plan on how this will happen. 
            Similarly, if we claim that we will reach some sustainability target, the content should provide concrete plans and measures on how we will reach that target.

            Ambition claim:
            "{claim.RelatedText}"

            Content to review (from a page one click away):
            [BEGIN CONTENT]
            {evidenceContent}
            [END CONTENT]

            Requirement:
            As substantiation, examples (more than one) of how we will reach our ambition must be added (or a link to the plan), directly with the claim.

            Task:
            - Answer whether this content provides sufficient plans and measures for the ambition claim.
            - Look for more than one concrete examples of how the ambition will be achieved.
            - Consider presence of specific actions, timelines, methodologies, or detailed implementation plans.
            - Look for evidence that work has already begun (voorbeelden dat we al begonnen zijn).
                    - IMPORTANT: The plans and measures must be Vattenfall's OWN concrete plans, not merely information about a third-party supplier. A page that only describes what a supplier (e.g. Renewi) does, without showing Vattenfall's own roadmap, commitments, or implementation steps, is NOT sufficient as a plan. The content must show how VATTENFALL will achieve the ambition, not just that a partner exists.

            Output JSON with fields:
            - ProvidesPlansAndMeasures: boolean
            - Reason: explanation of the plan for the specific claim, or an explanation saying that there is no plan provided.
            """;
    }

    private static string GetTopicSelectionPrompt(SustainabilityClaim claim, Dictionary<string, string> existingTopicsDescriptions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert in sustainability topics classification.");
        sb.AppendLine("Analyze the following sustainability claim and determine which topic it belongs to.");
        sb.AppendLine("If the claim fits into one of the existing topics, return that topic.");
        sb.AppendLine("If the claim represents a new topic not in the list, generate a short, descriptive name for the new topic and a brief description.");
        sb.AppendLine();
        sb.AppendLine("Claim:");
        sb.AppendLine($"\"{claim.ClaimText}\"");
        sb.AppendLine();
        sb.AppendLine("Existing Topics:");
        foreach (var topic in existingTopicsDescriptions)
        {
            sb.AppendLine($"- {topic.Key}: {topic.Value}");
        }
        sb.AppendLine();
        sb.AppendLine("Return the result in the specified JSON format.");
        return sb.ToString();
    }

    private static string GetSuggestedAlternativesMergePrompt(SustainabilityClaim claim, List<SustainabilityClaimComplianceEvaluation> evaluations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert in sustainability communication compliance.");
        sb.AppendLine("We have evaluated a sustainability claim and found multiple compliance issues.");
        sb.AppendLine("Different checks have identified violations and proposed different alternative phrasings.");
        sb.AppendLine("Your task is to combine these suggestions into a SINGLE, cohesive, and compliant alternative phrasing in Dutch.");
        sb.AppendLine("The merged alternative must address ALL the violations and improvements implied by the individual suggestions.");
        sb.AppendLine("Maintain the original intent of the claim as much as possible, but prioritize compliance and accuracy.");
        sb.AppendLine();
        sb.AppendLine("Original Claim:");
        sb.AppendLine($"\"{claim.ClaimText}\"");
        sb.AppendLine();
        sb.AppendLine("Evaluations (Violations and Suggestions):");
        
        int index = 1;
        foreach (var evaluation in evaluations)
        {
            sb.AppendLine($"--- Evaluation {index++} ---");
            if (evaluation.Violations.Count > 0)
            {
                sb.AppendLine("Violations:");
                foreach (var violation in evaluation.Violations)
                {
                    sb.AppendLine($"- [{violation.Code}] {violation.Message}");
                }
            }
            
            if (!string.IsNullOrWhiteSpace(evaluation.SuggestedAlternative))
            {
                sb.AppendLine($"Suggested Alternative: \"{evaluation.SuggestedAlternative}\"");
            }
            else
            {
                sb.AppendLine("Suggested Alternative: (None provided)");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Return the result in the specified JSON format.");
        return sb.ToString();
    }
}
