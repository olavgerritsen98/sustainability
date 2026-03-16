using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

/// <summary>
/// Writes batch report artifacts to blob storage.
/// </summary>
public sealed class SustainabilityClaimsBatchCheckWriteReportActivity
{
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>
    /// Creates a new report writer activity.
    /// </summary>
    public SustainabilityClaimsBatchCheckWriteReportActivity()
    {
        string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AzureWebJobsStorage is not configured.");

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    /// <summary>
    /// Writes the batch report CSVs.
    /// </summary>
    [Function(SustainabilityClaimsBatchCheckOrchestrator.WriteReportActivityName)]
    public async Task<BatchReportArtifacts> RunAsync(
        [ActivityTrigger] BatchRunResult result,
        FunctionContext context,
        CancellationToken ct)
    {
        ILogger logger = context.GetLogger<SustainabilityClaimsBatchCheckWriteReportActivity>();

        string containerName = GetOutputContainerName();
        BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        logger.LogInformation("Writing batch artifacts to container {ContainerUri} (name={ContainerName})", container.Uri, containerName);

        BatchReportWriter writer = new(container);
        BatchReportArtifacts artifacts = await writer.WriteAsync(result, ct);

        logger.LogInformation(
            "Report writing completed. Artifacts={ArtifactCount}",
            artifacts.BlobPaths.Count);

        bool isSuccess = result.ParseErrors.Count == 0 && result.UrlResults.All(r => r.Response is not null);
        await MoveInputBlobAsync(result, isSuccess, logger, ct);

        return artifacts;
    }
  
    private static string GetOutputContainerName()
    {
        string? configured = Environment.GetEnvironmentVariable("BATCH_OUTPUT_CONTAINER");
        return string.IsNullOrWhiteSpace(configured) ? "batch-output" : configured;
    }

    private static string GetSuccessContainerName()
    {
        string? configured = Environment.GetEnvironmentVariable("BATCH_SUCCESS_CONTAINER");
        return string.IsNullOrWhiteSpace(configured) ? "batch-success" : configured;
    }

    private static string GetPoisonContainerName()
    {
        string? configured = Environment.GetEnvironmentVariable("BATCH_POISON_CONTAINER");
        return string.IsNullOrWhiteSpace(configured) ? "batch-deadletter" : configured;
    }

    private async Task MoveInputBlobAsync(BatchRunResult result, bool isSuccess, ILogger logger, CancellationToken ct)
    {
        string destinationContainerName = isSuccess ? GetSuccessContainerName() : GetPoisonContainerName();

        BlobContainerClient sourceContainer = _blobServiceClient.GetBlobContainerClient(result.InputContainerName);
        BlobContainerClient destinationContainer = _blobServiceClient.GetBlobContainerClient(destinationContainerName);
        await destinationContainer.CreateIfNotExistsAsync(cancellationToken: ct);

        BlobClient sourceBlob = sourceContainer.GetBlobClient(result.InputBlobName);
        if (!await sourceBlob.ExistsAsync(ct))
        {
            logger.LogWarning(
                "Input blob not found for move. Source={SourceUri}",
                sourceBlob.Uri);
            return;
        }

        string fileName = Path.GetFileName(result.InputBlobName);
        string destinationBlobName = $"{result.RunTimestampUtc}/{fileName}";
        BlobClient destinationBlob = destinationContainer.GetBlobClient(destinationBlobName);

        logger.LogInformation(
            "Moving input blob. Source={SourceUri} Destination={DestinationUri} Container={DestinationContainer}",
            sourceBlob.Uri,
            destinationBlob.Uri,
            destinationContainerName);

        await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: ct);

        for (int attempt = 0; attempt < 10; attempt++)
        {
            BlobProperties props = (await destinationBlob.GetPropertiesAsync(cancellationToken: ct)).Value;
            if (props.CopyStatus != CopyStatus.Pending)
            {
                if (props.CopyStatus == CopyStatus.Success)
                    await sourceBlob.DeleteIfExistsAsync(cancellationToken: ct);

                logger.LogInformation(
                    "Input blob move completed. CopyStatus={CopyStatus}",
                    props.CopyStatus);
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }
}
