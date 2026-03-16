using GenAiIncubator.LlmUtils.Core.Models;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace GenAiIncubator.LlmUtils.Core.Helpers;

/// <summary>
/// Provides helper methods for plugin operations.
/// </summary>
public static class PluginHelpers
{

    private static readonly string PluginFolderName = "PluginLibrary";
    /// <summary>
    /// Gets the directory path for the plugin library.
    /// </summary>
    /// <returns>The plugin directory path.</returns>
    public static string GetPluginDirectoryPath() => Path.Combine(AppContext.BaseDirectory, PluginFolderName);

    /// <summary>
    /// Executes a plugin asynchronously. Plugin must be registered with the <paramref name="kernel"/> param. 
    /// This function will attempt to execute a function of the same name as the plugin.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="pluginName">The name of the plugin to execute. Should correspond to a plugin defined in the PluginLibrary.</param>
    /// <param name="input">The input parameters for the plugin.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the invocation result.</returns>
    public static async Task<LlmInvocationResult> ExecutePluginAsync(
        Kernel kernel,
        string pluginName,
        IDictionary<string, object> input,
        CancellationToken cancellationToken)
    {
        if (!kernel.Plugins.Contains(pluginName))
        {
            var pluginPath = GetPluginDirectoryPath();

            if (!Directory.Exists(pluginPath))
                throw new DirectoryNotFoundException($"Plugin directory not found at: {pluginPath}");

            kernel.ImportPluginFromPromptDirectory(pluginPath, pluginName);
        }

        if (!kernel.Plugins[pluginName].TryGetFunction(pluginName, out var function))
            throw new InvalidOperationException($"Function {pluginName} not found in the plugin");

        var context = new KernelArguments();
        foreach (var kvp in input)
        {
            if (kvp.Value is string stringValue)
                context[kvp.Key] = stringValue;
            else if (kvp.Value is not null)
                context[kvp.Key] = JsonSerializer.Serialize(kvp.Value);
            else
                context[kvp.Key] = string.Empty;
        }

        FunctionResult result = await function.InvokeAsync(kernel, context, cancellationToken: cancellationToken);
        return CreateInvocationResult(result);
    }


    private static LlmInvocationResult CreateInvocationResult(FunctionResult result)
    {
        return new() 
        {
            Content = result.GetValue<string>() ?? string.Empty,
            RenderedPrompt = result?.RenderedPrompt ?? string.Empty,
            TotalCost = 0 // TODO
        };
    }
}