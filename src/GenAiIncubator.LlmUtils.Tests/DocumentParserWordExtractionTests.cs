using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GenAiIncubator.LlmUtils.Core.Services.DocumentParsing;

namespace GenAiIncubator.LlmUtils.Tests;

/// <summary>
/// Tests that Word document parsing preserves paragraph boundaries so that
/// downstream LLM extraction receives readable, structured text.
/// </summary>
public sealed class DocumentParserWordExtractionTests
{
    private readonly DocumentParser _parser = new();

    /// <summary>
    /// Creates a minimal .docx byte array with the supplied paragraphs.
    /// </summary>
    private static byte[] CreateDocx(params string[] paragraphTexts)
    {
        using var ms = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            var body = new Body();

            foreach (var text in paragraphTexts)
            {
                body.Append(new Paragraph(new Run(new Text(text))));
            }

            mainPart.Document = new Document(body);
            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    [Fact]
    public async Task ParseWordDocument_PreservesParagraphBoundaries()
    {
        // Arrange – three distinct paragraphs
        var docxBytes = CreateDocx(
            "Groene energie bespaart CO₂.",
            "Wij gebruiken hernieuwbare bronnen.",
            "Dit is een duurzame keuze.");

        // Act
        var result = await _parser.ParseDocumentAsync(docxBytes, "docx");

        // Assert – paragraphs must be separated (not concatenated into one blob)
        Assert.Contains("Groene energie bespaart CO₂.", result.TextContent);
        Assert.Contains("Wij gebruiken hernieuwbare bronnen.", result.TextContent);
        Assert.Contains("Dit is een duurzame keuze.", result.TextContent);

        // The key check: text should NOT be concatenated without separators
        Assert.DoesNotContain("CO₂.Wij", result.TextContent);
        Assert.DoesNotContain("bronnen.Dit", result.TextContent);

        // Paragraphs should be on separate lines
        var lines = result.TextContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public async Task ParseWordDocument_EmptyDocument_ReturnsEmptyText()
    {
        var docxBytes = CreateDocx(); // no paragraphs

        var result = await _parser.ParseDocumentAsync(docxBytes, "docx");

        Assert.True(string.IsNullOrWhiteSpace(result.TextContent));
    }

    [Fact]
    public async Task ParseWordDocument_SkipsBlankParagraphs()
    {
        // Arrange – document with blank paragraphs in between
        var docxBytes = CreateDocx("First paragraph.", "", "   ", "Second paragraph.");

        var result = await _parser.ParseDocumentAsync(docxBytes, "docx");

        // Blank paragraphs should be filtered out
        var lines = result.TextContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("First paragraph.", lines[0]);
        Assert.Equal("Second paragraph.", lines[1]);
    }
}
