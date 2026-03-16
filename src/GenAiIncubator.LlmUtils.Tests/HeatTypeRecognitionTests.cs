using System.Text;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GenAiIncubator.LlmUtils.Tests;
public class HeaterTypeRecognitionTests
{
    private const double AcceptableRatio = 0.75;

    private readonly IServiceProvider _serviceProvider;
    private readonly HeaterRecognitionService _heaterTypeRecognition;
    private readonly ITestOutputHelper _output;

    public HeaterTypeRecognitionTests(ITestOutputHelper output)
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        _heaterTypeRecognition = _serviceProvider.GetRequiredService<HeaterRecognitionService>();
        _output = output;
    }

    [Fact]
    public async Task TypeRecognition_CVKetel_AtLeastAcceptableRatio()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/cv_ketel.jpg",
            "static/heaters/test_set/cv_ketel_2.jpeg",
            "static/heaters/test_set/cv_ketel_3.jpg",
            "static/heaters/test_set/cv_ketel_4.jpg",
            "static/heaters/test_set/cv_ketel_5.jpg",
            "static/heaters/test_set/cv_ketel_6.jpg",
            "static/heaters/test_set/cvketel-1.jpg",
            "static/heaters/test_set/cvketel-2.jpg",
            "static/heaters/test_set/cvketel-3.jpg",
            "static/heaters/test_set/cvketel-4.jpeg",
            "static/heaters/test_set/cvketel-5.jpg"
        };

        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.CVKetel);
    }

    [Fact]
    public async Task TypeRecognition_FullElectricHeatPump_AtLeastAcceptableRatio()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/full_electric_heat_pump.png",
            "static/heaters/test_set/full_electric_heat_pump_1.jpg",
            "static/heaters/test_set/full_electric_heat_pump_2.jpg",
            "static/heaters/test_set/full_electric_heat_pump_2.png",
            "static/heaters/test_set/full_electric_heat_pump_3.jpg",
            "static/heaters/test_set/full_electric_heat_pump_4.jpg"
        };

        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.FullElectricHeatPump);
    }

    [Fact]
    public async Task TypeRecognition_HybridHeatPump_AtLeastAcceptableRatio()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/hybrid_heat_pump.jpg",
            "static/heaters/test_set/hybrid_heat_pump_2.jpg",
            "static/heaters/test_set/hybrid_heat_pump_3.png",
            "static/heaters/test_set/hybrid_heat_pump_4.jpg",
            "static/heaters/test_set/hybrid_heat_pump_5.jpg",
            "static/heaters/test_set/hybrid_heat_pump_6.png",
            "static/heaters/test_set/hybrid_heat_pump_7.jpeg",
            "static/heaters/test_set/hybrid_heat_pump_8.jpg"
        };

        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.HybridHeatPump);
    }

    [Fact]
    public async Task TypeRecognition_Aircon_AtLeastAcceptableRatio()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/Airco binnenunit.webp",
            "static/heaters/test_set/Airco binnenunit 2.jpg",
            "static/heaters/test_set/Airco binnenunit 3.jpg",
            "static/heaters/test_set/Airco binnenunit 4.webp",
            "static/heaters/test_set/Airco buitenunit.jpg",
            "static/heaters/test_set/Airco buitenunit 2.jpg",
            "static/heaters/test_set/Airco buitenunit 3.jpg",
            "static/heaters/test_set/Airco buitenunit 4.webp",
            "static/heaters/test_set/airconditioning-1.jpeg",
            "static/heaters/test_set/airconditioning-2.jpeg",
        };

        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.AirConditioning);
    }

    [Fact]
    public async Task TypeRecognition_CityHeat_AtLeastAcceptableRatio()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/City heat.jpg",
            "static/heaters/test_set/City heat 2.jpg",
            "static/heaters/test_set/City heat 3.jpg",
            "static/heaters/test_set/City heat 4.jpg",
            "static/heaters/test_set/City heat 5.jpg",
            "static/heaters/test_set/City heat 6.jpg",
            "static/heaters/test_set/Stadswarmte 1.jpeg",
            "static/heaters/test_set/Stadswarmte 2.jpeg",
            "static/heaters/test_set/stadswarmte.jpg"
        };

        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.CityHeat);
    }

    [Fact]
    public async Task TypeRecognition_TestSet_Airheating()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/luchtverwarming_5.jpg",
            "static/heaters/test_set/luchtverwarming_7.jpg",
            "static/heaters/test_set/lucthverwarming_6.jpg",
            "static/heaters/test_set/luctvermwarming_2.jpg",
            "static/heaters/test_set/luctverwarming_1.jpg",
            "static/heaters/test_set/luctverwarming_3.jpg",
            "static/heaters/test_set/luctverwarming_4.jpg"
        };
        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.AirHeating);
    }

    [Fact]
    public async Task TypeRecognition_TestSet_CVKetel()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/cv_ketel.jpg",
            "static/heaters/test_set/cv_ketel_2.jpeg",
            "static/heaters/test_set/cv_ketel_3.jpg",
            "static/heaters/test_set/cv_ketel_4.jpg",
            "static/heaters/test_set/cv_ketel_5.jpg",
            "static/heaters/test_set/cv_ketel_6.jpg",
            "static/heaters/test_set/cvketel-1.jpg",
            "static/heaters/test_set/cvketel-2.jpg",
            "static/heaters/test_set/cvketel-3.jpg",
            "static/heaters/test_set/cvketel-4.jpeg",
            "static/heaters/test_set/cvketel-5.jpg"
        };
        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.CVKetel);
    }

    [Fact]
    public async Task TypeRecognition_TestSet_Electric()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/full_electric_heat_pump.png",
            "static/heaters/test_set/full_electric_heat_pump_1.jpg",
            "static/heaters/test_set/full_electric_heat_pump_2.jpg",
            "static/heaters/test_set/full_electric_heat_pump_2.png",
            "static/heaters/test_set/full_electric_heat_pump_3.jpg",
            "static/heaters/test_set/full_electric_heat_pump_4.jpg"
        };
        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.FullElectricHeatPump);
    }

    [Fact]
    public async Task TypeRecognition_TestSet_CityHeat()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/City heat.jpg",
            "static/heaters/test_set/City heat 2.jpg",
            "static/heaters/test_set/City heat 3.jpg",
            "static/heaters/test_set/City heat 4.jpg",
            "static/heaters/test_set/City heat 5.jpg",
            "static/heaters/test_set/City heat 6.jpg",
            "static/heaters/test_set/Stadswarmte 1.jpeg",
            "static/heaters/test_set/Stadswarmte 2.jpeg",
            "static/heaters/test_set/stadswarmte.jpg"
        };
        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.CityHeat);
    }

    [Fact]
    public async Task TypeRecognition_TestSet_Boiler()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/cvketel-1.jpg",
            "static/heaters/test_set/cvketel-2.jpg",
        };
        // boilers are classified as CVKetel
        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.CVKetel);
    }

    [Fact]
    public async Task TypeRecognition_OwnHeatSource_AtLeastAcceptableRatio()
    {
        var imagePaths = new[]
        {
            "static/heaters/test_set/eigen_warmtebron_1.jpg",
            "static/heaters/test_set/eigen_warmtebron_2.jpg",
            "static/heaters/test_set/eigen_warmtebron_3.jpg",
            "static/heaters/test_set/eigen_warmtebron_4.jpg",
            "static/heaters/test_set/eigen_warmtebron_5.jpg"
        };

        await VerifyHeaterTypeBatch(imagePaths, HeaterTypesEnum.OwnHeatSource);
    }

    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task RunTestSet_And_ProduceExcelReport()
    {
        const string testFolderDir = "static/heaters/";
        const string testFolderPath = "test_set/";
        string baseDir = GetProjectDirectory();
        string testDirPath = Path.Combine(baseDir, testFolderDir, testFolderPath);

        if (!Directory.Exists(testDirPath))
            throw new DirectoryNotFoundException(testDirPath);

        string[] files = [.. Directory
            .GetFiles(testDirPath)
            // .Where(_ => Path.GetFileName(_) == "stadswarmte.jpg") // For testing
            .Select(f => Path.Combine(testDirPath, Path.GetFileName(f)))];

        var reportRows = new List<(string File, HeaterTypesEnum Expected, List<HeaterTypesEnum> Recognized, bool IsMainCorrect, bool IsAltCorrect)>();

        foreach (var relative in files)
        {
            List<HeaterTypesEnum> recognized = await GetHeaterTypes(relative);
            string name = Path.GetFileName(relative)
                .ToLowerInvariant()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "");

            HeaterTypesEnum expected = name switch
            {
                var n when n.Contains("airco")
                    || n.Contains("airconditioning")
                    => HeaterTypesEnum.AirConditioning,

                var n when n.Contains("luctverwarming")
                    || n.Contains("luchtverwarming")
                    => HeaterTypesEnum.AirHeating,

                var n when n.Contains("eigenwarmtebron")
                    => HeaterTypesEnum.OwnHeatSource,

                var n when n.Contains("ketel")
                    || n.Contains("cvketel")
                    => HeaterTypesEnum.CVKetel,

                var n when n.Contains("electric")
                    => HeaterTypesEnum.FullElectricHeatPump,

                var n when n.Contains("hybrid")
                    => HeaterTypesEnum.HybridHeatPump,

                var n when n.Contains("cityheat")
                    || n.Contains("stadswarmte")
                    => HeaterTypesEnum.CityHeat,

                var n when n.Contains("boiler")
                    => HeaterTypesEnum.CVKetel,

                _ => HeaterTypesEnum.Unknown
            };

            bool isMainCorrect = recognized.First() == expected;
            bool isAltCorrect = isMainCorrect || recognized.Contains(expected);
            reportRows.Add((relative, expected, recognized, isMainCorrect, isAltCorrect));
        }

        // Calculate summary statistics
        int totalFiles = reportRows.Count;
        int mainCorrect = reportRows.Count(r => r.IsMainCorrect);
        int altCorrect = reportRows.Count(r => r.IsAltCorrect);
        int mainIncorrect = totalFiles - mainCorrect;
        int altIncorrect = totalFiles - altCorrect;
        double mainAccuracy = totalFiles > 0 ? (double)mainCorrect / totalFiles : 0;
        double altAccuracy = totalFiles > 0 ? (double)altCorrect / totalFiles : 0;
        
        // Additional useful statistics
        int unknownPredictions = reportRows.Count(r => r.Recognized.First() == HeaterTypesEnum.Unknown);
        var mostCommonMistake = reportRows
            .Where(r => !r.IsMainCorrect && r.Expected != HeaterTypesEnum.Unknown)
            .GroupBy(r => new { Expected = r.Expected, Predicted = r.Recognized.First() })
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        
        // Files that were correct in alternatives but not main prediction
        int savedByAlternatives = reportRows.Count(r => !r.IsMainCorrect && r.IsAltCorrect);

        // Group by expected type for per-category stats
        var categoryStats = reportRows
            .GroupBy(r => r.Expected)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Count(),
                MainCorrect = g.Count(r => r.IsMainCorrect),
                AltCorrect = g.Count(r => r.IsAltCorrect)
            })
            .OrderBy(s => s.Category.ToString());

        // Create separate files for better organization
        string targetDir = Path.Combine(baseDir, testFolderDir, "reports");
        Directory.CreateDirectory(targetDir); // Ensure directory exists
        
        // Create Summary Statistics file
        string summaryPath = Path.Combine(targetDir, "HeaterTypeReport_Summary.tsv");
        var summarySb = new StringBuilder();
        
        // Summary statistics
        summarySb.AppendLine("SUMMARY STATISTICS\t");
        summarySb.AppendLine($"Total Files\t{totalFiles}");
        summarySb.AppendLine($"Main Recognition Correct\t{mainCorrect} ({mainAccuracy:P})");
        summarySb.AppendLine($"Main Recognition Incorrect\t{mainIncorrect}");
        summarySb.AppendLine($"Alternative Recognition Correct\t{altCorrect} ({altAccuracy:P})");
        summarySb.AppendLine($"Alternative Recognition Incorrect\t{altIncorrect}");
        summarySb.AppendLine($"Saved by Alternatives\t{savedByAlternatives}");
        summarySb.AppendLine($"Unknown Predictions\t{unknownPredictions}");
        if (mostCommonMistake != null)
            summarySb.AppendLine($"Most Common Mistake\t{mostCommonMistake.Key.Expected} → {mostCommonMistake.Key.Predicted} ({mostCommonMistake.Count()} times)");
        summarySb.AppendLine($"Generated On\t{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        summarySb.AppendLine("\t");

        // Per-category breakdown
        summarySb.AppendLine("PER-CATEGORY BREAKDOWN\t");
        summarySb.AppendLine("Category\tTotal\tMain Correct\tMain %\tMain Incorrect\tAlt Correct\tAlt %\tAlt Incorrect");
        foreach (var stat in categoryStats)
        {
            double mainPct = stat.Total > 0 ? (double)stat.MainCorrect / stat.Total : 0;
            double altPct = stat.Total > 0 ? (double)stat.AltCorrect / stat.Total : 0;
            int mainIncorrectCat = stat.Total - stat.MainCorrect;
            int altIncorrectCat = stat.Total - stat.AltCorrect;
            summarySb.AppendLine($"{stat.Category}\t{stat.Total}\t{stat.MainCorrect}\t{mainPct:P}\t{mainIncorrectCat}\t{stat.AltCorrect}\t{altPct:P}\t{altIncorrectCat}");
        }

        File.WriteAllText(summaryPath, summarySb.ToString());

        // Create Detailed Results file
        string detailsPath = Path.Combine(targetDir, "HeaterTypeReport_Details.tsv");
        var detailsSb = new StringBuilder();
        
        detailsSb.AppendLine("File\tExpected\tRecognized Main\tRecognized Alternatives\tIsMainCorrect\tIsAltCorrect");
        foreach (var r in reportRows)
            detailsSb.AppendLine($"{r.File}\t{r.Expected}\t{r.Recognized.First()}\t{string.Join('/',r.Recognized.Skip(1))}\t{r.IsMainCorrect}\t{r.IsAltCorrect}");

        File.WriteAllText(detailsPath, detailsSb.ToString());
        
        // Output summary to test console
        _output.WriteLine($"→ Summary report generated at: {summaryPath}");
        _output.WriteLine($"→ Details report generated at: {detailsPath}");
        _output.WriteLine($"→ Summary: {mainCorrect}/{totalFiles} ({mainAccuracy:P}) main recognition accuracy");
        _output.WriteLine($"→ Summary: {altCorrect}/{totalFiles} ({altAccuracy:P}) alternative recognition accuracy");
        
        // Output per-category summary
        _output.WriteLine("→ Per-category accuracy:");
        foreach (var stat in categoryStats)
        {
            double mainPct = stat.Total > 0 ? (double)stat.MainCorrect / stat.Total : 0;
            _output.WriteLine($"   {stat.Category}: {stat.MainCorrect}/{stat.Total} ({mainPct:P})");
        }
    }

    private async Task VerifyHeaterTypeBatch(
        string[] relativePaths,
        HeaterTypesEnum expectedHeaterType)
    {
        var recognitionResults = await Task.WhenAll(
            relativePaths.Select(async path =>
            {
                List<HeaterTypesEnum> recognizedTypes = await GetHeaterTypes(path);
                return new
                {
                    Path = path,
                    RecognizedTypes = recognizedTypes,
                    IsCorrect = recognizedTypes.Contains(expectedHeaterType)
                };
            })
        );

        int total = recognitionResults.Length;
        var incorrectResults = recognitionResults
            .Where(x => !x.IsCorrect)
            .ToList();

        int correctCount = total - incorrectResults.Count;
        int minRequired = (int)Math.Ceiling(total * AcceptableRatio);

        string incorrectFilesSummary = string.Join(
            Environment.NewLine,
            incorrectResults.Select(x => $"{x.Path} (actual: {string.Join(", ", x.RecognizedTypes)})")
        );

        _output.WriteLine(incorrectResults.Count == 0 
            ? "All files recognized correctly."
            : $"Incorrectly recognized files:\n{incorrectFilesSummary}");
        
        _output.WriteLine("Summary of recognition results:");
        foreach (var result in recognitionResults)
        {
            _output.WriteLine($"{result.Path} (actual: {string.Join(", ", result.RecognizedTypes)})");
        }
        Assert.True(
            correctCount >= minRequired,
            $"[{expectedHeaterType}] recognized correctly in " +
            $"{correctCount}/{total} images ({(double)correctCount / total:P}). " +
            $"Expected at least {minRequired} correct detections ({AcceptableRatio:P}).\n" +
            $"Incorrect files:\n{incorrectFilesSummary}"
        );
    }

    private async Task<List<HeaterTypesEnum>> GetHeaterTypes(string relativePath)
    {
        string basePath = GetProjectDirectory();
        string filePath = Path.Combine(basePath, relativePath);
        byte[] image = File.ReadAllBytes(filePath);

        HeaterTypeClassificationResponse result =
            await _heaterTypeRecognition.ExtractHeaterType(image, cancellationToken: CancellationToken.None);

        return [result.HeaterType, ..result.HeaterTypeAlt];
    }

    private static string GetProjectDirectory()
    {
        // Navigate up from the bin/Debug/net8.0 directory to the project root
        string baseDir = AppContext.BaseDirectory;
        DirectoryInfo? dir = new DirectoryInfo(baseDir);
        
        // Go up until we find the project file or reach a reasonable limit
        while (dir != null && dir.Name != "GenAiIncubator.LlmUtils.Tests")
        {
            dir = dir.Parent;
        }
        
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find project directory");
    }
}