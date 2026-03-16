using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsCheckerAsync;
using GenAiIncubator.LlmUtils_Functions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

/// <summary>
/// Durable orchestrator for batch sustainability claims checks.
/// </summary>
public static class SustainabilityClaimsBatchCheckOrchestrator
{
    public const string OrchestratorName = "SustainabilityClaimsBatchCheck_Orchestrator";
    public const string ParseCsvActivityName = "SustainabilityClaimsBatchCheck_ParseCsv";
    public const string WriteReportActivityName = "SustainabilityClaimsBatchCheck_WriteReport";

    /// <summary>
    /// Runs the batch orchestration: download CSV, parse rows, fan-out analyses, and write reports.
    /// </summary>
    [Function(OrchestratorName)]
    public static async Task<BatchReportArtifacts> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        FunctionContext functionContext)
    {
        ILogger logger = functionContext.GetLogger(nameof(SustainabilityClaimsBatchCheckOrchestrator));

        BatchRunTriggerInput input = context.GetInput<BatchRunTriggerInput>()
            ?? throw new InvalidOperationException("Orchestration input is missing.");

        logger.LogInformationReplaySafe(
            context,
            "Batch orchestration started. InstanceId={InstanceId}, FileId={FileId}, Input={Container}/{Blob}, MaxDop={MaxDop}",
            context.InstanceId,
            input.FileId,
            input.InputContainerName,
            input.InputBlobName,
            input.MaxDegreeOfParallelism);

        BatchParseResult parsedResult = await context.CallActivityAsync<BatchParseResult>(
            ParseCsvActivityName,
            input);

        logger.LogInformationReplaySafe(
            context,
            "CSV parsed. Rows={RowCount}, ParseErrors={ErrorCount}",
            parsedResult.Items.Count,
            parsedResult.Errors.Count);

        List<BatchUrlResult> urlResults = await AnalyzeUrlsAsync(
            context,
            logger,
            parsedResult.Items,
            input.MaxDegreeOfParallelism,
            input.LogEachUrl);

        int ok = urlResults.Count(r => r.Response is not null);
        int failed = urlResults.Count - ok;
        logger.LogInformationReplaySafe(
            context,
            "URL analysis completed. Total={Total}, Succeeded={Succeeded}, Failed={Failed}",
            urlResults.Count,
            ok,
            failed);

        BatchRunResult runResult = new()
        {
            RunId = context.InstanceId,
            RunFolder = BuildRunFolder(input.RunTimestampUtc, context.InstanceId),
            FileId = input.FileId,
            RunTimestampUtc = input.RunTimestampUtc,
            RunDateUtc = input.RunDateUtc,
            InputBlobName = input.InputBlobName,
            InputContainerName = input.InputContainerName,
            Items = parsedResult.Items,
            ParseErrors = parsedResult.Errors,
            UrlResults = urlResults
        };

        logger.LogInformationReplaySafe(
            context,
            "Writing reports. InstanceId={InstanceId}, FileId={FileId}, Timestamp={Timestamp}",
            context.InstanceId,
            input.FileId,
            input.RunTimestampUtc);

        return await context.CallActivityAsync<BatchReportArtifacts>(
            WriteReportActivityName,
            runResult);
    }

    private static async Task<List<BatchUrlResult>> AnalyzeUrlsAsync(
        TaskOrchestrationContext context,
        ILogger logger,
        List<BatchInputItem> items,
        int maxDegreeOfParallelism,
        bool logEachUrl)
    {
        int effectiveDop = Math.Max(1, maxDegreeOfParallelism);
        List<BatchUrlResult> results = [];
        int total = items.Count;
        int completedCount = 0;
        int succeededCount = 0;
        int failedCount = 0;

        foreach (BatchInputItem[] chunk in items.Chunk(effectiveDop))
        {
            var tasks = new List<Task<BatchUrlResult>>();
            foreach (BatchInputItem item in chunk)
            {
                tasks.Add(AnalyzeSingleUrlAsync(context, item));
            }

            BatchUrlResult[] completed;
            if (logEachUrl)
            {
                completed = await DrainWithPerUrlProgressAsync(
                    context,
                    logger,
                    tasks,
                    total,
                    completedCount);
            }
            else
            {
                completed = await Task.WhenAll(tasks);
            }

            results.AddRange(completed);

            completedCount += completed.Length;
            succeededCount += completed.Count(r => r.Response is not null);
            failedCount += completed.Count(r => r.Response is null);

            logger.LogInformationReplaySafe(
                context,
                "Progress: {Completed}/{Total} URLs processed (Succeeded={Succeeded}, Failed={Failed})",
                completedCount,
                total,
                succeededCount,
                failedCount);
        }

        return results;
    }

    private static async Task<BatchUrlResult[]> DrainWithPerUrlProgressAsync(
        TaskOrchestrationContext context,
        ILogger logger,
        List<Task<BatchUrlResult>> tasks,
        int total,
        int completedCountBeforeChunk)
    {
        List<BatchUrlResult> completed = [];
        int completedInChunk = 0;

        while (tasks.Count > 0)
        {
            Task<BatchUrlResult> finishedTask = await Task.WhenAny(tasks);
            tasks.Remove(finishedTask);

            BatchUrlResult finished = await finishedTask;
            completed.Add(finished);
            completedInChunk++;

            int completedCount = completedCountBeforeChunk + completedInChunk;

            string url = finished.Item.PageUrl;
            int rowNumber = finished.Item.RowNumber;
            if (finished.Response is not null)
            {
                logger.LogInformationReplaySafe(
                    context,
                    "Processed Row={RowNumber}: {Url}",
                    completedCount,
                    total,
                    rowNumber,
                    url);
            }
            else
            {
                logger.LogInformationReplaySafe(
                    context,
                    "Processed Row={RowNumber}: {Url} FAILED ({Error})",
                    completedCount,
                    total,
                    rowNumber,
                    url,
                    finished.Error ?? "Unknown error");
            }
        }

        return [.. completed];
    }

    private static async Task<BatchUrlResult> AnalyzeSingleUrlAsync(
        TaskOrchestrationContext context,
        BatchInputItem item)
    {
        if (!TryCreatePageUri(item.PageUrl, out Uri? uri))
        {
            return new BatchUrlResult
            {
                Item = item,
                Error = "Invalid PageUrl."
            };
        }
        if (string.IsNullOrEmpty(item.ResponsibleForContents) && string.IsNullOrEmpty(item.AccountableForContents))
        {
            return new BatchUrlResult
            {
                Item = item,
                Error = "At least one of ResponsibleForContents or AccountableForContents must be provided."
            };
        }

        SustainabilityClaimsCheckRequest request = new() { Url = uri?.ToString() ?? string.Empty };
        try
        {
            SustainabilityClaimsCheckResponse response = await context.CallActivityAsync<SustainabilityClaimsCheckResponse>(
                SustainabilityClaimsCheckAsyncOrchestrator.ActivityName,
                request);

            return new BatchUrlResult
            {
                Item = item,
                Response = response
            };
        }
        catch (Exception ex)
        {
            return new BatchUrlResult
            {
                Item = item,
                Error = ex.Message
            };
        }
    }

    private static bool TryCreatePageUri(string pageUrl, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(pageUrl))
            return false;

        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out Uri? parsed))
            return false;

        if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        uri = parsed;
        return true;
    }

    private static string BuildRunFolder(string runTimestampUtc, string instanceId)
    {
        if (string.IsNullOrWhiteSpace(runTimestampUtc))
            runTimestampUtc = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);

        string truncatedInstanceId = new([.. instanceId.Take(4)]);

        return $"{runTimestampUtc}_{truncatedInstanceId}";
    }
}
