namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

public interface IBatchReportWriter
{
    Task<BatchReportArtifacts> WriteAsync(BatchRunResult result, CancellationToken ct);
}
