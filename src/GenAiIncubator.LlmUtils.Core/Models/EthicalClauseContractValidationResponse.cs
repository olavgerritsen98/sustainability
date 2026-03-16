namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents the result of validating whether a contract contains an ethical clause.
/// </summary>
/// <param name="HasEthicalClause">True if an ethical clause is present; otherwise false.</param>
/// <param name="Reasoning">Human-readable reasoning that explains the outcome.</param>
/// <param name="ExtractedClauseText">Optional extracted clause text if available.</param>
public sealed record EthicalClauseContractValidationResponse(
    bool HasEthicalClause,
    string Reasoning,
    string? ExtractedClauseText = null);


