using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using GenAiIncubator.LlmUtils.Core.Options;
using OpenAI.Audio;

namespace GenAiIncubator.LlmUtils.Core.Services;

/// <summary>
/// Provides transcription services using the Whisper model from Azure OpenAI.
/// </summary>
/// <remarks>
/// This service utilizes the AzureOpenAIClient to transcribe audio files into text using the Whisper model.
/// </remarks>
public class WhisperTranscriptionService
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly AudioClient _audioClient;
    private readonly string deploymentName = "whisper";

    /// <summary>
    /// Initializes a new instance of the <see cref="WhisperTranscriptionService"/> class.
    /// </summary>
    /// <param name="kernelOptions">Options holding endpoint and api key of the azure openai instance.</param>
    public WhisperTranscriptionService(KernelOptions kernelOptions)
    {
        var credential = new AzureKeyCredential(kernelOptions.AzureOpenAIApiKey);
        _openAiClient = new AzureOpenAIClient(new Uri(kernelOptions.Endpoint), credential);
        _audioClient = _openAiClient.GetAudioClient(deploymentName);
    }

    /// <summary>
    /// Transcribes the audio file at the specified path into text using the Whisper model.
    /// </summary>
    /// <param name="audioFilePath">The path to the audio file to be transcribed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the transcribed text.</returns>
    public async Task<string> TranscribeAudioAsync(string audioFilePath)
    {
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Audio file not found.", audioFilePath);
        ClientResult<AudioTranscription> result = await _audioClient.TranscribeAudioAsync(audioFilePath);
        Console.WriteLine("Transcribed text:");
        return result.Value.Text;
    }
}