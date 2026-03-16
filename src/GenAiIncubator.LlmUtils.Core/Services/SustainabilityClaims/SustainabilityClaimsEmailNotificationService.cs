using GenAiIncubator.LlmUtils.Core.Options;
using GenAiIncubator.LlmUtils.Core.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;

/// <summary>
/// Iterates over a parsed meta-report and dispatches Dutch-language notification emails
/// according to the sustainability claims compliance rules:
/// <list type="bullet">
/// <item>Page compliant → do nothing.</item>
/// <item>Page not compliant, at least one owner (accountable or responsible) known → email all available owners.</item>
/// </list>
/// </summary>
public sealed class SustainabilityClaimsEmailNotificationService
{
    private const long MaxAttachmentBytes = 3 * 1024 * 1024; // 3 MB — stays within Graph sendMail's ~4 MB total message limit

    private readonly IEmailService _emailService;
    private readonly GraphOptions _options;
    private readonly ILogger<SustainabilityClaimsEmailNotificationService> _logger;

    public SustainabilityClaimsEmailNotificationService(
        IEmailService emailService,
        IOptions<GraphOptions> options,
        ILogger<SustainabilityClaimsEmailNotificationService> logger)
    {
        _emailService = emailService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Processes all rows in a meta-report and sends notification emails where required.
    /// </summary>
    public async Task ProcessReportRowsAsync(
        IEnumerable<SustainabilityClaimsEmailNotificationRow> rows,
        IReadOnlyDictionary<string, byte[]>? attachmentsByBlobPath,
        CancellationToken ct = default)
    {
        foreach (var row in rows)
        {
            if (ct.IsCancellationRequested)
                break;

            await ProcessRowAsync(row, attachmentsByBlobPath, ct);
        }
    }

    // ─── private ────────────────────────────────────────────────────────────────

    private async Task ProcessRowAsync(
        SustainabilityClaimsEmailNotificationRow row,
        IReadOnlyDictionary<string, byte[]>? attachmentsByBlobPath,
        CancellationToken ct)
    {
        // Skip rows without a URL (e.g. blank lines or section headers in the CSV)
        if (string.IsNullOrWhiteSpace(row.PageUrl))
            return;

        // Page was not processed due to an error
        if (!string.IsNullOrWhiteSpace(row.Error))
        {
            _logger.LogWarning(
                "Page {PageUrl} was not processed. Error: {Error}. No email sent.",
                row.PageUrl, row.Error);
            return;
        }

        bool isCompliant = string.Equals(row.PaginaCompliant, "true", StringComparison.OrdinalIgnoreCase);
        if (isCompliant)
        {
            _logger.LogDebug("Page {PageUrl} is compliant. No email sent.", row.PageUrl);
            return;
        }

        // Only send email if at least one owner is present
        var recipients = new[] { row.AccountableForContents, row.ResponsibleForContents }
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (recipients.Length == 0)
        {
            _logger.LogWarning(
                "Page {PageUrl} is not compliant but no accountable or responsible owner is present. No email sent.",
                row.PageUrl);
            return;
        }

        var attachments = BuildAttachments(row, attachmentsByBlobPath);

        _logger.LogInformation(
            "Page {PageUrl} is not compliant. Sending email to: {Recipients}.",
            row.PageUrl, string.Join(", ", recipients));

        await _emailService.SendAsync(
            recipients,
            $"Niet compliant duurzaamheidsclaim op webpagina {row.PageUrl}",
            BuildNonCompliantEmailBody(row),
            attachments,
            ct);
    }

    /// <summary>
    /// Builds the Dutch email body for a non-compliant page with at least one known owner.
    /// Uses a simple HTML template with minimal styling.
    /// </summary>
    private string BuildNonCompliantEmailBody(SustainabilityClaimsEmailNotificationRow row)
    {
        string pageUrl = HtmlEncode(row.PageUrl);
        string aiToolUrl = HtmlEncode(_options.AiToolUrl);
        string claimsFound = HtmlEncode(row.ClaimsFound);
        string claimsCompliant = HtmlEncode(row.ClaimsCompliant);
        string claimsNotCompliant = HtmlEncode(row.ClaimsNotCompliant);

        return $@"
    <p>We hebben met de AI-tool voor duurzaamheidsclaims de webpagina gevalideerd waar jullie verantwoordelijk voor zijn:</p>
    <p><a href='{pageUrl}'>{pageUrl}</a></p>
    <p>De pagina bevat 1 of meerdere claims die niet compliant zijn.</p>
    <p>Totaal aantal claims: {claimsFound}<br>
    Compliant: {claimsCompliant}<br>
    Niet compliant: {claimsNotCompliant}</p>
    <p>In de bijlage vind je de details en op basis daarvan kan je aan de slag om de content compliant te maken. Als je het niet eens bent met de uitkomst van de AI-tool, breng je input dan mee naar het “inloopspreekuur duurzaamheidsclaims” om je case te bespreken.</p>
    <p>Als je de content hebt gewijzigd, kan je zelf de AI-tool gebruiken om te checken of de content nu wel compliant is. Klik op de volgende link om de AI-tool te openen: <a href='{aiToolUrl}'>{aiToolUrl}</a></p>
    <p>Veel succes!</p>
    <p style='color:#888;font-size:13px;'>PS Ben jij niet meer de accountable of responsible van deze webpagina, stuur deze mail dan door naar de juiste persoon EN wijzig dit ook in Optimizely.</p>
    ";
    }

    // BuildNonCompliantFallbackEmailBody and BuildErrorEmailBody removed — no longer needed

    private static string HtmlEncode(string value)
        => System.Net.WebUtility.HtmlEncode(value);

    private static IEnumerable<EmailAttachment>? BuildAttachments(
        SustainabilityClaimsEmailNotificationRow row,
        IReadOnlyDictionary<string, byte[]>? attachmentsByBlobPath)
    {
        if (attachmentsByBlobPath is null || string.IsNullOrWhiteSpace(row.PageReportBlobPath))
            return null;

        if (!attachmentsByBlobPath.TryGetValue(row.PageReportBlobPath, out byte[]? bytes))
            return null;

        string fileName = row.PageReportBlobPath.Split('/').Last();
        return [new EmailAttachment(fileName, bytes)];
    }
}
