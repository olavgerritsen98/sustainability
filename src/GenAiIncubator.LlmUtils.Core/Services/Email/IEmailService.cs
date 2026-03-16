namespace GenAiIncubator.LlmUtils.Core.Services.Email;

/// <summary>
/// Abstraction for sending emails via an external mail provider.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends an HTML email to a single recipient.</summary>
    Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>Sends an HTML email to multiple recipients (all listed in To:).</summary>
    Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>Sends an HTML email to multiple recipients with optional file attachments.</summary>
    Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody, IEnumerable<EmailAttachment>? attachments, CancellationToken ct = default);
}
