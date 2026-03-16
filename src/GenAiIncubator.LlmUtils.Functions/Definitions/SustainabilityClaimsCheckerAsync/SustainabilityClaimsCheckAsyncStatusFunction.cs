using System.Net;
using System.Text.Json;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.DurableTask.Client;
using Microsoft.OpenApi.Models;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsCheckerAsync;

public class SustainabilityClaimsCheckAsyncStatusFunction
{
    private static readonly TimeSpan LongPollDelay = TimeSpan.FromSeconds(18);

    [Function("SustainabilityClaimsCheckAsync_Status")]
    [OpenApiOperation(
        operationId: "SustainabilityClaimsCheckAsync_Status",
        Summary = "Gets async sustainability claims check status",
        Description = "Poll this endpoint after calling SustainabilityClaimsCheckAsync_Start.\n\nImportant: The Start endpoint returns 'pollUrl' which points to THIS operation. This exists in addition to Durable's built-in 'statusQueryGetUri' so tool callers can use a stable, Swagger-documented endpoint and response schema.\n\nLong polling behavior:\n- If the job is not yet completed, this endpoint waits ~18 seconds and checks status once more before responding (to reduce chatty polling).\n\nHow to use:\n1) Call SustainabilityClaimsCheckAsync_Start and capture 'jobId' (or just use the returned pollUrl).\n2) GET this endpoint with that jobId (it may take up to ~18 seconds to respond).\n3) If runtimeStatus == Completed, 'result' contains the SustainabilityClaimsCheckResponse.\n4) If runtimeStatus == Failed, check 'failureDetails'.\n\nClient guidance: call this endpoint repeatedly until runtimeStatus is Completed/Failed/Terminated.",
        Visibility = OpenApiVisibilityType.Important
    )]
    [OpenApiParameter(
        name: "jobId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Description = "The jobId/instanceId returned by SustainabilityClaimsCheckAsync_Start"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(SustainabilityClaimsCheckAsyncStatusResponse),
        Description = "Current status. When runtimeStatus is Completed, 'result' contains the SustainabilityClaimsCheckResponse."
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.NotFound,
        "application/json",
        typeof(ErrorResponse),
        Description = "Not found if jobId does not exist"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.BadRequest,
        "application/json",
        typeof(ErrorResponse),
        Description = "Bad request if jobId is missing"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(ErrorResponse),
        Description = "Server error if status lookup fails"
    )]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "sustainability-claims-check/async/{jobId}")] HttpRequestData req,
        string jobId,
        [DurableClient] DurableTaskClient client,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return await req.CreateBadRequestResponseAsync("jobId is required.");

        OrchestrationMetadata? instance = await client.GetInstanceAsync(jobId, getInputsAndOutputs: true, cancellation: ct);
        if (instance is null)
            return await req.WriteJsonAsync(new ErrorResponse { ErrorMessage = "Job not found." }, HttpStatusCode.NotFound);

        // Long-poll once: if not in a terminal state, wait a bit and check again.
        if (!IsTerminal(instance.RuntimeStatus))
        {
            try
            {
                await Task.Delay(LongPollDelay, ct);
            }
            catch (OperationCanceledException)
            {
                // If the caller cancels, return the best known status.
            }

            var refreshed = await client.GetInstanceAsync(jobId, getInputsAndOutputs: true, cancellation: ct);
            if (refreshed is not null)
                instance = refreshed;
        }

        var failureDetails = instance.FailureDetails is null
            ? null
            : $"{instance.FailureDetails.ErrorType}: {instance.FailureDetails.ErrorMessage}";

        SustainabilityClaimsCheckResponse? result = null;
        if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
            && !string.IsNullOrWhiteSpace(instance.SerializedOutput))
        {
            try
            {
                result = JsonSerializer.Deserialize<SustainabilityClaimsCheckResponse>(instance.SerializedOutput);
            }
            catch
            {
                // Keep null; caller can still inspect runtimeStatus.
            }
        }

        var response = new SustainabilityClaimsCheckAsyncStatusResponse
        {
            JobId = instance.InstanceId,
            RuntimeStatus = instance.RuntimeStatus switch
            {
                OrchestrationRuntimeStatus.Pending => SustainabilityClaimsCheckAsyncRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Running => SustainabilityClaimsCheckAsyncRuntimeStatus.Running,
                OrchestrationRuntimeStatus.Completed => SustainabilityClaimsCheckAsyncRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed => SustainabilityClaimsCheckAsyncRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Terminated => SustainabilityClaimsCheckAsyncRuntimeStatus.Terminated,
                OrchestrationRuntimeStatus.ContinuedAsNew => SustainabilityClaimsCheckAsyncRuntimeStatus.ContinuedAsNew,
                OrchestrationRuntimeStatus.Suspended => SustainabilityClaimsCheckAsyncRuntimeStatus.Suspended,
                _ => SustainabilityClaimsCheckAsyncRuntimeStatus.Running
            },
            Result = result,
            FailureDetails = failureDetails,
            CreatedAt = instance.CreatedAt.ToString("O"),
            LastUpdatedAt = instance.LastUpdatedAt.ToString("O")
        };

        return await req.WriteJsonAsync(response, HttpStatusCode.OK);
    }

    private static bool IsTerminal(OrchestrationRuntimeStatus status)
        => status is OrchestrationRuntimeStatus.Completed
            or OrchestrationRuntimeStatus.Failed
            or OrchestrationRuntimeStatus.Terminated;
}
