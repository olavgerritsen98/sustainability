namespace GenAiIncubator.LlmUtils.ReportRunner.Models;

public record ErrorRow
{
    public string Url { get; init; } = string.Empty;
    public string ErrorType { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}


