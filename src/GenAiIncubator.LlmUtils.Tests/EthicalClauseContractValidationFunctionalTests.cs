using System.Text.Json;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Services.EthicalClauseContractValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace GenAiIncubator.LlmUtils.Tests;

/// <summary>
/// Functional tests that validate ethical clause detection end-to-end:
/// file parsing (PDF/DOCX) -> text redaction (Azure AI Language) -> LLM semantic validation.
/// </summary>
public class EthicalClauseContractValidationFunctionalTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly EthicalClauseContractValidationService _validator;

    public EthicalClauseContractValidationFunctionalTests()
    {
        ServiceCollection services = new();
        services.AddLlmUtils();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        _validator = serviceProvider.GetRequiredService<EthicalClauseContractValidationService>();
    }

    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task ContractsFromFolder_ShouldMatchExpectedResults()
    {
        (string contractsDir, string expectationsPath) = GetTestDataPaths();
        if (!Directory.Exists(contractsDir))
            throw new ArgumentException($"Contracts folder not found: '{contractsDir}'. Add test data to run this functional test.");

        if (!File.Exists(expectationsPath))
            throw new ArgumentException($"Expected results file not found: '{expectationsPath}'. Add expected_results.json to run this functional test.");

        TestSuite suite = LoadSuite(expectationsPath);
        if (suite.Cases.Count == 0)
            throw new ArgumentException("expected_results.json contains no cases yet.");

        foreach (var testCase in suite.Cases)
        {
            string contractPath = Path.Combine(contractsDir, testCase.FileName);
            Assert.True(File.Exists(contractPath), $"Missing contract file: '{contractPath}'.");

            byte[] bytes = await File.ReadAllBytesAsync(contractPath);
            string extension = Path.GetExtension(contractPath).TrimStart('.');
            EthicalClauseContractValidationResponse result =
                await _validator.ValidateEthicalClauseContractAsync(bytes, extension, CancellationToken.None);

            Assert.Equal(
                testCase.ExpectedHasEthicalClause,
                result.HasEthicalClause);
        }
    }

    private static (string ContractsDir, string ExpectationsPath) GetTestDataPaths()
    {
        string baseDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "static", "ethical_clause_contract_validation"));

        string contractsDir = Path.Combine(baseDir, "contracts");
        string expectationsPath = Path.Combine(baseDir, "expected_results.json");
        return (contractsDir, expectationsPath);
    }

    private static TestSuite LoadSuite(string expectationsPath)
    {
        string json = File.ReadAllText(expectationsPath);
        var suite = JsonSerializer.Deserialize<TestSuite>(json, JsonOptions);
        return suite ?? new TestSuite([]);
    }

    private sealed record TestSuite(IReadOnlyList<TestCase> Cases);

    private sealed record TestCase(string FileName, bool ExpectedHasEthicalClause);
}


