using System.Net;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.ExtractHeaterType;

/// <summary>
/// Azure Function that exposes functionality to classify an uploaded image
/// into one of several possible heater types using a recognition service.
/// </summary>
public class ExtractHeaterTypeFunction(HeaterRecognitionService heaterRecognitionService) 
    : BaseLlmUtilsFunction<ExtractHeaterTypeRequest, ExtractHeaterTypeResponse>
{
    private readonly HeaterRecognitionService _heaterRecognitionService = heaterRecognitionService;

    protected async override Task<ExtractHeaterTypeResponse> ExecuteFileAsync(
        byte[] fileContents,
        string fileType,
        CancellationToken ct)
    {
        HeaterTypeClassificationResponse result = await _heaterRecognitionService.ExtractHeaterType(fileContents, fileType, ct);
        return new ExtractHeaterTypeResponse {
            HeaterType = result.HeaterType,
            Reason     = result.Reason,
            HeaterTypeAlt = result.HeaterTypeAlt
        };
    }


    [Function("ExtractHeaterType")]
    [OpenApiOperation(
        operationId: "ExtractHeaterType",
        Summary     = "Extract heater type from an uploaded image",
        Description = "Uses image recognition to identify the heater type",
        Visibility  = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "multipart/form-data",
        typeof(ExtractHeaterTypeRequest),
        Description = "The image file to be uploaded.",
        Required    = true
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(ExtractHeaterTypeResponse),
        Description = "The recognized heater type"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.BadRequest,
        "application/json",
        typeof(ErrorResponse),
        Description = "Bad request if the image file is missing or invalid"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(ErrorResponse),
        Description = "Server error if recognition fails"
    )]
    public override Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
        => base.RunAsync(req, context, cancellationToken);
}
