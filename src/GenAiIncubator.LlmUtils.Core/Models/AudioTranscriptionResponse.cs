namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents the response of an audio transcription.
/// </summary>
public class AudioTranscriptionResponse 
{
    /// <summary>
    /// The transcription of the audio.
    /// </summary>
    public required string Transcription { get; set; } = string.Empty;

    /// <summary>
    /// The length of the transcribed audio.
    /// </summary>
    public string AudioLength { get; set; } = string.Empty;
}