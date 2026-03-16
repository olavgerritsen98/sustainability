namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

public sealed class BatchParseResult
{
    public List<BatchInputItem> Items { get; init; } = [];
    public List<BatchRowError> Errors { get; init; } = [];
}
