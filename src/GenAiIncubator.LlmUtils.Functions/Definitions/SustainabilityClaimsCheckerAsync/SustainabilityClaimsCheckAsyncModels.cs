using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsCheckerAsync;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SustainabilityClaimsCheckAsyncRuntimeStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Terminated,
    ContinuedAsNew,
    Suspended
}

public sealed class SustainabilityClaimsCheckAsyncStartResponse
{
    public required string JobId { get; init; }

    public required Uri StatusQueryGetUri { get; init; }

    public required Uri TerminatePostUri { get; init; }

    public required Uri PurgeHistoryDeleteUri { get; init; }

    public required Uri SendEventPostUri { get; init; }

    public required Uri PollUrl { get; init; }
}

public sealed class SustainabilityClaimsCheckAsyncStatusResponse
{
    public required string JobId { get; init; }

    public required SustainabilityClaimsCheckAsyncRuntimeStatus RuntimeStatus { get; init; }

    // Populated when RuntimeStatus == Completed
    public SustainabilityClaimsCheckResponse? Result { get; init; }

    // Populated when RuntimeStatus == Failed
    public string? FailureDetails { get; init; }

    public string? CreatedAt { get; init; }

    public string? LastUpdatedAt { get; init; }
}
