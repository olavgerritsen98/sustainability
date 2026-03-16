namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

public sealed class BatchInputItem
{
    public required int RowNumber { get; init; }
    public required string PageUrl { get; init; }
    public string? PublicationDate { get; init; }
    public string? ResponsibleForContents { get; init; }
    public string? AccountableForContents { get; init; }
}
