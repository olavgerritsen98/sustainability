namespace GenAiIncubator.LlmUtils.Core.Services.ContractTextExtraction;

/// <summary>
/// Extracts the ethical clause section from a contract PDF without OCR.
/// </summary>
public interface IContractEthicalClauseExtractor
{
    /// <summary>
    /// Extracts the ethical clause section text from the provided contract PDF.
    /// </summary>
    /// <param name="pdfBytes">Contract PDF bytes.</param>
    /// <returns>The extracted ethical clause section text, or empty string if not found.</returns>
    string ExtractEthicalClauseSection(byte[] pdfBytes);
}


