using System.Text;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsBatchChecksTests;

/// <summary>
/// Unit tests for parsing batch input CSVs.
/// </summary>
public sealed class SustainabilityClaimsBatchCheckParseCsvActivityTests
{
    [Fact]
    public void Parse_ParsesRowsAndCapturesValidationErrors()
    {
        const string csv = """
PageUrl,PublicationDate,ResponsibleForContents,AccountableForContents
https://example.com/page1,30-8-2018,"Name, With Comma",acc1
not-a-url,31-13-2020,resp2,acc2
""";

        BatchParseResult result = BatchCsvParser.Parse(Encoding.UTF8.GetBytes(csv), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Errors.Count);

        BatchInputItem first = result.Items[0];
        Assert.Equal(2, first.RowNumber);
        Assert.Equal("https://example.com/page1", first.PageUrl);
        Assert.Equal("30-8-2018", first.PublicationDate);

        BatchInputItem second = result.Items[1];
        Assert.Equal(3, second.RowNumber);
        Assert.Equal("not-a-url", second.PageUrl);

        Assert.Contains(result.Errors, e => e.RowNumber == 3 && e.Message.Contains("PageUrl", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.RowNumber == 3 && e.Message.Contains("PublicationDate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_ThrowsWhenHeaderRowIsMissing()
    {
        const string csv = """
https://example.com/page1,30-8-2018,resp,acc
https://example.com/page2,31-8-2018,resp2,acc2
""";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BatchCsvParser.Parse(Encoding.UTF8.GetBytes(csv), CancellationToken.None));

        Assert.Contains("header", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
