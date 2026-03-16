using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;

/// <summary>
/// Describes a specific violation against a requirement code for a claim.
/// </summary>
public class RequirementViolation
{
    /// <summary>
    /// The requirement that was violated.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required RequirementCode Code { get; set; }

    /// <summary>
    /// Human-readable message explaining the violation.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Warning message if applicable.
    /// </summary>
    public string Warning { get; internal set; } = string.Empty;
}
