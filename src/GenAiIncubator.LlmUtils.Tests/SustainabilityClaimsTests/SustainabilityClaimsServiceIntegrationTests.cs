using Microsoft.Extensions.DependencyInjection;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

public class SustainabilityClaimsServiceIntegrationTests
{
    private readonly ISustainabilityClaimsService _service;

    public SustainabilityClaimsServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        var serviceProvider = services.BuildServiceProvider();
    _service = serviceProvider.GetRequiredService<ISustainabilityClaimsService>();
    }

    // [Fact(Skip = "URL might change/become unavailable, so this is skipped by default")]
    [Fact]
    public async Task CheckSustainabilityClaimsAsync_VattenfallAmsterdam_BasicAssertions()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));
        var url = "https://www.vattenfall.nl//stroom/stroometiket/";

        var evaluations = await _service.CheckSustainabilityClaimsAsync(url, cts.Token);

        Assert.NotNull(evaluations);
        Assert.NotEmpty(evaluations);
        SustainabilityClaimComplianceEvaluation evaluation = evaluations.First();
        Assert.NotNull(evaluation.Claim);
    }
}
