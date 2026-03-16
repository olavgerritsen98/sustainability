using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GenAiIncubator.LlmUtils.Core.Enums;

/// <summary>
/// Codes for requirements used to evaluate claims across general and type-specific rules.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SustainabilityTopicsEnum
{
    /// <summary>
    /// Green Gas
    /// </summary>
    [Description("Green Gas")]
    GreenGas,

    /// <summary>
    /// Green Electricity
    /// </summary>
    [Description("Green Electricity")]
    GreenElectricity,

    /// <summary>
    /// Heat and Cold
    /// </summary>
    [Description("Heat and Cold (warmte en koude) for products and services, network infrastructure, etc.")]
    HeatAndCold,

    /// <summary>
    /// Paris Climate Targets
    /// </summary>
    [Description("Claims directly mentioning the Paris Climate Targets")]
    ParisClimateTargets,

    /// <summary>
    /// Wording: Fossil free living in one generation
    /// </summary>
    [Description("Wording choice: 'Fossil free living in one generation'")]
    FossilFreeLivingInOneGeneration,

    /// <summary>
    /// Statement that expresses a comparison between a product, service, or company and another product, service, or company.
    /// </summary>
    [Description("A statement comparing a product, service, or company to another specific product, service, or company in terms of sustainability. This is only applicable if the claim is comparing two specific products, services, or companies. For example, comparing a green product with natural gas in general isn't considered a comparison statement, as it's not comparing a product or service to some competitor's or other party's.")]
    ComparisonStatement,

    /// <summary>
    /// Statement that uses superlative wording implying absolute superiority (e.g. 'most sustainable', 'largest', 'best').
    /// </summary>
    [Description("A statement using superlatives implying absolute superiority (e.g. 'most sustainable', 'largest', 'best').")]
    SuperlativesStatement,

    /// <summary>
    /// Energy Label such as Electricity Label (stroometiket) or Heat Label (warmteetiket)
    /// </summary>
    [Description("Electricity Label (stroometiket) or Heat Label (warmteetiket)")]
    EnergyLabel,

    /// <summary>
    /// Statement about sustainable production of electricity.
    /// </summary>
    // [Description("A statement about sustainable production of electricity.")]
    // SustainableProduction

    /// <summary>
    /// Wording: CO2 neutral reduction compensated free
    /// </summary>
    // [Description("Wording choice: 'CO2 neutral', 'CO2 reduction', 'CO2 compensated', 'CO2 free'")]
    // CO2neutralReductionCompensatedFree,

    /// <summary>
    /// Sustainable Brand Index
    /// </summary>
    // [Description("Sustainable Brand Index")]
    // SBI,

}
