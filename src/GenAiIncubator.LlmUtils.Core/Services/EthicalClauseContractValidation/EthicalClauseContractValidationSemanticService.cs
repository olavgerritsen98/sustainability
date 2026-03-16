using GenAiIncubator.LlmUtils.Core.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using GenAiIncubator.LlmUtils.Core.Models;

namespace GenAiIncubator.LlmUtils.Core.Services.EthicalClauseContractValidation;

/// <summary>
/// Provides LLM-backed semantic checks for validating clauses against a contract.
/// </summary>
public class EthicalClauseContractValidationSemanticService(Kernel kernel)
{
    private readonly Kernel _kernel = kernel.Clone();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string EthicalClauseToValidate = """
    Following is the template for the ethical clause, which must be included in all contracts with suppliers and partners. 
    This template is meant to be used by Adjusting the terms (Seller/Buyer/Agreement) to language used in the respective contract.
    While the exact wording may vary, it must not deviate from the meaning and the tone of the clause.
    The subsectioning doesn't have to be exactly the same either, as long as the content is the same.

    Clause X. ETHICAL CLAUSE
    (X.1) The Seller confirms that they acknowledge the Buyer’s Code of Conduct for Suppliers and Partners, as amended or adjusted from time to time (the “Code”). The Code valid at the time of Agreement signing for the Seller is attached to this Agreement as Appendices.
    The Seller further agrees that it respects and acts according to the principles of the UN Global Compact on which the Vattenfall Code is based and that it has due diligence processes, including but not limited to policies, procedures and programs in place to ensure compliance with the principles from the UN Global Compact and applicable national legislation.
    (X.2) Either Party shall be entitled but is not obliged to conduct or have conducted an audit or assessment of the other Party and its Affiliates for the sole purpose of determining compliance with the Code and the UN Global Compact principles including processes to ensure monitoring compliance there of as it relates to the performance of this Agreement (the “Purpose”). Any such audit or assessment shall be made during normal business hours and only at the other party’s and its Affiliates offices or operations that are involved in the performance of this Agreement. Either Party is thereby for the Purpose, inter alia, entitled to visit permitted sites, review management systems and interview employees and managers. The audit or assessment may be conducted by the requesting Party and/or by a reputable third-party auditing firm reasonably acceptable to the other Party. Each Party agrees to cooperate to the extent possible and reasonable in order to facilitate the audit or assessment and will use its best endeavours to ensure that its Affiliates do the same. The audit or assessment rights do not encompass access to confidential or proprietary information.
    (X.3) The Seller shall address any violations of the Code or the UN Global Compact principles that come to their knowledge and take appropriate action. The Buyer has the right to suspend or terminate the Agreement without notice, if the Seller
    and/or its Affiliates, offices or operations involved in the performance of this Agreement demonstrably commits or has committed a breach of the Code or the UN Global Compact principles, which is so severe that continuing with the Agreement until the end of its term is reasonably unacceptable, and, in case rectification is possible, if the Seller and/or its Affiliate do not rectify the non-compliance within a reasonable period of time following a written notification.
    (X.4) For the purpose of this clause [X], “Affiliate” shall mean with respect to a Party any entity which is directly or indirectly
    (i) controlled by that Party; or
    (ii) owning or controlling that Party; or
    (iii) under the same ownership or control as that Party.
    """;

    /// <summary>
    /// Determines whether a clause is explicitly supported by a contract.
    /// </summary>
    /// <param name="contractContent">The contract content as plain text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple containing the boolean decision and the raw model response.</returns>
    public async Task<EthicalClauseContractValidationResponse> ContractContainsEthicalClauseAsync(
        string contractContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contractContent))
            throw new ArgumentException("Contract content must be provided.", nameof(contractContent));

        ChatHistory prompt = BuildClauseSupportPrompt(contractContent);
        string raw = await ExecuteSemanticCheckAsync(prompt, cancellationToken);
        return DeserializeDecisionOrThrow(raw);
    }

    /// <summary>
    /// Determines whether the ethical clause is explicitly supported by the text contained in an image (vision model).
    /// </summary>
    public async Task<EthicalClauseContractValidationResponse> ContractContainsEthicalClauseAsync(
        ImageContent image,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);
        return await ContractContainsEthicalClauseAsync([image], cancellationToken);
    }

    /// <summary>
    /// Determines whether the ethical clause is explicitly supported by the text contained in one or more images (vision model).
    /// </summary>
    public async Task<EthicalClauseContractValidationResponse> ContractContainsEthicalClauseAsync(
        IReadOnlyList<ImageContent> images,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(images);
        if (images.Count == 0)
            throw new ArgumentException("At least one image must be provided.", nameof(images));

        ChatHistory prompt = BuildClauseSupportPrompt(images);
        string raw = await ExecuteSemanticCheckAsync(prompt, cancellationToken);
        return DeserializeDecisionOrThrow(raw);
    }

    private async Task<string> ExecuteSemanticCheckAsync(ChatHistory prompt, CancellationToken cancellationToken)
    {
        return await ChatHelpers.ExecutePromptAsync(
            _kernel,
            prompt,
            outputFormatType: typeof(EthicalClauseContractValidationDecision),
            cancellationToken: cancellationToken);
    }

    private static EthicalClauseContractValidationResponse DeserializeDecisionOrThrow(string raw)
    {
        EthicalClauseContractValidationDecision? decision;
        try
        {
            decision = JsonSerializer.Deserialize<EthicalClauseContractValidationDecision>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Failed to deserialize structured ethical clause validation output.");
        }

        return new EthicalClauseContractValidationResponse(
            HasEthicalClause: decision?.IsSupported ?? false,
            Reasoning: decision?.Reasoning ?? "LLM output could not be parsed or was empty.");
    }

    private static ChatHistory BuildClauseSupportPrompt(string contractContent)
    {
        ChatHistory history = [];
        history.AddUserMessage($$"""
        You are a legal expert. Below is the text of a contract. Determine if the following clause is explicitly supported by the contract:

        ### Contract ###

        {{contractContent}}

        ### END Contract ###

        ### Clause ###

        {{EthicalClauseToValidate}}

        ### END Clause ###

        We're looking for the paragraph to be reasonably similar, but not necessarily word-for-word identical. 
        Terminology may vary based on the context of the contract, as well as content organization (e.g., subsectioning). 
        However, the meaning and content must remain consistent with the template provided.

        OUTPUT FORMAT:
        Return a JSON object with:
            - isSupported: boolean (true if supported, otherwise false)
            - reasoning: string (short explanation)
        """);

        return history;
    }

    private static ChatHistory BuildClauseSupportPrompt(IReadOnlyList<ImageContent> images)
    {
        ChatHistory history = [];

        var items = new ChatMessageContentItemCollection
        {
            new TextContent($$"""
            You are a legal expert. The next input contains one or more images from a contract (e.g., scanned pages or screenshots).
            Determine if the following clause is explicitly supported by the contract content shown in the images.

            ### Clause ###

            {{EthicalClauseToValidate}}

            ### END Clause ###

            We're looking for the paragraph to be reasonably similar, but not necessarily word-for-word identical.
            Terminology may vary based on the context of the contract, as well as content organization (e.g., subsectioning).
            However, the meaning and content must remain consistent with the template provided.

            OUTPUT FORMAT:
            Return a JSON object with:
                - isSupported: boolean (true if supported, otherwise false)
                - reasoning: string (short explanation)
            """),
        };

        foreach (ImageContent image in images)
        {
            items.Add(image);
        }

        history.AddMessage(AuthorRole.User, items);
        return history;
    }

}


