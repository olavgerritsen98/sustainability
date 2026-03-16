using System.Text.Json;
using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Services.UnwantedDataClassification;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GenAiIncubator.LlmUtils.Tests.UnwantedDataCleanupTests;

public class UnwantedDataClassificationTests
{
    private const double AcceptableRatio = 0.5;
    private const string expectedResultsFileName = "data_cleanup_test_set.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ITestOutputHelper _output;
    private readonly UnwantedDataClassificationService _unwantedDataClassificationService;

    public UnwantedDataClassificationTests(ITestOutputHelper output)
    {
        _output = output;
        var services = new ServiceCollection();
        services.AddLlmUtils();
        var sp = services.BuildServiceProvider();
        _unwantedDataClassificationService = sp.GetRequiredService<UnwantedDataClassificationService>();
    }


    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task TestUnwantedDataClassification()
    {
        using var cts = new CancellationTokenSource();
        UnwantedDataClassificationReport report = await CreateTestReport(cts.Token);

        Assert.NotEmpty(report.Results);

        // Group results by expected document type and verify each group passes the minimum ratio
        var groupedResults = report.Results.GroupBy(r => r.ExpectedDocumentType);

        foreach (var group in groupedResults)
        {
            var documentType = group.Key;
            var results = group.ToList();
            var total = results.Count;
            var correctCount = results.Count(r => r.IsCorrect);
            var minRequired = (int)Math.Ceiling(total * AcceptableRatio);
            var actualRatio = (double)correctCount / total;

            var incorrectResults = results.Where(r => !r.IsCorrect).ToList();
            var incorrectFilesSummary = string.Join(
                Environment.NewLine,
                incorrectResults.Select(r => $"  {Path.GetFileName(r.Path)} (expected: {r.ExpectedDocumentType}, actual: {r.ClassificationResponse.DocumentType}) - Expected unwanted data: [{string.Join(", ", r.ExpectedUnwantedDataTypes)}], Actual unwanted data: [{string.Join(", ", r.ClassificationResponse.UnwantedData.Select(u => u.UnwantedDataType))}]")
            );

            _output.WriteLine($"Document Type: {documentType}");
            _output.WriteLine($"  Correct: {correctCount}/{total} ({actualRatio:P})");
            _output.WriteLine($"  Required: {minRequired}/{total} ({AcceptableRatio:P})");
            
            if (incorrectResults.Count > 0)
            {
                _output.WriteLine($"  Incorrect files:");
                _output.WriteLine(incorrectFilesSummary);
            }

            Assert.True(
                correctCount >= minRequired,
                $"[{documentType}] classification failed. " +
                $"Recognized correctly in {correctCount}/{total} documents ({actualRatio:P}). " +
                $"Expected at least {minRequired} correct classifications ({AcceptableRatio:P}).\n" +
                $"Incorrect files:\n{incorrectFilesSummary}"
            );
        }
    }

    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task CreateUnwantedDataClassificationReport()
    {
        using var cts = new CancellationTokenSource();
        UnwantedDataClassificationReport report = await CreateTestReport(cts.Token);

        var csvContent = report.ToCsv(includeExpectedResults: true);
        var projectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

        var outputCsvPath = Path.Combine(
            projectRoot,
            "static",
            "data_cleanup_test_docs",
            "unwanted_data_classification_report.csv");

        await File.WriteAllTextAsync(outputCsvPath, csvContent);
        _output.WriteLine($"CSV report written to: {outputCsvPath}");
    }

    private async Task<UnwantedDataClassificationReport> CreateTestReport(CancellationToken token)
    {
        Dictionary<string, DocumentTypesEnum> dataCleanupTestFolders = new()
        {
            // { "static/data_cleanup_test_docs/pseudanonymised/Medical", DocumentTypesEnum.JudicialDocument },
            // { "static/data_cleanup_test_docs/pseudanonymised/FHV_Aanmelding", DocumentTypesEnum.DebtOrFinancialAssistance },
            // { "static/data_cleanup_test_docs/pseudanonymised/FHV_Volmacht", DocumentTypesEnum.DebtOrFinancialAssistance },
            // { "static/data_cleanup_test_docs/pseudanonymised/SHV_Aanmelding", DocumentTypesEnum.DebtOrFinancialAssistance },
            // { "static/data_cleanup_test_docs/pseudanonymised/SHV_Saldo_Opgave", DocumentTypesEnum.DebtOrFinancialAssistance },

            // { "static/data_cleanup_test_docs/msd_pseudanonymised/Driverslicence", DocumentTypesEnum.DriversLicense },
            { "static/data_cleanup_test_docs/msd_pseudanonymised/FHV-SHV", DocumentTypesEnum.DebtOrFinancialAssistance },
            { "static/data_cleanup_test_docs/msd_pseudanonymised/ID", DocumentTypesEnum.ID },
            { "static/data_cleanup_test_docs/msd_pseudanonymised/Passport", DocumentTypesEnum.Passport },
        };

        var report = new UnwantedDataClassificationReport { Results = [] };

        bool firstRun = true;
        foreach (var kvp in dataCleanupTestFolders)
        {
            if (!firstRun)
            {
                _output.WriteLine($"Waiting 1 minute before processing next folder: {kvp.Key}");
                await Task.Delay(TimeSpan.FromMinutes(1), token); // Wait 1 minute to avoid rate limiting
            }
            firstRun = false;
            var testResults = await ProcessUnwantedDataTestFolderAsync(kvp.Key, kvp.Value, token);
            report.AddTestResult(testResults);
        }

        _output.WriteLine("Unwanted Data Classification Test Report:");
        _output.WriteLine(report.CreateTestReport());
        return report;
    }

    private async Task<UnwantedDataClassificationTestResult[]> ProcessUnwantedDataTestFolderAsync(string folderPath, DocumentTypesEnum expectedDocumentType, CancellationToken token)
    {
        string[] testFiles = GetTestFiles(folderPath);
        string expectedResultsFile = testFiles.FirstOrDefault(x => Path.GetFileName(x) == expectedResultsFileName) ?? "";
        if (string.IsNullOrEmpty(expectedResultsFile))
            throw new FileNotFoundException($"The expected results file was not found in the folder '{folderPath}'.");

        string expectedResultsJson = await File.ReadAllTextAsync(expectedResultsFile, token);
        var expectedTestResults = JsonSerializer.Deserialize<List<UnwantedDataClassificationExpectedResult>>(expectedResultsJson, JsonOptions) ?? [];

        var testResults = new List<UnwantedDataClassificationTestResult>();
        
        foreach (var file in testFiles)
        {
            // Skip the expected results JSON file and any other JSON files
            if (Path.GetFileName(file) == expectedResultsFileName || Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            UnwantedDataClassificationResponse unwantedData = await GetUnwantedDataAsync(file, token);
            string currFileName = Path.GetFileName(file);
            
            if (!expectedTestResults.Any(_ => _.DocName == currFileName))
            {
                Console.Error.WriteLine($"Warning: No expected results found for file '{currFileName}'.");
                throw new InvalidOperationException($"No expected results found for file '{currFileName}'.");
            }
            
            List<UnwantedDataTypesEnum> expectedDataTypes = [
                ..
                expectedTestResults
                    .First(_ => _.DocName == currFileName)
                    .UnwantedDataList
                    .Select(x => Enum.Parse<UnwantedDataTypesEnum>(x, true))
            ];
            
            bool isCorrect = unwantedData.DocumentType == expectedDocumentType;
            if (expectedDataTypes.Count == 0 && unwantedData.UnwantedData.Count != 0)
                isCorrect = false;
            else if (unwantedData.UnwantedData.Any(_ => !expectedDataTypes.Contains(_.UnwantedDataType)))
                isCorrect = false;
                
            testResults.Add(new UnwantedDataClassificationTestResult
            {
                Path = file,
                ClassificationResponse = unwantedData,
                ExpectedDocumentType = expectedDocumentType,
                ExpectedUnwantedDataTypes = expectedDataTypes,
                IsCorrect = isCorrect
            });
        }

        return [.. testResults];
    }

    private static string[] GetTestFiles(string folderPath)
    {
        var basePath = AppContext.BaseDirectory;
        var projectDir = Path.GetFullPath(Path.Combine(basePath, "../../../"));
        var fullFolderPath = Path.Combine(projectDir, folderPath);

        if (!Directory.Exists(fullFolderPath))
            throw new DirectoryNotFoundException($"The folder '{fullFolderPath}' does not exist.");

        string[] files = [.. Directory.GetFiles(fullFolderPath, "*.*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(projectDir, path))];
        return files;
    }

    // TODO: Generalise this for other tests? 
    private async Task VerifyBatch(
        string[] relativePaths,
        DocumentTypesEnum expectedDocumentType,
        CancellationToken cancellationToken)
    {
        var recognitionResults = await Task.WhenAll(
            relativePaths.Select(async path =>
            {
                UnwantedDataClassificationResponse unwantedData = await GetUnwantedDataAsync(path, cancellationToken);
                return new
                {
                    Path = path,
                    RecognizedType = unwantedData,
                    Reason = unwantedData.DocumentTypeReasoning,
                    unwantedData.UnwantedData,
                    IsCorrect = unwantedData.DocumentType == expectedDocumentType
                };
            })
        );

        // TODO: verify agains unwanted data too

        int total = recognitionResults.Length;
        var incorrectResults = recognitionResults
            .Where(x => !x.IsCorrect)
            .ToList();

        int correctCount = total - incorrectResults.Count;
        int minRequired = (int)Math.Ceiling(total * AcceptableRatio);

        string incorrectFilesSummary = string.Join(
            Environment.NewLine, 
            incorrectResults.Select(x => $"{x.Path} (actual: {x.RecognizedType})")
        );

        Assert.True(true
            // correctCount >= minRequired,
            // $"[{expectedHeaterType}] recognized correctly in " +
            // $"{correctCount}/{total} images ({(double)correctCount / total:P}). " +
            // $"Expected at least {minRequired} correct detections ({AcceptableRatio:P}).\n" +
            // $"Incorrect files:\n{incorrectFilesSummary}"
        );
    }

    private async Task<UnwantedDataClassificationResponse> GetUnwantedDataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string fileExtension = Path.GetExtension(filePath).TrimStart('.');
        try
        {
            byte[] document = File.ReadAllBytes(filePath);
            try
            {
                Console.WriteLine($"Checking for unwanted data in file {filePath}...");
                return await _unwantedDataClassificationService.CheckForUnwantedData(document, fileExtension, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                // throw new InvalidOperationException($"Error executing unwanted data classification for file {filePath}: {ex.Message}", ex);
                Console.WriteLine($"Error executing unwanted data classification for file {filePath}: {ex.Message}");
                return UnwantedDataClassificationResponse.GetErrorClassificationResponse();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
            throw;
        } 
    }
}
