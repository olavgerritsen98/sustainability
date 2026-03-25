using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using Xunit;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

/// <summary>
/// Unit tests for <see cref="SustainabilityClaimsEmailNotificationService.ExtractEmails"/>.
/// These are pure unit tests — no external dependencies required.
/// </summary>
public sealed class SustainabilityClaimsEmailNotificationServiceTests
{
    // ─── ExtractEmails ───────────────────────────────────────────────────────────

    [Fact]
    public void ExtractEmails_FreetextWithDisplayName_ReturnsEmail()
    {
        // Matches the real Optimizely format: "Lastname Firstname (CF-FB) user@example.com | Team Name"
        const string input = "Sewdien Soraya (CF-FB) olav.gerritsen@vattenfall.com| A-team";

        var result = SustainabilityClaimsEmailNotificationService.ExtractEmails(input).ToList();

        Assert.Single(result);
        Assert.Equal("olav.gerritsen@vattenfall.com", result[0], ignoreCase: true);
    }

    [Fact]
    public void ExtractEmails_PlainEmail_ReturnsThatEmail()
    {
        const string input = "user@example.com";

        var result = SustainabilityClaimsEmailNotificationService.ExtractEmails(input).ToList();

        Assert.Single(result);
        Assert.Equal("user@example.com", result[0], ignoreCase: true);
    }

    [Fact]
    public void ExtractEmails_MultipleEmailsInField_ReturnsAll()
    {
        const string input = "first@example.com second@example.com";

        var result = SustainabilityClaimsEmailNotificationService.ExtractEmails(input).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains("first@example.com", result, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("second@example.com", result, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Firstname Lastname (Team) | A-team")]
    public void ExtractEmails_NoEmailPresent_ReturnsEmpty(string? input)
    {
        var result = SustainabilityClaimsEmailNotificationService.ExtractEmails(input).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractEmails_DuplicateEmailDifferentCase_DeduplicatedByService()
    {
        // The service deduplicates with OrdinalIgnoreCase after SelectMany;
        // ExtractEmails itself returns both — dedup happens at call site.
        const string input = "User@Example.COM user@example.com";

        var raw = SustainabilityClaimsEmailNotificationService.ExtractEmails(input).ToList();
        var deduped = raw.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Equal(2, raw.Count);    // regex finds both
        Assert.Single(deduped);        // dedup collapses them
    }

    [Fact]
    public void ExtractEmails_RealCsvRowValue_ReturnsCorrectEmail()
    {
        // Taken from the actual meta-report CSV attached to the bug report
        const string input = "Berg Rob van den (CF-FH) olav.gerritsen@vattenfall.com | Digital Sales";

        var result = SustainabilityClaimsEmailNotificationService.ExtractEmails(input).ToList();

        Assert.Single(result);
        Assert.Equal("olav.gerritsen@vattenfall.com", result[0], ignoreCase: true);
    }
}
