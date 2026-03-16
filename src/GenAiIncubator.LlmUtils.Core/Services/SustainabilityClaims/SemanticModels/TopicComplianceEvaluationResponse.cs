namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Response schema for selecting or creating a topic for a claim requiring evidence.
/// </summary>
public class TopicComplianceEvaluationResponse
{
    /// <summary>
    /// Indicates whether the claim is compliant with the topic-specific requirement.
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// The reasoning behind the compliance evaluation.
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Suggested alternative wording for the claim if it is not compliant (optional).
    /// </summary>
    public string SuggestedAlternative { get; set; } = string.Empty;

    /// <summary>
    /// Warning message if applicable (optional).
    /// </summary>
    public string Warning { get; set; } = string.Empty;
}