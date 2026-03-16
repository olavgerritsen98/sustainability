namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents the structured LLM output for determining whether an ethical clause is supported by a contract.
/// </summary>
public sealed class EthicalClauseContractValidationDecision
{
    /// <summary>
    /// True if the clause is explicitly supported by the contract; otherwise false.
    /// </summary>
    public required bool IsSupported { get; set; }

    /// <summary>
    /// Short human-readable reasoning for the decision.
    /// </summary>
    public required string Reasoning { get; set; }
}


