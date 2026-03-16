namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

public sealed class BatchRunTriggerInput
{
    public required string InputContainerName { get; init; }
    public required string InputBlobName { get; init; }
    public required string FileId { get; init; }
    public required string RunTimestampUtc { get; init; }
    public required DateTime RunDateUtc { get; init; }
    public required int MaxDegreeOfParallelism { get; init; }
    public bool LogEachUrl { get; init; }
}
