using System.Net;
using GenAiIncubator.LlmUtils.Core.Services.EthicalClauseContractValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.EthicalClauseContractValidation;

/// <summary>
/// Azure Function that validates whether an uploaded contract contains an ethical clause.
/// </summary>
public class EthicalClauseContractValidationFunction(
    EthicalClauseContractValidationService ethicalClauseContractValidationService)
    : BaseLlmUtilsFunction<EthicalClauseContractValidationRequest, EthicalClauseContractValidationResponse>
{
    private readonly EthicalClauseContractValidationService _ethicalClauseContractValidationService = ethicalClauseContractValidationService;

    protected override string? Validate(EthicalClauseContractValidationRequest request)
    {
        // Only validate JSON fields when request comes as JSON.
        // Multipart uploads bypass this and go through ExecuteFileAsync.
        if (request.Base64Document is null && request.FileType is null)
            return null;

        if (string.IsNullOrWhiteSpace(request.Base64Document))
            return "Base64Document is required for JSON requests.";
        if (string.IsNullOrWhiteSpace(request.FileType))
            return "FileType is required for JSON requests.";

        return null;
    }

    protected override async Task<EthicalClauseContractValidationResponse> ExecuteRequestAsync(
        EthicalClauseContractValidationRequest request,
        CancellationToken ct)
    {
        byte[] fileContents;
        try
        {
            fileContents = Convert.FromBase64String(request.Base64Document!);
        }
        catch (FormatException)
        {
            throw new InvalidDataException("Invalid base64 document data provided.");
        }

        var result = await _ethicalClauseContractValidationService.ValidateEthicalClauseContractAsync(
            fileContents,
            request.FileType!,
            ct);

        return new EthicalClauseContractValidationResponse
        {
            HasEthicalClause = result.HasEthicalClause,
            Reasoning = result.Reasoning,
            ExtractedClauseText = result.ExtractedClauseText
        };
    }

    protected override async Task<EthicalClauseContractValidationResponse> ExecuteFileAsync(
        byte[] fileContents,
        string fileType,
        CancellationToken ct)
    {
        var result = await _ethicalClauseContractValidationService.ValidateEthicalClauseContractAsync(fileContents, fileType, ct);

        return new EthicalClauseContractValidationResponse
        {
            HasEthicalClause = result.HasEthicalClause,
            Reasoning = result.Reasoning,
            ExtractedClauseText = result.ExtractedClauseText
        };
    }

    [Function("EthicalClauseContractValidation")]
    [OpenApiOperation(
        operationId: "EthicalClauseContractValidation",
        Summary = "Validate whether a contract contains an ethical clause",
        Description = "Uploads a contract (PDF/Word), parses it, and validates if an ethical clause is present.",
        Visibility = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "multipart/form-data",
        typeof(EthicalClauseContractValidationRequest),
        Description = "The contract file (PDF/Word) to be uploaded.",
        Required = true
    )]
    [OpenApiRequestBody(
        "application/json",
        typeof(EthicalClauseContractValidationRequest),
        Description = "Base64 encoded contract and file type (JSON alternative to multipart upload).",
        Required = true
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(EthicalClauseContractValidationResponse),
        Description = "Validation result indicating whether an ethical clause is present"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.BadRequest,
        "application/json",
        typeof(ErrorResponse),
        Description = "Bad request if the file is missing or invalid"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(ErrorResponse),
        Description = "Server error if validation fails"
    )]
    public override Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
        => base.RunAsync(req, context, cancellationToken);
}


