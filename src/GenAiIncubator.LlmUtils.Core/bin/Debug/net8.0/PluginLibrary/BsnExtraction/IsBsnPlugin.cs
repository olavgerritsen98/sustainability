using System.ComponentModel;
using GenAiIncubator.LlmUtils.Core.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace GenAiIncubator.LlmUtils.Core.PluginLibrary.BsnExtraction;

/// <summary>
/// Legacy BSN recognition plugin. 
/// This functionality has been moved to UnwantedDataClassificationSemanticService.
/// This class is kept for backward compatibility and may be removed in future versions.
/// </summary>
[Obsolete("Use UnwantedDataClassificationSemanticService instead. This plugin is no longer actively used.")]
[Description("Provides functionality to check if a string is a valid BSN (Burgerservicenummer).")]
public class BsnRecognitionPlugin()
{
    /// <summary>
    /// Checks if the provided string is a valid BSN (Burgerservicenummer).
    /// </summary>
    /// <param name="bsn">The candidate string to validate as BSN.</param>
    /// <returns>True if the string is a valid BSN; otherwise, false.</returns>
    [KernelFunction("is_bsn")]
    [Description("Checks if the provided string is a valid BSN (Burgerservicenummer)")]
    public static async Task<bool> IsBSNAsync(
        [Description("The candidate string to validate as BSN")] string bsn)
    {
        return
            !string.IsNullOrWhiteSpace(bsn) &&
            bsn.Length == 9 &&
            bsn.All(char.IsDigit);
    }

    /// <summary>
    /// Extracts the BSN from a passport image. (Not implemented yet.)
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the extracted BSN as a string.</returns>
    [KernelFunction("extract_bsn_from_passport")]
    [Description("Extracts the BSN (Burgerservicenummer, Dutch social security number) from an image of a passport.")]
    public async Task<string> ExtractBsnFromPassportAsync(ImageContent image)
    {
        Kernel kernel = new(); // TODO: should we inject it?
        AzureOpenAIPromptExecutionSettings executionSettings = new()
        {
            MaxTokens = 3000,
            Temperature = 0.1,
            TopP = 0.5,
        };

        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        
        var chatHistory = GetPassportExtractionChatHistory(image);

        try
        {
            ChatMessageContent result = await chatService.GetChatMessageContentAsync(
                chatHistory, executionSettings, kernel);

            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while executing the chat prompt.", ex);
        }
    }

    /// <summary>
    /// Returns the prompt used for extracting a BSN from a passport image.
    /// </summary>
    private static ChatHistory GetPassportExtractionChatHistory(ImageContent inputImage)
    {
        ChatHistory chatHistory = new("""
            You are a helpful assistant that extracts the BSN (Burgerservicenummer, Dutch social security number) from passport images.
        """);
        chatHistory.AddUserMessage("""
            [CONTEXT]
            You are given an image of a passport, and are asked to extract the BSN from it.
            The BSN is a 9-digit number. In the provided image, it is possible that the BSN is pursfully hidden. 
            In that case, you should return an explanation saying that the place where the BSN is usually located is hidden. 
            If there is no BSN in the image, you should return an explanation saying so.
            """);
        chatHistory.AddUserMessage("""
            In passports pre 2014, the BSN is also localed as part of the MRZ (Machine Readable Zone) at the bottom of the passport page. In the bottom line, it's the last 9 digits before the "<<<<<" delimiter.
            Here is a picture showing the exact location of the BSN in a pre 2014 passport:
        """);
        var imagePath = Path.Combine(AppContext.BaseDirectory, "PluginLibrary", "BsnExtraction", "passport_pre_24.png");
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        var exampleImage = new ImageContent(imageBytes, "image/png");
        chatHistory.AddUserMessage([exampleImage]);
        chatHistory.AddUserMessage("""
            Now, you will be given an image of a passport.
            Your task is to extract the BSN from the image, or say that the BSN is not present or hidden.
        """);
        chatHistory.AddUserMessage([inputImage]);
        return chatHistory;
    }
}
