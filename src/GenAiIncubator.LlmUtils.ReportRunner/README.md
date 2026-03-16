# GenAiIncubator.LlmUtils.ReportRunner

Small console app used for running internal report jobs.

## Report generation

### Sustainability Claims Report 
See **SustainabilityClaimsReportJob.cs**.

## Batch tools

Initially meant for report generation, it came in handy to trigger a file upload from here for a BatchSustainabilityClaimsCheck. The Durable Function will generate the report and upload it to the output container itself. 

### Upload test CSV to trigger batch check

This repo contains a blob-triggered batch flow in the Functions project (`batch-input` container).
To avoid manually uploading files via Storage Explorer, you can upload a local CSV from the terminal.

Command:
- `dotnet run --project src/GenAiIncubator.LlmUtils.ReportRunner upload-batch-csv --inputCsvFile testUrlsBatch.csv`

What it does:
- Uploads the CSV to the configured blob storage container (default: `batch-input`).
- If the Functions host is running (local or deployed), the blob trigger should schedule the orchestration.

Options:
- `--inputCsvFile <path>`: CSV file to upload (default: `testUrlsBatch.csv`)
- `--container <name>`: destination container (default: `batch-input`)
- `--blob <name>`: destination blob name (default: `yyyyMMdd-HHmmss_<filename>`)
- `--connection <connectionString>`: override the connection string resolution

Connection string resolution order:
1) `--connection`
2) `AzureWebJobsStorage` environment variable
3) `src/GenAiIncubator.LlmUtils.Functions/local.settings.json` → `Values.AzureWebJobsStorage`

Notes:
- For local runs, `local.settings.json` typically uses Azurite: `UseDevelopmentStorage=true`.
- The upload command only triggers the blob event; it does not start the Functions host.
