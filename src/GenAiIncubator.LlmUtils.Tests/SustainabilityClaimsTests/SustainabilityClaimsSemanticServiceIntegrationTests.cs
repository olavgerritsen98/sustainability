using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

/// <summary>
/// Integration tests for SustainabilityClaimsSemanticService with real-world content examples.
/// These tests validate the full end-to-end functionality with actual AI processing.
/// </summary>
public class SustainabilityClaimsSemanticServiceIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISustainabilityClaimsSemanticService _sustainabilityClaimsSemantiService;

    public SustainabilityClaimsSemanticServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        _sustainabilityClaimsSemantiService = _serviceProvider.GetRequiredService<ISustainabilityClaimsSemanticService>();
    }

    /*   Claims extraction tests   */
    [Fact]
    public async Task ExtractSustainabilityClaimsAsync_GroenGas_Narrative_ShouldReturnOneComparisonNotCompliant()
    {
        // Arrange
        var content = @"Groen gas afnemen of produceren? We offer 100% green gas.";

        // Act
        var generic = await _sustainabilityClaimsSemantiService.ExtractAllGenericSustainabilityClaimsAsync(content);
        var claims = new List<SustainabilityClaim>();
        foreach (var gc in generic)
        {
            var filtered = await _sustainabilityClaimsSemantiService.FilterVattenfallSustainabilityClaimAsync(gc, content);
            if (filtered is not null)
            {
                claims.Add(filtered);
            }
        }

        // Assert
        Assert.NotNull(claims);
        Assert.Single(claims);
        var claim = claims.Single();
        Assert.Equal(SustainabilityClaimType.Regular, claim.ClaimType);

        var evaluation = await _sustainabilityClaimsSemantiService.EvaluateSustainabilityClaimComplianceAsync(claim);
        Assert.False(evaluation.IsCompliant);
    }

    

    
    /*   Claims extra fields tests   */
    [Fact]
    public async Task ExtractSustainabilityClaimsAsync_ShouldPopulateRelatedTextContext()
    {
        // Arrange - Claim with surrounding context that should be surfaced as RelatedText
        var content = @"
            Introducing EcoSmart+ packaging.
            Our EcoSmart+ line uses 60% fewer materials than traditional designs while maintaining performance.
            Each product is manufactured using our patented low-emission process, reducing environmental impact by 45%.
        ";

        // Act
        var generic = await _sustainabilityClaimsSemantiService.ExtractAllGenericSustainabilityClaimsAsync(content);
        var result = new List<SustainabilityClaim>();
        foreach (var gc in generic)
        {
            var filtered = await _sustainabilityClaimsSemantiService.FilterVattenfallSustainabilityClaimAsync(gc, content);
            if (filtered is not null)
            {
                result.Add(filtered);
            }
        }

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Any(c => !string.IsNullOrWhiteSpace(c.RelatedText)),
            "Expected at least one claim to include non-empty RelatedText context.");
    }

    [Fact]
    public async Task ExtractSustainabilityClaimsAsync_ContentWithAbsoluteUrl_ShouldIncludeRelatedUrl()
    {
        // Arrange - Content contains a clear claim and an absolute URL right next to it
        var url = "https://evidence.example.com/lca-report";
        var content = $@"
            Our EcoSmart line cuts lifecycle CO2 emissions by 45% compared to prior models.
            See independent methodology here ({url}).
        ";

        // Act
        var generic = await _sustainabilityClaimsSemantiService.ExtractAllGenericSustainabilityClaimsAsync(content);
        var result = new List<SustainabilityClaim>();
        foreach (var gc in generic)
        {
            var filtered = await _sustainabilityClaimsSemantiService.FilterVattenfallSustainabilityClaimAsync(gc, content);
            if (filtered is not null)
            {
                result.Add(filtered);
            }
        }

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, c => (c.RelatedUrls?.Contains(url) ?? false));
    }

    [Fact]
    public async Task ExtractSustainabilityClaimsAsync_ContentWithMultipleUrls_ShouldIncludeUpToThreeRelatedUrls()
    {
        // Arrange - Claim with multiple nearby absolute URLs
        var url1 = "https://example.com/methodology";
        var url2 = "https://example.com/dataset";
        var url3 = "https://example.com/certification";
        var content = $@"
            Our packaging is 100% recyclable and verified by third-party audits.
            Learn more in the methodology ({url1}), explore raw data ({url2}),
            and view certification details ({url3}).
        ";

        // Act
        var generic = await _sustainabilityClaimsSemantiService.ExtractAllGenericSustainabilityClaimsAsync(content);
        var result = new List<SustainabilityClaim>();
        foreach (var gc in generic)
        {
            var filtered = await _sustainabilityClaimsSemantiService.FilterVattenfallSustainabilityClaimAsync(gc, content);
            if (filtered is not null)
            {
                result.Add(filtered);
            }
        }

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var withUrls = result.FirstOrDefault(c => (c.RelatedUrls?.Count ?? 0) > 0);
        Assert.NotNull(withUrls);
        var related = withUrls!.RelatedUrls;
        Assert.True(related.Count >= 1 && related.Count <= 3);
        Assert.Contains(url1, related);
        Assert.Contains(url2, related);
        Assert.Contains(url3, related);
    }

    [Fact]
    public async Task ExtractSustainabilityClaimsAsync_ContentWithUrl_ShouldIncludeRelatedUrl()
    {
        // Arrange - Claim with multiple nearby absolute URLs
        var url = "https://www.vattenfall.nl/stadsverwarming/warmte-etiket/";
        var content = $@"
        De koudenetten product op Vattenfall van Amsterdam haalden in 2024 85% van hun koude uit oppervlaktewater. Dat is een eindeloos hernieuwbare bron.
        Bekijk het warmte-etiket (https://www.vattenfall.nl/stadsverwarming/warmte-etiket/)
        ";

        // Act
        var generic = await _sustainabilityClaimsSemantiService.ExtractAllGenericSustainabilityClaimsAsync(content);
        var result = new List<SustainabilityClaim>();
        foreach (var gc in generic)
        {
            var filtered = await _sustainabilityClaimsSemantiService.FilterVattenfallSustainabilityClaimAsync(gc, content);
            if (filtered is not null)
            {
                result.Add(filtered);
            }
        }

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        SustainabilityClaim? resultClaim = result.FirstOrDefault(c => (c.RelatedUrls?.Count ?? 0) > 0);
        Assert.NotNull(resultClaim);
        var related = resultClaim!.RelatedUrls;
        Assert.True(related.Count == 1);
        Assert.Contains(url, related);
    }

   [Fact]
    public async Task ExtractSustainabilityClaimsAsync_GreenElectricity_Narrative_ShouldYieldOneRegularNonCompliantWithRelatedText()
    {
        // Arrange
        var content = @"Wat is groene stroom?
Groene stroom is stroom die wordt opgewekt uit hernieuwbare energiebronnen die onuitputtelijk zijn. Denk aan: zon, wind en water. Op ons stroometiket zie je dat ons product Groen uit Nederland 100% wordt opgewekt uit wind en zon uit eigen land.";

        // Act
        var generic = await _sustainabilityClaimsSemantiService.ExtractAllGenericSustainabilityClaimsAsync(content);
        var claims = new List<SustainabilityClaim>();
        foreach (var gc in generic)
        {
            var filtered = await _sustainabilityClaimsSemantiService.FilterVattenfallSustainabilityClaimAsync(gc, content);
            if (filtered is not null)
            {
                claims.Add(filtered);
            }
        }

        // Assert
        Assert.NotNull(claims);
        Assert.Single(claims);
        var claim = claims.Single();
        Assert.Equal(SustainabilityClaimType.Regular, claim.ClaimType);
        Assert.False(string.IsNullOrWhiteSpace(claim.RelatedText));

        var evaluation = await _sustainabilityClaimsSemantiService.EvaluateSustainabilityClaimComplianceAsync(claim);
        Assert.False(evaluation.IsCompliant);
    }
 
}
