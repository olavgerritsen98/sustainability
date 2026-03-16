using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;

/// <summary>
/// Contract for semantic (AI-based) operations for sustainability claims.
/// </summary>
public interface ISustainabilityClaimsSemanticService
{
    /// <summary>
    /// Broad extraction of all sustainability-related statements from the provided content,
    /// without applying the stricter Vattenfall claim definition.
    /// </summary>
    /// <param name="normalizedContent">Normalized content from the webpage.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of extracted sustainability-related statements.</returns>
    Task<List<SustainabilityClaim>> ExtractAllGenericSustainabilityClaimsAsync(
        string normalizedContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the Vattenfall-specific sustainability claim definition to a single
    /// broadly sustainability-related statement.
    /// </summary>
    /// <param name="genericClaim">A broadly extracted sustainability-related statement.</param>
    /// <param name="sourceContent">Full normalized content from which the statement was extracted.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A Vattenfall sustainability claim derived from the input statement, or null
    /// if the statement is not considered a Vattenfall sustainability claim.
    /// </returns>
    Task<SustainabilityClaim?> FilterVattenfallSustainabilityClaimAsync(
        SustainabilityClaim genericClaim,
        string sourceContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a sustainability claim meets compliance requirements for its type.
    /// </summary>
    /// <param name="claim">The sustainability claim to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A compliance evaluation containing any violations and suggested alternatives.</returns>
    Task<SustainabilityClaimComplianceEvaluation> EvaluateSustainabilityClaimComplianceAsync(
        SustainabilityClaim claim,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the provided content supplies sufficient, accessible substanciation supporting the claim.
    /// </summary>
    /// <param name="claim">The sustainability claim.</param>
    /// <param name="substanciationContent">Normalized content fetched from a related URL (one click away).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if content provides substanciation for the claim; otherwise false.</returns>
    Task<bool> DoesContentProvideSubstanciationForClaimAsync(
        SustainabilityClaim claim,
        string substanciationContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the provided content contains sufficient plans and measures supporting an ambition claim.
    /// </summary>
    /// <param name="claim">The sustainability ambition claim.</param>
    /// <param name="evidenceContent">Normalized content fetched from a related URL (one click away).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if content provides plans and measures for the ambition claim; otherwise false.</returns>
    Task<bool> DoesContentProvidePlansAndMeasuresForClaimAsync(
        SustainabilityClaim claim,
        string evidenceContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the topic for a claim requiring evidence.
    /// </summary>
    /// <param name="claim">The sustainability claim.</param>
    /// <param name="existingTopicsDescriptions">The existing topics descriptions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A key-value pair of the topic description and the topic name. Either a new topic or an existing one.</returns>
    Task<(string, string)> GetTopicForClaimRequiringEvidenceAsync(
        SustainabilityClaim claim,
        Dictionary<string, string> existingTopicsDescriptions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges multiple suggested alternatives into a single cohesive suggestion, taking into account the violations that triggered them.
    /// </summary>
    /// <param name="claim">The original sustainability claim.</param>
    /// <param name="evaluations">List of compliance evaluations (containing violations and suggestions) to merge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A single merged suggested alternative.</returns>
    Task<string> MergeSuggestedAlternativesAsync(
        SustainabilityClaim claim,
        List<SustainabilityClaimComplianceEvaluation> evaluations,
        CancellationToken cancellationToken = default);
}