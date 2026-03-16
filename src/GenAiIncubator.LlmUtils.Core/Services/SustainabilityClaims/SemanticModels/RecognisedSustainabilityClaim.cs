using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Models;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Represents a single extracted sustainability claim and its classified type.
/// </summary>
public class RecognisedSustainabilityClaim
{
    /// <summary>
    /// The raw text of the claim as found in the content.
    /// </summary>
    public required string ClaimText { get; set; }

    /// <summary>
    /// The classified claim type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SustainabilityClaimType ClaimType { get; set; } = SustainabilityClaimType.Unspecified;

    /// <summary>
    /// The reasoning behind the claim classification.
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Additional surrounding text that provides context for the claim.
    /// Useful when the main claim text is short or when nearby text may
    /// support evidence checks in subsequent steps.
    /// </summary>
    public string RelatedText { get; set; } = string.Empty;

    /// <summary>
    /// Potential evidence URLs that are directly on or next to the claim (one click away).
    /// These are initially extracted from the base HTML content.
    /// </summary>
    public List<string> RelatedUrls { get; set; } = [];

    /// <summary>
    /// List of topics that the claim is related to.
    /// </summary>
    public List<SustainabilityTopicsEnum> RelatedTopics { get; set; } = [];
}