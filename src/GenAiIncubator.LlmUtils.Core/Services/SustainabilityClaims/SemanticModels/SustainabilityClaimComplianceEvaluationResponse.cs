using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Represents the response of a call to the sustainability claims extraction service.
/// </summary>
public class SustainabilityClaimComplianceEvaluationResponse
{
    /// <summary>
    /// Gets or sets the list of requirement violations found during the compliance evaluation.
    /// </summary>
    public required List<RequirementViolation> Violations { get; set; }

    /// <summary>
    /// Gets or sets the suggested alternative text that complies with sustainability claim requirements.
    /// </summary>
    public string? SuggestedAlternative { get; set; }
}
