namespace GenAiIncubator.LlmUtils.ReportRunner.Infrastructure;

public interface IReportWriter
{
    Task WriteAsync<T>(string path, IEnumerable<T> rows, CancellationToken cancellationToken = default);
}


