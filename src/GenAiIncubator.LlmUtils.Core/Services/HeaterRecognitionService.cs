using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services;

/// <summary>
/// Provides a service for extracting the heater type from an image.
/// </summary>
/// <param name="kernel">The injected kernel object.</param>
/// <param name="imageValidationService">The injected image validation service.</param>
public class HeaterRecognitionService(Kernel kernel, IImageValidationService imageValidationService)
{
    private readonly Kernel _kernel = kernel;
    private readonly IImageValidationService _imageValidationService = imageValidationService;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Validates if the provided image bytes represent a valid image file.
    /// </summary>
    /// <param name="imageBytes">The image bytes to validate.</param>
    /// <param name="filetype">File type of the image (png, jpg...).</param>
    /// <returns>True if the image is valid, false otherwise.</returns>
    public bool ValidateImageBytes(byte[] imageBytes, string filetype)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return false;

        // Create filename and content type from filetype
        var fileName = $"image.{filetype}";
        var contentType = $"image/{filetype}";
        
        return _imageValidationService.IsValidImageBytes(imageBytes, fileName, contentType);
    }

    /// <summary>
    /// Returns the heater type of a given input image.
    /// </summary>
    /// <param name="image">The image to classify.</param>
    /// <param name="filetype">File type of the image to classify (png, jpg...).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The recognized heater type along with an explanation.</returns>
    public async Task<HeaterTypeClassificationResponse> ExtractHeaterType(byte[] image, string filetype = "jpg", CancellationToken cancellationToken = default)
    {
        // Validate the image before processing
        if (!ValidateImageBytes(image, filetype))
        {
            return new() { HeaterType = HeaterTypesEnum.Unknown, Reason = "Invalid image file: The provided image does not meet validation criteria (size, format, or file signature)." };
        }
        StringBuilder sb = new();
        foreach (HeaterTypesEnum heaterType in Enum.GetValues(typeof(HeaterTypesEnum)))
            sb.AppendLine($"\t- {heaterType}");
        string heaterTypesList = sb.ToString();
        string prompt = HeaterExtractionPrompt(heaterTypesList);

        ImageContent imageContent = new(image, $"image/{filetype}");

        Type outputFormatType = typeof(HeaterTypeClassificationResponse);
        string result = await ChatHelpers.ExecutePromptAsync(
            _kernel,
            prompt,
            imageContent,
            outputFormatType,
            cancellationToken
        );
        try
        {
            var res = JsonSerializer.Deserialize<HeaterTypeClassificationResponse>(result, JsonOptions) ?? new() { HeaterType = HeaterTypesEnum.Unknown };
            return res;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing JSON: {ex.Message}");
            return new() { HeaterType = HeaterTypesEnum.Unknown };
        }
    }

    private string HeaterExtractionPrompt(string heaterTypesList)
    {
        return $@"""
               Extract the heater type from the given image. Acceptable heater types: 
                    {heaterTypesList}

               If the heater type is not clear, provide the most likely type, as well as a list of possible alternative types.
               If the heater type is clear, provide the most likely type and leave the alternative types list empty.
               The value 'OtherHeatingSetup' should only be used in case you recognize a heating setup that is not listed above.
               """;
    }
}