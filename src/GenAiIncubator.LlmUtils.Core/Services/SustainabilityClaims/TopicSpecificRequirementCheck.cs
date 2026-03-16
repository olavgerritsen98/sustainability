using System.Collections.Generic;
using System.Linq;
using Azure;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;

/// <summary>
/// Abstract base class for topic-specific requirements.
/// </summary>
public abstract class TopicSpecificRequirementCheck
{
    /// <summary>
    /// The requirement code associated with the topic-specific requirement.
    /// </summary>
    public abstract RequirementCode AssociatedRequirementCode { get; }

    /// <summary>
    /// The sustainability topic associated with the requirement.
    /// </summary>
    public abstract SustainabilityTopicsEnum AssociatedTopic { get; }

    /// <summary>
    /// Checks the topic-specific requirement.      
    /// </summary>
    /// <param name="claim">The sustainability claim to evaluate.</param>
    /// <param name="sourceContent">Source content containing the claim.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A sustainability claim compliance evaluation.</returns>
    public async Task<SustainabilityClaimComplianceEvaluation> CheckTopicComplianceAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken)
    {
        List<TopicComplianceEvaluationResponse> responses = await CheckRequirementAsync(claim, sourceContent, cancellationToken);
        List<RequirementViolation> violations = [];
        foreach (var r in responses)
        {
            r.Reasoning = RemoveHtmlBreaks(r.Reasoning);
            r.Warning = RemoveHtmlBreaks(r.Warning);
            r.SuggestedAlternative = RemoveHtmlBreaks(r.SuggestedAlternative);
        }
        foreach (var violation in responses.Where(r => !r.IsCompliant))
        {
            violations.Add(new RequirementViolation
            {
                Code = AssociatedRequirementCode,
                Message = violation.Reasoning,
            });
        }

        return new SustainabilityClaimComplianceEvaluation
        {
            Claim = claim,
            Violations = violations,
            Warnings = [.. responses.Select(r => r.Warning).Where(s => !string.IsNullOrWhiteSpace(s))],
            SuggestedAlternative = string.Join(" | ", responses
                .Where(r => !r.IsCompliant)
                .Select(r => r.SuggestedAlternative?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct())
        };
    }

    /// <summary>
    /// Checks the topic-specific requirement for a list of claims.
    /// If topic requires checking on multiple claims, this method can be overridden to provide optimized implementation.
    /// </summary>
    /// <param name="claims">List of sustainability claims to evaluate.</param>
    /// <param name="sourceContent">Source content containing the claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of sustainability claim compliance evaluations.</returns>
    public async Task<List<SustainabilityClaimComplianceEvaluation>> CheckTopicComplianceAsync(List<SustainabilityClaim> claims, string sourceContent, CancellationToken cancellationToken)
    {
        var evaluations = new List<SustainabilityClaimComplianceEvaluation>();
        foreach (var claim in claims)
        {
            var evaluation = await CheckTopicComplianceAsync(claim, sourceContent, cancellationToken);
            evaluations.Add(evaluation);
        }
        return evaluations;
    }

    /// <summary>
    /// Checks the topic-specific requirement.
    /// </summary>
    /// <param name="claim">The sustainability claim to evaluate.</param>
    /// <param name="sourceContent"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A topic compliance evaluation response.</returns>
    protected abstract Task<List<TopicComplianceEvaluationResponse>> CheckRequirementAsync(SustainabilityClaim claim, string sourceContent, CancellationToken cancellationToken = default);
    private static string RemoveHtmlBreaks(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        return input
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase);
    }
}