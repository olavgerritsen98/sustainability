using System.Text.Json.Serialization;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Response schema for selecting or creating a topic for a claim requiring evidence.
/// </summary>
public class SustainabilityClaimTopicSelectionResponse
{
    /// <summary>
    /// The name of the existing topic to reuse (required when ReuseExistingTopic is true).
    /// </summary>
    [JsonPropertyName("topicName")]
    public string? TopicName { get; set; }

    /// <summary>
    /// The description of the existing topic (optional, echo from input for clarity).
    /// </summary>
    [JsonPropertyName("topicDescription")]
    public string? TopicDescription { get; set; }
}


