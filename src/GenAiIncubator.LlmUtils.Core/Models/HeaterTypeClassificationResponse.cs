using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents the response of a heater type classification.
/// </summary>
public class HeaterTypeClassificationResponse 
{
    /// <summary>
    /// The most likely recognised heater type. If there is uncertainty, HeaterTypeAlt provides an alternative.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required HeaterTypesEnum HeaterType { get; set; } = HeaterTypesEnum.Unknown;

    /// <summary>
    /// Alternative classifications for the recognised heater type, if the primary classification is uncertain.
    /// </summary>
    public List<HeaterTypesEnum> HeaterTypeAlt { get; set; } = [];

    /// <summary>
    /// The reason for the classification.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}