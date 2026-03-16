namespace GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;

/// <summary>
/// The evaluation result for a single sustainability claim, including per-requirement violations and a suggested alternative.
/// </summary>
public class SustainabilityClaimComplianceEvaluation
{
    /// <summary>
    /// The claim that was evaluated.
    /// </summary>
    public required SustainabilityClaim Claim { get; set; }

    /// <summary>
    /// Whether the claim satisfies all applicable requirements.
    /// Returns true when there are no violations, false otherwise.
    /// </summary>
    public bool IsCompliant => Violations.Count == 0;

    /// <summary>
    /// Detailed list of requirement violations (empty when compliant).
    /// </summary>
    public List<RequirementViolation> Violations { get; set; } = [];

    /// <summary>
    /// List of warnings that were raised during the evaluation, but did not result in a violation.
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Optional suggested alternative phrasing when the claim is non-compliant.
    /// </summary>
    public string SuggestedAlternative { get; set; } = string.Empty;
}
