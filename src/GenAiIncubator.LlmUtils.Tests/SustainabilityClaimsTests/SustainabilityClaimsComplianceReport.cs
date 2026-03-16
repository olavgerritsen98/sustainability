using System.Text;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

/// <summary>
/// Represents a comprehensive report of sustainability claims compliance test results.
/// </summary>
public class SustainabilityClaimsComplianceReport
{
    /// <summary>
    /// All test results in this report.
    /// </summary>
    public List<SustainabilityClaimsComplianceTestResult> Results { get; set; } = [];

    /// <summary>
    /// Add a test result to the report.
    /// </summary>
    /// <param name="result">The test result to add.</param>
    public void AddTestResult(SustainabilityClaimsComplianceTestResult result)
    {
        Results.Add(result);
    }

    /// <summary>
    /// Add multiple test results to the report.
    /// </summary>
    /// <param name="results">The test results to add.</param>
    public void AddTestResults(IEnumerable<SustainabilityClaimsComplianceTestResult> results)
    {
        Results.AddRange(results);
    }

    /// <summary>
    /// Create a summary test report as a string.
    /// </summary>
    /// <returns>A formatted string containing the test summary.</returns>
    public string CreateTestReport()
    {
        if (Results.Count == 0)
            return "No test results to report.";

        var sb = new StringBuilder();
        
        int totalTests = Results.Count;
        int correctTests = Results.Count(r => r.IsCorrect);
        int incorrectTests = totalTests - correctTests;
        int errorTests = Results.Count(r => !string.IsNullOrEmpty(r.Error));
        double accuracy = totalTests > 0 ? (double)correctTests / totalTests : 0;

        sb.AppendLine("SUSTAINABILITY CLAIMS COMPLIANCE TEST REPORT");
        sb.AppendLine("=" + new string('=', 50));
        sb.AppendLine($"Total Tests: {totalTests}");
        sb.AppendLine($"Correct: {correctTests} ({accuracy:P})");
        sb.AppendLine($"Incorrect: {incorrectTests}");
        sb.AppendLine($"Errors: {errorTests}");
        sb.AppendLine();

        // Group by requirement for detailed breakdown
        var byRequirement = Results.GroupBy(r => r.ExpectedResult.RequirementCode);
        sb.AppendLine("BREAKDOWN BY REQUIREMENT:");
        sb.AppendLine("-" + new string('-', 30));
        
        foreach (var group in byRequirement.OrderBy(g => g.Key.ToString()))
        {
            int reqTotal = group.Count();
            int reqCorrect = group.Count(r => r.IsCorrect);
            double reqAccuracy = reqTotal > 0 ? (double)reqCorrect / reqTotal : 0;
            
            sb.AppendLine($"{group.Key}: {reqCorrect}/{reqTotal} ({reqAccuracy:P})");
        }

        // Show incorrect results
        var incorrectResults = Results.Where(r => !r.IsCorrect && string.IsNullOrEmpty(r.Error)).ToList();
        if (incorrectResults.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("INCORRECT PREDICTIONS:");
            sb.AppendLine("-" + new string('-', 30));
            
            foreach (var result in incorrectResults.Take(10)) // Show first 10
            {
                sb.AppendLine($"Expected: {(result.ExpectedResult.ShouldBeCompliant ? "Compliant" : "Non-Compliant")}, Actual: {(result.ActualResult.IsCompliant ? "Compliant" : "Non-Compliant")}");
                sb.AppendLine($"Claim: {result.ExpectedResult.ClaimText[..Math.Min(100, result.ExpectedResult.ClaimText.Length)]}...");
                sb.AppendLine($"Requirement: {result.ExpectedResult.RequirementCode}");
                sb.AppendLine();
            }
            
            if (incorrectResults.Count > 10)
                sb.AppendLine($"... and {incorrectResults.Count - 10} more incorrect results");
        }

        // Show error results
        var errorResults = Results.Where(r => !string.IsNullOrEmpty(r.Error)).ToList();
        if (errorResults.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERROR CASES:");
            sb.AppendLine("-" + new string('-', 30));
            
            foreach (var result in errorResults.Take(5)) // Show first 5 errors
            {
                sb.AppendLine($"Claim: {result.ExpectedResult.ClaimText[..Math.Min(100, result.ExpectedResult.ClaimText.Length)]}...");
                sb.AppendLine($"Error: {result.Error}");
                sb.AppendLine();
            }
            
            if (errorResults.Count > 5)
                sb.AppendLine($"... and {errorResults.Count - 5} more errors");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Convert the report to CSV format.
    /// </summary>
    /// <param name="includeExpectedResults">Whether to include expected results in the CSV.</param>
    /// <returns>A CSV-formatted string.</returns>
    public string ToCsv(bool includeExpectedResults = true)
    {
        var sb = new StringBuilder();
        
        if (includeExpectedResults)
        {
            sb.AppendLine("ClaimText,RequirementCode,ShouldBeCompliant,IsCorrect,ViolationCodes,SuggestedAlternative,Error");
        }
        else
        {
            sb.AppendLine("ClaimText,RequirementCode,ViolationCodes,SuggestedAlternative,Error");
        }

        foreach (var result in Results)
        {
            string claimText = EscapeCsvField(result.ExpectedResult.ClaimText);
            string requirementCode = result.ExpectedResult.RequirementCode.ToString();
            string violationCodes = string.Join("; ", result.ActualResult.Violations.Select(v => v.Code.ToString()));
            string suggestedAlternative = EscapeCsvField(result.ActualResult.SuggestedAlternative);
            string error = EscapeCsvField(result.Error ?? "");

            if (includeExpectedResults)
            {
                sb.AppendLine($"{claimText},{requirementCode},{result.ExpectedResult.ShouldBeCompliant},{result.IsCorrect},{violationCodes},{suggestedAlternative},{error}");
            }
            else
            {
                sb.AppendLine($"{claimText},{requirementCode},{violationCodes},{suggestedAlternative},{error}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escape a field for CSV format.
    /// </summary>
    /// <param name="field">The field to escape.</param>
    /// <returns>The escaped field.</returns>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // If the field contains comma, quote, or newline, wrap it in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
