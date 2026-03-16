using System.Globalization;
using System.Text;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

/// <summary>
/// Starts a batch sustainability claims run when a CSV is uploaded.
/// </summary>
public sealed class SustainabilityClaimsBatchCheckBlobTriggerFunction
{
    private const string InputContainerName = "batch-input";
    private const int DefaultMaxDop = 3;

    /// <summary>
    /// Runs when a CSV is uploaded to the input container and schedules the batch orchestrator.
    /// </summary>
    [Function("SustainabilityClaimsBatchCheck_BlobTrigger")]
    public async Task RunAsync(
        [BlobTrigger(InputContainerName + "/{name}", Connection = "AzureWebJobsStorage")] BlobClient blobClient,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        CancellationToken ct)
    {
        string name = blobClient.Name;
        if (string.IsNullOrWhiteSpace(name))
            return;

        ILogger logger = context.GetLogger<SustainabilityClaimsBatchCheckBlobTriggerFunction>();

        string fileId = SanitizeFileId(Path.GetFileNameWithoutExtension(name));
        int maxDop = ReadMaxDegreeOfParallelism();
        bool logEachUrl = ReadLogEachUrl();
        DateTime runDateUtc = DateTime.UtcNow;
        string runTimestampUtc = runDateUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        var input = new BatchRunTriggerInput
        {
            InputContainerName = InputContainerName,
            InputBlobName = name,
            FileId = fileId,
            RunDateUtc = runDateUtc,
            RunTimestampUtc = runTimestampUtc,
            MaxDegreeOfParallelism = maxDop,
            LogEachUrl = logEachUrl
        };

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            SustainabilityClaimsBatchCheckOrchestrator.OrchestratorName,
            input,
            cancellation: ct);

        logger.LogInformation(
            "Batch sustainability claims run scheduled. InstanceId={InstanceId}, BlobName={BlobName}",
            instanceId,
            name);
    }

    private static int ReadMaxDegreeOfParallelism()
    {
        string? raw = Environment.GetEnvironmentVariable("BATCH_MAX_DOP");
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dop)
            && dop > 0
            ? dop
            : DefaultMaxDop;
    }

    private static bool ReadLogEachUrl()
    {
        string? raw = Environment.GetEnvironmentVariable("BATCH_LOG_EACH_URL");
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "input";

        var builder = new StringBuilder(raw.Length);
        foreach (char ch in raw)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')
            {
                builder.Append(ch);
            }
            else
            {
                builder.Append('_');
            }
        }

        string sanitized = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "input" : sanitized;
    }
}
