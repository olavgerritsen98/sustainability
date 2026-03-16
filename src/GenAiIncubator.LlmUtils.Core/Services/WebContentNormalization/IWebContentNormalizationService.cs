namespace GenAiIncubator.LlmUtils.Core.Services.WebContentNormalization;

/// <summary>
/// Fetches webpage content and normalizes it into LLM-friendly plain text.
/// </summary>
public interface IWebContentNormalizationService
{
    /// <summary>
    /// Fetches the content at the specified URL and returns a cleaned, normalized text.
    /// </summary>
    /// <param name="url">The URL of the webpage.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Normalized, LLM-ready text extracted from the webpage.</returns>
    Task<string> FetchAndNormalizeAsync(string url, CancellationToken cancellationToken = default);
}


