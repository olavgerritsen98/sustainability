namespace GenAiIncubator.LlmUtils_Functions.Definitions.SummariseConversation;

public class SummarizeConversationRequest
{
    public required string Conversation { get; set; }
}

public class SummarizeConversationResponse
{
    public required string Summary { get; set; }
}
