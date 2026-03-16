using System.Net;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.CreateUserStory;

/// <summary>
/// Azure Function that generates a new user story based on a conversation.
/// </summary>
public class CreateUserStoryFunction(
        UserStoryCreationService userStoryCreationService
    ) : BaseLlmUtilsFunction<CreateUserStoryRequest, CreateUserStoryResponse>
{
    private readonly UserStoryCreationService _userStoryCreationService = userStoryCreationService;

    /// <inheritdoc/>
    protected override string? Validate(CreateUserStoryRequest request)
        => string.IsNullOrWhiteSpace(request.Conversation)
            ? "Conversation content cannot be null or empty."
            : null;

    /// <inheritdoc/>
    protected override async Task<CreateUserStoryResponse> ExecuteRequestAsync(
        CreateUserStoryRequest request,
        CancellationToken ct)
    {
        var userStory = await _userStoryCreationService
            .CreateUserStoryAsync(request.Conversation, ct);
        return new CreateUserStoryResponse
        {
            UserStory = userStory.UserStory,
            MissingInfo = userStory.MissingInfo
        };
    }

    [Function("CreateUserStory")]
    [OpenApiOperation(
        operationId: "CreateUserStory",
        Summary     = "Creates a user story",
        Description = "Generates a new user story using supplied details",
        Visibility  = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "application/json",
        typeof(CreateUserStoryRequest),
        Description = "The request containing details for the user story",
        Required    = true
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(CreateUserStoryResponse),
        Description = "The created user story"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.BadRequest,
        "application/json",
        typeof(ErrorResponse),
        Description = "Bad request if input is invalid"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(ErrorResponse),
        Description = "Server error if creation fails"
    )]
    public override Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
        => base.RunAsync(req, context, cancellationToken);
}
