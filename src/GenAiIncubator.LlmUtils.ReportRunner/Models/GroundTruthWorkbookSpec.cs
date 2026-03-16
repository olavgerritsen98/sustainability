namespace GenAiIncubator.LlmUtils.ReportRunner.Models;

/// <summary>
/// Describes a ground truth labels workbook and its URL to tab mappings.
/// </summary>
public sealed record GroundTruthWorkbookSpec
{
    /// <summary>
    /// Absolute path to the ground truth workbook.
    /// </summary>
    public string WorkbookPath { get; init; } = string.Empty;

    /// <summary>
    /// Mapping from URL to tab (sheet) name in the ground truth workbook.
    /// Keys are treated case-insensitively at runtime.
    /// </summary>
    public IReadOnlyDictionary<string, string> UrlToTabName { get; init; } = new Dictionary<string, string>();
}


