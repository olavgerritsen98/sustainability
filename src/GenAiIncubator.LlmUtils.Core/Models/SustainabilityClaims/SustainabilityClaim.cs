using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;

/// <summary>
/// Represents a single extracted sustainability claim and its classified type.
/// </summary>
public class SustainabilityClaim
{
    /// <summary>
    /// The raw text of the claim as found in the content.
    /// </summary>
    public required string ClaimText { get; set; }

    /// <summary>
    /// The classified claim type.
    /// </summary>
    public SustainabilityClaimType ClaimType { get; set; } = SustainabilityClaimType.Unspecified;

    /// <summary>
    /// Additional surrounding text that provides context for the claim.
    /// Helpful to inform downstream checks (e.g., substanciation, evidence or requirement evaluation).
    /// </summary>
    public string RelatedText { get; set; } = string.Empty;

    /// <summary>
    /// Short reasoning describing why this text was considered a sustainability claim and how it was classified.
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Potential substanciation URLs that are directly on or next to the claim (one click away).
    /// Extracted from the base HTML during claim extraction.
    /// </summary>
    public List<string> RelatedUrls { get; set; } = [];

    /// <summary>
    /// The canonical/source URL from which this claim was extracted (page where the claim lives).
    /// Useful for providing page-level context distinct from immediate RelatedUrls evidence links.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// List of topics that the claim is related to.
    /// </summary>
    public List<SustainabilityTopicsEnum> RelatedTopics { get; set; } = [];

    /// <summary>
    /// A summary of the page from which the claim was extracted.
    /// Provides context for the claim within the broader content.
    /// </summary>
    public string PageSummary { get; set; } = string.Empty;
}
