using System.Net;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

public class SustainabilityClaimsCheckFromTextFunction(ISustainabilityClaimsService sustainabilityClaimsService)
    : BaseLlmUtilsFunction<SustainabilityClaimsCheckFromTextRequest, SustainabilityClaimsCheckResponse>
{
    protected override string? Validate(SustainabilityClaimsCheckFromTextRequest request)
        => string.IsNullOrWhiteSpace(request.Content) ? "Content is required." : null;

    protected override async Task<SustainabilityClaimsCheckResponse> ExecuteRequestAsync(
        SustainabilityClaimsCheckFromTextRequest request,
        CancellationToken ct)
    {
        using LlmTokenUsageScope tokenScope = LlmTokenUsageContext.BeginScope();

        List<SustainabilityClaimComplianceEvaluation> evaluations =
            await sustainabilityClaimsService.CheckSustainabilityClaimsFromStringContentAsync(
                request.Content,
                "DirectTextContent",
                ct
            );

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

    [Function("SustainabilityClaimsCheckFromText")]
    [OpenApiOperation(
        operationId: "SustainabilityClaimsCheckFromText",
        Summary = "Checks sustainability claims in provided text",
        Description = "Analyzes text for sustainability claims compliance",
        Visibility = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "application/json",
        typeof(SustainabilityClaimsCheckFromTextRequest),
        Description = "The text to analyze for sustainability claims",
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
