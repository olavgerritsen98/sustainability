namespace GenAiIncubator.LlmUtils.Core.Services.Email;

/// <summary>Represents a file to attach to an outbound email.</summary>
public sealed record EmailAttachment(
    string FileName,
    byte[] Content,
    string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
