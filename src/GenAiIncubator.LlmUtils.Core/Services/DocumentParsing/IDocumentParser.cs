using GenAiIncubator.LlmUtils.Core.Models;

namespace GenAiIncubator.LlmUtils.Core.Services.DocumentParsing;

/// <summary>
/// Interface for parsing documents into a standardized format for AI processing.
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Parses a document into its constituent parts (text and images).
    /// </summary>
    /// <param name="document">The document bytes.</param>
    /// <param name="documentExtension">The document extension (pdf, docx, png, etc.).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A parsed document containing text and image content.</returns>
    Task<ParsedDocument> ParseDocumentAsync(byte[] document, string documentExtension, CancellationToken cancellationToken = default);
}
