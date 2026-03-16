using System.Linq;
using System.Text;

namespace GenAiIncubator.LlmUtils.Tests.UnwantedDataCleanupTests;

public class UnwantedDataClassificationReport
{
    public IList<UnwantedDataClassificationTestResult> Results { get; set; } = [];

    public void AddTestResult(UnwantedDataClassificationTestResult testResult)
    {
        Results.Add(testResult);
    }

    public void AddTestResult(IList<UnwantedDataClassificationTestResult> testResults)
    {
        foreach (var testResult in testResults)
            Results.Add(testResult);
    }

    public string CreateTestReport()
    {
        var total = Results.Count;
        var passed = Results.Count(r => r.IsCorrect);
        var failed = total - passed;
        var correctDocumentTypeCount = Results.Count(r => r.ClassificationResponse.DocumentType == r.ExpectedDocumentType);
        var correctExtractedDataCount = Results.Count(r =>
            r.ExpectedUnwantedDataTypes.OrderBy(_ => _).SequenceEqual(
                r.ClassificationResponse.UnwantedData.Select(u => u.UnwantedDataType).OrderBy(_ => _)
            )
        );
        var correctDocumentTypeRate = total > 0 ? (correctDocumentTypeCount * 100.0 / total).ToString("F2") + "%" : "N/A";
        var correctExtractedDataRate = total > 0 ? (correctExtractedDataCount * 100.0 / total).ToString("F2") + "%" : "N/A";

        var sb = new StringBuilder()
            .AppendLine($"Unwanted Data Classification Report")
            .AppendLine($"===============================")
            .AppendLine($"Total tests : {total}")
            .AppendLine($"Passed      : {passed}")
            .AppendLine($"Failed      : {failed}")
            .AppendLine($"Document Type success rate: {correctDocumentTypeRate}")
            .AppendLine($"Extracted Data success rate: {correctExtractedDataRate}");

        if (failed > 0)
        {
            sb.AppendLine()
              .AppendLine("Failures details:");

            foreach (var r in Results.Where(r => !r.IsCorrect))
            {
                var actualType = r.ClassificationResponse.DocumentType;
                var actualUnwanted = string.Join(",", r.ClassificationResponse.UnwantedData.Select(u => u.UnwantedDataType));
                var expectedUnwanted = string.Join(",", r.ExpectedUnwantedDataTypes);

                sb.AppendLine($"- Path: {r.Path}");
                sb.AppendLine($"  Expected Type       : {r.ExpectedDocumentType}");
                sb.AppendLine($"  Actual Type         : {actualType}");
                sb.AppendLine($"  Expected Unwanted   : {expectedUnwanted}");
                sb.AppendLine($"  Actual Unwanted     : {actualUnwanted}");
            }
        }

        return sb.ToString();
    }

    public string ToCsv(bool includeExpectedResults = true)
    {
        var sb = new StringBuilder();
        
        if (includeExpectedResults)
        {
            sb.AppendLine("Path,ExpectedType,ActualType,ExpectedUnwanted,ActualUnwanted,IsCorrect,IsDocumentTypeCorrect,IsExtractedDataCorrect,Reasoning,UnwantedDataReasoning");
            foreach (var r in Results)
            {
                var expected = r.ExpectedDocumentType;
                var actual = r.ClassificationResponse.DocumentType;
                var expUnw = string.Join(";", r.ExpectedUnwantedDataTypes);
                var actUnw = string.Join(";", r.ClassificationResponse.UnwantedData.Select(u => u.UnwantedDataType));
                var isDocumentTypeCorrect = expected == actual;
                var isExtractedDataCorrect = r.ExpectedUnwantedDataTypes.SequenceEqual(
                    r.ClassificationResponse.UnwantedData.Select(u => u.UnwantedDataType)
                );
                var unwantedDataReasoning = string.Join(" | ", r.ClassificationResponse.UnwantedData.Select(u => u.Reason));
                sb.AppendLine($"\"{r.Path}\",\"{expected}\",\"{actual}\",\"{expUnw}\",\"{actUnw}\",\"{r.IsCorrect}\",\"{isDocumentTypeCorrect}\",\"{isExtractedDataCorrect}\",\"{r.ClassificationResponse.DocumentTypeReasoning}\",\"{unwantedDataReasoning}\"");
            }
        }
        else
        {
            sb.AppendLine("Path,DocumentType,UnwantedDataTypes");
            foreach (var r in Results)
            {
                var actual = r.ClassificationResponse.DocumentType;
                var actUnw = string.Join(";", r.ClassificationResponse.UnwantedData.Select(u => u.UnwantedDataType));
                sb.AppendLine($"\"{r.Path}\",\"{actual}\",\"{actUnw}\"");
            }
        }
        
        return sb.ToString();
    }
}