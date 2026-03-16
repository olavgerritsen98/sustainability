using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace GenAiIncubator.LlmUtils_Functions.Logging;

internal static class DurableReplaySafeLoggerExtensions
{
    public static void LogInformationReplaySafe(
        this ILogger logger,
        TaskOrchestrationContext orchestrationContext,
        string message,
        params object?[] args)
    {
        if (!orchestrationContext.IsReplaying)
            logger.LogInformation(message, args);
    }

    public static void LogWarningReplaySafe(
        this ILogger logger,
        TaskOrchestrationContext orchestrationContext,
        string message,
        params object?[] args)
    {
        if (!orchestrationContext.IsReplaying)
            logger.LogWarning(message, args);
    }

    public static void LogErrorReplaySafe(
        this ILogger logger,
        TaskOrchestrationContext orchestrationContext,
        Exception exception,
        string message,
        params object?[] args)
    {
        if (!orchestrationContext.IsReplaying)
            logger.LogError(exception, message, args);
    }
}
