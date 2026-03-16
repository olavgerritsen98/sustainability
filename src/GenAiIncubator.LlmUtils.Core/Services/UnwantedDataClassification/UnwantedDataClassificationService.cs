using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Services.DocumentParsing;

namespace GenAiIncubator.LlmUtils.Core.Services.UnwantedDataClassification;

/// <summary>
/// Provides a service for recognizing unwanted data in documents with additional validation logic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnwantedDataClassificationService"/> class.
/// </remarks>
/// <param name="semanticService">The semantic service for AI operations.</param>
/// <param name="documentParser">The document parser for handling various file formats.</param>
public class UnwantedDataClassificationService(
    UnwantedDataClassificationSemanticService semanticService,
    IDocumentParser documentParser)
{
    private readonly UnwantedDataClassificationSemanticService _semanticService = semanticService;
    private readonly IDocumentParser _documentParser = documentParser;

    /// <summary>
    /// Checks if a document contains unwanted data, such as PII or sensitive information.
    /// Performs additional validation for BSN data when detected.
    /// </summary>
    /// <param name="document">The document to check.</param>
    /// <param name="documentExtension">The extension of the document to check (png, jpg, pdf...).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The classification result.</returns>
    public async Task<UnwantedDataClassificationResponse> CheckForUnwantedData(
        byte[] document,
        string documentExtension,
        CancellationToken cancellationToken)
    {
        ParsedDocument parsedDocument;
        try
        {
            parsedDocument = await _documentParser.ParseDocumentAsync(document, documentExtension, cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            return new UnwantedDataClassificationResponse
            {
                DocumentType = DocumentTypesEnum.Unknown,
                DocumentTypeReasoning = $"Document type '{documentExtension}' is not supported for processing. {ex.Message}",
                UnwantedData = []
            };
        }

        UnwantedDataClassificationResponse response;
        try
        {
            response = await _semanticService.ClassifyUnwantedDataAsync(parsedDocument, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Error executing unwanted data classification prompt: {ex.Message}");
            throw;
        }

        bool isPassport = response.DocumentType == DocumentTypesEnum.Passport;
        response = await ValidateBsnDataAsync(response, parsedDocument, isPassport, cancellationToken);

        return response;
    }

    /// <summary>
    /// Validates BSN data in the document and updates the response accordingly.
    /// </summary>
    /// <param name="response">The initial classification response.</param>
    /// <param name="parsedDocument">The parsed document content.</param>
    /// <param name="isPassport">True if the document is a passport.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated classification response.</returns>
    private async Task<UnwantedDataClassificationResponse> ValidateBsnDataAsync(
        UnwantedDataClassificationResponse response,
        ParsedDocument parsedDocument,
        bool isPassport,
        CancellationToken cancellationToken)
    {
        try
        {
            string extractedBsn = await _semanticService.ExtractBsnFromDocumentAsync(parsedDocument, isPassport, cancellationToken);
            RecognizedUnwantedDataType? bsnData = response.UnwantedData.FirstOrDefault(x => x.UnwantedDataType == UnwantedDataTypesEnum.BSN);
            if (bsnData != null)
            {
                if (!string.IsNullOrEmpty(extractedBsn))
                    bsnData.Reason = "Valid BSN detected.";
                else
                    response.UnwantedData.Remove(bsnData);
            }
            else if (!string.IsNullOrEmpty(extractedBsn))
            {
                response.UnwantedData.Add(new RecognizedUnwantedDataType
                {
                    UnwantedDataType = UnwantedDataTypesEnum.BSN,
                    Reason = "Valid BSN detected."
                });
            }
        }
        catch (Exception ex)
        {
            var bsnData = response.UnwantedData.FirstOrDefault(x => x.UnwantedDataType == UnwantedDataTypesEnum.BSN);
            if (bsnData != null)
            {
                bsnData.Reason = $"BSN validation failed due to error: {ex.Message}. Original reason: {bsnData.Reason}";
            }
        }
        return response;
    }

}