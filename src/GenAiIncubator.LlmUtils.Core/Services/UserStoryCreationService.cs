using System.Net;
using System.Text.Json;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services;

/// <summary>
/// Provides functionality to create user stories from input requirements.
/// </summary>
public class UserStoryCreationService(Kernel kernel)
{
    private readonly Kernel _kernel = kernel;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Generates a user story based on the provided requirements.
    /// </summary>
    /// <param name="transcription"></param>
    /// <param name="cancellationToken">Optional. The cancellation token.</param>
    /// <returns>The generated user story.</returns>
    public async Task<UserStoryCreationResponse> CreateUserStoryAsync(
        string transcription,
        CancellationToken cancellationToken = default)
    {
        var extractedTranscription = await ExecuteConversationLastStoryExtractor(transcription, cancellationToken);
        var createdUserStory = await ExecuteUserStoryCreation(extractedTranscription, cancellationToken);
        
        var res = WebUtility.HtmlDecode(createdUserStory);
        // TODO fix dirty hack
        res = res.Replace("```json", "").Replace("```", "");

        var userStoryResponse = JsonSerializer.Deserialize<UserStoryCreationResponse>(res, JsonOptions);

        return userStoryResponse ?? throw new InvalidOperationException("Failed to parse the user story response.");
    }
    
    
    private async Task<string> ExecuteConversationLastStoryExtractor(string transcription, CancellationToken cancellationToken = default)
    {
        // TODO: Handle big contents (Map Reduce or something)

        var extractLastStoryInput = new Dictionary<string, object>
        {
            { "content", transcription }
        };
        var extractLastStoryResult = await PluginHelpers.ExecutePluginAsync(
            _kernel,
            PluginNamesEnum.ConversationLastStoryExtractor.ToString(),
            extractLastStoryInput,
            cancellationToken
        );
        return extractLastStoryResult.Content;
    }

    private async Task<string> ExecuteUserStoryCreation(string transcription, CancellationToken cancellationToken = default)
    {
        var storyCreationInput = new Dictionary<string, object>
        {
            { "conversation", transcription}
        }; 
        var storyCreationResult = await PluginHelpers.ExecutePluginAsync(
            _kernel,
            PluginNamesEnum.UserStoryCreation.ToString(),
            storyCreationInput,
            cancellationToken
        );
        return storyCreationResult.Content;
    }
}