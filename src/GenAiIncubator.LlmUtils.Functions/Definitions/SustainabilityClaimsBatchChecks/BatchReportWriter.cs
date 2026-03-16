using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ClosedXML.Excel;
using CsvHelper.Configuration;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsBatchChecks;

internal sealed class BatchReportWriter(BlobContainerClient containerClient) : IBatchReportWriter
{
    public async Task<BatchReportArtifacts> WriteAsync(BatchRunResult result, CancellationToken ct)
    {
        BatchReportArtifacts artifacts = new();
        string basePrefix = $"{result.RunFolder}/";
        string timestamp = result.RunTimestampUtc;
        string fileId = result.FileId;

        // Pre-compute per-page xlsx blob names so they can be embedded in the meta-report CSV
        Dictionary<int, string> pageXlsxPathsByRowNumber = result.UrlResults
            .Where(r => r.Response is not null)
            .ToDictionary(
                r => r.Item.RowNumber,
                r => $"{containerClient.Name}/{BuildPageBlobBaseName(basePrefix, r.Item.RowNumber - 1, r.Item.PageUrl, fileId, timestamp)}.xlsx");

        List<BatchReportFormatter.MetaReportRow> metaRows = BatchReportFormatter.BuildMetaReportRows(result, pageXlsxPathsByRowNumber);
        List<BatchReportFormatter.CostReportRow> costRows = BatchReportFormatter.BuildCostReportRows(result);

        string metaBlobName = $"{basePrefix}meta-report_{fileId}_{timestamp}.csv";
        await WriteCsvReportAsync(metaBlobName, metaRows, ct);
        artifacts.BlobPaths.Add(metaBlobName);

        string metaXlsxBlobName = $"{basePrefix}meta-report_{fileId}_{timestamp}.xlsx";
        await WriteXlsxReportAsync(metaXlsxBlobName, metaRows, "MetaReport", ct);
        artifacts.BlobPaths.Add(metaXlsxBlobName);

        string costBlobName = $"{basePrefix}cost-report_{fileId}_{timestamp}.csv";
        await WriteCsvReportAsync(costBlobName, costRows, ct);
        artifacts.BlobPaths.Add(costBlobName);

        string costXlsxBlobName = $"{basePrefix}cost-report_{fileId}_{timestamp}.xlsx";
        await WriteXlsxReportAsync(costXlsxBlobName, costRows, "CostReport", ct);
        artifacts.BlobPaths.Add(costXlsxBlobName);

        await WritePerPageReportsAsync(basePrefix, fileId, timestamp, result, artifacts, ct);

        return artifacts;
    }

    private async Task WriteCsvReportAsync<T>(string blobName, List<T> rows, CancellationToken ct)
    {
        await using var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";"
        };

        using (var csv = new CsvHelper.CsvWriter(writer, csvConfig))
        {
            await csv.WriteRecordsAsync(rows, ct);
        }
        await writer.FlushAsync(ct);

        stream.Position = 0;
        BlobClient blob = containerClient.GetBlobClient(blobName);
        Response<BlobContentInfo> response = await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);
        if (response.GetRawResponse().Status >= 400)
            throw new InvalidOperationException($"Failed to upload report blob '{blobName}'. Error: {response.GetRawResponse().ReasonPhrase}, code: {response.GetRawResponse().Status}");
    }

    private async Task WriteXlsxReportAsync<T>(string blobName, List<T> rows, string sheetName, CancellationToken ct)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.AddWorksheet(sheetName);
        worksheet.Cell(1, 1).InsertTable(rows, sheetName, true);
        worksheet.Columns().AdjustToContents();

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        BlobClient blob = containerClient.GetBlobClient(blobName);
        Response<BlobContentInfo> response = await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);
        if (response.GetRawResponse().Status >= 400)
            throw new InvalidOperationException($"Failed to upload report blob '{blobName}'. Error: {response.GetRawResponse().ReasonPhrase}, code: {response.GetRawResponse().Status}");
    }

    private async Task WritePerPageReportsAsync(
        string basePrefix,
        string fileId,
        string timestamp,
        BatchRunResult result,
        BatchReportArtifacts artifacts,
        CancellationToken ct)
    {
        foreach (BatchUrlResult urlResult in result.UrlResults)
        {
            if (urlResult.Response is null)
                continue;

            int rowNumber = urlResult.Item.RowNumber - 1; // first row is column names
            string pageBaseName = BuildPageBlobBaseName(basePrefix, rowNumber, urlResult.Item.PageUrl, fileId, timestamp);
            string pageBlobName = $"{pageBaseName}.csv";
            List<string[]> perPageRows = BatchReportFormatter.BuildPerPageRows(urlResult.Item, urlResult.Response, result.RunDateUtc);
            await WritePerPageReportAsync(pageBlobName, perPageRows, ct);
            artifacts.BlobPaths.Add(pageBlobName);

            string pageXlsxBlobName = $"{pageBaseName}.xlsx";
            await WritePerPageReportXlsxAsync(pageXlsxBlobName, perPageRows, ct);
            artifacts.BlobPaths.Add(pageXlsxBlobName);
        }
    }

    private async Task WritePerPageReportAsync(
        string blobName,
        List<string[]> rows,
        CancellationToken ct)
    {
        string csvContent = BatchReportFormatter.BuildPerPageCsv(rows);
        byte[] payload = Encoding.UTF8.GetBytes(csvContent);
        await using MemoryStream stream = new(payload);

        BlobClient blob = containerClient.GetBlobClient(blobName);
        Response<BlobContentInfo> uploadResponse = await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);
        if (uploadResponse.GetRawResponse().Status >= 400)
            throw new InvalidOperationException($"Failed to upload per-page report blob '{blobName}'. Error: {uploadResponse.GetRawResponse().ReasonPhrase}");
    }

    private async Task WritePerPageReportXlsxAsync(
        string blobName,
        List<string[]> rows,
        CancellationToken ct)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.AddWorksheet("PerPageReport");

        string? GetValue(int index, int column)
        {
            if (index < 0 || index >= rows.Count)
                return null;
            string[] row = rows[index];
            if (column < 0 || column >= row.Length)
                return null;
            return row[column];
        }

        void SetLabelValue(int row, int labelCol, int valueCol, string? label, string? value)
        {
            worksheet.Cell(row, labelCol).Value = label ?? string.Empty;
            worksheet.Cell(row, valueCol).Value = value ?? string.Empty;
        }

        worksheet.Cell(1, 2).Value = GetValue(0, 0) ?? "Samenvatting";
        worksheet.Cell(1, 2).Style.Font.Bold = true;

        SetLabelValue(2, 2, 3, GetValue(1, 0), GetValue(1, 1));
        SetLabelValue(3, 2, 3, GetValue(2, 0), GetValue(2, 1));
        SetLabelValue(4, 2, 3, GetValue(3, 0), GetValue(3, 1));

        worksheet.Cell(1, 4).Value = GetValue(5, 0) ?? "Pagina informatie";
        worksheet.Cell(1, 4).Style.Font.Bold = true;

        SetLabelValue(2, 4, 5, GetValue(6, 0), GetValue(6, 1));
        SetLabelValue(3, 4, 5, GetValue(7, 0), GetValue(7, 1));
        SetLabelValue(4, 4, 5, GetValue(8, 0), GetValue(8, 1));
        SetLabelValue(5, 4, 5, GetValue(9, 0), GetValue(9, 1));

        int headerRow = 7;
        string[] header = rows.Count > 11 ? rows[11] : [];
        for (int col = 0; col < header.Length; col++)
        {
            worksheet.Cell(headerRow, col + 1).Value = header[col];
            worksheet.Cell(headerRow, col + 1).Style.Font.Bold = true;
            worksheet.Cell(headerRow, col + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int dataStartIndex = 12;
        int dataRow = headerRow + 1;
        for (int i = dataStartIndex; i < rows.Count; i++)
        {
            string[] row = rows[i];
            for (int col = 0; col < row.Length; col++)
            {
                string value = row[col];
                if (col == 3)
                {
                    if (string.Equals(value, "Compliant", StringComparison.OrdinalIgnoreCase))
                    {
                        worksheet.Cell(dataRow, col + 1).Value = "✓";
                        worksheet.Cell(dataRow, col + 1).Style.Font.FontColor = XLColor.Green;
                        worksheet.Cell(dataRow, col + 1).Style.Font.Bold = true;
                    }
                    else if (string.Equals(value, "Not compliant", StringComparison.OrdinalIgnoreCase))
                    {
                        worksheet.Cell(dataRow, col + 1).Value = "✗";
                        worksheet.Cell(dataRow, col + 1).Style.Font.FontColor = XLColor.Red;
                        worksheet.Cell(dataRow, col + 1).Style.Font.Bold = true;
                    }
                    else
                    {
                        worksheet.Cell(dataRow, col + 1).Value = value;
                    }
                }
                else
                {
                    worksheet.Cell(dataRow, col + 1).Value = value;
                }
            }
            dataRow++;
        }

        worksheet.Columns(1, 1).Width = 4;
        worksheet.Columns(2, 2).Width = 80;
        worksheet.Columns(3, 3).Width = 16;
        worksheet.Columns(4, 4).Width = 18;
        worksheet.Columns(5, 5).Width = 55;
        worksheet.Columns(6, 6).Width = 80;

        worksheet.Columns(2, 2).Style.Alignment.WrapText = true;
        worksheet.Columns(5, 6).Style.Alignment.WrapText = true;
        worksheet.Rows().AdjustToContents();

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        BlobClient blob = containerClient.GetBlobClient(blobName);
        Response<BlobContentInfo> uploadResponse = await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);
        if (uploadResponse.GetRawResponse().Status >= 400)
            throw new InvalidOperationException($"Failed to upload per-page report blob '{blobName}'. Error: {uploadResponse.GetRawResponse().ReasonPhrase}");
    }

    /// <summary>Builds the base blob name (without extension) for a per-page report.</summary>
    private static string BuildPageBlobBaseName(string basePrefix, int rowNumber, string pageUrl, string fileId, string timestamp)
    {
        string slug = SanitizeUrlForFileName(pageUrl);
        return $"{basePrefix}pages/page-{rowNumber}_{slug}_{fileId}_{timestamp}";
    }

    /// <summary>Converts a URL into a filesystem-safe slug (max 50 characters).</summary>
    private static string SanitizeUrlForFileName(string url)
    {
        string s = Regex.Replace(url, @"^https?://", string.Empty, RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"[^a-zA-Z0-9]", "-");
        s = Regex.Replace(s, @"-{2,}", "-");
        s = s.Trim('-');
        if (s.Length > 50)
            s = s[..50].TrimEnd('-');
        return s.ToLowerInvariant();
    }
}
