using GenAiIncubator.LlmUtils.ReportRunner.Models;

namespace GenAiIncubator.LlmUtils.ReportRunner.Services;

/// <summary>
/// Supplies URLs and manual workbook mapping for a report run.
/// </summary>
public interface IReportInputProvider
{
    Task<ReportInput> GetAsync(CancellationToken cancellationToken);
}


