namespace GenAiIncubator.LlmUtils_Functions.Definitions.ClassifyCustomerJourney;

public class ClassifyCustomerJourneyRequest
{
    public required string Conversation { get; set; }
    public bool UseSummary { get; set; }
    public bool TwoStepClassification { get; set; }
}

public class ClassifyCustomerJourneyResponse
{
    public required string MainJourney { get; set; }
    public required string Subcategory { get; set; }
}
