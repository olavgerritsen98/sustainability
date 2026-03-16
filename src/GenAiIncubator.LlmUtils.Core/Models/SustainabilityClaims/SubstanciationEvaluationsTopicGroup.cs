
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;

/// <summary>
/// A group of claims with missing substanciation that are related to a specific topic.
/// </summary>
public class SubstanciationEvaluationsTopicGroup : IEquatable<SubstanciationEvaluationsTopicGroup>
{
    /// <summary>
    /// The topic of the claims.
    /// </summary>
    public required string Topic { get; set; }

    /// <summary>
    /// The topic description of the claims.
    /// </summary>
    public required string TopicDescription { get; set; }

    /// <summary>
    /// The claims in the group.
    /// </summary>
    public List<SustainabilityClaimComplianceEvaluation> Evaluations { get; set; } = [];

    /// <summary>
    /// Whether the group is satisfied.
    /// A group is satisfied if at least one evaluation is not violating the requirement.
    /// </summary>
    public bool IsSatisfied => Evaluations.Any(c => !c.Violations
        .Select(v => v.Code)
        .Contains(RequirementCode.General_FactuallyCorrectWithSubstanciation)
    );

    /// <summary>
    /// Determines whether the specified <paramref name="other"/> is equal to the current instance.
    /// Equality is based solely on the value of <see cref="Topic"/>.
    /// </summary>
    /// <param name="other">The other instance to compare with the current instance.</param>
    /// <returns><c>true</c> if the <paramref name="other"/> has the same <see cref="Topic"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(SubstanciationEvaluationsTopicGroup? other) =>
        other is not null && System.StringComparer.Ordinal.Equals(Topic, other.Topic);

    /// <summary>
    /// Determines whether the specified <paramref name="obj"/> is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is SubstanciationEvaluationsTopicGroup other && Equals(other));

    /// <summary>
    /// Returns a hash code for the current instance based solely on <see cref="Topic"/>.
    /// </summary>
    /// <returns>An integer hash code.</returns>
    public override int GetHashCode() => Topic is null ? 0 : System.StringComparer.Ordinal.GetHashCode(Topic);
}
