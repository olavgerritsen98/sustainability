# Batch Sustainability Claims Checker (CSV → Durable → Reports)

This folder contains the **batch sustainability claims checker** for processing a CSV of URLs via Azure Functions + Durable Functions.

## What it does
- A CSV is uploaded to blob storage (`batch-input` container).
- A blob trigger starts a Durable orchestration.
- The orchestrator parses the CSV, fan-outs per URL, calls the existing `SustainabilityClaimsCheckAsyncActivity`, and aggregates results.
- Reports are written to the output container (`batch-output`) under a per-run folder.

## Architecture (high level)
1. **Blob trigger** (`SustainabilityClaimsBatchCheck_BlobTrigger`)
   - Runs on new blobs in `batch-input`.
   - Creates a Durable instance (`runId`), derives a `fileId` from the blob filename, sets a UTC timestamp.

2. **Orchestrator** (`SustainabilityClaimsBatchCheck_Orchestrator`)
   - Calls the parse activity (download + parse).
   - Fans out URL checks with a concurrency limit (`BATCH_MAX_DOP`).
   - Aggregates all results and calls the report activity.

3. **Parse activity** (`SustainabilityClaimsBatchCheck_ParseCsv`)
   - Downloads the CSV from blob storage.
   - Parses required headers:
     - `PageUrl` (required)
     - `PublicationDate` (optional, `d-M-yyyy` or `dd-MM-yyyy`)
     - `ResponsibleForContents` (optional)
     - `AccountableForContents` (optional)
   - Validates URLs and dates and returns row-level errors.

4. **Per-URL analysis**
   - Reuses `SustainabilityClaimsCheckAsyncActivity` (already used by async HTTP flow).
   - The output is `SustainabilityClaimsCheckResponse`.

5. **Report activity** (`SustainabilityClaimsBatchCheck_WriteReport`)
   - Writes outputs into `batch-output/{runId}/...`.
   - Produces:
     - `meta-report_<fileId>_<yyyyMMdd-HHmmss>.csv`
     - `pages/page-<rowNumber>_<fileId>_<yyyyMMdd-HHmmss>.csv`
     - `manifest_<fileId>_<yyyyMMdd-HHmmss>.json`
   - Moves the input blob to a **success** or **poison** container after report generation.

## Output formats (summary)
**Meta report** (`meta-report_...csv`):
- Input CSV columns + appended:
  - `Pagina compliant`
  - `Claims found`
  - `Claims compliant`
  - `Claims not compliant`
  - `Error`

**Per-page report** (`pages/page-...csv`):
- Summary section (Samenvatting)
- Page info section (Pagina informatie)
- Claims table with:
  - `Nr`, `Claim`, `Claimtype`, `Compliance Status`, `Schendingen`, `Alternatief/Suggestie`

## Configuration
- `AzureWebJobsStorage`: required (Functions storage + blob containers)
- `BATCH_MAX_DOP`: max parallel URL analyses (default: 3)
- `BATCH_OUTPUT_CONTAINER`: optional override (default: `batch-output`)
- `BATCH_SUCCESS_CONTAINER`: optional override (default: `batch-success`)
- `BATCH_POISON_CONTAINER`: optional override (default: `batch-deadletter`)

## Local testing (Azurite)
1. Start Azurite.
2. Set `AzureWebJobsStorage=UseDevelopmentStorage=true` in `local.settings.json`.
3. Create containers in Azurite: `batch-input`, `batch-output`, `batch-success` (optional: `batch-deadletter`).
4. Upload a CSV into `batch-input` and watch the function run.

## Key files
- Orchestrator: `SustainabilityClaimsBatchCheckOrchestrator.cs`
- Blob trigger: `SustainabilityClaimsBatchCheckBlobTriggerFunction.cs`
- Parse activity: `SustainabilityClaimsBatchCheckParseCsvActivity.cs`
- Report activity: `SustainabilityClaimsBatchCheckWriteReportActivity.cs`
- CSV parsing: `BatchCsvParser.cs`
- Report formatting: `BatchReportFormatter.cs`
- Report writer: `BatchReportWriter.cs`
