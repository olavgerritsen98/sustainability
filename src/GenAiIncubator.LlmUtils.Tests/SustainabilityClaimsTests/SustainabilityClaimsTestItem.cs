using System.Text.Json.Serialization;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

public class SustainabilityClaimTestItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("translatedClaim")]
    public string TranslatedClaim { get; set; } = string.Empty;

    [JsonPropertyName("minExtractedSubstring")]
    public string? MinExtractedSubstring { get; set; }

    [JsonPropertyName("isClaim")]
    public bool IsClaim { get; set; }

    [JsonPropertyName("claimType")]
    public string ClaimType { get; set; } = string.Empty;

    [JsonPropertyName("topics")]
    public string[] Topics { get; set; } = [];

    [JsonPropertyName("expectedViolations")]
    public string[] ExpectedViolations { get; set; } = [];

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    [JsonPropertyName("sourceUrl")]
    public string SourceUrl { get; set; } = string.Empty;
}
