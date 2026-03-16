namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Represents the response for the suggested alternative merge operation.
/// </summary>
public class SuggestedAlternativeMergeResponse
{
    /// <summary>
    /// The merged suggested alternative text.
    /// </summary>
    public required string SuggestedAlternative { get; set; }
}
