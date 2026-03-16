using System.Globalization;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace GenAiIncubator.LlmUtils.ReportRunner.BatchTools;

internal static class UploadBatchCsvCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        string filePath = GetArgValue(args, "--inputCsvFile") ?? "testUrlsBatch.csv";
        string containerName = GetArgValue(args, "--container") ?? "batch-input";
        string? blobName = GetArgValue(args, "--blob");
        string? connectionString = GetArgValue(args, "--connection")
            ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? TryReadFunctionsLocalSettingsConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("No connection string found. Set AzureWebJobsStorage or pass --connection, or ensure src/GenAiIncubator.LlmUtils.Functions/local.settings.json exists.");
            return 2;
        }

        string resolvedFilePath = Path.GetFullPath(filePath);
        if (!File.Exists(resolvedFilePath))
        {
            Console.Error.WriteLine($"CSV file not found: {resolvedFilePath}");
            return 2;
        }

        blobName ??= BuildDefaultBlobName(Path.GetFileName(resolvedFilePath));

        BlobServiceClient serviceClient = new(connectionString);
        BlobContainerClient container = serviceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        BlobClient blob = container.GetBlobClient(blobName);

        await using FileStream stream = File.OpenRead(resolvedFilePath);
        await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);

        Console.WriteLine($"Uploaded: {blob.Uri}");
        Console.WriteLine("If the Functions host is running, the blob trigger should schedule the orchestration.");
        return 0;
    }

    private static string BuildDefaultBlobName(string fileName)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return $"{timestamp}_{fileName}";
    }

    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 >= args.Length)
                return null;

            return args[i + 1];
        }

        return null;
    }

    private static string? TryReadFunctionsLocalSettingsConnectionString()
    {
        try
        {
            string repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            string settingsPath = Path.Combine(repoRoot, "src", "GenAiIncubator.LlmUtils.Functions", "local.settings.json");
            if (!File.Exists(settingsPath))
                return null;

            using FileStream stream = File.OpenRead(settingsPath);
            using JsonDocument doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("Values", out JsonElement values))
                return null;

            if (!values.TryGetProperty("AzureWebJobsStorage", out JsonElement storage))
                return null;

            return storage.GetString();
        }
        catch
        {
            return null;
        }
    }

    private static string FindRepoRoot(string startDir)
    {
        DirectoryInfo? current = new(startDir);
        while (current is not null)
        {
            string sln = Path.Combine(current.FullName, "GenAiIncubator.LlmUtils.sln");
            if (File.Exists(sln))
                return current.FullName;

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
