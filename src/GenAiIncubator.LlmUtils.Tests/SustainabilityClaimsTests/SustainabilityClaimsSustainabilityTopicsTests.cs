using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

/// <summary>
/// Tests for sustainability topics semantic service.
/// </summary>
public class SustainabilityClaimsSustainabilityTopicsTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<TopicSpecificRequirementCheck> checks;

    public SustainabilityClaimsSustainabilityTopicsTests()
    {
        var services = new ServiceCollection();
        services.AddLlmUtils();
        _serviceProvider = services.BuildServiceProvider();
        checks = _serviceProvider.GetRequiredService<List<TopicSpecificRequirementCheck>>();
    }

    private T GetConcreteCheck<T>() where T : TopicSpecificRequirementCheck =>
        checks.Single(c => c is T) as T ??
        throw new InvalidOperationException($"No check of type {typeof(T).Name} found.");


    [Fact]
    public async Task CheckSustainabilityClaimsAsync_GreenGas_NotCompliant()
    {
        // Arrange
        var content = "Groen gas is een gas op basis van biogas. Biogas wordt gemaakt uit biomassa: afval uit landbouw, industrie en huishoudens. Met groen gas in het gasnet is er minder aardgas nodig voor het verwarmen van woningen en in de industrie. Dat scheelt in de uitstoot van CO2.";
        SustainabilityClaim claim = new() {
                ClaimText = content,
                ClaimType = SustainabilityClaimType.Regular,
                RelatedText = content,
                Reasoning = "Contains a claim about green gas reducing CO2 emissions.",
                RelatedTopics = [ SustainabilityTopicsEnum.GreenGas ]
        };

        // Act
        GreenGasRequirementCheck check = GetConcreteCheck<GreenGasRequirementCheck>();
        SustainabilityClaimComplianceEvaluation evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.False(evaluation.IsCompliant);
        Assert.NotEmpty(evaluation.Violations.Where(v => v.Code == RequirementCode.GreenGas));
        Assert.Contains(SustainabilityTopicsEnum.GreenGas, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_GreenGas_Compliant()
    {
        // Arrange
        var content = "Choose Green Gas — certified by VertiCer and produced from Dutch organic waste. There are still CO₂ emissions, but the CO₂ released was previously absorbed by the organic materials used, so no additional CO₂ is added to the atmosphere.";
        SustainabilityClaim claim = new() {
                ClaimText = content,
                ClaimType = SustainabilityClaimType.Regular,
                RelatedText = content,
                Reasoning = "Contains a claim about green gas reducing CO2 emissions.",
                RelatedTopics = [ SustainabilityTopicsEnum.GreenGas ]
        };

        // Act
        GreenGasRequirementCheck check = GetConcreteCheck<GreenGasRequirementCheck>();
        SustainabilityClaimComplianceEvaluation evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.True(evaluation.IsCompliant);
        Assert.Contains(SustainabilityTopicsEnum.GreenGas, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_GreenElectricity_Compliant()
    {
        // Arrange
        var content = "With the product Groen uit Nederland you receive 100% renewable electricity from Dutch wind, water, and sun. Vattenfall also supplies grey electricity alongside green electricity. This sustainability benefit applies only to the Groen uit Nederland product. View our electricity label here (https://www.vattenfall.nl/stroom/stroometiket/) to see the sources of your electricity. The stroometiket shows which sources our electricity is generated from and provides detailed information about the origin of our electricity. The label covers the year 2023.";
        SustainabilityClaim claim = new() {
                ClaimText = content,
                ClaimType = SustainabilityClaimType.Regular,
                RelatedText = content,
                Reasoning = "Contains a claim about green electricity reducing CO2 emissions.",
                RelatedTopics = [ SustainabilityTopicsEnum.GreenElectricity ]
        };

        // Act
        GreenElectricityRequirementCheck check = GetConcreteCheck<GreenElectricityRequirementCheck>();
        SustainabilityClaimComplianceEvaluation evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.True(evaluation.IsCompliant);
        Assert.Contains(SustainabilityTopicsEnum.GreenElectricity, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_GreenElectricity_NotCompliant()
    {
        // Arrange
        var content = "All our electricity products are green electricity, where you receive 100% renewable electricity from Dutch wind, water and sun.";
        SustainabilityClaim claim = new() {
                ClaimText = content,
                ClaimType = SustainabilityClaimType.Regular,
                RelatedText = content,
                Reasoning = "Contains a claim about green electricity reducing CO2 emissions.",
                RelatedTopics = [ SustainabilityTopicsEnum.GreenElectricity ]
        };

        // Act
        GreenElectricityRequirementCheck check = GetConcreteCheck<GreenElectricityRequirementCheck>();
        SustainabilityClaimComplianceEvaluation evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.False(evaluation.IsCompliant);
        Assert.Contains(RequirementCode.GreenElectricity, evaluation.Violations.Select(v => v.Code));
        Assert.Contains(SustainabilityTopicsEnum.GreenElectricity, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_Heat_Compliant()
    {
        // Arrange
        var content = "Our district heating network in Amsterdam uses residual heat from industry, which can contribute to lower CO₂ emissions compared to some conventional heating methods. Not all our sources are yet sustainable; by 2040, we aim to switch entirely to renewable heat sources. For details on the current sources and CO₂ emissions, see our heat label, which shows the mix of heat sources and associated CO₂ emissions for your area (https://www.vattenfall.nl/stadsverwarming). For information on how CO₂ emissions are calculated and independently validated, please see the methodology and TNO validation report available at [insert actual direct public link to the report, e.g., https://www.vattenfall.nl/duurzaamheid/tno-rapport].";
        SustainabilityClaim claim = new() {
                ClaimText = content,
                ClaimType = SustainabilityClaimType.Regular,
                RelatedText = content,
                RelatedUrls = ["https://www.vattenfall.nl/stadsverwarming/warmte-etiket/"],
                Reasoning = "",
                RelatedTopics = [ SustainabilityTopicsEnum.HeatAndCold ]
        };

        // Act
        HeatRequirementCheck check = GetConcreteCheck<HeatRequirementCheck>();
        SustainabilityClaimComplianceEvaluation evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.True(evaluation.IsCompliant);
        Assert.Contains(SustainabilityTopicsEnum.HeatAndCold, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_Heat_NotCompliant()
    {
        // Arrange
        var content = "Vattenfall's district heating is completely natural-gas-free and 100 % sustainable.";
        SustainabilityClaim claim = new() {
                ClaimText = content,
                ClaimType = SustainabilityClaimType.Regular,
                RelatedText = content,
                Reasoning = "",
                RelatedTopics = [ SustainabilityTopicsEnum.HeatAndCold ]
        };

        // Act
        HeatRequirementCheck check = GetConcreteCheck<HeatRequirementCheck>();
        SustainabilityClaimComplianceEvaluation evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.False(evaluation.IsCompliant);
        Assert.Contains(RequirementCode.HeatAndCold, evaluation.Violations.Select(v => v.Code));
        Assert.Contains(SustainabilityTopicsEnum.HeatAndCold, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_FossilFreeLiving_NotCompliant_FFL1()
    {
        // Arrange (fails FFL-1: frames as achieved today)
        var content =
            "Vattenfall already enables fossil-free living today for all customers.";

        SustainabilityClaim claim = new()
        {
            ClaimText = content,
            ClaimType = SustainabilityClaimType.Regular,
            RelatedText = content,
            Reasoning = "",
            RelatedTopics = [ SustainabilityTopicsEnum.FossilFreeLivingInOneGeneration ]
        };

        // Act
        FossilFreeLivingRequirementCheck check = GetConcreteCheck<FossilFreeLivingRequirementCheck>();
        var evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.False(evaluation.IsCompliant);
        Assert.Single(evaluation.Violations.Where(v => v.Code == RequirementCode.FossilFreeLivingInOneGeneration));
        Assert.Contains(SustainabilityTopicsEnum.FossilFreeLivingInOneGeneration, evaluation.Claim.RelatedTopics);
    }



    [Fact]
    public async Task CheckSustainabilityClaimsAsync_FossilFreeLiving_NotCompliant_FFL5()
    {
        // Arrange (fails FFL-5: exaggerated/superiority tone)
        var content =
            "We are already fossil-free and far ahead of every other supplier.";

        List<SustainabilityClaim> claims = [
            new()
            {
                ClaimText = content,
                ClaimType = SustainabilityClaimType.Regular,
                RelatedText = content,
                Reasoning = "",
                RelatedTopics = [ SustainabilityTopicsEnum.FossilFreeLivingInOneGeneration ]
            }
        ];

        // Act
        FossilFreeLivingRequirementCheck check = GetConcreteCheck<FossilFreeLivingRequirementCheck>();
        var evaluation = await check.CheckTopicComplianceAsync(claims.Single(), content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.False(evaluation.IsCompliant);
        Assert.Single(evaluation.Violations.Where(v => v.Code == RequirementCode.FossilFreeLivingInOneGeneration));
        Assert.Contains(SustainabilityTopicsEnum.FossilFreeLivingInOneGeneration, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_ComparisonsSuperlatives_Compliant()
    {
        // Arrange (passes COMP-1..COMP-5)
        var content =
            "This year’s solar panels emit 20% less CO2 during production compared with our 2020 model. " +
            "Independent LCA by TNO (2024) confirms the result for identical test conditions. " +
            "According to the 2024 Dutch Energy Index, we achieved the lowest CO2 intensity among major suppliers. " +
            "See methodology and sources (link).";

        List<SustainabilityClaim> claims = [
            new()
            {
                ClaimText = content,
                ClaimType = SustainabilityClaimType.Regular,
                RelatedText = content,
                Reasoning = "",
                RelatedTopics = [ SustainabilityTopicsEnum.ComparisonStatement ]
            }
        ];

        // Act
        ComparisonRequirementCheck check = GetConcreteCheck<ComparisonRequirementCheck>();
        var evaluation = await check.CheckTopicComplianceAsync(claims.Single(), content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.Contains(SustainabilityTopicsEnum.ComparisonStatement, evaluation.Claim.RelatedTopics);
        Assert.True(evaluation.IsCompliant);
    }

    // --- Superlatives (now separated from Comparison) ---

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_Superlatives_Compliant()
    {
        // Arrange (superlative with explicit independent evidence/timeframe)
        var content = "According to the independent 2024 Dutch Energy Index, we have the lowest CO2 intensity among major suppliers (verified by TNO 2024).";
        SustainabilityClaim claim = new()
        {
            ClaimText = content,
            ClaimType = SustainabilityClaimType.Regular,
            RelatedText = content,
            Reasoning = "Contains a superlative 'lowest' with independent index reference and year.",
            RelatedTopics = [ SustainabilityTopicsEnum.SuperlativesStatement ]
        };

        // Act
        var check = GetConcreteCheck<SuperlativesRequirementCheck>();
        var evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.True(evaluation.IsCompliant); // all detected superlatives evidenced
        Assert.Contains(SustainabilityTopicsEnum.SuperlativesStatement, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_Superlatives_NotCompliant()
    {
        // Arrange (fails: absolute superiority without evidence)
        var content = "We are the most sustainable energy supplier."; // no independent evidence
        SustainabilityClaim claim = new()
        {
            ClaimText = content,
            ClaimType = SustainabilityClaimType.Regular,
            RelatedText = content,
            Reasoning = "Uses unsupported superlative.",
            RelatedTopics = [ SustainabilityTopicsEnum.SuperlativesStatement ]
        };

        // Act
        var check = GetConcreteCheck<SuperlativesRequirementCheck>();
        var evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.False(evaluation.IsCompliant);
        Assert.Single(evaluation.Violations.Where(v => v.Code == RequirementCode.Superlatives));
        Assert.Contains(SustainabilityTopicsEnum.SuperlativesStatement, evaluation.Claim.RelatedTopics);
    }

    [Fact]
    public async Task CheckSustainabilityClaimsAsync_Superlatives_NoSuperlative_Compliant_NotApplicable()
    {
        // Arrange (no superlative wording -> treated as compliant / not applicable)
        var content = "With the product Groen uit Nederland you receive 100% renewable electricity from Dutch wind, water and sun."; // descriptive, no superiority claim
        SustainabilityClaim claim = new()
        {
            ClaimText = content,
            ClaimType = SustainabilityClaimType.Regular,
            RelatedText = content,
            Reasoning = "No superlatives present.",
            RelatedTopics = [ SustainabilityTopicsEnum.SuperlativesStatement ]
        };

        // Act
        var check = GetConcreteCheck<SuperlativesRequirementCheck>();
        var evaluation = await check.CheckTopicComplianceAsync(claim, content, CancellationToken.None);

        // Assert
        Assert.NotNull(evaluation);
        Assert.True(evaluation.IsCompliant);
        Assert.Contains(SustainabilityTopicsEnum.SuperlativesStatement, evaluation.Claim.RelatedTopics);
        // Should have no violations because check not applicable
        Assert.Empty(evaluation.Violations);
    }
}
