using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents a document that has been parsed into its constituent parts for AI processing.
/// </summary>
public class ParsedDocument
{
    /// <summary>
    /// The extracted text content from the document.
    /// </summary>
    public string TextContent { get; set; } = string.Empty;

    /// <summary>
    /// The extracted images from the document (including PDF pages converted to images).
    /// </summary>
    public List<ImageContent> Images { get; set; } = new();

    /// <summary>
    /// The original document extension for reference.
    /// </summary>
    public string DocumentExtension { get; set; } = string.Empty;

    /// <summary>
    /// Creates a ParsedDocument with only images (for image files).
    /// </summary>
    public static ParsedDocument FromImages(List<ImageContent> images, string extension)
    {
        return new ParsedDocument
        {
            Images = images,
            DocumentExtension = extension
        };
    }

    /// <summary>
    /// Creates a ParsedDocument with text and images (for Word documents).
    /// </summary>
    public static ParsedDocument FromTextAndImages(string textContent, List<ImageContent> images, string extension)
    {
        return new ParsedDocument
        {
            TextContent = textContent,
            Images = images,
            DocumentExtension = extension
        };
    }

    /// <summary>
    /// Creates a ParsedDocument with only a single image (for simple image files).
    /// </summary>
    public static ParsedDocument FromSingleImage(ImageContent image, string extension)
    {
        return new ParsedDocument
        {
            Images = [image],
            DocumentExtension = extension
        };
    }
}
