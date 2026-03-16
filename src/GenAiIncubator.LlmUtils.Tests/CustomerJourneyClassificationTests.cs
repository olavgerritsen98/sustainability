using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GenAiIncubator.LlmUtils.Tests;

public class CustomerJourneyClassificationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CustomerJourneyClassificationService _customerJourneyClassification;

    public CustomerJourneyClassificationTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        _customerJourneyClassification = _serviceProvider.GetRequiredService<CustomerJourneyClassificationService>();
    }

    [Fact]
    public async Task ClassifyComplaintConversation_ShouldReturnMainAndSubCategory()
    {
        string conversation = TestContent.ComplaintConversation;
        CustomerJourneyClassification? result = await _customerJourneyClassification.ClassifyConversationInCustomerJourneyAsync(
            conversation, 
            useSummary: true, 
            twoStepClassification: false, 
            cancellationToken: CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Classification)); 
        Assert.False(string.IsNullOrWhiteSpace(result.Subcategory)); 
    }

    [Fact]
    public async Task ClassifyPositiveFeedbackConversation_ShouldReturnMainAndSubCategory()
    {
        string conversation = TestContent.PositiveFeedbackConversation;
        var result = await _customerJourneyClassification.ClassifyConversationInCustomerJourneyAsync(
            conversation, 
            useSummary: true, 
            twoStepClassification: false, 
            cancellationToken: CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Classification)); 
        Assert.False(string.IsNullOrWhiteSpace(result.Subcategory)); 
    }

    [Fact]
    public async Task ClassifyIssueResolutionConversation_ShouldReturnMainAndSubCategory()
    {
        string conversation = TestContent.IssueResolutionConversation;
        var result = await _customerJourneyClassification.ClassifyConversationInCustomerJourneyAsync(
            conversation, 
            useSummary: true, 
            twoStepClassification: true, 
            cancellationToken: CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Classification)); 
        Assert.False(string.IsNullOrWhiteSpace(result.Subcategory)); 
    }

    [Fact]
    public async Task ClassifyEmptyConversation_ShouldReturnEmptyMainAndSubCategory()
    {
        var result = await _customerJourneyClassification.ClassifyConversationInCustomerJourneyAsync(
            string.Empty, 
            useSummary: true, 
            twoStepClassification: false, 
            cancellationToken: CancellationToken.None);

        // TODO: assert unclasifyiable
    }
}
