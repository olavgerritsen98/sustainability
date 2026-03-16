using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.UnwantedDataClassification;

public class UnwantedDataClassificationRequest
{
    /// <summary>
    /// Base64 encoded image data to be analyzed for unwanted data
    /// </summary>
    public required string Base64Image { get; set; }
    
    /// <summary>
    /// File extension of the image (e.g., "jpg", "png", "pdf")
    /// </summary>
    public required string FileType { get; set; }
}

public class UnwantedDataClassificationResponse
{
    /// <summary>
    /// Dictionary containing unwanted data extracted from the document.
    /// The key represents the data type, and the value represents the reasoning 
    /// explaining where this information was found in the input image.
    /// </summary>
    public required IList<RecognizedUnwantedData> UnwantedData { get; set; }
}

public class RecognizedUnwantedData
{
    public required UnwantedDataTypesEnum UnwantedDataType { get; set; }
}

