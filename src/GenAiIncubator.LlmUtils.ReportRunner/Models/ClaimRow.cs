namespace GenAiIncubator.LlmUtils.ReportRunner.Models;

public record ClaimRow
{
    public string Url { get; init; } = string.Empty;
    public string ClaimId { get; init; } = string.Empty;
    public string ClaimText { get; init; } = string.Empty;
    public string RelatedText { get; init; } = string.Empty;
    public string ClaimReason { get; init; } = string.Empty;
    public string TranslatedClaim { get; init; } = string.Empty;
    public string ClaimType { get; init; } = string.Empty;
    public bool IsCompliant { get; init; }
    public int NumViolations { get; init; }
    public string ViolationCodes { get; init; } = string.Empty;
    public string Warnings { get; init; } = string.Empty;
    public string SuggestedAlternative { get; init; } = string.Empty;
    public string RelatedUrls { get; init; } = string.Empty;
    public string RunTimestamp { get; init; } = string.Empty;
}


