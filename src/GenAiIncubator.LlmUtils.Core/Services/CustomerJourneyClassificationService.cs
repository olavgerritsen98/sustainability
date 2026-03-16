using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services;

/// <summary>
/// Provides a service for classifying a conversation in a customer journey.
/// A customer journey has a main customer journey type and a subcategory.
/// </summary>
public class CustomerJourneyClassificationService(Kernel kernel, ConversationSummarisationService conversationSummarisationService)
{
    private readonly Kernel _kernel = kernel;
    private readonly ConversationSummarisationService conversationSummarisationService = conversationSummarisationService;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Classifies a conversation in a customer journey. 
    /// A list of customer journeys can be found here: TODO
    /// </summary>
    /// <param name="conversation">The conversation to classify.</param>
    /// <param name="useSummary">Whether to use to use a summary or the full conversation in the classification LLM calls. Can be usefull if conversations are too large.</param>
    /// <param name="twoStepClassification">Two step classification first classifies the main topic with a first LLM call, then the corresponding subcategory in another.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sanitized text.</returns>
    public async Task<CustomerJourneyClassification> ClassifyConversationInCustomerJourneyAsync(
        string conversation,
        bool useSummary = true,
        bool twoStepClassification = false,
        CancellationToken cancellationToken = default
        )
    {
        // TODO: if conv too big: summarize

        string prompt = GetClassificationPrompt(conversation);
        string result = await ChatHelpers.ExecutePromptAsync(
            _kernel,
            prompt,
            outputFormatType: typeof(CustomerJourneyClassification),
            cancellationToken: cancellationToken
        );
        try
        {
            return JsonSerializer.Deserialize<CustomerJourneyClassification>(result, JsonOptions) ?? new InvalidClassification();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing JSON: {ex.Message}");
            return new InvalidClassification();
        }
    }

    private static string GetClassificationPrompt(string conversation, string additionalInformation = "")
    {
        string categories = CustomerJourneyHelper.GetJourneysSubcategories();
        return @$"""
               You are presented with some content and a list customer journey topics. These topics are the main category you should classify the content in.
               When you have chosen a main category, you then need to choose a subcategory from the list of subcategories for that main category.

               [BEGIN CATEGORIES]
               { categories }
               [END CATEGORIES]

               [START ADDITIONAL CONTEXT ON CONTENT]
               { additionalInformation }
               [END ADDITIONAL CONTEXT ON CONTENT]

               [CONVERSATION]
               { conversation }
               [END CONVERSATION]
               """;
    }
}