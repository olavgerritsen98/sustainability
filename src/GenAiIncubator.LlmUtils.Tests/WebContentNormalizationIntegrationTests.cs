using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Services.WebContentNormalization;
using Microsoft.Extensions.DependencyInjection;

namespace GenAiIncubator.LlmUtils.Tests;

/// <summary>
/// Integration tests that make real HTTP calls.
/// These tests are marked as [Fact(Skip = "Integration test")] by default.
/// Remove Skip to run them when needed.
/// </summary>
public class WebContentNormalizationIntegrationTests
{
    private readonly IWebContentNormalizationService _service;

    public WebContentNormalizationIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        var serviceProvider = services.BuildServiceProvider();
        _service = serviceProvider.GetRequiredService<IWebContentNormalizationService>();
    }

    [Fact(Skip = "Integration test - remove Skip to run manually")]
    public async Task FetchAndNormalizeAsync_RealWebsite_Success()
    {
        // Act
        var result = await _service.FetchAndNormalizeAsync("https://httpbin.org/html");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.DoesNotContain("<html>", result);
        Assert.DoesNotContain("<script>", result);
        // Should contain some readable content
        Assert.True(result.Length > 50);
    }

    [Fact(Skip = "Integration test - remove Skip to run manually")]
    public async Task FetchAndNormalizeAsync_SimpleTextPage_Success()
    {
        // Act
        var result = await _service.FetchAndNormalizeAsync("https://httpbin.org/robots.txt");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        // Should return as-is for plain text
        Assert.Contains("User-agent", result);
    }

    [Fact(Skip = "Integration test - remove Skip to run manually")]
    public async Task FetchAndNormalizeAsync_InvalidUrl_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.FetchAndNormalizeAsync("https://thissitedoesnotexist12345.com"));
    }

    // [Fact(Skip = "Integration test - remove Skip to run manually")]
    [Fact]
    public async Task FetchAndNormalizeFlexPrijsPageAsync_RealWebsite_Success()
    {
        // Act
        var result = await _service.FetchAndNormalizeAsync("https://www.vattenfall.nl/grootzakelijk/groen-gas/");

        // Assert
        Assert.True(result.Length > 50);
    }
}
