namespace GenAiIncubator.LlmUtils.Core.Enums;

/// <summary>
/// The supported sustainability claim types identified on webpages or documents.
/// </summary>
public enum SustainabilityClaimType
{
    /// <summary>
    /// The claim has not yet been classified into one of the supported types.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// A statement that a product, service, activity, or company has a positive, less harmful,
    /// or no impact on the environment or other sustainability aspects.
    /// </summary>
    Regular = 1,

    /// <summary>
    /// A statement about a future aim or goal regarding sustainability.
    /// </summary>
    Ambition = 2,

    /// <summary>
    /// A statement comparing a product, service, or company to another in terms of sustainability.
    /// </summary>
    Comparison = 3,
}
