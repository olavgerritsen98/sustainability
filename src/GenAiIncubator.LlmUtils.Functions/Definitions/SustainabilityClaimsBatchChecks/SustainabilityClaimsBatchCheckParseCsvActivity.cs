using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

/// <summary>
/// Parses the uploaded CSV into typed batch input items and row-level errors.
/// </summary>
public sealed class SustainabilityClaimsBatchCheckParseCsvActivity
{
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>
    /// Parses the CSV payload into structured rows.
    /// </summary>
    [Function(SustainabilityClaimsBatchCheckOrchestrator.ParseCsvActivityName)]
    public Task<BatchParseResult> RunAsync(
        [ActivityTrigger] BatchRunTriggerInput input,
        FunctionContext context,
        CancellationToken ct)
    {
        ILogger logger = context.GetLogger<SustainabilityClaimsBatchCheckParseCsvActivity>();
        return RunInternalAsync(input, logger, ct);
    }

    /// <summary>
    /// Creates a new parse activity.
    /// </summary>
    public SustainabilityClaimsBatchCheckParseCsvActivity()
    {
        string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AzureWebJobsStorage is not configured.");

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    private async Task<BatchParseResult> RunInternalAsync(BatchRunTriggerInput input, ILogger logger, CancellationToken ct)
    {
        BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(input.InputContainerName);
        BlobClient blob = container.GetBlobClient(input.InputBlobName);

        logger.LogInformation(
            "Downloading CSV for parsing. Container={ContainerUri} Blob={BlobUri}",
            container.Uri,
            blob.Uri);

        var response = await blob.DownloadContentAsync(ct);
        byte[] bytes = response.Value.Content.ToArray();

        logger.LogInformation("CSV downloaded. Bytes={ByteCount}", bytes.Length);

        BatchParseResult parsed = BatchCsvParser.Parse(bytes, ct);
        logger.LogInformation(
            "CSV parsed. Rows={RowCount}, Errors={ErrorCount}",
            parsed.Items.Count,
            parsed.Errors.Count);

        return parsed;
    }
}
