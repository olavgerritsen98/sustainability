using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Internal model entry for LLM output representing a single unwanted data evaluation.
/// </summary>
public sealed class LlmUnwantedDataEntry
{
    /// <summary>
    /// The unwanted data type evaluated.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required UnwantedDataTypesEnum UnwantedDataType { get; init; }

    /// <summary>
    /// Whether the type is present in the document.
    /// </summary>
    public required bool IsPresent { get; init; }

    /// <summary>
    /// Reasoning for the assessment.
    /// </summary>
    public required string Reason { get; init; }

}


