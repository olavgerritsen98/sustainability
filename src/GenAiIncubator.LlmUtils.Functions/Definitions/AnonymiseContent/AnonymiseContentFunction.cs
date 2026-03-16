using System.Net;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.AnonymiseContent;

/// <summary>
/// Azure Function that anonymizes personally identifiable information (PII) from supplied text.
/// </summary>
public class AnonymiseContentFunction(PIIRemovalService piiRemovalService)
    : BaseLlmUtilsFunction<AnonymizeContentRequest, AnonymizeContentResponse>
{
    private readonly PIIRemovalService _piiRemovalService = piiRemovalService;

    /// <summary>
    /// Validates the incoming request, ensuring the content is not null or empty.
    /// </summary>
    protected override string? Validate(AnonymizeContentRequest request)
        => string.IsNullOrWhiteSpace(request.Content)
            ? "Content cannot be null or empty."
            : null;

    /// <summary>
    /// Executes the PII removal logic using the injected service.
    /// </summary>
    protected override async Task<AnonymizeContentResponse> ExecuteRequestAsync(
        AnonymizeContentRequest request,
        CancellationToken ct)
    {
        var anonymized = await _piiRemovalService.AnonymiseContentAsync(request.Content!, ct);
        return new AnonymizeContentResponse { AnonymizedContent = anonymized };
    }

    [Function("AnonymiseContent")]
    [OpenApiOperation(
        operationId: "AnonymiseContent",
        Summary = "Anonymizes PII",
        Description = "Removes PII from the supplied content",
        Visibility = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(AnonymizeContentRequest),
        Required = true
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(AnonymizeContentResponse)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.BadRequest,
        contentType: "application/json",
        bodyType: typeof(ErrorResponse)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(ErrorResponse)
    )]
    public override Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
        => base.RunAsync(req, context, cancellationToken);
}
