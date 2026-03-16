using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents the response of a call to the unwanted data classification service.
/// </summary>
public class UnwantedDataClassificationResponse 
{
    /// <summary>
    /// The type of document being classified.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required DocumentTypesEnum DocumentType { get; set; } 

    /// <summary>
    /// The reasoning behind the document type classification.
    /// </summary>
    public required string DocumentTypeReasoning { get; set; }

    /// <summary>
    /// A dictionary containing the types of unwanted data identified in the document and their reasoning.
    /// </summary>
    public required IList<RecognizedUnwantedDataType> UnwantedData { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="UnwantedDataClassificationResponse"/> with error values.
    /// </summary>
    /// <returns></returns>
    public static UnwantedDataClassificationResponse GetErrorClassificationResponse()
    {
        return new UnwantedDataClassificationResponse
        {
            DocumentType = DocumentTypesEnum.Unknown,
            DocumentTypeReasoning = "Unknown",
            UnwantedData = [],
        };
    }
}
