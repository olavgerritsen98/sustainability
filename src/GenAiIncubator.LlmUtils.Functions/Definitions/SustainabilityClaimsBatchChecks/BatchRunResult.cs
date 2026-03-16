namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

public sealed class BatchRunResult
{
    public required string RunId { get; init; }
    public required string RunFolder { get; init; }
    public required string FileId { get; init; }
    public required string RunTimestampUtc { get; init; }
    public required DateTime RunDateUtc { get; init; }
    public required string InputContainerName { get; init; }
    public required string InputBlobName { get; init; }
    public List<BatchInputItem> Items { get; init; } = [];
    public List<BatchRowError> ParseErrors { get; init; } = [];
    public List<BatchUrlResult> UrlResults { get; init; } = [];
}
