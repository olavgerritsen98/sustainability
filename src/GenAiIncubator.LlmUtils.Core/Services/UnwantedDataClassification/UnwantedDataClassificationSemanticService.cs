using System.Text.Json;
using System.Text.Json.Serialization;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace GenAiIncubator.LlmUtils.Core.Services.UnwantedDataClassification;

/// <summary>
/// Provides semantic (AI-based) operations for unwanted data classification.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnwantedDataClassificationSemanticService"/> class.
/// </remarks>
/// <param name="kernel">The injected kernel object.</param>
public class UnwantedDataClassificationSemanticService(Kernel kernel)
{
    private readonly Kernel _kernel = kernel.Clone();
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Performs the initial classification of unwanted data in a document using AI.
    /// </summary>
    /// <param name="parsedDocument">The parsed document containing text and images.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The classification result.</returns>
    public async Task<UnwantedDataClassificationResponse> ClassifyUnwantedDataAsync(
        ParsedDocument parsedDocument,
        CancellationToken cancellationToken)
    {
        string unwantedDataTypesList = EnumExtensions.GetFormattedDescriptionList<UnwantedDataTypesEnum>();
        ChatHistory prompt = UnwantedDataRecognitionPrompt(unwantedDataTypesList, parsedDocument);

        Type outputFormatType = typeof(LlmUnwantedDataMatrix);

        string result = await ChatHelpers.ExecutePromptAsync(
            _kernel,
            prompt,
            outputFormatType,
            cancellationToken: cancellationToken
        );

        result = result.Trim().Trim('`').Replace("\\\"", "\"");
        if (result.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            result = result[4..].TrimStart();

        try
        {
            var model = JsonSerializer.Deserialize<LlmUnwantedDataMatrix>(result, JsonOptions);
            if (model == null)
                return UnwantedDataClassificationResponse.GetErrorClassificationResponse();

            // Deduplicate entries by type (last write wins)
            Dictionary<UnwantedDataTypesEnum, LlmUnwantedDataEntry> byType = [];
            foreach (var entry in model.UnwantedData)
                byType[entry.UnwantedDataType] = entry;

            UnwantedDataClassificationResponse mapped = new()
            {
                DocumentType = model.DocumentType,
                DocumentTypeReasoning = model.DocumentTypeReasoning,
                UnwantedData = [.. byType
                    .Where(kv => kv.Value.IsPresent)
                    .Select(kv => new RecognizedUnwantedDataType
                    {
                        UnwantedDataType = kv.Key,
                        Reason = kv.Value.Reason
                    })]
            };

            return mapped;
        }
        catch (Exception)
        {
            // _logger.LogError(ex, "Error parsing JSON response");
            return UnwantedDataClassificationResponse.GetErrorClassificationResponse();
        }
    }

    /// <summary>
    /// Extracts BSN from a document using AI with multiple attempts to mitigate hallucinations.
    /// </summary>
    /// <param name="parsedDocument">The parsed document containing images.</param>
    /// <param name="isPassport">True if the document is an image of a passport.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first non-empty extracted BSN or empty string if none found.</returns>
    public async Task<string> ExtractBsnFromDocumentAsync(
        ParsedDocument parsedDocument,
        bool isPassport = false,
        CancellationToken cancellationToken = default)
    {
        var result = await ExtractBsnFromDocumentInternalAsync(parsedDocument, isPassport, cancellationToken);

        // If it's a passport, skip the validation. This is to minimize false negatives, at the expense of some false positives.
        // Passports often have the BSN in the MRZ, which can be hard to extract reliably.
        if (isPassport)
            return result;

        return !string.IsNullOrWhiteSpace(result) && ValidateBsn(result) ? result : string.Empty;
    }

    private async Task<string> ExtractBsnFromDocumentInternalAsync(
        ParsedDocument parsedDocument,
        bool isPassport = false,
        CancellationToken cancellationToken = default)
    {
        if (parsedDocument == null || (parsedDocument.Images.Count == 0 && string.IsNullOrWhiteSpace(parsedDocument.TextContent)))
            return string.Empty;

        AzureOpenAIPromptExecutionSettings executionSettings = new()
        {
            MaxTokens = 3000,
            Temperature = 0.1,
            TopP = 0.5,
        };

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = GetBsnExtractionChatHistory(parsedDocument, isPassport);

        try
        {
            ChatMessageContent result = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, _kernel, cancellationToken: cancellationToken);
            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while executing the BSN extraction prompt.", ex);
        }
    }

    private static bool ValidateBsn(string bsn)
    {
        bool isValid = !string.IsNullOrWhiteSpace(bsn) && bsn.Length == 9 && bsn.All(char.IsDigit);
        if (!isValid)
            return false;

        int sum = 9 * (bsn[0] - '0') +
                  8 * (bsn[1] - '0') +
                  7 * (bsn[2] - '0') +
                  6 * (bsn[3] - '0') +
                  5 * (bsn[4] - '0') +
                  4 * (bsn[5] - '0') +
                  3 * (bsn[6] - '0') +
                  2 * (bsn[7] - '0') +
                  -1 * (bsn[8] - '0');

        return sum % 11 == 0;
    }

    private static ChatHistory GetBsnExtractionChatHistory(ParsedDocument document, bool isPassport = false)
    {
        ChatHistory chatHistory = new("""
            You are a helpful assistant that extracts the BSN (Burgerservicenummer, Dutch social security number) from images.
        """);
        chatHistory.AddUserMessage("""
            [CONTEXT]
            You are given the text and/or images extracted from a document, and are asked to extract a BSN from it (citizen service number), if present.
            The BSN is a 9-digit number. If there is no BSN in the image, you should return an explanation saying so and explaining what the document is.
            """);
        if (isPassport)
        {
            ChatHistory passportExplanations = PassportExplanationsPrompt();
            foreach (var message in passportExplanations)
            {
                chatHistory.Add(message);
            }
        }
        chatHistory.AddUserMessage("""
            Now, you will be given an image. Your task is to extract the BSN from the image.
            You should respond with either:
                - a 9 digit number (the extracted BSN)
                - an explanation saying there is no BSN present in the document and what kind of document this is.
            Do not add any additional text or explanations to your response, either than an empty string or a BSN.
            [END CONTEXT]
        """);
        chatHistory.AddUserMessage($"""
            [INPUT DOCUMENT TEXT]
            {document.TextContent}
            [END INPUT DOCUMENT TEXT]
        """);
        chatHistory.AddUserMessage("""
            [INPUT DOCUMENT IMAGES]
        """);
        foreach (var image in document.Images)
        {
            chatHistory.AddUserMessage([image]);
        }
        chatHistory.AddUserMessage("""
            [END INPUT DOCUMENT IMAGES]
        """);
        return chatHistory;
    }

    private static ChatHistory UnwantedDataRecognitionPrompt(string unwantedDataTypesList, ParsedDocument parsedDocument)
    {
        ChatHistory chatHistory = [];
        chatHistory.AddUserMessage($$"""
        You are given an image of a document. Your task is to look for *concrete* instances of unwanted data in the document. 
        For context, the image is a document that our company has received from a customer.
        In order to adhere to Articles 9 and 10 of the General Data Protection Regulation (GDPR),
        there is a set of data types that we do not want to process or store. 
        Your classification will be used in order to filter out unwanted data from the document.

        Unwanted data types to evaluate:
        {{unwantedDataTypesList}}

        - For these unwanted data types, we're only interested in *concrete* instances of these data types actually being present in the document. This means that general mentions of issues or concepts related to these data types, without specific details, should not be considered as presence of unwanted data.

        OUTPUT FORMAT REQUIREMENTS:
        - Produce UnwantedData as an array with one entry per unwanted data type.
        - Each entry must contain:
            • unwantedDataType: enum name as string.
            • isPresent: true if and only if a concrete instance exists in the text or images; otherwise false.
            • reason: brief rationale. For false, use a short neutral statement (e.g., "Not observed"). 
                • Do not include any concrete sensitive information in the reasoning, to avoid displaying it in the logs.
        - Do not claim presence unless you have a concrete instance.
        - Never include an entry with isPresent=false claiming the document contains that data.
        - Include all enum types in the array, even when isPresent is false.
        """);

        chatHistory.AddUserMessage("""
        Since dutch passports and ID cards are often reocurring, here is an example image of each, for your reference:
        """);
        ImageContent passportImage = new(
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "PluginLibrary", "BsnExtraction", "passport_pre_24.png")),
            "image/png");
        ImageContent idImage = new(
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "PluginLibrary", "BsnExtraction", "Netherlands-ID.webp")),
            "image/webp");
        chatHistory.AddUserMessage([passportImage, idImage]);

        chatHistory.AddUserMessage($$"""
        The document to classify contains the following text:
        [BEGIN DOCUMENT TEXT]
        {{parsedDocument.TextContent}}
        [END DOCUMENT TEXT]
        """);
        if (parsedDocument.Images.Count != 0)
        {
            chatHistory.AddUserMessage($$"""
            The document to classify also contains the following images:
            [BEGIN DOCUMENT IMAGES]
            """);
                chatHistory.AddUserMessage([.. parsedDocument.Images]);
            chatHistory.AddUserMessage($$"""
            [END DOCUMENT IMAGES]
            """);
        }
        return chatHistory;
    }

    private static ChatHistory PassportExplanationsPrompt()
    {
        var chatHistory = new ChatHistory("""
            This image is from a passport, which can contain the BSN in various locations.
            In passports pre 2014, the BSN is also localed as part of the MRZ (Machine Readable Zone) at the bottom of the passport page. In the bottom line, it's the last 9 digits before the "<<<<<" delimiter.
            Here is a picture showing the exact location of the BSN in a pre 2014 passport:
        """);
        var imagePath = Path.Combine(AppContext.BaseDirectory, "PluginLibrary", "BsnExtraction", "passport_pre_24.png");
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        var pre24passportExample = new ImageContent(imageBytes, "image/png");
        chatHistory.AddUserMessage("""
            In this example, the BSN is 999999990. It's important to get out the 9 digits before the "<<<<<" delimiter. Use the example as reference for the positioning of the BSN in the passport.
        """);
        chatHistory.AddUserMessage([pre24passportExample]);
        chatHistory.AddUserMessage("""
            In some cases, some of these 9 last digits may be hidden, in which case there is no BSN to extract. 
            Be careful not to use the digits from that line that are before the 9 digits preceding the "<<<<<" delimiter, as those are not part of the BSN.
            For example, in the following image, the BSN is hidden:
        """);
        var hiddenBsnImagePath = Path.Combine(AppContext.BaseDirectory, "PluginLibrary", "BsnExtraction", "hiddenBsnExampleImage.PNG");
        var hiddenBsnImageBytes = File.ReadAllBytes(hiddenBsnImagePath);
        var hiddenBsnExampleImage = new ImageContent(hiddenBsnImageBytes, "image/png");
        chatHistory.AddUserMessage([hiddenBsnExampleImage]);
        chatHistory.AddUserMessage("""
            In this the following image, the BSN is visible ():
        """);
        var visibleBsnImagePath = Path.Combine(AppContext.BaseDirectory, "PluginLibrary", "BsnExtraction", "visibleBsnExampleImage.PNG");
        byte[] visibleBsnImageBytes = File.ReadAllBytes(visibleBsnImagePath);
        var visibleBsnExampleImage = new ImageContent(visibleBsnImageBytes, "image/png");
        chatHistory.AddUserMessage([visibleBsnExampleImage]);

        return chatHistory;
    }
}
