namespace GenAiIncubator.LlmUtils_Functions.Definitions.EthicalClauseContractValidation;

/// <summary>
/// Represents the API response for ethical clause contract validation.
/// </summary>
public class EthicalClauseContractValidationResponse
{
    /// <summary>
    /// True if an ethical clause is present; otherwise false.
    /// </summary>
    public required bool HasEthicalClause { get; set; }

    /// <summary>
    /// Human-readable reasoning that explains the outcome.
    /// </summary>
    public required string Reasoning { get; set; }

    /// <summary>
    /// Optional extracted clause text if available.
    /// </summary>
    public string? ExtractedClauseText { get; set; }
}


