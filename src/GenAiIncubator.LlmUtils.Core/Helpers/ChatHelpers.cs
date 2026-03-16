using System.Reflection;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace GenAiIncubator.LlmUtils.Core.Helpers;

/// <summary>
/// Provides helper methods for executing chat prompts.
/// </summary>
public static class ChatHelpers
{
    /// <summary>
    /// Executes a chat prompt asynchronously.
    /// </summary>
    /// <param name="kernel">The kernel to use for executing the prompt.</param>
    /// <param name="prompt">The prompt to execute.</param>
    /// <param name="images">Image content to include in the prompt.</param>
    /// <param name="outputFormatType">Optional output format type.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response as a string.</returns>
    public static async Task<string> ExecutePromptAsync(
        Kernel kernel,
        string prompt,
        List<ImageContent> images,
        Type? outputFormatType = null,
        CancellationToken? cancellationToken = default)
    {
        return await ExecutePromptAsync(kernel, prompt, (IEnumerable<ImageContent>)images, outputFormatType, cancellationToken);
    }

    /// <summary>
    /// Executes a chat prompt asynchronously.
    /// </summary>
    /// <param name="kernel">The kernel to use for executing the prompt.</param>
    /// <param name="prompt">The prompt to execute.</param>
    /// <param name="images">Image content to include in the prompt.</param>
    /// <param name="outputFormatType">Optional output format type.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response as a string.</returns>
    public static async Task<string> ExecutePromptAsync(
        Kernel kernel,
        string prompt,
        IEnumerable<ImageContent> images,
        Type? outputFormatType = null,
        CancellationToken? cancellationToken = default)
    {
        var chatHistory = new ChatHistory(prompt);
        if (images.Any())
        {
            ChatMessageContentItemCollection contentItems = [];
            foreach (ImageContent image in images)
                contentItems.Add(image);
            chatHistory.AddUserMessage(contentItems);
        }
        return await ExecutePromptAsync(kernel, chatHistory, outputFormatType, cancellationToken);
    }

    /// <summary>
    /// Executes a chat prompt asynchronously.
    /// </summary>
    /// <param name="kernel">The kernel to use for executing the prompt.</param>
    /// <param name="prompt">The prompt to execute.</param>
    /// <param name="outputFormatType">Optional output format type.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response as a string.</returns>
    public static async Task<string> ExecutePromptAsync(
        Kernel kernel,
        string prompt,
        Type? outputFormatType = null,
        CancellationToken? cancellationToken = default)
    {
        return await ExecutePromptAsync(kernel, prompt, [], outputFormatType, cancellationToken);
    }

    /// <summary>
    /// Executes a chat prompt asynchronously with a single image.
    /// </summary>
    /// <param name="kernel">The kernel to use for executing the prompt.</param>
    /// <param name="prompt">The prompt to execute.</param>
    /// <param name="image">Image content to include in the prompt.</param>
    /// <param name="outputFormatType">Optional output format type.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response as a string.</returns>
    public static async Task<string> ExecutePromptAsync(
        Kernel kernel,
        string prompt,
        ImageContent image,
        Type? outputFormatType = null,
        CancellationToken? cancellationToken = default)
    {
        return await ExecutePromptAsync(kernel, prompt, [image], outputFormatType, cancellationToken);
    }

    /// <summary>
    /// Executes a chat prompt asynchronously using a pre-built chat history.
    /// </summary>
    /// <param name="kernel">The kernel to use for executing the prompt.</param>
    /// <param name="chatHistory">The chat history to execute.</param>
    /// <param name="outputFormatType">Optional output format type.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response as a string.</returns>
    public static async Task<string> ExecutePromptAsync(
        Kernel kernel,
        ChatHistory chatHistory,
        Type? outputFormatType = null,
        CancellationToken? cancellationToken = default)
    {
        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {

            
            
            ResponseFormat = outputFormatType

        };

        if (outputFormatType != null)
        {
            executionSettings.ResponseFormat = outputFormatType;
        }

        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        try
        {
            // The underlying Azure OpenAI SDK handles HTTP 429 retries natively
            ChatMessageContent result = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken: cancellationToken ?? CancellationToken.None);

            RecordTokenUsage(result);
            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while executing the chat prompt.", ex);
        }
    }

    /// <summary>
    /// Strips markdown code block formatting from an LLM response to ensure clean JSON parsing.
    /// </summary>
    public static string CleanJsonResponse(string result)
    {
        if (string.IsNullOrWhiteSpace(result))
            return result;

        result = result.Trim();

        if (result.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Substring(7);
        }
        else if (result.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Substring(3);
        }

        if (result.EndsWith("```"))
        {
            result = result.Substring(0, result.Length - 3);
        }

        return result.Trim();
    }

    private static void RecordTokenUsage(ChatMessageContent result)
    {
        LlmTokenUsageContext? context = LlmTokenUsageContext.Current;
        if (context is null)
            return;

        if (TryGetTokensFromMetadata(result.Metadata, out int? inputTokens, out int? outputTokens))
        {
            context.RecordUsage(inputTokens, outputTokens);
        }
        else
        {
            context.RecordUsage(null, null);
        }
    }

    private static bool TryGetTokensFromMetadata(
        IReadOnlyDictionary<string, object?>? metadata,
        out int? inputTokens,
        out int? outputTokens)
    {
        inputTokens = null;
        outputTokens = null;

        if (metadata is null)
            return false;

        if (metadata.TryGetValue("Usage", out object? usageObject)
            || metadata.TryGetValue("usage", out usageObject))
        {
            return TryReadTokensFromUsageObject(usageObject, out inputTokens, out outputTokens);
        }

        return TryReadTokensFromUsageObject(metadata, out inputTokens, out outputTokens);
    }

    private static bool TryReadTokensFromUsageObject(
        object? usageObject,
        out int? inputTokens,
        out int? outputTokens)
    {
        inputTokens = null;
        outputTokens = null;

        if (usageObject is null)
            return false;

        if (usageObject is JsonElement jsonElement)
        {
            return TryReadTokensFromJsonElement(jsonElement, out inputTokens, out outputTokens);
        }

        if (usageObject is IReadOnlyDictionary<string, object?> readOnlyDict)
        {
            return TryReadTokensFromDictionary(readOnlyDict, out inputTokens, out outputTokens);
        }

        if (usageObject is IDictionary<string, object?> dict)
        {
            return TryReadTokensFromDictionary(dict, out inputTokens, out outputTokens);
        }

        if (usageObject is IDictionary<string, object> dictObject)
        {
            return TryReadTokensFromDictionary(dictObject, out inputTokens, out outputTokens);
        }

        inputTokens = TryGetIntProperty(usageObject, "InputTokenCount", "InputTokens", "PromptTokens");
        outputTokens = TryGetIntProperty(usageObject, "OutputTokenCount", "OutputTokens", "CompletionTokens");

        return inputTokens.HasValue || outputTokens.HasValue;
    }

    private static bool TryReadTokensFromDictionary<T>(
        IEnumerable<KeyValuePair<string, T>> dict,
        out int? inputTokens,
        out int? outputTokens)
    {
        inputTokens = TryGetIntFromDictionary(dict, "input_tokens", "InputTokens", "prompt_tokens", "PromptTokens");
        outputTokens = TryGetIntFromDictionary(dict, "output_tokens", "OutputTokens", "completion_tokens", "CompletionTokens");

        return inputTokens.HasValue || outputTokens.HasValue;
    }

    private static int? TryGetIntFromDictionary<T>(IEnumerable<KeyValuePair<string, T>> dict, params string[] keys)
    {
        foreach (string key in keys)
        {
            foreach (var pair in dict)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryConvertToInt(pair.Value, out int value))
                    return value;
            }
        }

        return null;
    }

    private static bool TryReadTokensFromJsonElement(
        JsonElement element,
        out int? inputTokens,
        out int? outputTokens)
    {
        inputTokens = null;
        outputTokens = null;

        if (element.ValueKind != JsonValueKind.Object)
            return false;

        if (TryGetJsonInt(element, out int inputValue, "input_tokens", "prompt_tokens"))
            inputTokens = inputValue;

        if (TryGetJsonInt(element, out int outputValue, "output_tokens", "completion_tokens"))
            outputTokens = outputValue;

        return inputTokens.HasValue || outputTokens.HasValue;
    }

    private static bool TryGetJsonInt(JsonElement element, out int value, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (element.TryGetProperty(key, out JsonElement property)
                && property.ValueKind == JsonValueKind.Number
                && property.TryGetInt32(out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    private static int? TryGetIntProperty(object usageObject, params string[] propertyNames)
    {
        Type type = usageObject.GetType();
        foreach (string name in propertyNames)
        {
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
                continue;

            object? value = property.GetValue(usageObject);
            if (TryConvertToInt(value, out int intValue))
                return intValue;
        }

        return null;
    }

    private static bool TryConvertToInt(object? value, out int intValue)
    {
        switch (value)
        {
            case null:
                intValue = 0;
                return false;
            case int i:
                intValue = i;
                return true;
            case long l:
                if (l > int.MaxValue)
                {
                    intValue = int.MaxValue;
                    return true;
                }
                if (l < int.MinValue)
                {
                    intValue = int.MinValue;
                    return true;
                }
                intValue = (int)l;
                return true;
            case decimal d:
                intValue = (int)d;
                return true;
            case double d:
                intValue = (int)d;
                return true;
            case float f:
                intValue = (int)f;
                return true;
            case string s:
                return int.TryParse(s, out intValue);
            case JsonElement jsonElement:
                if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetInt32(out intValue))
                    return true;
                intValue = 0;
                return false;
            default:
                intValue = 0;
                return false;
        }
    }
}
