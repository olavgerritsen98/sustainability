namespace GenAiIncubator.LlmUtils_Functions.Definitions.CreateUserStory;

public class CreateUserStoryRequest
{
    public required string Conversation { get; set; }
}

public class CreateUserStoryResponse
{
    public required string UserStory { get; set; }
    public string MissingInfo { get; set; } = string.Empty;
}