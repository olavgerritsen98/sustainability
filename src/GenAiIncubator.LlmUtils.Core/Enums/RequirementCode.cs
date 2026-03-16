namespace GenAiIncubator.LlmUtils.Core.Enums;

/// <summary>
/// Codes for requirements used to evaluate claims across general and type-specific rules.
/// </summary>
public enum RequirementCode
{
    /* Topic specific requirements */

    /// <summary>
    /// Sustainability requirements related to green electricity.
    /// </summary>
    GreenElectricity = SustainabilityTopicsEnum.GreenElectricity,

    /// <summary>
    /// Sustainability requirements related to green gas.
    /// </summary>
    GreenGas = SustainabilityTopicsEnum.GreenGas,

    /// <summary>
    /// Sustainability requirements related to heat or cold.
    /// </summary>
    HeatAndCold = SustainabilityTopicsEnum.HeatAndCold,

    /// <summary>
    /// Sustainability requirements related to the Paris Climate Agreement.
    /// </summary>
    ParisClimateTargets = SustainabilityTopicsEnum.ParisClimateTargets,

    /// <summary>
    /// Sustainability requirements related to wording: 'Fossil free living in one generation'
    /// </summary>
    FossilFreeLivingInOneGeneration = SustainabilityTopicsEnum.FossilFreeLivingInOneGeneration,

    /// <summary>
    /// Sustainability requirements related to comparison statements.
    /// </summary>
    Comparison = SustainabilityTopicsEnum.ComparisonStatement,

    /// <summary>
    /// Sustainability requirements related to superlative statements.
    /// </summary>
    Superlatives = SustainabilityTopicsEnum.SuperlativesStatement,

    /// <summary>
    /// Energy Label such as Electricity Label (stroometiket) or Heat Label (warmteetiket)
    /// </summary>
    EnergyLabel = SustainabilityTopicsEnum.EnergyLabel,

    /// <summary>
    /// Statement about sustainable production of electricity.
    /// </summary>
    // SustainableProduction,

    /// <summary>
    /// Sustainability requirements related to wording: 'CO2 neutral reduction compensated free'
    /// </summary>
    // CO2neutralReductionCompensatedFree,

    /// <summary>
    /// Sustainability requirements related to the Sustainable Brand Index.
    /// </summary>
    // SBI,

    /* General requirements (ACM 1-2, CDR 3.1) */

    /// <summary>
    /// Is the claim clear and unambiguous?
    /// </summary>
    General_ClearAndUnambiguous,

    /// <summary>
    /// Is the claim factually correct, with substanciation?
    /// </summary>
    General_FactuallyCorrectWithSubstanciation,

    /// <summary>
    /// Is the claim not misleading?
    /// </summary>
    // General_NotMisleading,


    /* Ambition (ACM 1,2,4; CDR 3.2) */

    /// <summary>
    /// It is clearly stated that this is an ambition/future goal (not the current situation).
    /// </summary>
    Ambition_ClearlyLabeledAsAmbition,

    /// <summary>
    /// Ambition is based on concrete, objective, and verifiable targets.
    /// </summary>
    Ambition_ConcreteObjectiveVerifiableTargets,

    /// <summary>
    /// Plans and measures are described to support feasibility of the ambition.
    /// </summary>
    Ambition_PlansAndMeasuresPresent,
}
