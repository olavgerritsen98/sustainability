namespace GenAiIncubator.LlmUtils.ReportRunner.Services;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, CancellationToken cancellationToken);
}


