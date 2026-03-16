namespace GenAiIncubator.LlmUtils_Functions.Definitions.EthicalClauseContractValidation;

/// <summary>
/// Request contract for validating a contract for an ethical clause.
/// Supports:
/// - multipart/form-data (file upload)
/// - application/json (base64 + file type)
/// </summary>
public class EthicalClauseContractValidationRequest
{
    /// <summary>
    /// Base64-encoded contract file bytes (PDF).
    /// Only used for JSON requests.
    /// </summary>
    public string? Base64Document { get; set; }

    /// <summary>
    /// File extension of the uploaded document (e.g., "pdf").
    /// Only used for JSON requests.
    /// </summary>
    public string? FileType { get; set; }
}


