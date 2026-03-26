using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Text.Json;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.WebContentNormalization;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;

/// <summary>
/// Provides the orchestration and pluggable steps for checking sustainability claims
/// on a webpage or document according to ACM/CDR/UCPD-aligned requirements.
/// </summary>
/// <param name="webContentNormalizationService">Service that fetches and normalizes webpage content.</param>
/// <param name="sustainabilityClaimsSemanticService">Service that extracts sustainability claims and evaluates their compliance.</param>
/// <param name="topicSpecificRequirementChecks">List of services that checks the topic specific requirement.</param>
public class SustainabilityClaimsService(
    IWebContentNormalizationService webContentNormalizationService,
    ISustainabilityClaimsSemanticService sustainabilityClaimsSemanticService,
    List<TopicSpecificRequirementCheck> topicSpecificRequirementChecks) : ISustainabilityClaimsService
{
    /// <summary>
    /// Main orchestration entry point. Intended flow:
    /// 1) Fetch and normalize content from the URL
    /// 2) Extract sustainability claims and their types
    /// 3) Evaluate claims against general and type-specific requirements; propose alternatives
    /// 4) Aggregate results and return
    /// </summary>
    /// <param name="url">The URL of the page to analyze.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The overall sustainability check result.</returns>
    public async Task<List<SustainabilityClaimComplianceEvaluation>> CheckSustainabilityClaimsAsync(
        string url, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and normalize content
        string normalizedContent = await webContentNormalizationService.FetchAndNormalizeAsync(url, cancellationToken);
        return await CheckSustainabilityClaimsFromStringContentAsync(normalizedContent, url, cancellationToken);
    }

    /// <summary>
    /// Checks sustainability claims from provided string content.
    /// </summary>
    /// <param name="normalizedContent"></param>
    /// <param name="url"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<SustainabilityClaimComplianceEvaluation>> CheckSustainabilityClaimsFromStringContentAsync(
        string normalizedContent, string url, CancellationToken cancellationToken = default)
    {
        // 2. Broad extraction of sustainability-related statements, then
        //    Vattenfall-specific filtering per statement.
        List<SustainabilityClaim> genericClaims = [];
        try
        {
            genericClaims = await sustainabilityClaimsSemanticService.ExtractAllGenericSustainabilityClaimsAsync(normalizedContent, cancellationToken);
        }
        catch (Exception ex)
        {
            return [
                new SustainabilityClaimComplianceEvaluation
                {
                    Claim = new SustainabilityClaim { ClaimText = "Document Analysis Failed", ClaimType = SustainabilityClaimType.Unspecified },
                    Violations = [new RequirementViolation { Code = RequirementCode.General_ClearAndUnambiguous, Message = $"LLM Extraction Error: {ex.Message}" }],
                    SuggestedAlternative = "The AI model failed to return a valid response. Please try again."
                }
            ];
        }

        var claims = new List<SustainabilityClaim>();

        foreach (var genericClaim in genericClaims)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            SustainabilityClaim? filtered = await sustainabilityClaimsSemanticService.FilterVattenfallSustainabilityClaimAsync(
                genericClaim,
                normalizedContent,
                cancellationToken);

            if (filtered is not null)
            {
                filtered.SourceUrl = url.Replace("//", "/");
                claims.Add(filtered);
            }
        }

        // Deduplicate claims with identical text to prevent the same claim from being reported twice
        claims = claims
            .GroupBy(c => c.ClaimText.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        // 3. Evaluate each claim for compliance
        var evaluationsBag = new ConcurrentBag<SustainabilityClaimComplianceEvaluation>();
        var step3ParallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(claims, step3ParallelOptions, async (claim, ct) =>
        {
            try
            {
                var evaluation = await sustainabilityClaimsSemanticService.EvaluateSustainabilityClaimComplianceAsync(claim, ct);
                evaluationsBag.Add(evaluation);
            }
            catch (Exception ex)
            {
                evaluationsBag.Add(new SustainabilityClaimComplianceEvaluation
                {
                    Claim = claim,
                    Violations = [new RequirementViolation { Code = RequirementCode.General_ClearAndUnambiguous, Message = $"LLM Evaluation Error: {ex.Message}" }],
                    SuggestedAlternative = string.Empty
                });
            }
        });

        List<SustainabilityClaimComplianceEvaluation> evaluations = [.. evaluationsBag];

        // 4. For claims failing specific requirements
        await ProcessRequirementSpecificChecksAsync(evaluations, cancellationToken);

        // 5. Check the topic specific requirements and aggregate all results
        return await ProcessTopicSpecificChecksAndAggregateAsync(evaluations, normalizedContent, cancellationToken);
    }

    /// <summary>
    /// Processes requirement-specific checks by examining related URLs for additional evidence.
    /// </summary>
    private async Task ProcessRequirementSpecificChecksAsync(
        List<SustainabilityClaimComplianceEvaluation> evaluations,
        CancellationToken cancellationToken)
    {
        var topicsThatNeedSubstanciation = new HashSet<SubstanciationEvaluationsTopicGroup>();

        // Use a Lazy<Task<string?>> cache to perfectly prevent race conditions and "Cache Stampedes"
        var urlContentCache = new ConcurrentDictionary<string, Lazy<Task<string?>>>(StringComparer.OrdinalIgnoreCase);

        // This lock protects the HashSet from crashing when multiple threads try to write to it at the exact same time
        using var topicUpdateLock = new SemaphoreSlim(1, 1);

        var evaluationsWithViolations = evaluations.Where(e => e.Violations.Count > 0).ToList();

        // The Traffic Cop: Throttle parallel executions to max 3 at a time to protect memory and OpenAI rate limits
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(evaluationsWithViolations, parallelOptions, async (evaluation, ct) =>
        {
            var violationsToRemove = new HashSet<RequirementCode>();

            foreach (var violation in evaluation.Violations)
            {
                if (violationsToRemove.Contains(violation.Code)) continue; // Already satisfied

                // Pass the lazy cache down into the checker
                bool requirementSatisfied = await CheckRequirementAsync(
                    violation.Code,
                    evaluation.Claim,
                    urlContentCache,
                    ct);

                if (requirementSatisfied)
                {
                    lock (violationsToRemove) { violationsToRemove.Add(violation.Code); }
                }

                if (violation.Code == RequirementCode.General_FactuallyCorrectWithSubstanciation)
                {
                    // Securely lock the thread while updating the shared HashSet
                    await topicUpdateLock.WaitAsync(ct);
                    try
                    {
                        await UpdateTopicsThatNeedSubstanciationAsync(topicsThatNeedSubstanciation, evaluation, ct);
                    }
                    finally
                    {
                        topicUpdateLock.Release();
                    }
                }
            }

            // Remove satisfied violations
            if (violationsToRemove.Count > 0)
            {
                evaluation.Violations = [.. evaluation.Violations.Where(v => !violationsToRemove.Contains(v.Code))];
            }
        });

        foreach (var topic in topicsThatNeedSubstanciation)
        {
            if (topic.IsSatisfied)
            {
                foreach (var evaluation in topic.Evaluations)
                {
                    evaluation.Warnings.Add($"Topic {topic.Topic} is satisfied.");
                    evaluation.Violations = [.. evaluation.Violations.Where(v => v.Code != RequirementCode.General_FactuallyCorrectWithSubstanciation)];
                }
            }
        }
    }

    private async Task UpdateTopicsThatNeedSubstanciationAsync(
        HashSet<SubstanciationEvaluationsTopicGroup> existingTopicGroup,
        SustainabilityClaimComplianceEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        (string topic, string topicDescription) =
            await sustainabilityClaimsSemanticService.GetTopicForClaimRequiringEvidenceAsync(
                evaluation.Claim,
                existingTopicGroup.Select(t => (t.Topic, t.TopicDescription)).ToDictionary(t => t.Topic, t => t.TopicDescription),
                cancellationToken
            );

        if (existingTopicGroup.Any(t => t.Topic == topic))
        {
            existingTopicGroup.First(t => t.Topic == topic).Evaluations.Add(evaluation);
        }
        else
        {
            existingTopicGroup.Add(new SubstanciationEvaluationsTopicGroup
            {
                Topic = topic,
                TopicDescription = topicDescription,
                Evaluations = [evaluation]
            });
        }
    }

    /// <summary>
    /// Checks a specific requirement by examining the claim and its related content.
    /// </summary>
    private async Task<bool> CheckRequirementAsync(
        RequirementCode requirementCode,
        SustainabilityClaim claim,
        ConcurrentDictionary<string, Lazy<Task<string?>>> urlContentCache,
        CancellationToken cancellationToken)
    {
        return requirementCode switch
        {
            RequirementCode.General_FactuallyCorrectWithSubstanciation =>
                await CheckSubstanciationRequirementAsync(claim, urlContentCache, cancellationToken),
            RequirementCode.Ambition_PlansAndMeasuresPresent =>
                await CheckPlansAndMeasuresRequirementAsync(claim, urlContentCache, cancellationToken),
            _ => true, // Return true for unsupported requirements (they can't be checked via related content)
        };
    }

    /// <summary>
    /// Checks evidence requirements by examining related URLs.
    /// </summary>
    private async Task<bool> CheckSubstanciationRequirementAsync(
        SustainabilityClaim claim,
        ConcurrentDictionary<string, Lazy<Task<string?>>> urlContentCache,
        CancellationToken cancellationToken)
    {
        string sourceUrl = claim.SourceUrl.Replace("//", "/") ?? string.Empty;
        if (sourceUrl.Contains(HeatRequirementCheck.HeatLabelUrl, StringComparison.OrdinalIgnoreCase) ||
            sourceUrl.Contains(GreenElectricityRequirementCheck.ElectricityBusinessLabelUrl, StringComparison.OrdinalIgnoreCase) ||
            sourceUrl.Contains(GreenElectricityRequirementCheck.ElectricityConsumerLabelUrl, StringComparison.OrdinalIgnoreCase))
            return true; // Bypass substanciation check for energy label claims

        return await CheckUrlsAsync(claim, async (content, ct) =>
            await sustainabilityClaimsSemanticService.DoesContentProvideSubstanciationForClaimAsync(claim, content, ct),
            urlContentCache,
            cancellationToken);
    }

    /// <summary>
    /// Checks plans and measures requirements by examining related URLs.
    /// </summary>
    private async Task<bool> CheckPlansAndMeasuresRequirementAsync(
        SustainabilityClaim claim,
        ConcurrentDictionary<string, Lazy<Task<string?>>> urlContentCache,
        CancellationToken cancellationToken)
    {
        bool ambitionHasPlansAndMeasures = await CheckUrlsAsync(claim, async (content, ct) =>
            await sustainabilityClaimsSemanticService.DoesContentProvidePlansAndMeasuresForClaimAsync(claim, content, ct),
            urlContentCache,
            cancellationToken);

        if (claim.RelatedUrls.Any(_ => _.Contains(".pdf", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return ambitionHasPlansAndMeasures;
    }

    /// <summary>
    /// Generic method to check URLs for a specific requirement.
    /// Now uses a Lazy ConcurrentDictionary cache to prevent Cache Stampedes and duplicate downloads.
    /// </summary>
    private async Task<bool> CheckUrlsAsync(
        SustainabilityClaim claim,
        Func<string, CancellationToken, Task<bool>> requirementCheck,
        ConcurrentDictionary<string, Lazy<Task<string?>>> urlContentCache,
        CancellationToken cancellationToken)
    {
        if (claim.RelatedUrls?.Count == 0) return false;

        foreach (var url in claim.RelatedUrls!)
        {
            try
            {
                // This completely prevents the Cache Stampede. 
                // If 3 threads hit this simultaneously, only one HTTP request is created.
                // The other 2 threads will automatically await the first thread's result.
                string? content = await urlContentCache.GetOrAdd(
                    url,
                    u => new Lazy<Task<string?>>(() => TryFetchContentFromUrlAsync(u, cancellationToken))
                ).Value;

                if (string.IsNullOrEmpty(content)) continue;

                if (await requirementCheck(content, cancellationToken))
                    return true;
            }
            catch
            {
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to fetch and normalize content from a URL if it's a valid HTTP/HTTPS URL.
    /// </summary>
    private async Task<string?> TryFetchContentFromUrlAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var absolute) ||
            (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps))
            return null;

        var targetUrl = absolute.ToString();
        return await webContentNormalizationService.FetchAndNormalizeAsync(targetUrl, cancellationToken);
    }

    private async Task<List<SustainabilityClaimComplianceEvaluation>> ProcessTopicSpecificChecksAndAggregateAsync(
        List<SustainabilityClaimComplianceEvaluation> baseEvaluations,
        string sourceContent,
        CancellationToken cancellationToken)
    {
        var finalEvaluations = new List<SustainabilityClaimComplianceEvaluation>();

        foreach (var baseEvaluation in baseEvaluations)
        {
            var partialEvaluations = new List<SustainabilityClaimComplianceEvaluation> { baseEvaluation };

            foreach (var topic in baseEvaluation.Claim.RelatedTopics)
            {
                TopicSpecificRequirementCheck? requirementCheck = topicSpecificRequirementChecks.FirstOrDefault(x => x.AssociatedTopic == topic);
                if (requirementCheck != null)
                {
                    try
                    {
                        SustainabilityClaimComplianceEvaluation topicSpecificEvaluation = await requirementCheck.CheckTopicComplianceAsync(baseEvaluation.Claim, sourceContent, cancellationToken);
                        partialEvaluations.Add(topicSpecificEvaluation);
                    }
                    catch (Exception ex)
                    {
                        baseEvaluation.Warnings.Add($"Topic check failed for {topic}: {ex.Message}");
                    }
                }
            }

            var aggregateEvaluation = new SustainabilityClaimComplianceEvaluation
            {
                Claim = baseEvaluation.Claim,
                Violations = [.. partialEvaluations.SelectMany(e => e.Violations)],
                Warnings = [.. partialEvaluations.SelectMany(e => e.Warnings)],
                SuggestedAlternative = string.Empty
            };

            bool isCompliant = partialEvaluations.All(e => e.Violations.Count == 0);
            if (!isCompliant && (partialEvaluations.Count > 1 || !string.IsNullOrWhiteSpace(baseEvaluation.SuggestedAlternative) || baseEvaluation.Violations.Count > 0))
            {
                try
                {
                    aggregateEvaluation.SuggestedAlternative = await sustainabilityClaimsSemanticService.MergeSuggestedAlternativesAsync(
                        baseEvaluation.Claim,
                        partialEvaluations,
                        cancellationToken);
                }
                catch (Exception)
                {
                    aggregateEvaluation.SuggestedAlternative = "Could not generate merged alternative due to an AI processing error.";
                }
            }
            finalEvaluations.Add(aggregateEvaluation);
        }

        return finalEvaluations;
    }
}
