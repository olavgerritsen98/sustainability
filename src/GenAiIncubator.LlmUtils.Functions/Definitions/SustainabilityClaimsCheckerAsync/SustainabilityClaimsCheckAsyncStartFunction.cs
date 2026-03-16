using System.Net;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.DurableTask.Client;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsCheckerAsync;

public class SustainabilityClaimsCheckAsyncStartFunction
{
    [Function("SustainabilityClaimsCheckAsync_Start")]
    [OpenApiOperation(
        operationId: "SustainabilityClaimsCheckAsync_Start",
        Summary = "Starts an async sustainability claims check",
        Description = "Starts a long-running sustainability claims check. Provide 'url' for web analysis or 'filename' for uploaded document analysis.\n\nReturns a 'jobId' plus TWO ways to poll for completion.\n\nRequest body example:\n{ \"url\": \"https://example.com/page\" }\nOR\n{ \"filename\": \"report.pdf\" }\n\nAfter calling this:\n1) Read 'jobId' from the response.\n2) Poll ONE of the following until completion:\n   A) pollUrl (recommended): This is THIS function app's status endpoint (SustainabilityClaimsCheckAsync_Status). It returns a stable, Swagger-documented JSON shape including 'runtimeStatus' and (when completed) 'result'.\n   B) statusQueryGetUri (Durable built-in): This is a Durable runtime callback URL. It returns Durable's standard status payload (including 'runtimeStatus' and, when completed, 'output'). Some tool callers do not like calling arbitrary URLs or URLs with query parameters.\n3) Stop polling when runtimeStatus == Completed, then read the final result ('result' for pollUrl, or 'output' for statusQueryGetUri).\n\nTypical poll interval: 2-5 seconds.",
        Visibility = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "application/json",
        typeof(SustainabilityClaimsCheckRequest),
        Description = "Provide 'url' for web analysis or 'filename' for uploaded document analysis.",
        Required = true
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.Accepted,
        "application/json",
        typeof(SustainabilityClaimsCheckAsyncStartResponse),
        Description = "Accepted. Use pollUrl (recommended) or statusQueryGetUri to poll until runtimeStatus is Completed, then read the final result."
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.BadRequest,
        "application/json",
        typeof(ErrorResponse),
        Description = "Bad request if the input is invalid"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(ErrorResponse),
        Description = "Server error if scheduling fails"
    )]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sustainability-claims-check/async/start")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        CancellationToken ct)
    {
        var model = await req.ReadJsonAsync<SustainabilityClaimsCheckRequest>();
        if (model is null)
            return await req.CreateBadRequestResponseAsync("Invalid JSON format.");

        if (string.IsNullOrWhiteSpace(model.Url) && string.IsNullOrWhiteSpace(model.Filename))
            return await req.CreateBadRequestResponseAsync("Either Url or Filename is required.");

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            SustainabilityClaimsCheckAsyncOrchestrator.OrchestratorName,
            model,
            cancellation: ct);

        var durableResponse = await client.CreateCheckStatusResponseAsync(req, instanceId, cancellation: ct);

        // The built-in Durable response includes the management URIs, but the shape can vary.
        // For tool calling, return a stable, minimal payload containing those URIs.
        var payload = durableResponse.ReadBodyAsJson();
        if (payload is null)
            return durableResponse;

        var payloadElement = payload.Value;
        var statusQueryGetUri = payloadElement.GetUri("statusQueryGetUri");
        var terminatePostUri = payloadElement.GetUri("terminatePostUri");
        var purgeHistoryDeleteUri = payloadElement.GetUri("purgeHistoryDeleteUri");
        var sendEventPostUri = payloadElement.GetUri("sendEventPostUri");

        if (statusQueryGetUri is null || terminatePostUri is null || purgeHistoryDeleteUri is null || sendEventPostUri is null)
            return durableResponse;

        var pollUrl = new Uri($"{req.Url.GetLeftPart(UriPartial.Authority)}/api/sustainability-claims-check/async/{instanceId}");

        return await req.WriteJsonAsync(
            new SustainabilityClaimsCheckAsyncStartResponse
            {
                JobId = instanceId,
                StatusQueryGetUri = statusQueryGetUri,
                TerminatePostUri = terminatePostUri,
                PurgeHistoryDeleteUri = purgeHistoryDeleteUri,
                SendEventPostUri = sendEventPostUri,
                PollUrl = pollUrl
            },
            HttpStatusCode.Accepted);
    }
}

internal static class DurableResponseJsonExtensions
{
    public static System.Text.Json.JsonElement? ReadBodyAsJson(this HttpResponseData response)
    {
        try
        {
            response.Body.Position = 0;
            using var doc = System.Text.Json.JsonDocument.Parse(response.Body);
            // Clone because JsonDocument will be disposed.
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }

    public static Uri? GetUri(this System.Text.Json.JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;

        if (prop.ValueKind == System.Text.Json.JsonValueKind.String
            && Uri.TryCreate(prop.GetString(), UriKind.Absolute, out var uri))
            return uri;

        return null;
    }
}
