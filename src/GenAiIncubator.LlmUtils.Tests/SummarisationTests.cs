using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GenAiIncubator.LlmUtils.Tests;

// TODO: add tests on semantics and number of sentences and stuff
public class ConversationSummarisationServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConversationSummarisationService _conversationSummarisation;

    public ConversationSummarisationServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        _conversationSummarisation = _serviceProvider.GetRequiredService<ConversationSummarisationService>();
    }

    [Fact]
    public async Task SummariseConversationComplaint_ShouldReturnSummary()
    {
        string conversation = TestContent.ComplaintConversation;
        string result = await _conversationSummarisation.SummariseConversationAsync(conversation, cancellationToken: CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public async Task SummariseConversationPositiveFeedback_ShouldReturnSummary()
    {
        string conversation = TestContent.PositiveFeedbackConversation;
        string result = await _conversationSummarisation.SummariseConversationAsync(conversation, cancellationToken: CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public async Task SummariseConversationIssueResolution_ShouldReturnSummary()
    {
        string conversation = TestContent.IssueResolutionConversation;
        string result = await _conversationSummarisation.SummariseConversationAsync(conversation, cancellationToken: CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public async Task SummariseEmptyConversation_ShouldReturnEmptySummary()
    {
        string result = await _conversationSummarisation.SummariseConversationAsync("", cancellationToken: CancellationToken.None);
    }
}