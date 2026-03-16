namespace GenAiIncubator.LlmUtils.Core.Options;

/// <summary>
/// Options for Azure AI Language (Text Analytics) integration.
/// </summary>
public class AzureLanguageOptions
{
    /// <summary>
    /// The Azure AI Language endpoint (e.g., https://&lt;resource-name&gt;.cognitiveservices.azure.com/).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The Azure AI Language API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}


