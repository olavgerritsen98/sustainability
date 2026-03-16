
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GenAiIncubator.LlmUtils.Tests;

public class WhisperTranscriptionServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WhisperTranscriptionService _whisperTranscriptionService;

    public WhisperTranscriptionServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        _whisperTranscriptionService = _serviceProvider.GetRequiredService<WhisperTranscriptionService>();
    }

    private string GetFullFilePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "static/conversations", fileName);
        // Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);

    [Fact]
    public async Task TranscribeWhisper_ShouldReturnTranscription()
    {
        string filepath = GetFullFilePath("customer-conversation.wav");
        string result = await _whisperTranscriptionService.TranscribeAudioAsync(filepath);
        Assert.False(string.IsNullOrWhiteSpace(result));
    }
}