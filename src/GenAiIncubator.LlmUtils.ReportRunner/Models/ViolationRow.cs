namespace GenAiIncubator.LlmUtils.ReportRunner.Models;

public record ViolationRow
{
    public string Url { get; init; } = string.Empty;
    public string ClaimId { get; init; } = string.Empty;
    public string ViolationCode { get; init; } = string.Empty;
    public string ViolationMessage { get; init; } = string.Empty;
}


