using System.Net;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.DocumentParsing;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

public class SustainabilityClaimsCheckFunction(
    ISustainabilityClaimsService sustainabilityClaimsService,
    IDocumentParser documentParser)
    : BaseLlmUtilsFunction<SustainabilityClaimsCheckRequest, SustainabilityClaimsCheckResponse>
{
    private readonly ISustainabilityClaimsService _service = sustainabilityClaimsService;
    private readonly IDocumentParser _documentParser = documentParser;

    protected override string? Validate(SustainabilityClaimsCheckRequest request)
        => string.IsNullOrWhiteSpace(request.Url) ? "Url is required." : null;

    protected override async Task<SustainabilityClaimsCheckResponse> ExecuteRequestAsync(
        SustainabilityClaimsCheckRequest request,
        CancellationToken ct)
    {
        using LlmTokenUsageScope tokenScope = LlmTokenUsageContext.BeginScope();

        List<SustainabilityClaimComplianceEvaluation> evaluations = await _service.CheckSustainabilityClaimsAsync(request.Url!, ct);

        LlmTokenUsageContext usage = tokenScope.Context;
        int? inputTokens = null;
        int? outputTokens = null;

        if (usage.CallCount == 0)
        {
            inputTokens = 0;
            outputTokens = 0;
        }
        else if (usage.UsageObservedCount > 0)
        {
            inputTokens = usage.InputTokens;
            outputTokens = usage.OutputTokens;
        }

        return new SustainabilityClaimsCheckResponse
        {
            IsCompliant = evaluations.All(e => e.IsCompliant),
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Evaluations = [.. evaluations.Select(e => new SustainabilityClaimEvaluation
            {
                ClaimText = e.Claim.ClaimText,
                ClaimType = SustainabilityClaimKnowledge.DutchClaimTypeNames[e.Claim.ClaimType],
                IsCompliant = e.IsCompliant,
                Violations = [.. e.Violations.Select(v => $"{v.Code}: {v.Message}")],
                SuggestedAlternative = e.SuggestedAlternative
            })]
        };
    }

    [Function("SustainabilityClaimsCheck")]
    [OpenApiOperation(
        operationId: "SustainabilityClaimsCheck",
        Summary = "Checks sustainability claims from URL",
        Description = "Analyzes URL content for sustainability claims compliance",
        Visibility = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "application/json",
        typeof(SustainabilityClaimsCheckRequest),
        Description = "The URL to analyze for sustainability claims",
        Required = true
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(SustainabilityClaimsCheckResponse),
        Description = "The sustainability claims analysis result"
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
        Description = "Server error if analysis fails"
    )]
    public override Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
        => base.RunAsync(req, context, cancellationToken);
}


