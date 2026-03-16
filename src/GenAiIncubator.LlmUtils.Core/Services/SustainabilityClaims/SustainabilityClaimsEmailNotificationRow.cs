namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;

/// <summary>
/// Represents a single row parsed from a sustainability claims meta-report CSV,
/// used as input for the email notification service.
/// </summary>
public sealed class SustainabilityClaimsEmailNotificationRow
{
    /// <summary>The URL of the web page that was validated.</summary>
    public string PageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the person responsible for the page content (from Optimizely).
    /// Used both as Responsible name in the email greeting and as a recipient.
    /// </summary>
    public string ResponsibleForContents { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the person accountable for the page content (from Optimizely).
    /// Used both as Accountable name in the email greeting and as a recipient.
    /// </summary>
    public string AccountableForContents { get; set; } = string.Empty;

    /// <summary>
    /// Whether the page is compliant ("true") or not ("false").
    /// An empty value combined with a non-empty <see cref="Error"/> indicates the page was not processed.
    /// </summary>
    public string PaginaCompliant { get; set; } = string.Empty;

    /// <summary>Total number of sustainability claims found on the page.</summary>
    public string ClaimsFound { get; set; } = string.Empty;

    /// <summary>Number of claims that are compliant.</summary>
    public string ClaimsCompliant { get; set; } = string.Empty;

    /// <summary>Number of claims that are not compliant.</summary>
    public string ClaimsNotCompliant { get; set; } = string.Empty;

    /// <summary>
    /// Non-empty when the page could not be processed due to a function error (e.g. HTTP 404, 403, 301).
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Blob path of the per-URL xlsx report, formatted as <c>{containerName}/{blobPath}</c>.
    /// Empty when no report was generated (e.g. parse error rows).
    /// </summary>
    public string PageReportBlobPath { get; set; } = string.Empty;
}
