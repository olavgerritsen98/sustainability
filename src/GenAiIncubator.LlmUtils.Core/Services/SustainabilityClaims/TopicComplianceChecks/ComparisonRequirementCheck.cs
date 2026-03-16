using System.Text.Json;
using System.Linq;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using GenAiIncubator.LlmUtils.Core.Services.WebContentNormalization;
using Microsoft.SemanticKernel;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks.Constants;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

/// <summary>
/// Implementation of the CO2 neutral topic-specific requirement.
/// </summary>
public class ComparisonRequirementCheck(
    Kernel kernel,
    IWebContentNormalizationService webContentNormalizationService) : TopicSpecificRequirementCheck
{
    /// <inheritdoc />
    public override RequirementCode AssociatedRequirementCode => RequirementCode.Comparison;

    /// <inheritdoc />
    public override SustainabilityTopicsEnum AssociatedTopic => SustainabilityTopicsEnum.ComparisonStatement;


    /// <summary>
    /// Checks the sustainable production requirement.
    /// </summary>
    /// <param name="claim"></param>
    /// <param name="sourceContent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected override async Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent = "", CancellationToken cancellationToken = default)
    {
        // 0. Scope filter: determine whether this is a direct product-vs-product (our product vs competitor product) comparison.
        var scopeFilter = await kernel.ExecuteTopicSubCheckAsync(
            "ComparisonScopeFilter",
            BuildComparisonScopeFilterPrompt(claim),
            cancellationToken);

        if (!scopeFilter.IsCompliant)
        {
            // Not a direct product vs competitor product comparison; skip deeper checks.
            return [ new TopicComplianceEvaluationResponse
            {
                IsCompliant = true,
                Reasoning = "[ComparisonScopeFilter] Product-level comparison checks not applicable: " + scopeFilter.Reasoning,
                Warning = string.Empty,
                SuggestedAlternative = string.Empty
            }];
        }

        // 1. Run comparators extraction first
        var comparatorsResult = await kernel.ExecuteTopicSubCheckAsync(
            "ComparatorsExtraction",
            BuildComparatorsExtractionPrompt(claim),
            cancellationToken);

        // Short-circuit: if comparators are not clear, no further comparison-derived checks are meaningful.
        if (!comparatorsResult.IsCompliant)
            return [comparatorsResult];

        var results = new List<TopicComplianceEvaluationResponse> { comparatorsResult };

        // 2. Defined basis (include comparators context)
        var definedBasis = await kernel.ExecuteTopicSubCheckAsync(
            "DefinedBasis",
            BuildDefinedBasisPrompt(claim, comparatorsResult),
            cancellationToken);
        results.Add(definedBasis);

        // 3. Fair like-for-like (comparators context)
        var fairLikeForLike = await kernel.ExecuteTopicSubCheckAsync(
            "FairLikeForLike",
            BuildFairLikeForLikePrompt(claim, comparatorsResult),
            cancellationToken);
        results.Add(fairLikeForLike);

        // 4. Transparency & clarity (comparators + URL evidence)
        var urlContents = await GetClaimUrlsContentAsync(claim, cancellationToken) ?? [];
        var transparency = await kernel.ExecuteTopicSubCheckAsync(
            "TransparencyAndClarity",
            BuildTransparencyPrompt(claim, urlContents, comparatorsResult),
            cancellationToken);
        results.Add(transparency);

        return results;
    }

    private async Task<List<string>> GetClaimUrlsContentAsync(SustainabilityClaim claim, CancellationToken cancellationToken)
    {
        List<string> urlContents = [];
        foreach (var url in claim.RelatedUrls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var absolute) ||
                (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps))
                continue;

            var targetUrl = absolute.ToString();
            string content = await webContentNormalizationService.FetchAndNormalizeAsync(targetUrl, cancellationToken);
            urlContents.Add(content);
        }
        return urlContents;
    }

    // Superlatives now handled by SuperlativesRequirementCheck.
    // Comparison flow:
    // 1. Extract comparator pair.
    // 2. Evaluate defined basis (metric + baseline).
    // 3. Assess fairness (like-for-like scope/timeframe/geography).
    // 4. Validate transparency & clarity with related URL evidence.

    #region Prompt Builders
    private static readonly string SystemPreamble = $"""
System instruction:
You are an expert compliance analyst specializing in sustainability communications and regulatory requirements.

{SharedPromptConstants.CopyHandboekRules}
""";
    private static string BuildComparisonScopeFilterPrompt(SustainabilityClaim claim) => $"""
{SystemPreamble}

Sub-check: Comparison Scope Filter
Goal: Decide if the claim represents a direct comparison between a specific branded product/service of OUR company and a specific branded product/service of ANOTHER company.

[CRITERIA TO PROCEED]
Return compliant (true) ONLY IF:
• Two concrete, branded offerings are referenced (e.g. "Our SolarPlus battery" vs "CompetitorX StormCell").
• Or our named product vs a clearly named competitor company product.

Return non-compliant (false filter triggering skip) IF:
• Comparison is between our product and a generic commodity or category (e.g. "our green gas" vs "natural gas").
• Comparison is between two generic categories ("district heating" vs "natural gas heating").
• Only one product is clearly defined and the other side is generic ("Our EcoHeat" vs "other systems", "natural gas").
• Ambiguous which competitor product is referenced.

[OUTPUT MAPPING]
Use IsCompliant=true to indicate product-level comparison requiring deeper checks.
Use IsCompliant=false to indicate deeper product comparison checks NOT applicable; reasoning should briefly state why (e.g. "Generic commodity comparison").

[BEGIN CLAIM]
{claim.ClaimText}
[END CLAIM]

Tasks:
1. Identify the two sides of the comparison.
2. Decide if both are branded products from distinct companies.
3. Set IsCompliant accordingly as per criteria.
4. Reasoning: concise justification (do not fabricate brands if absent).
""";

    private static string BuildComparatorsExtractionPrompt(SustainabilityClaim claim) => $"""
{SystemPreamble}

Sub-check: Comparators Extraction
Goal: Determine if the claim clearly defines exactly TWO entities/items being compared.

[COMPLIANCE RULE]
Mark compliant ONLY if two distinct comparators can be unambiguously identified from the claim itself (explicitly or implicitly) without guessing.
Mark non-compliant if:
• Fewer than two comparators are identifiable.
• More than two are referenced with no clear primary pair.
• One side of the comparison is vague (e.g. "others", "more sustainable" without stating compared-to entity or timeframe).

[OUTPUT MAPPING GUIDANCE]
Reasoning: Briefly list the comparators or explain why ambiguous.
SuggestedAlternative: Provide a concise rewrite explicitly naming both sides (only when non-compliant).
Warning: Leave empty unless ambiguity could mislead (then state short caution).

[BEGIN CLAIM]
{claim.ClaimText}
[END CLAIM]

Tasks:
1. Identify comparator A and comparator B (or explain absence/ambiguity).
2. Decide compliance per rule above.
3. If non-compliant, propose a clearer alternative wording including both entities and basis (e.g. metric/timeframe if implied).
""";

    private static string BuildDefinedBasisPrompt(SustainabilityClaim claim, TopicComplianceEvaluationResponse comparators) => $"""
{SystemPreamble}

Sub-check: Defined Basis
Goal: Determine whether the claim defines WHAT is compared (metric/attribute) and AGAINST WHAT (baseline/timeframe/product cohort).

[GUIDANCE]
• Acceptable bases: explicit metric (CO₂ intensity, renewable %, efficiency), timeframe ("vs 2020"), product version ("new model"), peer set ("major Dutch suppliers").
• Vague phrases ("better for the environment") without metric or reference fail.

[BEGIN CLAIM]
{claim.ClaimText}
[END CLAIM]

[COMPARATORS CONTEXT]
{comparators.Reasoning}

Tasks:
1. Identify stated metric/attribute (or note missing).
2. Identify baseline/reference (or note missing).
3. Decide compliance; propose improvement if missing pieces.
""";

    private static string BuildFairLikeForLikePrompt(SustainabilityClaim claim, TopicComplianceEvaluationResponse comparators) => $"""
{SystemPreamble}

Sub-check: Fair Like-for-Like
Goal: Evaluate whether the comparison uses equivalent scope (product/service category), functional unit, geography, and timeframe.

[GUIDANCE]
• Mismatch examples: electricity vs heat, national vs EU scope, 2024 data vs 2020 data without acknowledging change, different product classes.
• Accept if differences are transparently qualified (e.g. "compared with our 2020 electricity mix").

[BEGIN CLAIM]
{claim.ClaimText}
[END CLAIM]

[COMPARATORS CONTEXT]
{comparators.Reasoning}

Tasks:
1. List dimensions (scope, unit, geography, timeframe).
2. Note any mismatch and why misleading.
3. Suggest fix if unfair.
""";


    private static string BuildTransparencyPrompt(SustainabilityClaim claim, List<string> urlContents, TopicComplianceEvaluationResponse comparators) => $"""
{SystemPreamble}

Sub-check: Transparency & Clarity
Goal: Verify indicator, reference year/source, and accessible proof via provided URLs.

[GUIDANCE]
• Indicator: what metric (CO₂ intensity, renewable share, etc.).
• Reference: year or baseline clearly stated.
• Proof: link or page excerpt supporting claim within one click.

[BEGIN CLAIM]
{claim.ClaimText}
[END CLAIM]

[RELATED URLS]
{string.Join(", ", claim.RelatedUrls)}

[URL CONTENTS]
{string.Join("\n\n", claim.RelatedUrls.Select((u,i) => $"URL {i+1}: {u}\n<<<CONTENT START>>>\n{(urlContents.Count > i ? (urlContents[i].Length > 3000 ? urlContents[i][..3000] + "..." : urlContents[i]) : "(No content fetched)")}\n<<<CONTENT END>>>"))}

[COMPARATORS CONTEXT]
{comparators.Reasoning}

Tasks:
1. State indicator found (or missing).
2. State reference year/baseline (or missing).
 3. Cite any proof excerpt from the URL CONTENTS (or note absence).
 4. Check whether proof is current (<= 24 months old if date present) and accessible.
 5. Recommend additions if gaps.
""";

    #endregion
}