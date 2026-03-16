using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Models;

namespace GenAiIncubator.LlmUtils.Tests.UnwantedDataCleanupTests;

public class UnwantedDataClassificationTestResult
{
    public required string Path { get; set; }
    public required UnwantedDataClassificationResponse ClassificationResponse { get; set; }
    public required DocumentTypesEnum ExpectedDocumentType { get; set; }
    public required IList<UnwantedDataTypesEnum> ExpectedUnwantedDataTypes { get; set; }
    public required bool IsCorrect { get; set; }
}