namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

public class SustainabilityClaimsCheckRequest
{
    public string? Url { get; set; }
    public string? Filename { get; set; }

    /// <summary>
    /// Optional user identifier used to scope file lookups to a single user's upload directory.
    /// When set, the system only searches the directory belonging to this user, preventing
    /// cross-user file access.
    /// </summary>
    public string? UserId { get; set; }
}

public class SustainabilityClaimsCheckFromTextRequest
{
    public required string Content { get; set; }
}

public class SustainabilityClaimsCheckResponse
{
    public bool IsCompliant { get; set; }
    public List<SustainabilityClaimEvaluation> Evaluations { get; set; } = [];
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
}

public class SustainabilityClaimEvaluation
{
    public required string ClaimText { get; set; }
    public required string ClaimType { get; set; }
    public bool IsCompliant { get; set; }
    public List<string> Reasons { get; set; } = [];
    public string SuggestedAlternative { get; set; } = string.Empty;
    public List<string> Violations { get; set; } = [];
}


