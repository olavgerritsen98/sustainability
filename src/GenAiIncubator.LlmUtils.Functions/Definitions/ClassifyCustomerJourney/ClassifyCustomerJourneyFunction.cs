using System.Net;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.ClassifyCustomerJourney;

/// <summary>
/// Azure Function that classifies a conversation into customer-journey stages.
/// </summary>
public class ClassifyCustomerJourneyFunction(
        CustomerJourneyClassificationService classificationService
    ) : BaseLlmUtilsFunction<ClassifyCustomerJourneyRequest, ClassifyCustomerJourneyResponse>
{
    private readonly CustomerJourneyClassificationService _classificationService = classificationService;

    /// <inheritdoc/>
    protected override string? Validate(ClassifyCustomerJourneyRequest request)
        => string.IsNullOrWhiteSpace(request.Conversation)
            ? "Conversation content cannot be null or empty."
            : null;

    /// <inheritdoc/>
    protected override async Task<ClassifyCustomerJourneyResponse> ExecuteRequestAsync(
        ClassifyCustomerJourneyRequest request,
        CancellationToken ct)
    {
        var classification = await _classificationService
            .ClassifyConversationInCustomerJourneyAsync(
                request.Conversation,
                request.UseSummary,
                request.TwoStepClassification,
                ct
            );

        return new ClassifyCustomerJourneyResponse
        {
            MainJourney = classification.Classification,
            Subcategory = classification.Subcategory
        };
    }

    [Function("ClassifyCustomerJourney")]
    [OpenApiOperation(
        operationId: "ClassifyCustomerJourney",
        Summary     = "Classifies a conversation in a customer journey",
        Description = "Classifies the conversation into relevant journey stages",
        Visibility  = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "application/json",
        typeof(ClassifyCustomerJourneyRequest),
        Description = "The conversation classification request",
        Required    = true
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(ClassifyCustomerJourneyResponse),
        Description = "The classification result"
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
        Description = "Server error if classification fails"
    )]
    public override Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
        => base.RunAsync(req, context, cancellationToken);
}
