using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsBatchChecksTests;

/// <summary>
/// Unit tests for batch report formatting helpers.
/// </summary>
public sealed class BatchReportFormatterTests
{
    [Fact]
    public void BuildMetaReportRows_ComputesSummaryColumnsAndErrors()
    {
        var item1 = new BatchInputItem
        {
            RowNumber = 2,
            PageUrl = "https://example.com/page1",
            PublicationDate = "30-8-2018",
            ResponsibleForContents = "Responsible",
            AccountableForContents = "Accountable"
        };

        var item2 = new BatchInputItem
        {
            RowNumber = 3,
            PageUrl = string.Empty,
            PublicationDate = "31-13-2020",
            ResponsibleForContents = "Resp2",
            AccountableForContents = "Acc2"
        };

        var response = new SustainabilityClaimsCheckResponse
        {
            IsCompliant = false,
            Evaluations =
            [
                new SustainabilityClaimEvaluation
                {
                    ClaimText = "Claim A",
                    ClaimType = "Ambitie",
                    IsCompliant = true,
                    Violations = [],
                    SuggestedAlternative = string.Empty
                },
                new SustainabilityClaimEvaluation
                {
                    ClaimText = "Claim B",
                    ClaimType = "Regulier",
                    IsCompliant = false,
                    Violations = ["Code: Message"],
                    SuggestedAlternative = "Alt"
                }
            ]
        };

        var result = new BatchRunResult
        {
            RunId = "run",
            RunFolder = "20260203-120000_run",
            FileId = "file",
            RunTimestampUtc = "20260203-120000",
            RunDateUtc = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
            InputBlobName = "input.csv",
            InputContainerName = "batch-input",
            Items = [item1, item2],
            ParseErrors =
            [
                new BatchRowError { RowNumber = 3, Message = "PageUrl is required." }
            ],
            UrlResults =
            [
                new BatchUrlResult { Item = item1, Response = response }
            ]
        };

        List<BatchReportFormatter.MetaReportRow> rows = BatchReportFormatter.BuildMetaReportRows(result);
        Assert.Equal(2, rows.Count);

        BatchReportFormatter.MetaReportRow row1 = rows[0];
        Assert.Equal("false", row1.PaginaCompliant);
        Assert.Equal("2", row1.ClaimsFound);
        Assert.Equal("1", row1.ClaimsCompliant);
        Assert.Equal("1", row1.ClaimsNotCompliant);
        Assert.True(string.IsNullOrWhiteSpace(row1.Error));

        BatchReportFormatter.MetaReportRow row2 = rows[1];
        Assert.Equal(string.Empty, row2.PaginaCompliant);
        Assert.Contains("PageUrl", row2.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPerPageCsv_WritesHeadersAndSanitizesNewlines()
    {
        var item = new BatchInputItem
        {
            RowNumber = 2,
            PageUrl = "https://example.com/page1",
            ResponsibleForContents = "Responsible",
            AccountableForContents = "Accountable"
        };

        var response = new SustainabilityClaimsCheckResponse
        {
            IsCompliant = true,
            Evaluations =
            [
                new SustainabilityClaimEvaluation
                {
                    ClaimText = "Line1\nLine2",
                    ClaimType = "Ambitie",
                    IsCompliant = true,
                    Violations = [],
                    SuggestedAlternative = "Alt"
                }
            ]
        };

        string csv = BatchReportFormatter.BuildPerPageCsv(
            item,
            response,
            new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Contains("Samenvatting", csv, StringComparison.Ordinal);
        Assert.Contains("Pagina informatie", csv, StringComparison.Ordinal);
        Assert.Contains("Claimtype", csv, StringComparison.Ordinal);
        Assert.Contains("Schendingen", csv, StringComparison.Ordinal);
        Assert.Contains("Alternatief/Suggestie", csv, StringComparison.Ordinal);
        Assert.Contains("02/02/2026", csv, StringComparison.Ordinal);
        Assert.Contains("Line1 Line2", csv, StringComparison.Ordinal);
    }
}
