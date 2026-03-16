using System.Net;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services;

/// <summary>
/// Service to remove Personally Identifiable Information (PII) from text.
/// </summary>
/// <param name="kernel">The injected kernel object.</param>
public class PIIRemovalService(Kernel kernel)
{
    private readonly Kernel _kernel = kernel;

    /// <summary>
    /// Removes Personally Identifiable Information (PII) from a given text.
    /// </summary>
    /// <param name="content">The input text to sanitize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sanitized text.</returns>
    public async Task<string> AnonymiseContentAsync(string content, CancellationToken cancellationToken)
    {
        var input = new Dictionary<string, object>
        {
            { "content", content } 
        };

        LlmInvocationResult result = await PluginHelpers.ExecutePluginAsync(
            _kernel,
            PluginNamesEnum.PIIRemoval.ToString(),
            input,
            cancellationToken
        );
        return WebUtility.HtmlDecode(result.Content);
    }
}