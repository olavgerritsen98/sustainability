using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GenAiIncubator.LlmUtils.Core.Models;
using Microsoft.SemanticKernel;
using PDFtoImage;
using SkiaSharp;

namespace GenAiIncubator.LlmUtils.Core.Services.DocumentParsing;

/// <summary>
/// Implementation of document parser that handles various document formats.
/// </summary>
public class DocumentParser : IDocumentParser
{
    private readonly List<string> _wordDocTypeExtensions =
    [
        "docx",
        "docm",
        "dotx",
        "dotm",
        "dot",
        "docb"
    ];

    /// <summary>
    /// Parses a document into its constituent parts (text and images).
    /// </summary>
    /// <param name="document">The document bytes.</param>
    /// <param name="documentExtension">The document extension (pdf, docx, png, etc.).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A parsed document containing text and image content.</returns>
    public async Task<ParsedDocument> ParseDocumentAsync(byte[] document, string documentExtension, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentExtension))
        {
            throw new ArgumentException("Document extension must be provided", nameof(documentExtension));
        }

        documentExtension = documentExtension.TrimStart('.').ToLowerInvariant();

        if (documentExtension.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return await ParsePdfDocumentAsync(document, documentExtension, cancellationToken);
        }
        else if (_wordDocTypeExtensions.Contains(documentExtension, StringComparer.OrdinalIgnoreCase))
        {
            return ParseWordDocument(document, documentExtension);
        }
        else if (documentExtension.ToLower().Contains("png", StringComparison.OrdinalIgnoreCase) ||
                 documentExtension.ToLower().Contains("jpg", StringComparison.OrdinalIgnoreCase) ||
                 documentExtension.ToLower().Contains("jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return ParseImageDocument(document, documentExtension);
        }
        else
        {
            throw new NotSupportedException($"Document type '{documentExtension}' is not supported.");
        }
    }

    /// <summary>
    /// Parses a PDF document by converting it to images.
    /// </summary>
    private async Task<ParsedDocument> ParsePdfDocumentAsync(byte[] pdfBytes, string extension, CancellationToken cancellationToken)
    {
        List<ImageContent> images = await ConvertPdfToImagesAsync(pdfBytes, cancellationToken);
        return ParsedDocument.FromImages(images, extension);
    }

    /// <summary>
    /// Parses a Word document by extracting text and embedded images.
    /// </summary>
    private ParsedDocument ParseWordDocument(byte[] docxBytes, string extension)
    {
        List<ImageContent> images = [];

        using var ms = new MemoryStream(docxBytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);

        // Extract embedded images
        List<ImagePart> imageParts = [.. wordDoc.MainDocumentPart?.GetPartsOfType<ImagePart>() ?? []];
        foreach (ImagePart imgPart in imageParts)
        {
            using var imgStream = imgPart.GetStream();
            using var br = new BinaryReader(imgStream);
            var bytes = br.ReadBytes((int)imgStream.Length);
            images.Add(new ImageContent(bytes, imgPart.ContentType));
        }

        var body = wordDoc.MainDocumentPart?.Document?.Body;
        var paragraphs = body?.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>() ?? [];
        string extractedText = string.Join(
            Environment.NewLine,
            paragraphs.Select(p => p.InnerText).Where(t => !string.IsNullOrWhiteSpace(t)));
        
        return ParsedDocument.FromTextAndImages(extractedText, images, extension);
    }

    /// <summary>
    /// Parses an image document by creating ImageContent.
    /// </summary>
    private static ParsedDocument ParseImageDocument(byte[] imageBytes, string extension)
    {
        if (extension.Contains("jpg", StringComparison.OrdinalIgnoreCase))
            extension = "jpeg";
        ImageContent imageContent = new(imageBytes, $"image/{extension}");
        return ParsedDocument.FromSingleImage(imageContent, extension);
    }

    /// <summary>
    /// Converts PDF pages to ImageContent objects using PDFsharp.
    /// Moved from ChatHelpers to centralize document parsing logic.
    /// </summary>
    private static async Task<List<ImageContent>> ConvertPdfToImagesAsync(byte[] pdfBytes, CancellationToken cancellationToken)
    {
        string pdfBase64 = Convert.ToBase64String(pdfBytes);
        var images = new List<ImageContent>();
        
        await foreach (var bmp in Conversion.ToImagesAsync(
            pdfAsBase64String: pdfBase64,
            pages: ..,
            password: null,
            options: default,
            cancellationToken: cancellationToken))
        {
            using var ms = new MemoryStream();
            bmp.Encode(ms, SKEncodedImageFormat.Png, quality: 100);
            ms.Seek(0, SeekOrigin.Begin);
            images.Add(new(ms.ToArray(), "image/png"));
        }

        return images;
    }
}
