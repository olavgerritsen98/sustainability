using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsCheckerAsync;

public static class SustainabilityClaimsCheckAsyncOrchestrator
{
    public const string OrchestratorName = "SustainabilityClaimsCheckAsync_Orchestrator";
    public const string ActivityName = "SustainabilityClaimsCheckAsync_Activity";

    [Function(OrchestratorName)]
    public static async Task<SustainabilityClaimsCheckResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<SustainabilityClaimsCheckRequest>()
            ?? throw new InvalidOperationException(
                "Orchestration input is missing (expected SustainabilityClaimsCheckRequest)."
            );

        return await context.CallActivityAsync<SustainabilityClaimsCheckResponse>(ActivityName, request);
    }
}
