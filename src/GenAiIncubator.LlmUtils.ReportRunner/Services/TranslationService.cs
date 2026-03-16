using GenAiIncubator.LlmUtils.Core.Helpers;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.ReportRunner.Services;

public class TranslationService : ITranslationService
{
    private readonly Kernel _kernel;
    private readonly string _targetLanguage;

    public TranslationService(Kernel kernel)
    {
        _kernel = kernel;
        _targetLanguage = Environment.GetEnvironmentVariable("TRANSLATION_TARGET_LANGUAGE") ?? "English";
    }

    public async Task<string> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string prompt = $"Translate the following text to {_targetLanguage}. Return only the translation with no commentary or extra quotes.\n\nText:\n{text}";

        string result = await ChatHelpers.ExecutePromptAsync(
            _kernel,
            prompt,
            outputFormatType: null,
            cancellationToken: cancellationToken);

        return result.Trim();
    }
}


