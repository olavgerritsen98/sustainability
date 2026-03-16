using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GenAiIncubator.LlmUtils.Tests;

public class PIIRemovalServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PIIRemovalService _piiRemoval;

    public PIIRemovalServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        _piiRemoval = _serviceProvider.GetRequiredService<PIIRemovalService>();
    }

    [Fact]
    public async Task AnonymiseContent_ShouldReturnAnonymisedContent()
    {
        string result = await _piiRemoval.AnonymiseContentAsync("John Doe lives in New York, his bank account is 123032984.", CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.DoesNotContain("John Doe", result);
        Assert.DoesNotContain("123032984", result);
    }

    [Fact]
    public async Task AnonymiseContent_ShouldReturnAnonymisedContent1()
    {
        string result = await _piiRemoval.AnonymiseContentAsync("This is a recording. I am Alexander Crocinates. I live in Amsterdam. I live in the center near the lake. There is a forest in here and my bank account number is 123-6872. And I like cuts and. My pin number is 1234 and I speak for a long time. I am very unhappy because of this conversation is taking a long time. Although I am frustrated and annoyed and the lemons password is 3142 and say everything. In that please.", CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.DoesNotContain("John Doe", result);
        Assert.DoesNotContain("123032984", result);
    }

    [Fact]
    public async Task AnonymiseContentTwice_ShouldReturnAnonymisedContent()
    {
        string result = await _piiRemoval.AnonymiseContentAsync("John Doe lives in New York, his bank account is 123032984, and he really likes cats.", CancellationToken.None);
        result = await _piiRemoval.AnonymiseContentAsync("John Doe lives in New York, his bank account is 123032984, and he really likes cats.", CancellationToken.None);
        Assert.DoesNotContain("John Doe", result);
        Assert.DoesNotContain("123032984", result);
        Assert.DoesNotContain("New York", result);
    }

    [Fact]
    public async Task AnonymiseContentEmpty_ShouldReturnEmptyContent()
    {
        string result = await _piiRemoval.AnonymiseContentAsync("", CancellationToken.None);
        Assert.True(string.IsNullOrEmpty(result.Replace("*", string.Empty))); // in case "***" is returned
    }

    [Fact]
    public async Task AnonymiseContentSpecialChars_ShouldReturnStringContent()
    {
        string result = await _piiRemoval.AnonymiseContentAsync("You're also a customer.", CancellationToken.None);
        Assert.DoesNotContain("&#39;", result);
    }
}
