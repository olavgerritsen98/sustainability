using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

public sealed class BatchUrlResult
{
    public required BatchInputItem Item { get; init; }
    public SustainabilityClaimsCheckResponse? Response { get; init; }
    public string? Error { get; init; }
}
