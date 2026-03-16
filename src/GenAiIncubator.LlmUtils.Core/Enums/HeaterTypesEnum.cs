using System.Text.Json.Serialization;

namespace GenAiIncubator.LlmUtils.Core.Enums;

/// <summary>
/// Represents the different types of heating systems.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HeaterTypesEnum
{
    /// <summary>
    /// Central heating boiler.
    /// </summary>
    CVKetel,

    /// <summary>
    /// Hybrid heat pump.
    /// </summary>
    HybridHeatPump,

    /// <summary>
    /// Fully electric heat pump.
    /// </summary>
    FullElectricHeatPump,

    /// <summary>
    /// Air heating system.
    /// </summary>
    AirHeating,

    /// <summary>
    /// Air heating system.
    /// </summary>
    AirConditioning,

    /// <summary>
    /// City heating system.
    /// </summary>
    CityHeat,

    /// <summary>
    /// Shared heating system.
    /// </summary>
    SharedHeating,

    /// <summary>
    /// Shared heating system.
    /// </summary>
    OwnHeatSource,

    /// <summary>
    /// Other heating setup.
    /// </summary>
    OtherHeatingSetup,

    /// <summary>
    /// Unknown heating system.
    /// </summary>
    Unknown
}