using System.Collections.Concurrent;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using GenAiIncubator.LlmUtils.ReportRunner.Infrastructure;
using GenAiIncubator.LlmUtils.ReportRunner.Models;
using GenAiIncubator.LlmUtils.ReportRunner.Services;
using Microsoft.Extensions.Hosting;
using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils.ReportRunner.Jobs;

public class SustainabilityClaimsReportJob(
	ISustainabilityClaimsService _service,
	IReportWriter _writer,
	IHostEnvironment _env,
	ITranslationService _translationService,
	IReportInputProvider _inputProvider,
	IGroundTruthSheetAppender _groundTruthSheetAppender) : IReportJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
		// Load dataset input (ground truth workbook mapping); derive URLs from mapping keys
		ReportInput reportInput = await _inputProvider.GetAsync(cancellationToken);
		string[] urls = reportInput.GroundTruth?.UrlToTabName?.Keys?.ToArray() ?? [];
        if (urls.Length == 0)
        {
            Console.WriteLine("No URLs configured. Edit SustainabilityClaimsReportJob to add URLs.");
            return;
        }

		string reportsRoot = Path.Combine(_env.ContentRootPath, "Resources", "reports", "sustainability", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(reportsRoot);

        string claimsCsvPath = Path.Combine(reportsRoot, "claims.csv");
        string violationsCsvPath = Path.Combine(reportsRoot, "violations.csv");
        string errorsCsvPath = Path.Combine(reportsRoot, "errors.csv");

        var claimRows = new ConcurrentBag<ClaimRow>();
        var violationRows = new ConcurrentBag<ViolationRow>();
        var errorRows = new ConcurrentBag<ErrorRow>();

        int maxDegreeOfParallelism = Math.Max(1, int.TryParse(Environment.GetEnvironmentVariable("RUNNER_MAX_DOP"), out var dop) ? dop : 3);

        Console.WriteLine($"Running {urls.Length} URLs with DOP={maxDegreeOfParallelism}...");

        await Parallel.ForEachAsync(urls, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = cancellationToken }, async (url, ct) =>
        {
            try
            {
                List<SustainabilityClaimComplianceEvaluation> evaluations = await _service.CheckSustainabilityClaimsAsync(url, ct);

                foreach (var evaluation in evaluations)
                {
                    string claimId = GenerateClaimId(url, evaluation.Claim.ClaimText);
                    string translatedClaim = await _translationService.TranslateAsync(
                        evaluation.Claim.ClaimText,
                        ct);

                    var claimRow = new ClaimRow
                    {
                        Url = url,
                        ClaimId = claimId,
                        ClaimText = SanitizeNewlines(evaluation.Claim.ClaimText),
                        RelatedText = SanitizeNewlines(evaluation.Claim.RelatedText),
                        ClaimReason = SanitizeNewlines(evaluation.Claim.Reasoning),
                        TranslatedClaim = SanitizeNewlines(translatedClaim),
                        ClaimType = evaluation.Claim.ClaimType.ToString(),
                        IsCompliant = evaluation.IsCompliant,
                        NumViolations = evaluation.Violations.Count,
                        ViolationCodes = string.Join("|", evaluation.Violations.Select(v => v.Code.ToString())),
                        Warnings = string.Join("|", evaluation.Warnings ?? new List<string>()),
                        SuggestedAlternative = SanitizeNewlines(evaluation.SuggestedAlternative),
                        RelatedUrls = string.Join("|", evaluation.Claim.RelatedUrls ?? []),
                        RunTimestamp = DateTime.UtcNow.ToString("o")
                    };
                    claimRows.Add(claimRow);

                    foreach (var v in evaluation.Violations)
                    {
                        violationRows.Add(new ViolationRow
                        {
                            Url = url,
                            ClaimId = claimId,
                            ViolationCode = v.Code.ToString(),
                            ViolationMessage = SanitizeNewlines(v.Message)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                errorRows.Add(new ErrorRow
                {
                    Url = url,
                    ErrorType = ex.GetType().Name,
                    ErrorMessage = SanitizeNewlines(ex.Message)
                });
            }
        });

        await _writer.WriteAsync(claimsCsvPath, claimRows, cancellationToken);
        await _writer.WriteAsync(violationsCsvPath, violationRows, cancellationToken);
        if (!errorRows.IsEmpty) await _writer.WriteAsync(errorsCsvPath, errorRows, cancellationToken);

		// Also create an Excel workbook with Summary, per-URL Claims, Violations, and ground truth results
		string xlsxPath = Path.Combine(reportsRoot, $"report-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.xlsx");
		CreateWorkbook(xlsxPath, urls, [.. claimRows], [.. violationRows], reportInput.GroundTruth, _groundTruthSheetAppender);

        Console.WriteLine($"Report written to: {reportsRoot}");
    }

    private static string GenerateClaimId(string url, string claimText)
    {
        string input = url + "|" + claimText;
        byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    /// <summary>
    /// Removes newlines from text to prevent CSV rows from being split across multiple lines.
    /// CsvHelper automatically handles commas and double quotes, so we only need to handle newlines.
    /// </summary>
    private static string SanitizeNewlines(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
			.Replace("\r\n", " ")
			.Replace("\n", " ")
			.Replace("\r", " ")
			.Trim();
	}

	private static void CreateWorkbook(
		string path,
		IReadOnlyCollection<string> urls,
		List<ClaimRow> claims,
		List<ViolationRow> violations,
		GroundTruthWorkbookSpec? groundTruth,
		IGroundTruthSheetAppender groundTruthSheetAppender)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		IWorkbook workbook = new XSSFWorkbook();

		// Styles
		ICellStyle boldStyle = workbook.CreateCellStyle();
		var boldFont = workbook.CreateFont();
		boldFont.IsBold = true;
		boldStyle.SetFont(boldFont);
		ICellStyle sectionHeaderStyle = boldStyle;
		ICellStyle dividerStyle = workbook.CreateCellStyle();
		dividerStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
		dividerStyle.FillPattern = FillPattern.SolidForeground;
		dividerStyle.BorderTop = BorderStyle.Medium;
		dividerStyle.BorderBottom = BorderStyle.Medium;

		// Summary sheet
		var summarySheet = workbook.CreateSheet("Summary");
		int r = 0;
		CreateCell(summarySheet, r, 0, "Summary").CellStyle = boldStyle; r += 2;

		int totalUrls = urls.Count;
		int totalClaims = claims.Count;
		int totalNonCompliant = claims.Count(c => !c.IsCompliant);
		int totalViolations = violations.Count;

		CreateRow(summarySheet, r).CreateCell(0).SetCellValue("URLs checked"); CreateRow(summarySheet, r).CreateCell(1).SetCellValue(totalUrls); r++;
		CreateRow(summarySheet, r).CreateCell(0).SetCellValue("Claims found"); CreateRow(summarySheet, r).CreateCell(1).SetCellValue(totalClaims); r++;
		CreateRow(summarySheet, r).CreateCell(0).SetCellValue("Non-compliant claims"); CreateRow(summarySheet, r).CreateCell(1).SetCellValue(totalNonCompliant); r++;
		CreateRow(summarySheet, r).CreateCell(0).SetCellValue("Requirements not met (violations)"); CreateRow(summarySheet, r).CreateCell(1).SetCellValue(totalViolations); r += 2;

		// Per URL breakdown
		CreateCell(summarySheet, r, 0, "Per URL").CellStyle = boldStyle; r++;
		var headerRow = CreateRow(summarySheet, r);
		headerRow.CreateCell(0).SetCellValue("URL");
		headerRow.CreateCell(1).SetCellValue("Claims");
		headerRow.CreateCell(2).SetCellValue("NonCompliant");
		headerRow.CreateCell(3).SetCellValue("Violations");
		foreach (var cell in headerRow.Cells) cell.CellStyle = boldStyle;
		r++;

		var claimsByUrl = claims.GroupBy(c => c.Url).ToDictionary(g => g.Key, g => g.ToList());
		var violationsByUrlCount = violations.GroupBy(v => v.Url).ToDictionary(g => g.Key, g => g.Count());

		foreach (var url in urls)
		{
			claimsByUrl.TryGetValue(url, out var cList);
			int urlClaims = cList?.Count ?? 0;
			int urlNonCompliant = cList?.Count(c => !c.IsCompliant) ?? 0;
			int urlViolations = violationsByUrlCount.TryGetValue(url, out int vCount) ? vCount : 0;

			var row = CreateRow(summarySheet, r);
			row.CreateCell(0).SetCellValue(url);
			row.CreateCell(1).SetCellValue(urlClaims);
			row.CreateCell(2).SetCellValue(urlNonCompliant);
			row.CreateCell(3).SetCellValue(urlViolations);
			r++;
		}
		AutoSizeColumns(summarySheet, 4);

		// Requirements not met by code
		r += 2;
		CreateCell(summarySheet, r, 0, "Requirements not met by code").CellStyle = boldStyle; r++;
		var rcHdr = CreateRow(summarySheet, r++);
		rcHdr.CreateCell(0).SetCellValue("RequirementCode");
		rcHdr.CreateCell(1).SetCellValue("Count");
		foreach (var cell in rcHdr.Cells) cell.CellStyle = boldStyle;
		var countByCode = violations
			.GroupBy(v => v.ViolationCode)
			.ToDictionary(g => g.Key, g => g.Count());
		var allRequirementCodes = Enum.GetNames(typeof(RequirementCode)).OrderBy(n => n);
		foreach (var codeName in allRequirementCodes)
		{
			var row = CreateRow(summarySheet, r++);
			row.CreateCell(0).SetCellValue(codeName);
			row.CreateCell(1).SetCellValue(countByCode.TryGetValue(codeName, out int cnt) ? cnt : 0);
		}
		AutoSizeColumns(summarySheet, 4);

		// Per-URL claim sheets
		var existingSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			summarySheet.SheetName
		};
		int index = 1;
		var violationsByUrl = violations.GroupBy(v => v.Url).ToDictionary(g => g.Key, g => g.ToList());
		foreach (var url in urls)
		{
			string sheetName = GenerateUniqueSheetName(url, index, existingSheetNames);
			var ws = workbook.CreateSheet(sheetName);
			existingSheetNames.Add(sheetName);

			int row = 0;
			var titleRow = CreateRow(ws, row++);
			titleRow.CreateCell(0).SetCellValue("Claims for URL");
			titleRow.CreateCell(1).SetCellValue(url);
			foreach (var cell in titleRow.Cells) cell.CellStyle = boldStyle;
			row++;

			var hdr = CreateRow(ws, row++);
            string[] headers =
            [
                nameof(ClaimRow.Url),
				nameof(ClaimRow.ClaimId),
				nameof(ClaimRow.ClaimText),
				nameof(ClaimRow.RelatedText),
                nameof(ClaimRow.ClaimReason),
				nameof(ClaimRow.TranslatedClaim),
				nameof(ClaimRow.ClaimType),
				nameof(ClaimRow.IsCompliant),
				nameof(ClaimRow.NumViolations),
				nameof(ClaimRow.ViolationCodes),
                    nameof(ClaimRow.Warnings),
				nameof(ClaimRow.SuggestedAlternative),
				nameof(ClaimRow.RelatedUrls),
				nameof(ClaimRow.RunTimestamp)
			];
			for (int i = 0; i < headers.Length; i++)
			{
				var cell = hdr.CreateCell(i);
				cell.SetCellValue(headers[i]);
				cell.CellStyle = boldStyle;
			}

			var groupList = claimsByUrl.TryGetValue(url, out var gl) && gl is not null ? gl : new List<ClaimRow>();
			foreach (var c in groupList)
			{
				var data = CreateRow(ws, row++);
				data.CreateCell(0).SetCellValue(c.Url);
				data.CreateCell(1).SetCellValue(c.ClaimId);
				data.CreateCell(2).SetCellValue(c.ClaimText);
				data.CreateCell(3).SetCellValue(c.RelatedText);
                data.CreateCell(4).SetCellValue(c.ClaimReason);
                data.CreateCell(5).SetCellValue(c.TranslatedClaim);
                data.CreateCell(6).SetCellValue(c.ClaimType);
                data.CreateCell(7).SetCellValue(c.IsCompliant);
                data.CreateCell(8).SetCellValue(c.NumViolations);
                data.CreateCell(9).SetCellValue(c.ViolationCodes);
                data.CreateCell(10).SetCellValue(c.Warnings);
                data.CreateCell(11).SetCellValue(c.SuggestedAlternative);
                data.CreateCell(12).SetCellValue(c.RelatedUrls);
                data.CreateCell(13).SetCellValue(c.RunTimestamp);
			}

            AutoSizeColumns(ws, 14);

			// Claims stats
			row += 1;
			var claimsStatsHeader = CreateRow(ws, row++);
			claimsStatsHeader.CreateCell(0).SetCellValue("Claims stats");
			foreach (var cell in claimsStatsHeader.Cells) cell.CellStyle = sectionHeaderStyle;

			int claimCount = groupList.Count;
			int nonCompliantClaims = groupList.Count(c => !c.IsCompliant);
			int compliantClaims = claimCount - nonCompliantClaims;
			double complianceRate = claimCount > 0 ? (double)compliantClaims / claimCount : 0.0;
			double avgViolationsPerClaim = claimCount > 0 ? groupList.Average(c => c.NumViolations) : 0.0;

			var cs1 = CreateRow(ws, row++); cs1.CreateCell(0).SetCellValue("Total claims"); cs1.CreateCell(1).SetCellValue(claimCount);
			var cs2 = CreateRow(ws, row++); cs2.CreateCell(0).SetCellValue("Non-compliant claims"); cs2.CreateCell(1).SetCellValue(nonCompliantClaims);
			var cs3 = CreateRow(ws, row++); cs3.CreateCell(0).SetCellValue("Compliance rate"); cs3.CreateCell(1).SetCellValue(complianceRate);
			var cs4 = CreateRow(ws, row++); cs4.CreateCell(0).SetCellValue("Avg. violations per claim"); cs4.CreateCell(1).SetCellValue(avgViolationsPerClaim);

			// Expected results (static template)
			row += 1;
			var expectedHeader = CreateRow(ws, row++);
			expectedHeader.CreateCell(0).SetCellValue("Expected results (fill manually)");
			foreach (var cell in expectedHeader.Cells) cell.CellStyle = sectionHeaderStyle;
			var er1 = CreateRow(ws, row++); er1.CreateCell(0).SetCellValue("Expected total claims"); er1.CreateCell(1).SetCellValue(0);
			var er2 = CreateRow(ws, row++); er2.CreateCell(0).SetCellValue("Expected non-compliant claims"); er2.CreateCell(1).SetCellValue(0);
			var er3 = CreateRow(ws, row++); er3.CreateCell(0).SetCellValue("Notes"); er3.CreateCell(1).SetCellValue("");

			// Divider between Claims section and Violations section
			row = InsertSectionDivider(ws, row, 12, dividerStyle);

			// Violations section for this URL
			row += 2;
			var vTitle = CreateRow(ws, row++);
			vTitle.CreateCell(0).SetCellValue("Violations for URL");
			foreach (var cell in vTitle.Cells) cell.CellStyle = boldStyle;

			var vHdr = CreateRow(ws, row++);
			string[] vHeaders = [nameof(ViolationRow.Url), nameof(ViolationRow.ClaimId), nameof(ViolationRow.ViolationCode), nameof(ViolationRow.ViolationMessage)];
			for (int i = 0; i < vHeaders.Length; i++)
			{
				var cell = vHdr.CreateCell(i);
				cell.SetCellValue(vHeaders[i]);
				cell.CellStyle = boldStyle;
			}
			var urlViolations = violationsByUrl.TryGetValue(url, out var vList) && vList is not null ? vList : new List<ViolationRow>();
			foreach (var v in urlViolations)
			{
				var vRow = CreateRow(ws, row++);
				vRow.CreateCell(0).SetCellValue(v.Url);
				vRow.CreateCell(1).SetCellValue(v.ClaimId);
				vRow.CreateCell(2).SetCellValue(v.ViolationCode);
				vRow.CreateCell(3).SetCellValue(v.ViolationMessage);
			}
			AutoSizeColumns(ws, 12);

			// Violations stats
			row += 1;
			var vStatsHeader = CreateRow(ws, row++);
			vStatsHeader.CreateCell(0).SetCellValue("Violations stats");
			foreach (var cell in vStatsHeader.Cells) cell.CellStyle = sectionHeaderStyle;
			int vCount = urlViolations.Count;
			double vPerClaim = claimCount > 0 ? (double)vCount / claimCount : 0.0;
			var vs1 = CreateRow(ws, row++); vs1.CreateCell(0).SetCellValue("Total violations"); vs1.CreateCell(1).SetCellValue(vCount);
			var vs2 = CreateRow(ws, row++); vs2.CreateCell(0).SetCellValue("Violations per claim"); vs2.CreateCell(1).SetCellValue(vPerClaim);

			// Violations by code (for this URL)
			var byCode = urlViolations.GroupBy(v => v.ViolationCode).OrderByDescending(g => g.Count());
			if (byCode.Any())
			{
				row += 1;
				var vbcHeader = CreateRow(ws, row++);
				vbcHeader.CreateCell(0).SetCellValue("Violations by code");
				foreach (var cell in vbcHeader.Cells) cell.CellStyle = sectionHeaderStyle;
				var vbcHdr = CreateRow(ws, row++);
				vbcHdr.CreateCell(0).SetCellValue("Code");
				vbcHdr.CreateCell(1).SetCellValue("Count");
				foreach (var cell in vbcHdr.Cells) cell.CellStyle = boldStyle;
				foreach (var g in byCode)
				{
					var rbc = CreateRow(ws, row++);
					rbc.CreateCell(0).SetCellValue(g.Key);
					rbc.CreateCell(1).SetCellValue(g.Count());
				}
			}

			// Divider between Violations section and Ground truth section
			row = InsertSectionDivider(ws, row, 12, dividerStyle);

			// Ground truth results (if mapping exists)
			row += 2;
			var mlrHeader = CreateRow(ws, row++);
			mlrHeader.CreateCell(0).SetCellValue("Ground truth results");
			foreach (var cell in mlrHeader.Cells) cell.CellStyle = sectionHeaderStyle;

			string? manualPath = groundTruth?.WorkbookPath;
			string? tabName = null;
			if (groundTruth?.UrlToTabName is not null && groundTruth.UrlToTabName.TryGetValue(url, out var mappedTab))
			{
				tabName = mappedTab;
			}
			if (!string.IsNullOrWhiteSpace(manualPath) && !string.IsNullOrWhiteSpace(tabName))
			{
				row = groundTruthSheetAppender.AppendSheet(ws, row, manualPath!, tabName!);
			}
			else
			{
				var note = CreateRow(ws, row++);
				note.CreateCell(0).SetCellValue("No ground truth mapping found or workbook not configured.");
			}
			index++;
		}

		// Violations sheet
		var violSheet = workbook.CreateSheet("Violations");
		int vr = 0;
		var vheader = CreateRow(violSheet, vr++);
		vheader.CreateCell(0).SetCellValue(nameof(ViolationRow.Url));
		vheader.CreateCell(1).SetCellValue(nameof(ViolationRow.ClaimId));
		vheader.CreateCell(2).SetCellValue(nameof(ViolationRow.ViolationCode));
		vheader.CreateCell(3).SetCellValue(nameof(ViolationRow.ViolationMessage));
		foreach (var cell in vheader.Cells) cell.CellStyle = boldStyle;
		foreach (var v in violations)
		{
			var row = CreateRow(violSheet, vr++);
			row.CreateCell(0).SetCellValue(v.Url);
			row.CreateCell(1).SetCellValue(v.ClaimId);
			row.CreateCell(2).SetCellValue(v.ViolationCode);
			row.CreateCell(3).SetCellValue(v.ViolationMessage);
		}
		AutoSizeColumns(violSheet, 4);

		using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
		workbook.Write(fs);
	}

	private static IRow CreateRow(ISheet sheet, int rowIndex)
	{
		return sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
	}

	private static ICell CreateCell(ISheet sheet, int rowIndex, int colIndex, string value)
	{
		var row = CreateRow(sheet, rowIndex);
		var cell = row.GetCell(colIndex) ?? row.CreateCell(colIndex);
		cell.SetCellValue(value);
		return cell;
	}

	private static void AutoSizeColumns(ISheet sheet, int columnCount)
	{
		for (int i = 0; i < columnCount; i++) sheet.AutoSizeColumn(i);
	}

	private static int InsertSectionDivider(ISheet sheet, int rowIndex, int columnCount, ICellStyle dividerStyle)
	{
		var row = CreateRow(sheet, rowIndex);
		for (int c = 0; c < columnCount; c++)
		{
			var cell = row.GetCell(c) ?? row.CreateCell(c);
			cell.CellStyle = dividerStyle;
		}
		// Merge the divider row across the table width
		NPOI.SS.Util.CellRangeAddress region = new(rowIndex, rowIndex, 0, columnCount - 1);
		try { sheet.AddMergedRegion(region); } catch { }
		row.Height = (short)(20 * 2);
		return rowIndex + 1;
	}

	private static string GenerateUniqueSheetName(string url, int index, HashSet<string> existingNames)
	{
		string baseName = $"{index:00} {CreateShortNameFromUrl(url)}";
		string sanitized = SanitizeSheetName(baseName);
		if (!existingNames.Contains(sanitized)) return sanitized;

		int suffix = 1;
		while (true)
		{
			string candidate = SanitizeSheetName($"{sanitized}-{suffix}");
			if (!existingNames.Contains(candidate)) return candidate;
			suffix++;
		}
	}

	private static string CreateShortNameFromUrl(string url)
	{
		try
		{
			var uri = new Uri(url);
			string host = uri.Host;
			string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			string lastSegment = segments.Length > 0 ? segments[^1] : string.Empty;
			string name = string.IsNullOrWhiteSpace(lastSegment) ? host : $"{host}/{lastSegment}";
			return name.Length > 25 ? name[..25] : name;
		}
		catch
		{
			return url.Length > 25 ? url[..25] : url;
		}
	}

	private static string SanitizeSheetName(string name)
	{
		// Remove invalid characters and limit to 31 chars
		string cleaned = string.Join(string.Empty, name.Where(ch => ch != ':' && ch != '\\' && ch != '/' && ch != '?' && ch != '*' && ch != '[' && ch != ']'));
		if (cleaned.Length > 31) cleaned = cleaned[..31];
		if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "Sheet";
		return cleaned;
    }
}


