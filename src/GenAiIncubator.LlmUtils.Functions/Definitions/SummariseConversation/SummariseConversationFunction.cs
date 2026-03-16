using System.Net;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SummariseConversation;


/// <summary>
/// Azure Function to summarize a conversation.
/// </summary>
public class SummariseConversationFunction(ConversationSummarisationService svc)
        : BaseLlmUtilsFunction<SummarizeConversationRequest, SummarizeConversationResponse>
{
    private readonly ConversationSummarisationService _svc = svc;

    /// <inheritdoc />
    protected override string? Validate(SummarizeConversationRequest request)
        => string.IsNullOrWhiteSpace(request.Conversation)
            ? "Conversation content cannot be null or empty."
            : null;

    /// <inheritdoc />
    protected override async Task<SummarizeConversationResponse> ExecuteRequestAsync(
        SummarizeConversationRequest request,
        CancellationToken ct)
    {
        var summary = await _svc.SummariseConversationAsync(request.Conversation, cancellationToken: ct);
        return new SummarizeConversationResponse { Summary = summary };
    }

    [Function("SummariseConversation")]
    [OpenApiOperation(
        operationId: "SummariseConversation",
        Summary     = "Summarizes a conversation",
        Description = "Generates a short summary from a conversation",
        Visibility  = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "application/json",
        typeof(SummarizeConversationRequest),
        Description = "The conversation to summarize",
        Required    = true
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(SummarizeConversationResponse),
        Description = "The summarized conversation"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.BadRequest,
        "application/json",
        typeof(ErrorResponse),
        Description = "Bad request if conversation is invalid"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(ErrorResponse),
        Description = "Server error if summarization fails"
    )]
    public override Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
        => base.RunAsync(req, context, cancellationToken);
}