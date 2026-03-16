using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.ReportRunner.BatchTools;
using GenAiIncubator.LlmUtils.ReportRunner.Infrastructure;
using GenAiIncubator.LlmUtils.ReportRunner.Jobs;
using GenAiIncubator.LlmUtils.ReportRunner.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (args.Length > 0 && string.Equals(args[0], "upload-batch-csv", StringComparison.OrdinalIgnoreCase))
{
    int code = await UploadBatchCsvCommand.RunAsync(args[1..], CancellationToken.None);
    Environment.Exit(code);
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLlmUtils(options =>
{
    options.Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? options.Endpoint;
    options.DeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? options.DeploymentName;
    options.AzureOpenAIApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? options.AzureOpenAIApiKey;
});

builder.Services.AddSingleton<IReportWriter, CsvReportWriter>();
builder.Services.AddSingleton<ITranslationService, TranslationService>();

// New services for dataset input and manual sheet inclusion
builder.Services.AddSingleton<IReportInputProvider, JsonReportInputProvider>();
builder.Services.AddSingleton<IGroundTruthSheetAppender, LabeledDatasetExcelSheetAppender>();

// Register the default job. For future jobs, register them and select via args/env.
builder.Services.AddSingleton<IReportJob, SustainabilityClaimsReportJob>();

using var app = builder.Build();

var job = app.Services.GetRequiredService<IReportJob>();
await job.RunAsync(default);
