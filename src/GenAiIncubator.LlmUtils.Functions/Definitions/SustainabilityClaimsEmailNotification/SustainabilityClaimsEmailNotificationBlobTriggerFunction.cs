using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsEmailNotification;

/// <summary>
/// Blob-triggered Azure Function that watches the <c>sustainability-email-input</c> container
/// for CSV files, parses each row and dispatches Dutch-language compliance notification emails
/// according to the rules defined in work item 808890.
/// <para>
/// Drop any sustainability claims meta-report CSV into the <c>sustainability-email-input</c>
/// container (same storage account as <c>AzureWebJobsStorage</c>) to trigger email notifications.
/// </para>
/// </summary>
public sealed class SustainabilityClaimsEmailNotificationBlobTriggerFunction
{
    private const string InputContainerName = "sustainability-email-input";

    private readonly SustainabilityClaimsEmailNotificationService _notificationService;
    private readonly BlobServiceClient _blobServiceClient;

    public SustainabilityClaimsEmailNotificationBlobTriggerFunction(
        SustainabilityClaimsEmailNotificationService notificationService,
        BlobServiceClient blobServiceClient)
    {
        _notificationService = notificationService;
        _blobServiceClient = blobServiceClient;
    }

    [Function("SustainabilityClaimsEmailNotification_BlobTrigger")]
    public async Task RunAsync(
        [BlobTrigger(InputContainerName + "/{name}", Connection = "AzureWebJobsStorage")] BlobClient blobClient,
        FunctionContext context,
        CancellationToken ct)
    {
        ILogger logger = context.GetLogger<SustainabilityClaimsEmailNotificationBlobTriggerFunction>();

        string blobName = blobClient.Name;

        if (!blobName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Blob '{BlobName}' is not a CSV file. Skipping.",
                blobName);
            return;
        }

        logger.LogInformation(
            "Processing sustainability claims email notification for blob '{BlobName}'.",
            blobName);

        List<SustainabilityClaimsEmailNotificationRow> rows;

        try
        {
            rows = await ParseCsvAsync(blobClient, logger, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse CSV from blob '{BlobName}'.", blobName);
            throw;
        }

        logger.LogInformation(
            "Parsed {RowCount} row(s) from '{BlobName}'. Processing email notifications.",
            rows.Count,
            blobName);

        Dictionary<string, byte[]> attachmentsByBlobPath = await DownloadAttachmentsAsync(rows, logger, ct);

        await _notificationService.ProcessReportRowsAsync(rows, attachmentsByBlobPath, ct);

        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);

        logger.LogInformation(
            "Finished processing email notifications for blob '{BlobName}'. Blob deleted from trigger container.",
            blobName);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────
    private async Task<Dictionary<string, byte[]>> DownloadAttachmentsAsync(
        List<SustainabilityClaimsEmailNotificationRow> rows,
        ILogger logger,
        CancellationToken ct)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        foreach (SustainabilityClaimsEmailNotificationRow row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.PageReportBlobPath))
                continue;

            // Only download for non-compliant pages; compliant pages never trigger an email
            bool isNonCompliant = !string.Equals(row.PaginaCompliant, "true", StringComparison.OrdinalIgnoreCase);
            if (!isNonCompliant)
                continue;

            int firstSlash = row.PageReportBlobPath.IndexOf('/');
            if (firstSlash < 0)
            {
                logger.LogWarning("Could not parse blob path '{BlobPath}'. Skipping attachment.", row.PageReportBlobPath);
                continue;
            }

            string containerName = row.PageReportBlobPath[..firstSlash];
            string blobName = row.PageReportBlobPath[(firstSlash + 1)..];

            try
            {
                BlobClient blobClient = _blobServiceClient
                    .GetBlobContainerClient(containerName)
                    .GetBlobClient(blobName);

                Azure.Response<Azure.Storage.Blobs.Models.BlobDownloadResult> download =
                    await blobClient.DownloadContentAsync(ct);

                result[row.PageReportBlobPath] = download.Value.Content.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to download attachment '{BlobPath}'. Email will be sent without attachment.",
                    row.PageReportBlobPath);
            }
        }

        return result;
    }
    private static async Task<List<SustainabilityClaimsEmailNotificationRow>> ParseCsvAsync(
        BlobClient blobClient,
        ILogger logger,
        CancellationToken ct)
    {
        var rows = new List<SustainabilityClaimsEmailNotificationRow>();

        var downloadResponse = await blobClient.DownloadStreamingAsync(cancellationToken: ct);

        await using var stream = downloadResponse.Value.Content;
        using var reader = new StreamReader(stream);

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            // BatchReportFormatter writes semicolon-delimited CSVs (matches Excel locale).
            Delimiter = ";",
            // Tolerant parsing: ignore missing columns and extra headers.
            // This keeps the function working even if BatchReportFormatter adds new columns later.
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, csvConfig);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                var row = csv.GetRecord<SustainabilityClaimsEmailNotificationRow>();
                if (row is not null)
                    rows.Add(row);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Skipping unparseable row at CSV line {Line}.",
                    csv.Context.Parser?.RawRow);
            }
        }

        return rows;
    }
}
