using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

public static class BatchReportFormatter
{
    private const int PerPageColumnCount = 6;
    private const string CostCurrency = "EUR";
    private const string CostModelId = "genaiinc-gpt-4.1"; // TODO: maybe we can get from the kernel instead of hardcoding

    private static readonly Dictionary<string, (decimal InputPer1M, decimal OutputPer1M)> ModelPrices =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Prices are per 1,000,000 tokens.
            { CostModelId, (3.23m, 12.90m) }
        };

    public static List<MetaReportRow> BuildMetaReportRows(
        BatchRunResult result,
        IReadOnlyDictionary<int, string>? pageXlsxPathsByRowNumber = null)
    {
        var rows = new List<MetaReportRow>();

        var errorsByRow = result.ParseErrors
            .GroupBy(e => e.RowNumber)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToList());

        var resultsByRow = result.UrlResults.ToDictionary(r => r.Item.RowNumber, r => r);

        foreach (BatchInputItem item in result.Items.OrderBy(i => i.RowNumber))
        {
            resultsByRow.TryGetValue(item.RowNumber, out BatchUrlResult? urlResult);

            string error = BuildErrorMessage(item.RowNumber, urlResult, errorsByRow);

            string? paginaCompliant = null;
            string? claimsFound = null;
            string? claimsCompliant = null;
            string? claimsNotCompliant = null;
            string? inputTokens = null;
            string? outputTokens = null;

            if (urlResult?.Response is SustainabilityClaimsCheckResponse response)
            {
                int total = response.Evaluations?.Count ?? 0;
                int compliant = response.Evaluations?.Count(e => e.IsCompliant) ?? 0;
                int notCompliant = total - compliant;

                paginaCompliant = response.IsCompliant ? "true" : "false";
                claimsFound = total.ToString(CultureInfo.InvariantCulture);
                claimsCompliant = compliant.ToString(CultureInfo.InvariantCulture);
                claimsNotCompliant = notCompliant.ToString(CultureInfo.InvariantCulture);
                inputTokens = response.InputTokens?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                outputTokens = response.OutputTokens?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            }

            rows.Add(new MetaReportRow
            {
                PageUrl = item.PageUrl,
                PublicationDate = item.PublicationDate ?? string.Empty,
                ResponsibleForContents = item.ResponsibleForContents ?? string.Empty,
                AccountableForContents = item.AccountableForContents ?? string.Empty,
                PaginaCompliant = paginaCompliant ?? string.Empty,
                ClaimsFound = claimsFound ?? string.Empty,
                ClaimsCompliant = claimsCompliant ?? string.Empty,
                ClaimsNotCompliant = claimsNotCompliant ?? string.Empty,
                InputTokens = inputTokens ?? string.Empty,
                OutputTokens = outputTokens ?? string.Empty,
                Error = error,
                PageReportBlobPath = pageXlsxPathsByRowNumber?.GetValueOrDefault(item.RowNumber) ?? string.Empty
            });
        }

        return rows;
    }

    public static List<CostReportRow> BuildCostReportRows(BatchRunResult result)
    {
        var rows = new List<CostReportRow>();

        var errorsByRow = result.ParseErrors
            .GroupBy(e => e.RowNumber)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToList());

        var resultsByRow = result.UrlResults.ToDictionary(r => r.Item.RowNumber, r => r);

        foreach (BatchInputItem item in result.Items.OrderBy(i => i.RowNumber))
        {
            resultsByRow.TryGetValue(item.RowNumber, out BatchUrlResult? urlResult);

            string error = BuildErrorMessage(item.RowNumber, urlResult, errorsByRow);

            string? inputTokens = null;
            string? outputTokens = null;
            string? inputPrice = null;
            string? outputPrice = null;
            string? inputCost = null;
            string? outputCost = null;
            string? totalCost = null;
            string? currency = null;

            if (urlResult?.Response is SustainabilityClaimsCheckResponse response)
            {
                if (response.InputTokens.HasValue)
                    inputTokens = response.InputTokens.Value.ToString(CultureInfo.InvariantCulture);

                if (response.OutputTokens.HasValue)
                    outputTokens = response.OutputTokens.Value.ToString(CultureInfo.InvariantCulture);

                if (ModelPrices.TryGetValue(CostModelId, out var pricing)
                    && response.InputTokens.HasValue
                    && response.OutputTokens.HasValue)
                {
                    inputPrice = pricing.InputPer1M.ToString("0.######", CultureInfo.InvariantCulture);
                    outputPrice = pricing.OutputPer1M.ToString("0.######", CultureInfo.InvariantCulture);

                    decimal inputCostValue = response.InputTokens.Value / 1_000_000m * pricing.InputPer1M;
                    decimal outputCostValue = response.OutputTokens.Value / 1_000_000m * pricing.OutputPer1M;
                    decimal totalCostValue = inputCostValue + outputCostValue;

                    inputCost = inputCostValue.ToString("0.######", CultureInfo.InvariantCulture);
                    outputCost = outputCostValue.ToString("0.######", CultureInfo.InvariantCulture);
                    totalCost = totalCostValue.ToString("0.######", CultureInfo.InvariantCulture);
                    currency = CostCurrency;
                }
            }

            rows.Add(new CostReportRow
            {
                PageUrl = item.PageUrl,
                ModelId = CostModelId,
                InputTokens = inputTokens ?? string.Empty,
                OutputTokens = outputTokens ?? string.Empty,
                InputPricePer1MTokens = inputPrice ?? string.Empty,
                OutputPricePer1MTokens = outputPrice ?? string.Empty,
                InputCost = inputCost ?? string.Empty,
                OutputCost = outputCost ?? string.Empty,
                TotalCost = totalCost ?? string.Empty,
                Currency = currency ?? string.Empty,
                Error = error
            });
        }

        return rows;
    }

    public static string BuildPerPageCsv(
        BatchInputItem item,
        SustainabilityClaimsCheckResponse response,
        DateTime runDateUtc)
    {
        List<string[]> rows = BuildPerPageRows(item, response, runDateUtc);
        return BuildPerPageCsv(rows);
    }

    public static string BuildPerPageCsv(List<string[]> rows)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var csv = new CsvWriter(writer, CreateExcelCsvConfiguration());

        foreach (string[] row in rows)
        {
            foreach (string field in row)
            {
                csv.WriteField(field);
            }
            csv.NextRecord();
        }

        return writer.ToString();
    }

    public static List<string[]> BuildPerPageRows(
        BatchInputItem item,
        SustainabilityClaimsCheckResponse response,
        DateTime runDateUtc)
    {
        int total = response.Evaluations?.Count ?? 0;
        int compliant = response.Evaluations?.Count(e => e.IsCompliant) ?? 0;
        int notCompliant = total - compliant;

        var rows = new List<string[]>();

        AddFixedWidthRow(rows, "Samenvatting");
        AddFixedWidthRow(rows, "Aantal gecontroleerde claims", total.ToString(CultureInfo.InvariantCulture));
        AddFixedWidthRow(rows, "Aantal compliant (voldoet aan eisen)", compliant.ToString(CultureInfo.InvariantCulture));
        AddFixedWidthRow(rows, "Aantal niet compliant", notCompliant.ToString(CultureInfo.InvariantCulture));
        AddFixedWidthRow(rows);

        AddFixedWidthRow(rows, "Pagina informatie");
        AddFixedWidthRow(rows, "URL", item.PageUrl);
        AddFixedWidthRow(rows, "Accountable", item.AccountableForContents ?? string.Empty);
        AddFixedWidthRow(rows, "Responsible", item.ResponsibleForContents ?? string.Empty);
        AddFixedWidthRow(rows, "Datum validatie", runDateUtc.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        AddFixedWidthRow(rows);

        AddFixedWidthRow(rows,
            "Nr",
            "Claim",
            "Claimtype",
            "Compliance Status",
            "Schendingen",
            "Alternatief/Suggestie");

        int index = 1;
        foreach (SustainabilityClaimEvaluation evaluation in response?.Evaluations ?? [])
        {
            AddFixedWidthRow(rows,
                index.ToString(CultureInfo.InvariantCulture),
                SanitizeNewlines(evaluation.ClaimText),
                SanitizeNewlines(evaluation.ClaimType),
                evaluation.IsCompliant ? "Compliant" : "Not compliant",
                SanitizeNewlines(string.Join(" | ", evaluation.Violations ?? [])),
                SanitizeNewlines(evaluation.SuggestedAlternative));
            index++;
        }

        return rows;
    }

    private static string BuildErrorMessage(
        int rowNumber,
        BatchUrlResult? urlResult,
        Dictionary<int, List<string>> parseErrors)
    {
        var errors = new List<string>();
        if (parseErrors.TryGetValue(rowNumber, out List<string>? rowErrors))
        {
            errors.AddRange(rowErrors);
        }

        if (!string.IsNullOrWhiteSpace(urlResult?.Error))
        {
            errors.Add(urlResult.Error!);
        }

        return string.Join(" | ", errors.Distinct());
    }

    private static void WriteSectionHeader(CsvWriter csv, string title)
    {
        WriteFixedWidthRow(csv, title);
    }

    private static void WriteLabelValue(CsvWriter csv, string label, string value)
    {
        WriteFixedWidthRow(csv, label, value);
    }

    private static void WriteBlankRow(CsvWriter csv)
    {
        WriteFixedWidthRow(csv);
    }

    private static void AddFixedWidthRow(List<string[]> rows, params string[] fields)
    {
        string[] row = new string[PerPageColumnCount];
        for (int i = 0; i < PerPageColumnCount; i++)
        {
            row[i] = i < fields.Length ? fields[i] : string.Empty;
        }
        rows.Add(row);
    }

    private static CsvConfiguration CreateExcelCsvConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";"
        };
    }

    private static void WriteFixedWidthRow(CsvWriter csv, params string[] fields)
    {
        int written = 0;

        foreach (string field in fields)
        {
            csv.WriteField(field);
            written++;
        }

        for (int i = written; i < PerPageColumnCount; i++)
        {
            csv.WriteField(string.Empty);
        }

        csv.NextRecord();
    }

    private static string SanitizeNewlines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Trim();
    }

    public sealed class MetaReportRow
    {
        public string PageUrl { get; set; } = string.Empty;
        public string PublicationDate { get; set; } = string.Empty;
        public string ResponsibleForContents { get; set; } = string.Empty;
        public string AccountableForContents { get; set; } = string.Empty;
        public string PaginaCompliant { get; set; } = string.Empty;
        public string ClaimsFound { get; set; } = string.Empty;
        public string ClaimsCompliant { get; set; } = string.Empty;
        public string ClaimsNotCompliant { get; set; } = string.Empty;
        public string InputTokens { get; set; } = string.Empty;
        public string OutputTokens { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string PageReportBlobPath { get; set; } = string.Empty;
    }

    public sealed class CostReportRow
    {
        public string PageUrl { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string InputTokens { get; set; } = string.Empty;
        public string OutputTokens { get; set; } = string.Empty;
        public string InputPricePer1MTokens { get; set; } = string.Empty;
        public string OutputPricePer1MTokens { get; set; } = string.Empty;
        public string InputCost { get; set; } = string.Empty;
        public string OutputCost { get; set; } = string.Empty;
        public string TotalCost { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
