using GenAiIncubator.LlmUtils.Core.Enums;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenAiIncubator.LlmUtils.Tests;

public class ImageValidationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HeaterRecognitionService _heaterRecognitionService;
    private readonly IImageValidationService _imageValidationService;

    public ImageValidationTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        _heaterRecognitionService = _serviceProvider.GetRequiredService<HeaterRecognitionService>();
        _imageValidationService = _serviceProvider.GetRequiredService<IImageValidationService>();
    }

    [Fact]
    public void ValidateImageBytes_ValidJpeg_ReturnsTrue()
    {
        // Arrange: Create minimal valid JPEG header
        var validJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        
        // Act
        var isValid = _imageValidationService.IsValidImageBytes(validJpegBytes, "test.jpg", "image/jpeg");
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateImageBytes_ValidPng_ReturnsTrue()
    {
        // Arrange: Create minimal valid PNG header
        var validPngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        
        // Act
        var isValid = _imageValidationService.IsValidImageBytes(validPngBytes, "test.png", "image/png");
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateImageBytes_InvalidSignature_ReturnsFalse()
    {
        // Arrange: Create invalid file signature
        var invalidBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        
        // Act
        var isValid = _imageValidationService.IsValidImageBytes(invalidBytes, "test.jpg", "image/jpeg");
        
        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateImageBytes_WrongContentType_ReturnsFalse()
    {
        // Arrange: Valid JPEG signature but wrong content type
        var validJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        
        // Act
        var isValid = _imageValidationService.IsValidImageBytes(validJpegBytes, "test.jpg", "text/plain");
        
        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateImageBytes_WrongExtension_ReturnsFalse()
    {
        // Arrange: Valid JPEG signature but wrong extension
        var validJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        
        // Act
        var isValid = _imageValidationService.IsValidImageBytes(validJpegBytes, "test.txt", "image/jpeg");
        
        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ExtractHeaterType_InvalidImage_ReturnsUnknownWithValidationError()
    {
        // Arrange: Create invalid image data
        var invalidImageBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        
        // Act
        var result = await _heaterRecognitionService.ExtractHeaterType(invalidImageBytes, "jpg");
        
        // Assert
        Assert.Equal(HeaterTypesEnum.Unknown, result.HeaterType);
        Assert.Contains("Invalid image file", result.Reason);
        Assert.Contains("validation criteria", result.Reason);
    }

    [Fact]
    public void ValidateImageBytes_EmptyArray_ReturnsFalse()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();
        
        // Act
        var isValid = _imageValidationService.IsValidImageBytes(emptyBytes, "test.jpg", "image/jpeg");
        
        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateImageBytes_NullArray_ReturnsFalse()
    {
        // Act
        var isValid = _imageValidationService.IsValidImageBytes(null!, "test.jpg", "image/jpeg");
        
        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidImage_ValidBase64Jpeg_ReturnsTrue()
    {
        // Arrange: Create a valid JPEG in base64
        var validJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var base64String = Convert.ToBase64String(validJpegBytes);
        
        // Act
        var isValid = _imageValidationService.IsValidImage(base64String);
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidImage_InvalidBase64_ReturnsFalse()
    {
        // Arrange: Invalid base64 string
        var invalidBase64 = "this is not base64!";
        
        // Act
        var isValid = _imageValidationService.IsValidImage(invalidBase64);
        
        // Assert
        Assert.False(isValid);
    }
}