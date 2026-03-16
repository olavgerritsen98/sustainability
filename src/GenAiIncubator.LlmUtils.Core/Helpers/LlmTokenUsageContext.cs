namespace GenAiIncubator.LlmUtils.Core.Helpers;

/// <summary>
/// Ambient, per-request token usage aggregator for LLM calls.
/// </summary>
public sealed class LlmTokenUsageContext
{
    private static readonly AsyncLocal<LlmTokenUsageContext?> CurrentContext = new();

    private long _inputTokens;
    private long _outputTokens;
    private int _callCount;
    private int _usageObservedCount;

    /// <summary>
    /// Gets the current usage context for the async flow, if any.
    /// </summary>
    public static LlmTokenUsageContext? Current => CurrentContext.Value;

    /// <summary>
    /// Gets the total input token count recorded in this scope.
    /// </summary>
    public int InputTokens => (int)_inputTokens;

    /// <summary>
    /// Gets the total output token count recorded in this scope.
    /// </summary>
    public int OutputTokens => (int)_outputTokens;

    /// <summary>
    /// Gets the number of successful LLM calls recorded in this scope.
    /// </summary>
    public int CallCount => _callCount;

    /// <summary>
    /// Gets the number of calls where token usage metadata was observed.
    /// </summary>
    public int UsageObservedCount => _usageObservedCount;

    /// <summary>
    /// Begins a new usage scope for the current async flow.
    /// </summary>
    public static LlmTokenUsageScope BeginScope()
    {
        LlmTokenUsageContext? previous = CurrentContext.Value;
        var context = new LlmTokenUsageContext();
        CurrentContext.Value = context;
        return new LlmTokenUsageScope(previous, context);
    }

    internal static void SetCurrent(LlmTokenUsageContext? context)
        => CurrentContext.Value = context;

    /// <summary>
    /// Records token usage for a successful call.
    /// </summary>
    /// <param name="inputTokens">Input token count, if available.</param>
    /// <param name="outputTokens">Output token count, if available.</param>
    public void RecordUsage(int? inputTokens, int? outputTokens)
    {
        Interlocked.Increment(ref _callCount);

        bool usageObserved = inputTokens.HasValue || outputTokens.HasValue;
        if (usageObserved)
            Interlocked.Increment(ref _usageObservedCount);

        if (inputTokens.HasValue)
            Interlocked.Add(ref _inputTokens, inputTokens.Value);

        if (outputTokens.HasValue)
            Interlocked.Add(ref _outputTokens, outputTokens.Value);
    }
}

/// <summary>
/// Disposes the current usage scope and restores the previous one.
/// </summary>
public readonly struct LlmTokenUsageScope : IDisposable
{
    private readonly LlmTokenUsageContext? _previous;

    /// <summary>
    /// Gets the context associated with this scope.
    /// </summary>
    public LlmTokenUsageContext Context { get; }

    internal LlmTokenUsageScope(LlmTokenUsageContext? previous, LlmTokenUsageContext context)
    {
        _previous = previous;
        Context = context;
    }

    /// <summary>
    /// Restores the previous usage context.
    /// </summary>
    public void Dispose()
        => LlmTokenUsageContext.SetCurrent(_previous);
}
