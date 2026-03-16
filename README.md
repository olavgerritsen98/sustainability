## GenAiIncubator LlmUtils

A .NET 8 solution with reusable LLM utilities, Azure Functions endpoints, and xUnit tests. The solution includes services for summarisation, unwanted data classification/cleanup, heater type image recognition, user story generation, and more.

### Projects
- `src/GenAiIncubator.LlmUtils.Core`: Core library (services, options, helpers, plugins)
- `src/GenAiIncubator.LlmUtils.Functions`: Azure Functions (HTTP endpoints wrapping core services)
- `src/GenAiIncubator.LlmUtils.Tests`: xUnit test project (unit/integration tests, dataset-driven reports)
- `infra/`: Bicep templates and CI/CD pipelines

### Prerequisites
- **.NET SDK 8.0**
- (Optional) **Azure Functions Core Tools** for local Functions runtime

### Quick start
1) Restore and build
```bash
dotnet restore GenAiIncubator.LlmUtils.sln
dotnet build GenAiIncubator.LlmUtils.sln
```

2) Run tests (fast defaults)
```bash
dotnet test GenAiIncubator.LlmUtils.sln
```

3) Include long‑running, dataset/report tests when needed
```bash
dotnet test GenAiIncubator.LlmUtils.sln -p:IncludeLongRunning=true
```

Notes:
- Tests are parallelized via `src/GenAiIncubator.LlmUtils.Tests/xunit.runner.json`.
- Long-running tests are tagged with `Category=LongRunning` and are excluded by default. Pass `-p:IncludeLongRunning=true` to include them.

### Configuration
- Core library settings: `src/GenAiIncubator.LlmUtils.Core/appsettings.json` and `appsettings.Development.json`
- Functions settings: `src/GenAiIncubator.LlmUtils.Functions/local.settings.json` (local development only)

Secrets and keys should be provided via environment variables or developer-local files and not committed.

### Running the Azure Functions locally
From the Functions project directory:
```bash
dotnet run --project src/GenAiIncubator.LlmUtils.Functions/GenAiIncubator.LlmUtils_Functions.csproj
```
Ensure `local.settings.json` has the needed values for any invoked services.

### Reports produced by tests
Some tests generate reports from labeled datasets. These are marked `Category=LongRunning` and are skipped by default.
- Unwanted data classification CSV: `src/GenAiIncubator.LlmUtils.Tests/static/data_cleanup_test_docs/unwanted_data_classification_report.csv`
- Heater type recognition TSV reports: `src/GenAiIncubator.LlmUtils.Tests/static/heaters/reports/HeaterTypeReport_Summary.tsv` and `HeaterTypeReport_Details.tsv`

Run them when needed with:
```bash
dotnet test -p:IncludeLongRunning=true
```

### Repository layout (high level)
```text
src/
  GenAiIncubator.LlmUtils.Core/
  GenAiIncubator.LlmUtils.Functions/
  GenAiIncubator.LlmUtils.Tests/
infra/
```

### License
Proprietary – internal use only unless stated otherwise.
