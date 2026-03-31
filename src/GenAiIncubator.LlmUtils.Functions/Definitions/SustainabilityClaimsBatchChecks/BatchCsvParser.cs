using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

/// <summary>
/// EU Guidelines for Green Claims Validation
/// Based on the Commission Notice on the interpretation and application of Directive 2005/29/EC
/// </summary>
public static class EuGreenClaimsGuidelines
{
    public const string ValidationPrompt = @"
You are an expert in EU consumer protection law, specifically regarding environmental claims under Directive 2005/29/EC.

Your task is to evaluate whether environmental claims comply with EU guidelines on green claims.

KEY PRINCIPLES:
1. SUBSTANTIATION: All environmental claims must be based on robust, verifiable scientific evidence
2. CLARITY: Claims must be clear, specific, and not misleading
3. ACCESSIBILITY: Evidence supporting claims must be easily accessible to consumers
4. LIFE-CYCLE CONSIDERATION: Environmental impacts should consider the full life cycle of products
5. NO FALSE IMPRESSION: Claims must not create false impression of environmental benefits

COMMON PROBLEMATIC CLAIMS:
- Vague terms like 'eco-friendly', 'green', 'sustainable' without specific substantiation
- Hidden trade-offs (highlighting one positive aspect while hiding negative ones)
- Claims without accessible proof
- Misleading imagery or labels
- Comparative claims without clear basis

EVALUATION CRITERIA:
For each claim, assess:
1. Is the claim specific and clear?
2. Is there verifiable evidence?
3. Does it consider the full life cycle?
4. Could it mislead average consumers?
5. Are there hidden trade-offs?

IMPORTANT: Be thorough and critical. A claim should only be considered compliant if it meets ALL requirements.
If there are doubts or missing information, indicate what additional evidence would be needed.
";
}


public static partial class BatchCsvParser
{
    private static readonly string[] PublicationDateFormats = ["d-M-yyyy", "dd-MM-yyyy"];
    private static readonly Regex MissingDelimiterBeforeUrlRegex =
        DelimitorBeforeUrlRegex();

    public static BatchParseResult Parse(byte[] csvBytes, CancellationToken ct)
    {
        BatchParseResult result = new();

        using var stream = new MemoryStream(csvBytes, writable: false);
        using var rawReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string rawCsv = rawReader.ReadToEnd();
        string sanitizedCsv = SanitizeCsv(rawCsv);
        using var reader = new StringReader(sanitizedCsv);

        CsvConfiguration config = new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => (args.Header ?? string.Empty).Trim().ToLowerInvariant(),
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<BatchInputCsvRowMap>();

        if (!csv.Read())
            throw new InvalidOperationException("CSV is empty or missing header row.");

        csv.ReadHeader();
        ValidateHeaderRecord(csv.HeaderRecord);

        foreach (BatchInputCsvRow row in csv.GetRecords<BatchInputCsvRow>())
        {
            ct.ThrowIfCancellationRequested();

            int rowNumber = csv.Context?.Parser?.Row ?? 0;
            string pageUrl = NormalizeRequiredField(row.PageUrl);
            string? publicationDate = NormalizeOptionalField(row.PublicationDate);
            string? responsible = NormalizeOptionalField(row.ResponsibleForContents);
            string? accountable = NormalizeOptionalField(row.AccountableForContents);

            result.Items.Add(new BatchInputItem
            {
                RowNumber = rowNumber,
                PageUrl = pageUrl,
                PublicationDate = publicationDate,
                ResponsibleForContents = responsible,
                AccountableForContents = accountable
            });

            ValidatePageUrl(pageUrl, rowNumber, result.Errors);
            ValidatePublicationDate(publicationDate, rowNumber, result.Errors);
        }

        return result;
    }

    private static string NormalizeRequiredField(string? value) => NormalizeField(value);

    private static string? NormalizeOptionalField(string? value)
    {
        string normalized = NormalizeField(value);
        return normalized.Length == 0 ? null : normalized;
    }

    private static string NormalizeField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();
        string unquoted = TrimEnclosingQuotes(trimmed);
        return unquoted.Trim();
    }

    private static string SanitizeCsv(string rawCsv)
    {
        if (string.IsNullOrEmpty(rawCsv))
            return rawCsv;

        using var input = new StringReader(rawCsv);
        StringBuilder output = new(rawCsv.Length);

        string? line;
        bool isFirstLine = true;

        while ((line = input.ReadLine()) is not null)
        {
            if (isFirstLine)
            {
                output.AppendLine(line);
                isFirstLine = false;
                continue;
            }

            // Repair common export issue: missing delimiter between PageName and PageUrl.
            // Example broken:  "inhuizing,""https://...  => fixed: "inhuizing,","https://...
            string fixedLine = MissingDelimiterBeforeUrlRegex.Replace(line, "\",\"");
            output.AppendLine(fixedLine);
        }

        return output.ToString();
    }

    private static string TrimEnclosingQuotes(string value)
    {
        string result = value;

        while (result.Length >= 2 && result.StartsWith('"') && result.EndsWith('"'))
        {
            result = result[1..^1];
        }

        // Collapse double double-quotes back to a single quote character inside the value.
        return result.Replace("\"\"", "\"");
    }

    private static void ValidatePageUrl(string pageUrl, int rowNumber, List<BatchRowError> errors)
    {
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            errors.Add(new BatchRowError { RowNumber = rowNumber, Message = "PageUrl is required." });
            return;
        }

        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out Uri? parsed))
        {
            errors.Add(new BatchRowError { RowNumber = rowNumber, Message = "PageUrl is not a valid absolute URL." });
            return;
        }

        if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new BatchRowError { RowNumber = rowNumber, Message = "PageUrl must be http or https." });
        }
    }

    private static void ValidatePublicationDate(string? publicationDate, int rowNumber, List<BatchRowError> errors)
    {
        if (string.IsNullOrWhiteSpace(publicationDate))
            return;

        if (DateTime.TryParseExact(
                publicationDate,
                PublicationDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            return;

        errors.Add(new BatchRowError
        {
            RowNumber = rowNumber,
            Message = "PublicationDate must be in d-M-yyyy or dd-MM-yyyy format."
        });
    }

    private static void ValidateHeaderRecord(string[]? headers)
    {
        if (headers is null || headers.Length == 0)
            throw new InvalidOperationException("CSV header row is missing.");

        HashSet<string> normalized = headers
            .Select(header => (header ?? string.Empty).Trim().ToLowerInvariant())
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] requiredHeaders =
        [
            "pageurl",
            "publicationdate",
            "responsibleforcontents",
            "accountableforcontents"
        ];

        var missing = requiredHeaders.Where(header => !normalized.Contains(header)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"CSV header row is missing required columns: {string.Join(", ", missing)}.");
        }
    }

    private sealed class BatchInputCsvRow
    {
        public string? PageName { get; set; }
        public string? PageUrl { get; set; }
        public string? PublicationDate { get; set; }
        public string? ResponsibleForContents { get; set; }
        public string? AccountableForContents { get; set; }
    }

    private sealed class BatchInputCsvRowMap : ClassMap<BatchInputCsvRow>
    {
        public BatchInputCsvRowMap()
        {
            Map(m => m.PageName).Name("pagename");
            Map(m => m.PageUrl).Name("pageurl");
            Map(m => m.PublicationDate).Name("publicationdate");
            Map(m => m.ResponsibleForContents).Name("responsibleforcontents");
            Map(m => m.AccountableForContents).Name("accountableforcontents");
        }
    }

    [GeneratedRegex("\"\"(?=https?://)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-NL")]
    private static partial Regex DelimitorBeforeUrlRegex();
}
