namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

internal sealed class BatchManifest
{
    public required string RunId { get; init; }
    public required string FileId { get; init; }
    public required string RunTimestampUtc { get; init; }
    public required string InputContainerName { get; init; }
    public required string InputBlobName { get; init; }
    public required int TotalRows { get; init; }
    public required int AnalyzedUrls { get; init; }
    public required int FailedUrls { get; init; }
    public List<string> ArtifactPaths { get; init; } = [];
}
