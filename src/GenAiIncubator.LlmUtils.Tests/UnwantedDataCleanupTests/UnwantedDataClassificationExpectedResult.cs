using System.Text.Json.Serialization;

namespace GenAiIncubator.LlmUtils.Tests.UnwantedDataCleanupTests;

public class UnwantedDataClassificationExpectedResult
{
    [JsonPropertyName("doc_name")]
    public string DocName { get; set; } = "";

    [JsonPropertyName("unwanted_data_list")]
    public List<string> UnwantedDataList { get; set; } = [];
}