namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Structured response for evaluating whether provided content offers sufficient substanciation for a claim.
/// </summary>
public class SustainabilityClaimSubstanciationCheckResponse
{
    /// <summary>
    /// Indicates whether the substanciation content provides sufficient support for the claim.
    /// </summary>
    public bool ProvidesSubstanciation { get; set; }

    /// <summary>
    /// Short explanation about the decision.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}



