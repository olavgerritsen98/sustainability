using System.Text.Json;
using Microsoft.Extensions.Hosting;
using GenAiIncubator.LlmUtils.ReportRunner.Models;

namespace GenAiIncubator.LlmUtils.ReportRunner.Services;

/// <summary>
/// Reads a ReportInput JSON file from a configured path (env var REPORT_INPUT_PATH) or falls back to a default.
/// </summary>
public sealed class JsonReportInputProvider(IHostEnvironment env) : IReportInputProvider
{
    private readonly IHostEnvironment _env = env;
    private const string ContentRootPlaceholder = "${CONTENT_ROOT}";
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<ReportInput> GetAsync(CancellationToken cancellationToken)
    {
        string? configuredPath = Environment.GetEnvironmentVariable("REPORT_INPUT_PATH");
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            // Default to examples-workbooks/labeled_dataset/dataset.json if present
            string defaultPath = Path.Combine(_env.ContentRootPath, "Resources", "examples-workbooks", "labeled_dataset", "dataset.json");
            configuredPath = defaultPath;
        }

        if (!File.Exists(configuredPath))
        {
            return new ReportInput
            {
                GroundTruth = null
            };
        }

        await using var fs = new FileStream(configuredPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var input = await JsonSerializer.DeserializeAsync<ReportInput>(fs, SerializerOptions, cancellationToken);

        if (input?.GroundTruth is not null)
        {
            if (!string.IsNullOrEmpty(input.GroundTruth.WorkbookPath) && input.GroundTruth.WorkbookPath.Contains(ContentRootPlaceholder, StringComparison.Ordinal))
            {
                input = input with
                {
                    GroundTruth = input.GroundTruth with
                    {
                        WorkbookPath = input.GroundTruth.WorkbookPath.Replace(ContentRootPlaceholder, _env.ContentRootPath, StringComparison.Ordinal)
                    }
                };
            }

            // Normalize dictionary to case-insensitive for lookups at runtime
            if (input.GroundTruth.UrlToTabName is not null && input.GroundTruth.UrlToTabName.Count > 0)
            {
                var normalized = new Dictionary<string, string>(input.GroundTruth.UrlToTabName, StringComparer.OrdinalIgnoreCase);
                input = input with
                {
                    GroundTruth = input.GroundTruth with
                    {
                        UrlToTabName = normalized
                    }
                };
            }
        }

        return input ?? new ReportInput();
    }
}


