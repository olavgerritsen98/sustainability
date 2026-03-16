namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

public sealed class BatchRowError
{
    public required int RowNumber { get; init; }
    public required string Message { get; init; }
}
