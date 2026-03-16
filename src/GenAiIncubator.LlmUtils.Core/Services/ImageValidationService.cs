using System.Buffers.Text;

namespace GenAiIncubator.LlmUtils.Core.Services;

/// <summary>
/// Interface for image validation services.
/// </summary>
public interface IImageValidationService
{
    /// <summary>
    /// Validates if the provided image bytes represent a valid image file.
    /// </summary>
    /// <param name="imageBytes">The image bytes to validate.</param>
    /// <param name="fileName">The filename including extension for validation.</param>
    /// <param name="contentType">The MIME content type of the image.</param>
    /// <returns>True if the image is valid, false otherwise.</returns>
    bool IsValidImageBytes(byte[] imageBytes, string fileName, string contentType);
    
    /// <summary>
    /// Validates if the provided base64 string represents a valid image.
    /// </summary>
    /// <param name="image">The base64 image string to validate.</param>
    /// <returns>True if the image is valid, false otherwise.</returns>
    bool IsValidImage(string image);
}

/// <summary>
/// Service for validating image files based on content type, file signature, and size.
/// </summary>
public class ImageValidationService : IImageValidationService
{
    private const int MaxImageSize = 6 * 1024 * 1024; // 6 MB

    private static readonly HashSet<string> PermittedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg", 
        "image/png"
    };

    private static readonly HashSet<string> PermittedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private static readonly Dictionary<string, List<byte[]>> FileSignature =
        new()
        {
            {
                ".jpeg", [
                    new byte[]
                    {
                        0xFF, 0xD8, 0xFF, 0xE0
                    },
                    new byte[]
                    {
                        0xFF, 0xD8, 0xFF, 0xE2
                    },
                    new byte[]
                    {
                        0xFF, 0xD8, 0xFF, 0xE3
                    }
                ]
            },
            {
                ".jpg", [
                    new byte[]
                    {
                        0xFF, 0xD8, 0xFF, 0xE0
                    },
                    new byte[]
                    {
                        0xFF, 0xD8, 0xFF, 0xE2
                    },
                    new byte[]
                    {
                        0xFF, 0xD8, 0xFF, 0xE3
                    }
                ]
            },
            {
                ".png", [
                    new byte[]
                    {
                        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
                    }
                ]
            },
        };

    /// <summary>
    /// Validates if the provided image bytes represent a valid image file.
    /// </summary>
    /// <param name="imageBytes">The image bytes to validate.</param>
    /// <param name="fileName">The filename including extension for validation.</param>
    /// <param name="contentType">The MIME content type of the image.</param>
    /// <returns>True if the image is valid, false otherwise.</returns>
    public bool IsValidImageBytes(byte[] imageBytes, string fileName, string contentType)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return false;

        // Check content type and extension
        var imageExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!PermittedContentTypes.Contains(contentType) || !PermittedExtensions.Contains(imageExtension))
        {
            return false;
        }

        // Check size and signature
        using var stream = new MemoryStream(imageBytes);
        return imageBytes.Length <= MaxImageSize && IsValidFileSignature(stream, imageExtension);
    }

    /// <summary>
    /// Validates if the provided base64 string represents a valid image.
    /// </summary>
    /// <param name="image">The base64 image string to validate.</param>
    /// <returns>True if the image is valid, false otherwise.</returns>
    public bool IsValidImage(string image)
    {
        // Strip the 'data:image/<image-type>;base64,' prefix if it exists
        if (image.StartsWith("data:image"))
        {
            image = image.Split(";base64,")[1];
        }

        // Validate if base64 string is valid
        var valid = Base64.IsValid(image);
        if (!valid)
            return false;

        try
        {
            // Validate the size of the image
            var bytes = Convert.FromBase64String(image);
            if (bytes.Length > MaxImageSize)
            {
                return false;
            }

            // Validate the imagetype
            var imageType = image.Substring(0, 5);
            var detectedExtension = imageType.ToUpper() switch
            {
                "IVBOR" => ".png",
                "/9J/4" => ".jpg",
                _ => string.Empty
            };

            using var stream = new MemoryStream(bytes);
            return IsValidFileSignature(stream, detectedExtension);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsValidFileSignature(Stream uploadedFileData, string ext)
    {
        if (string.IsNullOrEmpty(ext))
            return false;

        using var reader = new BinaryReader(uploadedFileData);
        var signatures = FileSignature[ext];
        var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));
        
        return signatures.Any(signature =>
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }
}