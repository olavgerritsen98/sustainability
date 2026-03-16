namespace GenAiIncubator.LlmUtils.ReportRunner.Models;

/// <summary>
/// Input for the report run: URLs plus optional manual workbook specification.
/// </summary>
public sealed record ReportInput
{
    public GroundTruthWorkbookSpec? GroundTruth { get; init; }
}


