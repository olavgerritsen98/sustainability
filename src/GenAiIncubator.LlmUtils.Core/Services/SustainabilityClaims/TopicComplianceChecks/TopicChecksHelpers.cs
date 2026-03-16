using System.Text.Json;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.SemanticModels;
using Microsoft.SemanticKernel;

namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks
{
	/// <summary>
	/// Extension methods on <see cref="Kernel"/> to support reusable execution of topic compliance sub-checks.
	/// </summary>
	public static class TopicChecksExtensions
	{
		/// <summary>
		/// Executes a single sub-check prompt against the LLM and parses a <see cref="TopicComplianceEvaluationResponse"/>.
		/// Adds the sub-check name as a prefix to the reasoning. Returns a standardized failure object if parsing fails.
		/// </summary>
		/// <param name="kernel">Semantic Kernel instance.</param>
		/// <param name="name">Human-readable sub-check identifier.</param>
		/// <param name="prompt">Prompt to execute.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The parsed or fallback compliance evaluation response.</returns>
		public static async Task<TopicComplianceEvaluationResponse> ExecuteTopicSubCheckAsync(
			this Kernel kernel,
			string name,
			string prompt,
			CancellationToken cancellationToken)
		{
			string raw = await ChatHelpers.ExecutePromptAsync(
				kernel,
				prompt,
				typeof(TopicComplianceEvaluationResponse),
				cancellationToken: cancellationToken);

			TopicComplianceEvaluationResponse? parsed = null;
			try
			{
				parsed = JsonSerializer.Deserialize<TopicComplianceEvaluationResponse>(raw);
				if (parsed != null)
				{
					parsed.Reasoning = $"[{name}] {parsed.Reasoning}";
				}
			}
			catch
			{
				// swallow and convert to standardized failure below
			}

			return parsed ?? new TopicComplianceEvaluationResponse
			{
				IsCompliant = false,
				Reasoning = $"Failed to parse response for sub-check '{name}'. Raw: {raw.Substring(0, Math.Min(raw.Length, 500))}",
				SuggestedAlternative = string.Empty
			};
		}
	}
}