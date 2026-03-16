using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Internal model representing the full matrix returned by the LLM for unwanted data classification.
/// </summary>
public sealed class LlmUnwantedDataMatrix
{
    /// <summary>
    /// The type of document being classified.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required DocumentTypesEnum DocumentType { get; init; }

    /// <summary>
    /// The reasoning behind the document type classification.
    /// </summary>
    public required string DocumentTypeReasoning { get; init; }

    /// <summary>
    /// Flat list of entries, one per unwanted data type.
    /// </summary>
    public required IReadOnlyList<LlmUnwantedDataEntry> UnwantedData { get; init; }
}


