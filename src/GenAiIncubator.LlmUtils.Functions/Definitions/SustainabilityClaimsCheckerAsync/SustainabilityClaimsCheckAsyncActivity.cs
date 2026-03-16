using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using GenAiIncubator.LlmUtils.Core.Helpers;
using GenAiIncubator.LlmUtils.Core.Models.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.DocumentParsing;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsCheckerAsync;

public class SustainabilityClaimsCheckAsyncActivity
{
    private readonly ISustainabilityClaimsService _service;
    private readonly IDocumentParser _documentParser;
    private readonly ShareClient? _shareClient;
    private readonly string _localUploadsRoot;

    public SustainabilityClaimsCheckAsyncActivity(
        ISustainabilityClaimsService sustainabilityClaimsService,
        IDocumentParser documentParser,
        IConfiguration configuration)
    {
        _service = sustainabilityClaimsService;
        _documentParser = documentParser;

        // 1. Setup Azure Client (Single initialization)
        var connectionString = configuration["LIBRECHAT_STORAGE_CONNECTION"];
        var shareName = configuration["LIBRECHAT_SHARE_NAME"] ?? "librechat-config";

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _shareClient = new ShareClient(connectionString, shareName);
        }

        // 2. Setup Local Path (Reads from local.settings.json)
        // Defaults to empty string if not found, handled in ReadFromLocalDiskAsync
        _localUploadsRoot = configuration["LibreChat:LocalUploadsRoot"] ?? string.Empty;
    }

    [Function(SustainabilityClaimsCheckAsyncOrchestrator.ActivityName)]
    public async Task<SustainabilityClaimsCheckResponse> RunAsync(
        [ActivityTrigger] SustainabilityClaimsCheckRequest request,
        CancellationToken ct)
    {
        using LlmTokenUsageScope tokenScope = LlmTokenUsageContext.BeginScope();

        List<SustainabilityClaimComplianceEvaluation> evaluations;

        if (!string.IsNullOrWhiteSpace(request.Filename))
        {
            var (fileBytes, resolvedName) = await ReadUploadedFileAsync(request.Filename, request.UserId, ct);
            var fileExtension = Path.GetExtension(resolvedName).TrimStart('.');
            
            var parsedDoc = await _documentParser.ParseDocumentAsync(fileBytes, fileExtension, ct);
            
            if (string.IsNullOrWhiteSpace(parsedDoc.TextContent))
            {
                throw new InvalidOperationException($"The document '{resolvedName}' appears to be empty or contains no extractable text.");
            }

            evaluations = await _service.CheckSustainabilityClaimsFromStringContentAsync(parsedDoc.TextContent, request.Filename, ct);
        }
        else
        {
             evaluations = await _service.CheckSustainabilityClaimsAsync(request.Url!, ct);
        }

        LlmTokenUsageContext usage = tokenScope.Context;
        int? inputTokens = usage.CallCount == 0 ? 0 : (usage.UsageObservedCount > 0 ? usage.InputTokens : null);
        int? outputTokens = usage.CallCount == 0 ? 0 : (usage.UsageObservedCount > 0 ? usage.OutputTokens : null);

        return new SustainabilityClaimsCheckResponse
        {
            IsCompliant = evaluations.All(e => e.IsCompliant),
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Evaluations = [.. evaluations.Select(e => new SustainabilityClaimEvaluation
            {
                ClaimText = e.Claim.ClaimText,
                ClaimType = SustainabilityClaimKnowledge.DutchClaimTypeNames[e.Claim.ClaimType],
                IsCompliant = e.IsCompliant,
                Violations = [.. e.Violations.Select(v => $"{v.Code}: {v.Message}")],
                SuggestedAlternative = e.SuggestedAlternative
            })]
        };
    }

    /// <summary>
    /// Reads an uploaded file from Azure File Share (if configured) or from a local disk path as fallback.
    /// </summary>
    private async Task<(byte[] FileBytes, string FileName)> ReadUploadedFileAsync(string filename, string? userId, CancellationToken ct)
    {
        // Priority 1: Azure File Share
        if (_shareClient != null)
        {
            return await ReadFromAzureFileShareAsync(filename, userId, ct);
        }

        // Priority 2: Local Disk
        return await ReadFromLocalDiskAsync(filename, userId, ct);
    }

    private async Task<(byte[] FileBytes, string FileName)> ReadFromAzureFileShareAsync(string filename, string? userId, CancellationToken ct)
    {
        try
        {
            // _shareClient is guaranteed not null here because of the check in ReadUploadedFileAsync
            var uploadsDir = _shareClient!.GetDirectoryClient("uploads/temp");

            // Direct path access (e.g. "userId/filename.pdf")
            if (filename.Contains('/'))
            {
                var parts = filename.Split('/');
                if (!string.IsNullOrWhiteSpace(userId) &&
                    !parts[0].Equals(userId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException(
                        "Access denied: you are not authorised to access files outside your own upload directory.");
                }

                var current = uploadsDir;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    current = current.GetSubdirectoryClient(parts[i]);
                }
                return await DownloadFileAsync(current.GetFileClient(parts[^1]), parts[^1], ct);
            }

            var sanitizedFilename = filename.Replace(" ", "_");

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userDirClient = uploadsDir.GetSubdirectoryClient(userId);
                return await FindFileInDirectoryAsync(userDirClient, filename, sanitizedFilename, ct);
            }

            await foreach (var userDir in uploadsDir.GetFilesAndDirectoriesAsync(cancellationToken: ct))
            {
                if (!userDir.IsDirectory) continue;

                var userDirClient = uploadsDir.GetSubdirectoryClient(userDir.Name);
                try
                {
                    return await FindFileInDirectoryAsync(userDirClient, filename, sanitizedFilename, ct);
                }
                catch (FileNotFoundException)
                {
                }
            }

            throw new FileNotFoundException(
                $"File '{filename}' not found in any user directory under 'uploads/temp' on share '{_shareClient.Name}'.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException
                                         and not FileNotFoundException and not UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Failed to read file '{filename}' from Azure File Share. Error: {ex.GetType().FullName}: {ex.Message}");
        }
    }

    private async Task<(byte[] FileBytes, string FileName)> FindFileInDirectoryAsync(
        ShareDirectoryClient dirClient, string filename, string sanitizedFilename, CancellationToken ct)
    {
        await foreach (var item in dirClient.GetFilesAndDirectoriesAsync(cancellationToken: ct))
        {
            if (item.IsDirectory) continue;

            var storedName = item.Name;

            var separatorIdx = storedName.IndexOf("__", StringComparison.Ordinal);
            var originalName = separatorIdx >= 0 ? storedName[(separatorIdx + 2)..] : storedName;

            if (originalName.Equals(filename, StringComparison.OrdinalIgnoreCase) ||
                originalName.Equals(sanitizedFilename, StringComparison.OrdinalIgnoreCase))
            {
                return await DownloadFileAsync(dirClient.GetFileClient(storedName), filename, ct);
            }
        }

        throw new FileNotFoundException($"File '{filename}' not found in directory '{dirClient.Name}'.");
    }

    private static async Task<(byte[] FileBytes, string FileName)> DownloadFileAsync(
        ShareFileClient fileClient, string displayName, CancellationToken ct)
    {
        var download = await fileClient.DownloadAsync(cancellationToken: ct);
        using var ms = new MemoryStream();
        await download.Value.Content.CopyToAsync(ms, ct);
        return (ms.ToArray(), displayName);
    }

    private async Task<(byte[] FileBytes, string FileName)> ReadFromLocalDiskAsync(string filename, string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_localUploadsRoot))
        {
            throw new InvalidOperationException(
                "Local upload root is not configured. Please set 'LibreChat:LocalUploadsRoot' in local.settings.json.");
        }

        string searchRoot;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            searchRoot = Path.Combine(_localUploadsRoot, userId);
            if (!Directory.Exists(searchRoot))
            {
                throw new FileNotFoundException($"No upload directory found for user ID '{userId}' at '{searchRoot}'.");
            }
        }
        else
        {
            searchRoot = _localUploadsRoot;
        }

        var filePath = FindUploadedFile(searchRoot, filename);

        if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(searchRoot), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied: path traversal detected.");
        }

        byte[] fileBytes = await File.ReadAllBytesAsync(filePath, ct);
        return (fileBytes, Path.GetFileName(filePath));
    }

    private static string FindUploadedFile(string rootPath, string originalFilename)
    {
        string sanitizedFilename = originalFilename.Replace(" ", "_");

        // Note: Directory.GetFiles is synchronous, but acceptable for local dev scenarios.
        // The pattern match ensures we find files even with prefixes (e.g. "timestamp__filename.pdf")
        var files = Directory.GetFiles(rootPath, "*" + sanitizedFilename, SearchOption.AllDirectories);

        var foundFile = files.FirstOrDefault();

        if (foundFile == null)
        {
            // Translated error message to English for consistency
            throw new FileNotFoundException(
                $"Could not find file '{originalFilename}' (searched as '*{sanitizedFilename}') in '{rootPath}' or subdirectories.");
        }

        return foundFile;
    }
}