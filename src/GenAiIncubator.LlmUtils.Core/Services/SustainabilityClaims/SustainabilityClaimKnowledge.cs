using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;

/// <summary>
/// Centralized knowledge strings for sustainability claim types and requirement descriptions.
/// Useful for prompt engineering and consistent messaging.
/// </summary>
public static class SustainabilityClaimKnowledge
{
    /// <summary>
    /// Maps claim types to natural-language descriptions and examples.
    /// </summary>
    public static readonly IReadOnlyDictionary<SustainabilityClaimType, string> ClaimTypeDescriptions =
        new Dictionary<SustainabilityClaimType, string>
        {
            [SustainabilityClaimType.Unspecified] =
                "An unspecified type of sustainability claim.",
            [SustainabilityClaimType.Regular] =
                "A statement that a product, service, activity, or company has a positive, less harmful, or no impact on the environment or other sustainability aspects. Example: 'This packaging is 100% recyclable.'",
            [SustainabilityClaimType.Ambition] =
                "A statement about a future aim or goal regarding sustainability. Example: 'We aim to be fully CO₂ neutral by 2030.'",
            [SustainabilityClaimType.Comparison] =
                "A statement comparing a product, service, or company to another in terms of sustainability. Example: '30% less plastic than the previous packaging.'",
        };

    /// <summary>
    /// Maps requirement codes to concise, consumable descriptions for checks and prompts.
    /// </summary>
    public static readonly IReadOnlyDictionary<RequirementCode, string> RequirementDescriptions =
        new Dictionary<RequirementCode, string>
        {
            // General requirements (ACM 1-2, CDR 3.1)
            [RequirementCode.General_ClearAndUnambiguous] =
                "The claim is clear and unambiguous; it is immediately clear to the average consumer what the claim means.",
            [RequirementCode.General_FactuallyCorrectWithSubstanciation] =
                "The claim is factually correct and supported by sufficient, up-to-date, and accessible substanciation. Substanciation can be evidence or plans and measures, or other statements that support the claim, in a way where the reader could potentially verify.",
            // [RequirementCode.General_NotMisleading] =
            //     "The claim is not misleading; A claim is misleading only if the omission results in a different or broader impression of sustainability benefit than what the claim actually covers. Describing what the company enables, supports, or offers (e.g., solutions that can help reduce emissions or costs) is not misleading, as long as no guaranteed outcome is stated.",

            // Ambition (ACM 1,2,4; CDR 3.2)
            [RequirementCode.Ambition_ClearlyLabeledAsAmbition] =
                "It is clearly stated that the claim is an ambition or future goal, not the current situation.",
            [RequirementCode.Ambition_ConcreteObjectiveVerifiableTargets] =
                "The ambition is based on concrete, objective, and verifiable targets.",
            [RequirementCode.Ambition_PlansAndMeasuresPresent] =
                "There are plans and measures described that support the feasibility of the ambition.",
        };

    /// <summary>
    /// Dutch display names for sustainability claim types, used in API responses.
    /// </summary>
    public static readonly IReadOnlyDictionary<SustainabilityClaimType, string> DutchClaimTypeNames =
        new Dictionary<SustainabilityClaimType, string>
        {
            [SustainabilityClaimType.Regular] = "Regulier",
            [SustainabilityClaimType.Ambition] = "Ambitie",
            [SustainabilityClaimType.Comparison] = "Vergelijkend",
            [SustainabilityClaimType.Unspecified] = "Ongespecificeerd",
        };

    /// <summary>
    /// General requirements that apply to all sustainability claims.
    /// </summary>
    public static readonly IReadOnlySet<RequirementCode> GeneralRequirements = new HashSet<RequirementCode>
    {
        RequirementCode.General_ClearAndUnambiguous,
        RequirementCode.General_FactuallyCorrectWithSubstanciation,
        // RequirementCode.General_NotMisleading
    };

    /// <summary>
    /// Type-specific requirements that apply in addition to general requirements.
    /// </summary>
    public static readonly IReadOnlyDictionary<SustainabilityClaimType, IReadOnlySet<RequirementCode>> TypeSpecificRequirements =
        new Dictionary<SustainabilityClaimType, IReadOnlySet<RequirementCode>>
        {
            [SustainabilityClaimType.Regular] = new HashSet<RequirementCode>(),
            [SustainabilityClaimType.Unspecified] = new HashSet<RequirementCode>(),
            [SustainabilityClaimType.Ambition] = new HashSet<RequirementCode>
            {
                RequirementCode.Ambition_ClearlyLabeledAsAmbition,
                RequirementCode.Ambition_ConcreteObjectiveVerifiableTargets,
                RequirementCode.Ambition_PlansAndMeasuresPresent
            },
        };

    /// <summary>
    /// Gets all applicable requirements for a given claim type (general + type-specific).
    /// </summary>
    /// <param name="claimType">The sustainability claim type.</param>
    /// <returns>A combined set of all applicable requirement codes.</returns>
    public static IReadOnlySet<RequirementCode> GetApplicableRequirements(SustainabilityClaimType claimType)
    {
        IReadOnlySet<RequirementCode> typeSpecific = TypeSpecificRequirements.TryGetValue(claimType, out var specific)
            ? specific
            : new HashSet<RequirementCode>();

        return GeneralRequirements.Union(typeSpecific).ToHashSet();
    }

}

