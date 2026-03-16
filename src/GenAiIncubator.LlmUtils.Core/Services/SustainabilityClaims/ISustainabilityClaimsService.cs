using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims
{
    /// <summary>
    /// Defines the orchestration contract for checking sustainability claims
    /// on a webpage or arbitrary normalized content according to ACM/CDR/UCPD-aligned requirements.
    /// </summary>
    public interface ISustainabilityClaimsService
    {
        /// <summary>
        /// Fetches, normalizes and evaluates sustainability claims found at the specified URL.
        /// </summary>
        /// <param name="url">The URL of the page to analyze.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The list of sustainability claim compliance evaluations.</returns>
        Task<List<SustainabilityClaimComplianceEvaluation>> CheckSustainabilityClaimsAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates sustainability claims from already-normalized content associated with a source URL.
        /// </summary>
        /// <param name="normalizedContent">The normalized content to inspect.</param>
        /// <param name="url">The source URL related to the content.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The list of sustainability claim compliance evaluations.</returns>
        Task<List<SustainabilityClaimComplianceEvaluation>> CheckSustainabilityClaimsFromStringContentAsync(string normalizedContent, string url, CancellationToken cancellationToken = default);
    }
}
