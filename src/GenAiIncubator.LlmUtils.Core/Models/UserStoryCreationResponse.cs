using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents the response of a heater type classification.
/// </summary>
public class UserStoryCreationResponse 
{
    /// <summary>
    /// Created user story.
    /// </summary>
    // [JsonConverter(typeof(JsonStringEnumConverter))]
    public required string UserStory { get; set; } = "";

    /// <summary>
    /// Missing info useful to create the user story.
    /// </summary>
    public string MissingInfo { get; set; } = "";
}