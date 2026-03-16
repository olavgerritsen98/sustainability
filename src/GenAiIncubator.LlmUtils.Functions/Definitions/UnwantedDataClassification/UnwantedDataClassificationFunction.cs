using System.Net;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Services.UnwantedDataClassification;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.UnwantedDataClassification;

/// <summary>
/// Azure Function that exposes functionality to classify an uploaded image
/// into one of several possible unwanted data types using a recognition service.
/// </summary>
public class UnwantedDataClassificationFunction(UnwantedDataClassificationService unwantedDataClassificationService) 
    : BaseLlmUtilsFunction<UnwantedDataClassificationRequest, UnwantedDataClassificationResponse>
{
    private readonly UnwantedDataClassificationService _unwantedDataClassificationService = unwantedDataClassificationService;

    protected async override Task<UnwantedDataClassificationResponse> ExecuteRequestAsync(
        UnwantedDataClassificationRequest request,
        CancellationToken ct)
    {
        // Decode base64 image
        byte[] fileContents;
        try
        {
            fileContents = Convert.FromBase64String(request.Base64Image);
        }
        catch (FormatException)
        {
            throw new InvalidDataException("Invalid base64 image data provided.");
        }

        LlmUtils.Core.Models.UnwantedDataClassificationResponse result;
        try
        {
            result = await _unwantedDataClassificationService.CheckForUnwantedData(fileContents, request.FileType, ct);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Error executing unwanted data classification: {ex.Message}");
            return new UnwantedDataClassificationResponse
            {
                UnwantedData = []
            };
        }

        return new UnwantedDataClassificationResponse
        {
            UnwantedData = [.. result.UnwantedData.Select(x => new RecognizedUnwantedData
            {
                UnwantedDataType = x.UnwantedDataType,
            })]
        };
    }

    protected override string? Validate(UnwantedDataClassificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Base64Image))
            return "Base64Image is required.";
        
        if (string.IsNullOrWhiteSpace(request.FileType))
            return "FileType is required.";

        return null;
    }

    [Function("UnwantedDataClassification")]
    [OpenApiOperation(
        operationId: "UnwantedDataClassification",
        Summary     = "Checks if an image contains unwanted data, such as PII or sensitive information",
        Description = "Uses image recognition to flag presence of unwanted data",
        Visibility  = OpenApiVisibilityType.Important
    )]
    [OpenApiRequestBody(
        "application/json",
        typeof(UnwantedDataClassificationRequest),
        Description = "The request containing base64 encoded image data and file type.",
        Required    = true
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(UnwantedDataClassificationResponse),
        Description = "The classification result, including document type, unwanted data and reasoning"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.BadRequest,
        "application/json",
        typeof(ErrorResponse),
        Description = "Bad request if the image data is missing or invalid"
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
