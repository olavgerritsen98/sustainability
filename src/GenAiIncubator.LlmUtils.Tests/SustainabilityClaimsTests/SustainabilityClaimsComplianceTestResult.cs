using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

/// <summary>
/// Represents a test result for a sustainability claim compliance evaluation.
/// </summary>
public class SustainabilityClaimsComplianceTestResult
{
    /// <summary>
    /// The expected test case that was executed.
    /// </summary>
    public required SustainabilityClaimsComplianceExpectedResult ExpectedResult { get; set; }

    /// <summary>
    /// The actual compliance evaluation result.
    /// </summary>
    public required SustainabilityClaimComplianceEvaluation ActualResult { get; set; }

    /// <summary>
    /// Whether the test result was correct.
    /// For compliance tests: if ShouldBeCompliant is true, then the specific RequirementCode should NOT be in violations.
    /// For violation tests: if ShouldBeCompliant is false, then the specific RequirementCode should be in violations.
    /// </summary>
    public bool IsCorrect =>
        ExpectedResult.ShouldBeCompliant
            ? !ActualResult.Violations.Select(v => v.Code).Contains(ExpectedResult.RequirementCode)
            : ActualResult.Violations.Select(v => v.Code).Contains(ExpectedResult.RequirementCode);

    /// <summary>
    /// Any errors that occurred during testing.
    /// </summary>
    public string? Error { get; set; }
}
