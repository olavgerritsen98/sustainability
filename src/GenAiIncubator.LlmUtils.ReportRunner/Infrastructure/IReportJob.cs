namespace GenAiIncubator.LlmUtils.ReportRunner.Infrastructure;

public interface IReportJob
{
    Task RunAsync(CancellationToken cancellationToken);
}


