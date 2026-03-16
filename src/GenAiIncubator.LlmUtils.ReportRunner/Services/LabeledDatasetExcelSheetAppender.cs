using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GenAiIncubator.LlmUtils.ReportRunner.Services;

/// <summary>
/// Minimal value-and-merges copy from a source sheet into a target sheet.
/// Styles, images and complex objects are not preserved.
/// </summary>
public sealed class LabeledDatasetExcelSheetAppender : IGroundTruthSheetAppender
{
    public int AppendSheet(ISheet targetSheet, int startRow, string workbookPath, string tabName)
    {
        if (!File.Exists(workbookPath))
        {
            var missingRow = EnsureRow(targetSheet, startRow++);
            missingRow.CreateCell(0).SetCellValue($"Ground truth workbook not found: {workbookPath}");
            return startRow;
        }

        using var fs = new FileStream(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        IWorkbook? srcWb = new XSSFWorkbook(fs);
        var srcSheet = srcWb.GetSheet(tabName);
        if (srcSheet == null)
        {
            var missingRow = EnsureRow(targetSheet, startRow++);
            missingRow.CreateCell(0).SetCellValue($"Tab not found: {tabName}");
            return startRow;
        }

        int targetRowIndex = startRow;
        var fillStyleCache = new Dictionary<string, ICellStyle>();
        for (int i = srcSheet.FirstRowNum; i <= srcSheet.LastRowNum; i++)
        {
            var srcRow = srcSheet.GetRow(i);
            var dstRow = EnsureRow(targetSheet, targetRowIndex++);
            if (srcRow == null) continue;

            // Copy row height (including zero-height/hidden)
            try
            {
                dstRow.ZeroHeight = srcRow.ZeroHeight;
                if (!srcRow.ZeroHeight && srcRow.Height > 0)
                {
                    dstRow.Height = srcRow.Height; // height is in twips (1/20 pt)
                }
            }
            catch { /* ignore row height copy errors */ }

            for (int c = srcRow.FirstCellNum; c < srcRow.LastCellNum; c++)
            {
                var srcCell = srcRow.GetCell(c);
                if (srcCell == null) continue;
                var dstCell = dstRow.GetCell(c) ?? dstRow.CreateCell(c);
                CopyCellValue(srcCell, dstCell);

                // Copy fill pattern and colors (XSSF RGB/theme-aware) to preserve coloring
                try
                {
                    var s = srcCell.CellStyle;
                    if (s.FillPattern == FillPattern.NoFill)
                    {
                        // Nothing to copy
                    }
                    else
                    {
                        string key;
                        if (s is XSSFCellStyle xs)
                        {
                            var fg = xs.FillForegroundColorColor as XSSFColor;
                            var bg = xs.FillBackgroundColorColor as XSSFColor;
                            string fgHex = fg?.ARGBHex ?? "";
                            string bgHex = bg?.ARGBHex ?? "";
                            key = $"xssf|{(int)s.FillPattern}|{fgHex}|{bgHex}";

                            if (!fillStyleCache.TryGetValue(key, out var cloned))
                            {
                                cloned = ((XSSFWorkbook)targetSheet.Workbook).CreateCellStyle();
                                cloned.FillPattern = s.FillPattern;
                                if (fg is not null) ((XSSFCellStyle)cloned).SetFillForegroundColor(fg);
                                if (bg is not null) ((XSSFCellStyle)cloned).SetFillBackgroundColor(bg);
                                fillStyleCache[key] = cloned;
                            }
                            dstCell.CellStyle = cloned;
                        }
                        else
                        {
                            // Fallback for HSSF/indexed styles
                            key = $"idx|{(int)s.FillPattern}|{s.FillForegroundColor}|{s.FillBackgroundColor}";
                            if (!fillStyleCache.TryGetValue(key, out var cloned))
                            {
                                cloned = targetSheet.Workbook.CreateCellStyle();
                                cloned.FillPattern = s.FillPattern;
                                cloned.FillForegroundColor = s.FillForegroundColor;
                                cloned.FillBackgroundColor = s.FillBackgroundColor;
                                fillStyleCache[key] = cloned;
                            }
                            dstCell.CellStyle = cloned;
                        }
                    }
                }
                catch { /* ignore style copy errors */ }
            }
        }

        // Copy merged regions
        for (int m = 0; m < srcSheet.NumMergedRegions; m++)
        {
            var region = srcSheet.GetMergedRegion(m);
            var shifted = new NPOI.SS.Util.CellRangeAddress(
                region.FirstRow + startRow - srcSheet.FirstRowNum,
                region.LastRow + startRow - srcSheet.FirstRowNum,
                region.FirstColumn,
                region.LastColumn);
            targetSheet.AddMergedRegion(shifted);
        }

        // Copy pictures (images)
        try
        {
            var xssfSrcSheet = srcSheet as XSSFSheet;
            var xssfTgtSheet = targetSheet as XSSFSheet;
            if (xssfSrcSheet is not null && xssfTgtSheet is not null)
            {
                var tgtDrawing = xssfTgtSheet.CreateDrawingPatriarch() as XSSFDrawing;
                if (tgtDrawing is not null)
                {
                    int minPicCol = int.MaxValue;
                    int maxPicCol = -1;
                    // Prefer existing drawings; fall back to CreateDrawingPatriarch shapes
                    var drawings = new List<XSSFDrawing>();
                    try { drawings.AddRange(xssfSrcSheet.GetRelations().OfType<XSSFDrawing>()); } catch { }
                    if (drawings.Count == 0)
                    {
                        var maybe = xssfSrcSheet.CreateDrawingPatriarch() as XSSFDrawing;
                        if (maybe is not null) drawings.Add(maybe);
                    }

                    foreach (var d in drawings)
                    {
                        var shapes = d.GetShapes();
                        if (shapes is null) continue;
                        foreach (var shape in shapes)
                        {
                            if (shape is XSSFPicture pic && pic.PictureData is not null)
                            {
                                var data = pic.PictureData;
                                int pictureIdx = targetSheet.Workbook.AddPicture(data.Data, data.PictureType);
                                var anchor = pic.GetAnchor();
                                if (anchor is XSSFClientAnchor ca)
                                {
                                    int rowShift = startRow - srcSheet.FirstRowNum;
                                    var newAnchor = new XSSFClientAnchor(ca.Dx1, ca.Dy1, ca.Dx2, ca.Dy2,
                                        ca.Col1, ca.Row1 + rowShift,
                                        ca.Col2, ca.Row2 + rowShift)
                                    {
                                        AnchorType = ca.AnchorType
                                    };
                                    tgtDrawing.CreatePicture(newAnchor, pictureIdx);
                                    if (ca.Col1 < minPicCol) minPicCol = ca.Col1;
                                    if (ca.Col2 > maxPicCol) maxPicCol = ca.Col2;
                                }
                            }
                        }
                    }
                    // Best-effort: copy column widths across any columns spanned by pictures to reduce perceived stretching
                    if (maxPicCol >= 0)
                    {
                        try { targetSheet.DefaultColumnWidth = srcSheet.DefaultColumnWidth; } catch { }
                        for (int col = minPicCol; col <= maxPicCol; col++)
                        {
                            try { targetSheet.SetColumnWidth(col, srcSheet.GetColumnWidth(col)); } catch { }
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore image copy errors to keep report generation robust
        }

        return targetRowIndex;
    }

    private static IRow EnsureRow(ISheet sheet, int rowIndex)
    {
        return sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
    }

    private static void CopyCellValue(ICell src, ICell dst)
    {
        switch (src.CellType)
        {
            case CellType.String:
                dst.SetCellValue(src.StringCellValue);
                break;
            case CellType.Numeric:
                dst.SetCellValue(src.NumericCellValue);
                break;
            case CellType.Boolean:
                dst.SetCellValue(src.BooleanCellValue);
                break;
            case CellType.Formula:
                // Copy cached value to keep it simple; not copying formulas cross-book
                dst.SetCellValue(src.ToString());
                break;
            case CellType.Blank:
                dst.SetCellValue(string.Empty);
                break;
            default:
                dst.SetCellValue(src.ToString());
                break;
        }
    }
}


