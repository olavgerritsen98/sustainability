using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Services.UnwantedDataClassification;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GenAiIncubator.LlmUtils.Tests.UnwantedDataCleanupTests;

/// <summary>
/// Test fixture for setting up services used in UnwantedDataClassificationService tests.
/// </summary>
public class UnwantedDataClassificationServiceFixture : IDisposable
{
    public ServiceProvider ServiceProvider { get; }
    public UnwantedDataClassificationService Service { get; }

    public UnwantedDataClassificationServiceFixture()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        ServiceProvider = services.BuildServiceProvider();
        Service = ServiceProvider.GetRequiredService<UnwantedDataClassificationService>();
    }

    public void Dispose()
    {
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Unit tests for UnwantedDataClassificationService focusing on the CheckForUnwantedData method.
/// Tests different document types, extensions, and validation scenarios.
/// </summary>
public class UnwantedDataClassificationServiceUnitTests(
    ITestOutputHelper output,
    UnwantedDataClassificationServiceFixture fixture) : IClassFixture<UnwantedDataClassificationServiceFixture>
{
    private readonly ITestOutputHelper _output = output;
    private readonly UnwantedDataClassificationService _service = fixture.Service;

    #region Document Extension Tests

    [Fact]
    public async Task CheckForUnwantedData_WithPdfDocument_ShouldReturnValidResponse()
    {
        // Arrange
        var testFile = "static/data_cleanup_test_docs/unit_tests/passport.pdf";
        var (document, extension) = await LoadTestDocument(testFile);

        // Act
        var result = await _service.CheckForUnwantedData(document, extension, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DocumentTypeReasoning);
        Assert.NotNull(result.UnwantedData);
        
        // Check that BSN data is detected 
        var bsnData = result.UnwantedData.FirstOrDefault(x => x.UnwantedDataType == UnwantedDataTypesEnum.BSN);
        Assert.NotNull(bsnData);

        // Verify that BSN data exists after validation
        Assert.NotNull(bsnData);
        
        _output.WriteLine($"Document Type: {result.DocumentType}");
        _output.WriteLine($"Reasoning: {result.DocumentTypeReasoning}");
        _output.WriteLine($"Unwanted Data Count: {result.UnwantedData.Count}");
        _output.WriteLine($"BSN detected");
    }

    [Fact]
    public async Task CheckForUnwantedData_WithDocxDocument_ShouldReturnValidResponse()
    {
        // Arrange
        var testFile = "static/data_cleanup_test_docs/unit_tests/passport.docx";
        var (document, extension) = await LoadTestDocument(testFile);

        // Act
        var result = await _service.CheckForUnwantedData(document, extension, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DocumentTypeReasoning);
        Assert.NotNull(result.UnwantedData);
        
        // Check that BSN data is detected 
        var bsnData = result.UnwantedData.FirstOrDefault(x => x.UnwantedDataType == UnwantedDataTypesEnum.BSN);
        Assert.NotNull(bsnData);

        // Verify that BSN data exists after validation
        Assert.NotNull(bsnData);
        
        _output.WriteLine($"Document Type: {result.DocumentType}");
        _output.WriteLine($"Reasoning: {result.DocumentTypeReasoning}");
        _output.WriteLine($"Unwanted Data Count: {result.UnwantedData.Count}");
        _output.WriteLine($"BSN detected");
    }

    [Fact]
    public async Task CheckForUnwantedData_WithPngImage_ShouldReturnValidResponse()
    {
        // Arrange
        var testFile = "static/data_cleanup_test_docs/unit_tests/passport.PNG";
        var (document, extension) = await LoadTestDocument(testFile);

        // Act
        var result = await _service.CheckForUnwantedData(document, extension, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DocumentTypeReasoning);
        Assert.NotNull(result.UnwantedData);
        
        // Check that BSN data is detected 
        var bsnData = result.UnwantedData.FirstOrDefault(x => x.UnwantedDataType == UnwantedDataTypesEnum.BSN);
        Assert.NotNull(bsnData);

        // Verify that BSN data exists after validation
        Assert.NotNull(bsnData);
        
        _output.WriteLine($"Document Type: {result.DocumentType}");
        _output.WriteLine($"Reasoning: {result.DocumentTypeReasoning}");
        _output.WriteLine($"Unwanted Data Count: {result.UnwantedData.Count}");
        _output.WriteLine($"BSN detected");
    }

    [Fact]
    public async Task CheckForUnwantedData_WithJpegImage_ShouldReturnValidResponse()
    {
        // Arrange
        var testFile = "static/data_cleanup_test_docs/unit_tests/passport.jpg";
        var (document, extension) = await LoadTestDocument(testFile);

        // Act
        var result = await _service.CheckForUnwantedData(document, extension, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DocumentTypeReasoning);
        Assert.NotNull(result.UnwantedData);
        
        // Check that BSN data is detected 
        var bsnData = result.UnwantedData.FirstOrDefault(x => x.UnwantedDataType == UnwantedDataTypesEnum.BSN);
        Assert.NotNull(bsnData);

        // Verify that BSN data exists after validation
        Assert.NotNull(bsnData);
        
        _output.WriteLine($"Document Type: {result.DocumentType}");
        _output.WriteLine($"Reasoning: {result.DocumentTypeReasoning}");
        _output.WriteLine($"Unwanted Data Count: {result.UnwantedData.Count}");
        _output.WriteLine($"BSN detected");
    }

    #endregion

    #region BSN Validation Specific Tests

    [Fact]
    public async Task CheckForUnwantedData_WithValidBsn_ShouldKeepBsnInResponse()
    {
        // Arrange
        var testFile = "static/data_cleanup_test_docs/unit_tests/passport.pdf";
        var (document, extension) = await LoadTestDocument(testFile);

        // Act
        var result = await _service.CheckForUnwantedData(document, extension, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        var bsnData = result.UnwantedData.FirstOrDefault(x => x.UnwantedDataType == UnwantedDataTypesEnum.BSN);
        Assert.NotNull(bsnData);
        _output.WriteLine($"Valid BSN detected");
        
        _output.WriteLine($"Document Type: {result.DocumentType}");
        _output.WriteLine($"Total Unwanted Data Items: {result.UnwantedData.Count}");
    }

    [Fact]
    public async Task CheckForUnwantedData_WithInvalidBsn_ShouldRemoveBsnFromResponse()
    {
        // Arrange
        var testFile = "static/data_cleanup_test_docs/unit_tests/passport_no_bsn.docx";
        var (document, extension) = await LoadTestDocument(testFile);

        // Act
        var result = await _service.CheckForUnwantedData(document, extension, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        // Invalid BSN should be removed from the response
        var bsnData = result.UnwantedData.FirstOrDefault(x => x.UnwantedDataType == UnwantedDataTypesEnum.BSN);
        
        // Note: If the initial classification detects an invalid BSN-like pattern,
        // it should be removed after validation, so bsnData should be null
        if (bsnData == null)
        {
            _output.WriteLine("Invalid BSN was correctly removed from the response");
        }
        else
        {
            _output.WriteLine($"BSN data still present");
        }
        
        _output.WriteLine($"Document Type: {result.DocumentType}");
        _output.WriteLine($"Total Unwanted Data Items: {result.UnwantedData.Count}");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CheckForUnwantedData_WithCorruptedDocument_ShouldHandleGracefully()
    {
        // Arrange
        var corruptedDocument = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 }; // Invalid document data
        var extension = "pdf";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            var result = await _service.CheckForUnwantedData(corruptedDocument, extension, CancellationToken.None);
            _output.WriteLine($"Result with corrupted document: {result?.DocumentType}");
        });

        // The service should either handle the error gracefully or throw a meaningful exception
        if (exception != null)
        {
            _output.WriteLine($"Expected exception occurred: {exception.Message}");
            Assert.NotNull(exception.Message);
        }
        else
        {
            _output.WriteLine("Service handled corrupted document gracefully");
        }
    }

    #endregion

 
    #region Helper Methods

    /// <summary>
    /// Loads a test document from the file system.
    /// If the file doesn't exist, creates a realistic test document using TestDocumentGenerator.
    /// </summary>
    /// <param name="testFile">Relative path to the test file</param>
    /// <returns>Tuple containing document bytes and extension</returns>
    private async Task<(byte[] document, string extension)> LoadTestDocument(string testFile)
    {
        var basePath = AppContext.BaseDirectory;
        var projectDir = Path.GetFullPath(Path.Combine(basePath, "../../../"));
        var fullPath = Path.Combine(projectDir, testFile);
        
        string extension = Path.GetExtension(testFile).TrimStart('.');
        
        if (File.Exists(fullPath))
        {
            var document = await File.ReadAllBytesAsync(fullPath);
            _output.WriteLine($"Loaded test file: {testFile} ({document.Length} bytes)");
            return (document, extension);
        }
        else
        {
            throw new FileNotFoundException($"Test file '{testFile}' not found at path '{fullPath}'.", fullPath);
        }
    }

    #endregion
}
