using System.Net;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services;
/// <summary>
/// Provides functionality to summarise conversations into concise summaries.
/// </summary>
public class ConversationSummarisationService(Kernel kernel)
{
    private readonly Kernel _kernel = kernel;

    /// <summary>
    /// Summarises a conversation in a few sentences.
    /// </summary>
    /// <param name="conversation">The conversation to summarise.</param>
    /// <param name="summaryAdditionalInstructions">Optional. Instructions describing what the summary should focus on.</param>
    /// <param name="examplesAndLength">Optional. List of examples and length in sentences, illustrating the desired format of the summary.</param>
    /// <param name="cancellationToken">Optional. The cancellation token.</param>
    /// <returns>The sanitized text.</returns>
    public async Task<string> SummariseConversationAsync(
        string conversation, 
        string summaryAdditionalInstructions = "",
        (IEnumerable<string>?, int) examplesAndLength  = default,
        CancellationToken cancellationToken = default)
    {
        // TODO: Handle big contents (Map Reduce or something)

        string examplesList, lengthInSentences;
        if (examplesAndLength == default((IEnumerable<string>?, int)))
        {
            examplesList = "";
            lengthInSentences = "5";
        }
        else 
        {
            examplesList = string.Join("\n", examplesAndLength.Item1 ?? []);
            lengthInSentences = examplesAndLength.Item2 == 0 ? examplesAndLength.Item2.ToString() : "5";
        }

        var inputs = new Dictionary<string, object>
        {
            { "content", conversation },
            { "summaryInstructions", summaryAdditionalInstructions },
            { "lengthInSentences", lengthInSentences },
            { "examples", examplesList } 
        };

        LlmInvocationResult result = await PluginHelpers.ExecutePluginAsync(
            _kernel,
            PluginNamesEnum.ConversationSummarisation.ToString(),
            inputs,
            cancellationToken
        );
        return WebUtility.HtmlDecode(result.Content);
    }
}