namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Represents the response of a call to the sustainability claims extraction service.
/// </summary>
public class SustainabilityClaimsExtractionResponse 
{
    /// <summary>
    /// The extracted sustainability claims.
    /// </summary>
    public required IList<RecognisedSustainabilityClaim> Claims { get; set; }

    /// <summary>
    /// A summary of the page from which the claims were extracted.
    /// </summary>
    public required string PageSummary { get; set; }
}
