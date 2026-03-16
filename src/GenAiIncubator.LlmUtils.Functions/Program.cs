using Azure.Storage.Blobs;
using GenAiIncubator.LlmUtils.Core.Extensions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// builder.Services.AddSingleton<ExceptionHandlingMiddleware>();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Services.AddLlmUtils();
builder.Services.AddSingleton(_ =>
    new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")
        ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured.")));
var app = builder.Build();

// app.UseExceptionHandler(exceptionHandlerApp 
//     => exceptionHandlerApp.Run(async context 
//         => await Results.Problem()
//                      .ExecuteAsync(context)));

app.Run();

