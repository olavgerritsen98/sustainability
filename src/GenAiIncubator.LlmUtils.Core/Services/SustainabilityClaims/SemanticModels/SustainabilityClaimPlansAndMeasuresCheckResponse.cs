namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Structured response for evaluating whether provided content contains sufficient plans and measures for an ambition claim.
/// </summary>
public class SustainabilityClaimPlansAndMeasuresCheckResponse
{
    /// <summary>
    /// Indicates whether the content provides sufficient plans and measures for the ambition claim.
    /// </summary>
    public bool ProvidesPlansAndMeasures { get; set; }

    /// <summary>
    /// Short explanation about the decision.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
