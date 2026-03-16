namespace GenAiIncubator.LlmUtils.Core.Options;

/// <summary>
/// Configuration for the Microsoft Graph email service.
/// TenantId, ClientId and ClientSecret map to an app registration with Mail.Send application permission.
/// ClientSecret should only be stored in environment variables / Azure Key Vault, never in source-controlled config files.
/// </summary>
public sealed class GraphOptions
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// App registration client secret. Set via environment variable only; never commit this value.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The shared mailbox used as sender: duurzaamheidsclaims@vattenfall.com.
    /// Requires Mail.Send application permission scoped to this mailbox.
    /// </summary>
    public string SenderAddress { get; set; } = "duurzaamheidsclaims@vattenfall.com";

    /// <summary>
    /// Receives emails when a non-compliant page has no known accountable/responsible owner.
    /// </summary>
    public string FallbackAddress { get; set; } = "olav.gerritsen@vattenfall.com";

    /// <summary>
    /// Receives emails when a page could not be processed due to a function error.
    /// Also included on all error notifications together with FallbackAddress.
    /// </summary>
    public string ErrorAddress { get; set; } = "olav.gerritsen@vattenfall.com";

    /// <summary>
    /// The URL to the AI tool that end-users can use to re-validate content.
    /// Embedded in the Dutch non-compliance email template.
    /// </summary>
    public string AiToolUrl { get; set; } = "https://librechat-ca-tst.mangowater-5b713ed8.westeurope.azurecontainerapps.io/";
}
