namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;

/// <summary>
/// Represents a single extracted sustainability claim and its classified type.
/// </summary>
public class FilteredSustainabilityClaim
{
    /// <summary>
    /// Initial recognised claim.
    /// </summary>
    public required RecognisedSustainabilityClaim Claim { get; set; }

    /// <summary>
    /// Indicates whether the claim is a Vattenfall sustainability claim.
    /// </summary>
    public required bool IsVattenfallClaim { get; set; }

    /// <summary>
    /// Reasoning behind the classification of whether this is a Vattenfall claim.
    /// </summary>
    public required string Reasoning { get; set; }
}