using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

public class SustainabilityClaimsEmailNotificationServiceTests
{
    // ─── ExtractEmails ───────────────────────────────────────────────────────────

    [Fact]
    public void ExtractEmails_PlainEmail_ReturnsThatEmail()
    {
        var result = SustainabilityClaimsEmailNotificationService
            .ExtractEmails("olav.gerritsen@vattenfall.com")
            .ToArray();

        Assert.Single(result);
        Assert.Equal("olav.gerritsen@vattenfall.com", result[0]);
    }

    [Fact]
    public void ExtractEmails_MixedTextWithEmail_ExtractsOnlyEmail()
    {
        const string input = "Sewdien Soraya (CF-FB) olav.gerritsen@vattenfall.com| A-team";

        var result = SustainabilityClaimsEmailNotificationService
            .ExtractEmails(input)
            .ToArray();

        Assert.Single(result);
        Assert.Equal("olav.gerritsen@vattenfall.com", result[0]);
    }

    [Fact]
    public void ExtractEmails_MixedTextWithPipeAndTeam_ExtractsOnlyEmail()
    {
        const string input = "Berg Rob van den (CF-FH) olav.gerritsen@vattenfall.com | Digital Sales";

        var result = SustainabilityClaimsEmailNotificationService
            .ExtractEmails(input)
            .ToArray();

        Assert.Single(result);
        Assert.Equal("olav.gerritsen@vattenfall.com", result[0]);
    }

    [Fact]
    public void ExtractEmails_NullInput_ReturnsEmpty()
    {
        var result = SustainabilityClaimsEmailNotificationService
            .ExtractEmails(null)
            .ToArray();

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractEmails_WhitespaceInput_ReturnsEmpty()
    {
        var result = SustainabilityClaimsEmailNotificationService
            .ExtractEmails("   ")
            .ToArray();

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractEmails_NoEmailInText_ReturnsEmpty()
    {
        var result = SustainabilityClaimsEmailNotificationService
            .ExtractEmails("Firstname Lastname (CF-FB) | Team")
            .ToArray();

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractEmails_MultipleEmailsInSameString_ReturnsAll()
    {
        const string input = "one@vattenfall.com and two@vattenfall.com";

        var result = SustainabilityClaimsEmailNotificationService
            .ExtractEmails(input)
            .ToArray();

        Assert.Equal(2, result.Length);
        Assert.Contains("one@vattenfall.com", result);
        Assert.Contains("two@vattenfall.com", result);
    }
}
