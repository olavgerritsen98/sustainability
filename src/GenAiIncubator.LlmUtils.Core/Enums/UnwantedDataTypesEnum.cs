using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GenAiIncubator.LlmUtils.Core.Enums;

/// <summary>
/// Represents the different types of sensitive or unwanted data that can be classified.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UnwantedDataTypesEnum
{
    /// <summary>
    /// BSN (Burgerservicenummer, Dutch social security number).
    /// </summary>
    [Description("BSN (Burgerservicenummer, Dutch social security number), a 9 digit number.")]
    BSN,

    /// <summary>
    /// NIN (National Identification Number).
    /// </summary>
    // [Description("National Identification Number (NIN).")]
    // NIN,

    /// <summary>
    /// Information about children under 16 years old.
    /// </summary>
    [Description("Information about children under 16 years old.")]
    ChildrenUnder16,

    /// <summary>
    /// Ethnic origin.
    /// </summary>
    [Description("Ethnic origin.")]
    EthnicOrigin,

    /// <summary>
    /// Political opinions.
    /// </summary>
    [Description("Political opinions.")]
    PoliticalOpinions,

    /// <summary>
    /// Religious or philosophical beliefs.
    /// </summary>
    [Description("Religious or philosophical beliefs.")]
    ReligiousOrPhilosophicalBeliefs,

    /// <summary>
    /// Trade union membership.
    /// </summary>
    [Description("Trade union membership.")]
    TradeUnionMembership,

    /// <summary>
    /// Genetic data processing.
    /// </summary>
    [Description("Genetic data processing.")]
    GeneticData,

    /// <summary>
    /// Biometric data for uniquely identifying a natural person.
    /// </summary>
    [Description("Biometric data for uniquely identifying a natural person.")]
    BiometricData,

    /// <summary>
    /// Data concerning health.
    /// </summary>
    [Description("Health data includes explicit mentions of specific physical or mental illnesses, diagnoses, or disabilities. General references to a person's health status (e.g., 'due to physical/mental condition') without naming a specific illness or diagnosis are not considered health data.")]
    HealthData,

    /// <summary>
    /// Data concerning a natural person's sex life or sexual orientation.
    /// </summary>
    [Description("Data concerning a natural person's sex life or sexual orientation.")]
    SexLifeOrOrientation,

    /// <summary>
    /// Criminal convictions.
    /// </summary>
    [Description("Criminal convictions.")]
    CriminalConvictions,

    /// <summary>
    /// Offences or related security measures based on Article 6(1).
    /// </summary>
    [Description("Offences or related security measures based on Article 6(1).")]
    OffencesOrSecurityMeasures
}