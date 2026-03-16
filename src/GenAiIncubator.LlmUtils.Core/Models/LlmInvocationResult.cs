using System.ComponentModel.DataAnnotations;

namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Wrapper around the result of a LLM invocation, meant to centralize the result of LLM 
/// invocations, which are executed in different ways and produce different result types.
/// </summary>
public class LlmInvocationResult 
{
    /// <summary>
    /// String content returned by the LLM invocation.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The final prompt that was rendered to the LLM.
    /// </summary>
    public string RenderedPrompt { get; set; } = string.Empty;

    /// <summary>
    /// The total cost associated with the LLM invocation.
    /// </summary>
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public double TotalCost { get; set; } = 0.0;
}