namespace GenAiIncubator.LlmUtils_Functions.Definitions.AnonymiseContent;

public class AnonymizeContentRequest 
{
    public required string Content { get; set; }
}

public class AnonymizeContentResponse
{
    public required string AnonymizedContent { get; set; }
}

public class InvalidAnonymizeContentResponse : AnonymizeContentResponse
{
    public InvalidAnonymizeContentResponse(string errorMessage)
    {
        AnonymizedContent = errorMessage;
    }
}