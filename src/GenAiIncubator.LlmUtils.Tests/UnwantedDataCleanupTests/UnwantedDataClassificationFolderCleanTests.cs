using GenAiIncubator.LlmUtils.Core.Services.UnwantedDataClassification;
using Xunit.Abstractions;
using GenAiIncubator.LlmUtils.Core.Models;

namespace GenAiIncubator.LlmUtils.Tests.UnwantedDataCleanupTests;

public class UnwantedDataClassificationFolderCleanTests(
    ITestOutputHelper output,
    UnwantedDataClassificationServiceFixture fixture) : IClassFixture<UnwantedDataClassificationServiceFixture>
{
    private readonly ITestOutputHelper _output = output;
    private readonly UnwantedDataClassificationService _service = fixture.Service;

    [Fact]
    public async Task AllFilesInFolder_ShouldContainNoUnwantedData()
    {
        // Replace this placeholder with an absolute path on your machine
        var folderPath = "/Users/alex/Repos/LlmUtils/src/GenAiIncubator.LlmUtils.Tests/static/data_cleanup_test_docs/unit_tests/local_TEMP/wanted2";

        Assert.True(Directory.Exists(folderPath),
            $"Set 'folderPath' to an existing directory on your machine. Current: '{folderPath}'");

        var filePaths = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
            .ToList();

        Assert.NotEmpty(filePaths);

        foreach (var filePath in filePaths)
        {
            var extension = Path.GetExtension(filePath).TrimStart('.');
            var document = await File.ReadAllBytesAsync(filePath);

            UnwantedDataClassificationResponse result;
            try
            {
                result = await _service.CheckForUnwantedData(document, extension, CancellationToken.None);
                string resultString = "NO UNWANTED DATA FOUND";
                if (result.UnwantedData.Any())
                    resultString = string.Join(", ", result.UnwantedData.Select(x => x.UnwantedDataType));
                _output.WriteLine($"Checked file: {filePath} | Result: {resultString}");
            }
            catch 
            {
                _output.WriteLine($"Error checking file: {filePath}");
            }

        }
    }
}


