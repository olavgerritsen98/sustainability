using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents unwanted data recognized in a document.
/// </summary>
public class RecognizedUnwantedDataType
{
    /// <summary>
    /// The type of unwanted data recognized.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required UnwantedDataTypesEnum UnwantedDataType { get; set; }

    /// <summary>
    /// The reason for the classification of the unwanted data.
    /// </summary>
    public required string Reason { get; set; }
}