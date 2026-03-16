using NPOI.SS.UserModel;

namespace GenAiIncubator.LlmUtils.ReportRunner.Services;

/// <summary>
/// Appends a source sheet from a ground truth workbook into a target sheet at a given row.
/// </summary>
public interface IGroundTruthSheetAppender
{
    int AppendSheet(ISheet targetSheet, int startRow, string workbookPath, string tabName);
}


