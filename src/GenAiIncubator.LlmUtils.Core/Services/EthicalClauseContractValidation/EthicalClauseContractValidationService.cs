using GenAiIncubator.LlmUtils.Core.Models;
using GenAiIncubator.LlmUtils.Core.Services.DocumentParsing;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.EthicalClauseContractValidation;

/// <summary>
/// Provides contract validation capabilities for detecting the presence of an ethical clause.
/// </summary>
public class EthicalClauseContractValidationService(
    EthicalClauseContractValidationSemanticService semanticService,
    IDocumentParser documentParser)
{
    private const int ImageBatchSize = 4;
    private const int ImageBatchOverlap = 1;

    /// <summary>
    /// Validates whether the supplied contract contains an ethical clause.
    /// </summary>
    /// <param name="contractFile">Contract file bytes.</param>
    /// <param name="contractFileExtension">Contract file extension (e.g., "pdf", "docx").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A validation response indicating whether an ethical clause was found.</returns>
    public async Task<EthicalClauseContractValidationResponse> ValidateEthicalClauseContractAsync(
        byte[] contractFile,
        string contractFileExtension,
        CancellationToken cancellationToken)
    {
        if (contractFile is null || contractFile.Length == 0)
        {
            return new EthicalClauseContractValidationResponse(
                HasEthicalClause: false,
                Reasoning: "No contract file content was provided.");
        }

        if (string.IsNullOrWhiteSpace(contractFileExtension))
        {
            return new EthicalClauseContractValidationResponse(
                HasEthicalClause: false,
                Reasoning: "Contract file extension must be provided.");
        }

        ParsedDocument documentToValidate;
        try
        {
            documentToValidate = await documentParser.ParseDocumentAsync(contractFile, contractFileExtension, cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            return new EthicalClauseContractValidationResponse(
                HasEthicalClause: false,
                Reasoning: $"Document type '{contractFileExtension}' is not supported for ethical clause validation. {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return new EthicalClauseContractValidationResponse(
                HasEthicalClause: false,
                Reasoning: ex.Message);
        }

        EthicalClauseContractValidationResponse? lastResponse = null;

        string contractContent = documentToValidate.TextContent ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(contractContent))
        {
            lastResponse = await semanticService.ContractContainsEthicalClauseAsync(contractContent, cancellationToken);
            if (lastResponse.HasEthicalClause)
            {
                return lastResponse with { Reasoning = $"[Found in text] {lastResponse.Reasoning}" };
            }
        }

        if (documentToValidate.Images is not null && documentToValidate.Images.Count != 0)
        {
            IReadOnlyList<ImageContent> images = documentToValidate.Images;
            foreach (ImageBatch batch in EnumerateImageBatches(images, ImageBatchSize, ImageBatchOverlap))
            {
                lastResponse = await semanticService.ContractContainsEthicalClauseAsync(batch.ImagesBatch, cancellationToken);
                if (lastResponse.HasEthicalClause)
                {
                    return lastResponse with { Reasoning = $"[Found in {batch.Location}] {lastResponse.Reasoning}" };
                }
            }
        }

        return lastResponse ?? new EthicalClauseContractValidationResponse(
            HasEthicalClause: false,
            Reasoning: "No contract content (text/images) was available to validate.");
    }

    private sealed record ImageBatch(
        IReadOnlyList<ImageContent> ImagesBatch,
        int? MinIndex,
        int? MaxIndex)
    {
        public string Location => (MinIndex is not null && MaxIndex is not null)
            ? (MinIndex == MaxIndex ? $"image page {MinIndex}" : $"image pages {MinIndex}-{MaxIndex}")
            : "image batch";
    }

    private static IEnumerable<ImageBatch> EnumerateImageBatches(
        IReadOnlyList<ImageContent> images,
        int batchSize,
        int overlap)
    {
        if (images.Count == 0)
            yield break;

        if (batchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be >= 1.");

        if (overlap < 0 || overlap >= batchSize)
            throw new ArgumentOutOfRangeException(nameof(overlap), overlap, "Overlap must be in the range [0, batchSize). ");

        int step = batchSize - overlap;
        if (step <= 0)
            throw new ArgumentOutOfRangeException(nameof(overlap), overlap, "Overlap must be smaller than batch size.");

        for (int batchStart = 0; batchStart < images.Count; batchStart += step)
        {
            int batchEndExclusive = Math.Min(batchStart + batchSize, images.Count);
            var imagesBatch = new List<ImageContent>(capacity: batchEndExclusive - batchStart);

            for (int i = batchStart; i < batchEndExclusive; i++)
            {
                imagesBatch.Add(images[i]);
            }

            if (imagesBatch.Count > 0)
            {
                int minPage = batchStart + 1;
                int maxPage = batchEndExclusive;
                yield return new ImageBatch(imagesBatch, minPage, maxPage);
            }
        }
    }
}

