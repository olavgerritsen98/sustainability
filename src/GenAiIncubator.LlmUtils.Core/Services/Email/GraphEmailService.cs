using Azure.Identity;
using GenAiIncubator.LlmUtils.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using FileAttachment = Microsoft.Graph.Models.FileAttachment;

namespace GenAiIncubator.LlmUtils.Core.Services.Email;

/// <summary>
/// Sends emails via Microsoft Graph API using an app registration with <c>Mail.Send</c>
/// application permission, scoped to the shared mailbox configured in <see cref="GraphOptions.SenderAddress"/>.
/// </summary>
public sealed class GraphEmailService : IEmailService
{
    private readonly GraphOptions _options;
    private readonly ILogger<GraphEmailService> _logger;
    private readonly GraphServiceClient _graphClient;

    public GraphEmailService(IOptions<GraphOptions> options, ILogger<GraphEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var credential = new ClientSecretCredential(
            _options.TenantId,
            _options.ClientId,
            _options.ClientSecret);

        // Application-level scope — sends on behalf of the shared mailbox via Users[address]/sendMail.
        _graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
    }

    /// <inheritdoc/>
    public Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default)
        => SendAsync([toAddress], subject, htmlBody, null, ct);

    /// <inheritdoc/>
    public Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody, CancellationToken ct = default)
        => SendAsync(toAddresses, subject, htmlBody, null, ct);

    /// <inheritdoc/>
    public async Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody, IEnumerable<EmailAttachment>? attachments, CancellationToken ct = default)
    {
        var addressList = toAddresses.ToList();
        var attachmentList = attachments?.ToList();

        var message = new Message
        {
            Subject = subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = htmlBody
            },
            ToRecipients = addressList
                .Select(a => new Recipient { EmailAddress = new EmailAddress { Address = a } })
                .ToList()
        };

        if (attachmentList is { Count: > 0 })
        {
            message.Attachments = attachmentList
                .Select<EmailAttachment, Microsoft.Graph.Models.Attachment>(a => new FileAttachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = a.FileName,
                    ContentBytes = a.Content,
                    ContentType = a.ContentType
                })
                .ToList();
        }

        var requestBody = new SendMailPostRequestBody
        {
            Message = message,
            SaveToSentItems = false
        };

        await _graphClient.Users[_options.SenderAddress].SendMail.PostAsync(requestBody, cancellationToken: ct);

        _logger.LogInformation(
            "Email sent from {Sender} to [{Recipients}] with subject '{Subject}'.",
            _options.SenderAddress,
            string.Join(", ", addressList),
            subject);
    }
}
