using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

public class SustainabilityClaimsSemanticServiceMergeTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISustainabilityClaimsSemanticService _sustainabilityClaimsSemanticService;

    public SustainabilityClaimsSemanticServiceMergeTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        _sustainabilityClaimsSemanticService = _serviceProvider.GetRequiredService<ISustainabilityClaimsSemanticService>();
    }

    [Fact]
    public async Task MergeSuggestedAlternativesAsync_WithMultipleSuggestions_ShouldReturnMergedResult()
    {
        // Arrange
        var claim = new SustainabilityClaim
        {
            ClaimText = "Our green energy is the best for the planet.",
            ClaimType = SustainabilityClaimType.Regular
        };

        var evaluations = new List<SustainabilityClaimComplianceEvaluation>
        {
            new SustainabilityClaimComplianceEvaluation
            {
                Claim = claim,
                Violations = 
                [
                    new RequirementViolation { Code = RequirementCode.General_ClearAndUnambiguous, Message = "The claim is vague." }
                ],
                SuggestedAlternative = "Our energy sources have lower carbon impact."
            },
            new SustainabilityClaimComplianceEvaluation
            {
                Claim = claim,
                Violations = 
                [
                    new RequirementViolation { Code = RequirementCode.General_FactuallyCorrectWithSubstanciation, Message = "No evidence provided." }
                ],
                SuggestedAlternative = "Our energy production emits 20% less CO2, as shown in our annual report."
            }
        };

        // Act
        var result = await _sustainabilityClaimsSemanticService.MergeSuggestedAlternativesAsync(claim, evaluations);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.NotEqual(evaluations[0].SuggestedAlternative, result);
        Assert.NotEqual(evaluations[1].SuggestedAlternative, result);
    }

    [Fact]
    public async Task MergeSuggestedAlternativesAsync_WithSingleSuggestion_ShouldReturnThatSuggestion()
    {
        // Arrange
        var claim = new SustainabilityClaim
        {
            ClaimText = "We use solar power.",
            ClaimType = SustainabilityClaimType.Regular
        };

        var expectedSuggestion = "We use 100% solar power generated from our own panels.";
        var evaluations = new List<SustainabilityClaimComplianceEvaluation>
        {
            new SustainabilityClaimComplianceEvaluation
            {
                Claim = claim,
                Violations = [],
                SuggestedAlternative = expectedSuggestion
            }
        };

        // Act
        var result = await _sustainabilityClaimsSemanticService.MergeSuggestedAlternativesAsync(claim, evaluations);

        // Assert
        Assert.Equal(expectedSuggestion, result);
    }

    [Fact]
    public async Task MergeSuggestedAlternativesAsync_WithNoSuggestions_ShouldReturnEmpty()
    {
        // Arrange
        var claim = new SustainabilityClaim
        {
            ClaimText = "We use solar power.",
            ClaimType = SustainabilityClaimType.Regular
        };

        var evaluations = new List<SustainabilityClaimComplianceEvaluation>();

        // Act
        var result = await _sustainabilityClaimsSemanticService.MergeSuggestedAlternativesAsync(claim, evaluations);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
