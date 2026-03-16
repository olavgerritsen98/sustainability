using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace GenAiIncubator.LlmUtils.Core.Services.ContractTextExtraction;

/// <summary>
/// Extracts the "Ethical Clause" section from a contract PDF without OCR.
/// </summary>
public partial class ContractEthicalClauseExtractor : IContractEthicalClauseExtractor
{
    /// <inheritdoc />
    public string ExtractEthicalClauseSection(byte[] pdfBytes)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
            return string.Empty;

        string fullText = ExtractPdfText(pdfBytes);
        if (string.IsNullOrWhiteSpace(fullText))
            return string.Empty;

        // Prefer the strict "§<n> Ethical Clause" header format.
        string section = ExtractBestSectionFromHeaders(
            fullText,
            headerRegex: EthicalClauseHeaderRegex(),
            requireStrongHeaderHeuristic: false);

        // Fallback: some contracts use a header mentioning "Code of Conduct" instead of "Ethical Clause".
        // We treat lines containing "code of conduct" as candidate section headers, but apply a stronger
        // heuristic to avoid matching body text references.
        if (string.IsNullOrWhiteSpace(section))
        {
            section = ExtractBestSectionFromHeaders(
                fullText,
                headerRegex: CodeOfConductHeaderRegex(),
                requireStrongHeaderHeuristic: true);
        }

        return string.IsNullOrWhiteSpace(section) ? string.Empty : NormalizeWhitespace(section);
    }

    private static string ExtractBestSectionFromHeaders(
        string fullText,
        Regex headerRegex,
        bool requireStrongHeaderHeuristic)
    {
        MatchCollection headerMatches = headerRegex.Matches(fullText);
        if (headerMatches.Count == 0)
            return string.Empty;

        string best = string.Empty;
        foreach (Match header in headerMatches)
        {
            string headerLine = header.Value;
            if (LooksLikeTableOfContentsLine(headerLine))
                continue;

            if (requireStrongHeaderHeuristic && !LooksLikeHeaderLine(headerLine))
                continue;

            int start = header.Index;
            int searchFrom = header.Index + header.Length;

            Match nextHeader = AnySectionHeaderRegex().Match(fullText, searchFrom);
            int end = nextHeader.Success ? nextHeader.Index : fullText.Length;

            string section = fullText.Substring(start, end - start);
            if (section.Length > best.Length)
                best = section;
        }

        // If all matches were filtered out, fall back to slicing from the last occurrence.
        if (string.IsNullOrWhiteSpace(best))
        {
            Match last = headerMatches[^1];
            int start = last.Index;
            int searchFrom = last.Index + last.Length;
            Match nextHeader = AnySectionHeaderRegex().Match(fullText, searchFrom);
            int end = nextHeader.Success ? nextHeader.Index : fullText.Length;
            best = fullText.Substring(start, end - start);
        }

        // If it is still tiny, it's likely a ToC-only match.
        return best.Length < 50 ? string.Empty : best;
    }

    private static string ExtractPdfText(byte[] pdfBytes)
    {
        using var ms = new MemoryStream(pdfBytes);
        using PdfDocument doc = PdfDocument.Open(ms);

        var sb = new StringBuilder(capacity: Math.Min(pdfBytes.Length, 64_000));
        foreach (var page in doc.GetPages())
        {
            string text = ContentOrderTextExtractor.GetText(page) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string NormalizeWhitespace(string input)
        => MyRegex().Replace(input.Trim(), "\n");

    private static bool LooksLikeHeaderLine(string line)
    {
        string trimmed = line.Trim();

        // Avoid accidental matches in running prose.
        // Heuristic: headers are short-ish and usually do not end with a period.
        if (trimmed.Length > 160)
            return false;
        if (trimmed.EndsWith(".", StringComparison.Ordinal))
            return false;

        int wordCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return wordCount <= 14;
    }

    private static bool LooksLikeTableOfContentsLine(string headerLine)
    {
        if (string.IsNullOrWhiteSpace(headerLine))
            return false;

        // Typical ToC entries have dot leaders and end with a page number.
        // Example: "§14 Ethical Clause .......... 11"
        if (DotLeaderRegex().IsMatch(headerLine) && PageNumberAtEndRegex().IsMatch(headerLine))
            return true;

        return false;
    }

    [GeneratedRegex(@"^\s*§\s*\d+\s+Ethical\s+Clause\b.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex EthicalClauseHeaderRegex();

    [GeneratedRegex(@"^\s*(?:§\s*\d+\s*)?.*\bcode\s+of\s+conduct\b.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex CodeOfConductHeaderRegex();

    [GeneratedRegex(@"^\s*§\s*\d+\b.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex AnySectionHeaderRegex();

    [GeneratedRegex(@"\.{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex DotLeaderRegex();

    [GeneratedRegex(@"\.{2,}\s*\d+\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex PageNumberAtEndRegex();

    [GeneratedRegex(@"[ \t]+\r?\n", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}


