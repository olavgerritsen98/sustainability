using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

/// <summary>
/// Represents the expected test result for a sustainability claim compliance test.
/// </summary>
public class SustainabilityClaimsComplianceExpectedResult
{
    /// <summary>
    /// The claim text being tested.
    /// </summary>
    public required string ClaimText { get; set; }

    /// <summary>
    /// Whether the claim should be compliant with the specified requirement.
    /// True if the claim should pass the requirement, false if it should violate it.
    /// </summary>
    public required bool ShouldBeCompliant { get; set; }

    /// <summary>
    /// The specific requirement being tested against.
    /// </summary>
    public required RequirementCode RequirementCode { get; set; }
}
