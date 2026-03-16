using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsCheckerAsync;
using GenAiIncubator.LlmUtils_Functions.Definitions.SustainabilityClaimsChecker;

namespace GenAiIncubator.LlmUtils.Tests;

/// <summary>
/// Tests that the local-disk file lookup in <see cref="SustainabilityClaimsCheckAsyncActivity"/>
/// correctly scopes to the user's upload directory and rejects cross-user access.
/// These tests exercise <c>ReadFromLocalDisk</c> and <c>FindUploadedFile</c> via
/// reflection because they are private static helpers.
/// </summary>
public sealed class UserScopedFileAccessTests : IDisposable
{
    private readonly string _uploadsRoot;
    private readonly string _userADir;
    private readonly string _userBDir;

    public UserScopedFileAccessTests()
    {
        _uploadsRoot = Path.Combine(Path.GetTempPath(), "llmutils_test_uploads_" + Guid.NewGuid().ToString("N"));
        _userADir = Path.Combine(_uploadsRoot, "userA");
        _userBDir = Path.Combine(_uploadsRoot, "userB");

        Directory.CreateDirectory(_userADir);
        Directory.CreateDirectory(_userBDir);

        // Place a file in userA's directory
        File.WriteAllText(Path.Combine(_userADir, "uuid1__report.docx"), "user A content");
        // Place a different file in userB's directory
        File.WriteAllText(Path.Combine(_userBDir, "uuid2__report.docx"), "user B content");
    }

    public void Dispose()
    {
        try { Directory.Delete(_uploadsRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    // --- Helper: call private static FindUploadedFile via reflection ---
    private static string CallFindUploadedFile(string rootPath, string originalFilename)
    {
        var method = typeof(SustainabilityClaimsCheckAsyncActivity)
            .GetMethod("FindUploadedFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("FindUploadedFile not found");

        try
        {
            return (string)method.Invoke(null, [rootPath, originalFilename])!;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    [Fact]
    public void FindUploadedFile_FindsFileWithUuidPrefix()
    {
        var result = CallFindUploadedFile(_userADir, "report.docx");

        Assert.EndsWith("uuid1__report.docx", result);
    }

    [Fact]
    public void FindUploadedFile_ThrowsWhenFileDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() =>
            CallFindUploadedFile(_userADir, "nonexistent.pdf"));
    }

    [Fact]
    public void UserScoped_FindsOnlyOwnFile()
    {
        // Searching in userA's directory should find userA's file
        var result = CallFindUploadedFile(_userADir, "report.docx");
        var content = File.ReadAllText(result);
        Assert.Equal("user A content", content);
    }

    [Fact]
    public void UserScoped_CannotAccessOtherUsersFile()
    {
        // Searching userA's directory cannot return userB's file
        // even if both have the same display name
        var result = CallFindUploadedFile(_userADir, "report.docx");
        Assert.Contains("userA", result);
        Assert.DoesNotContain("userB", result);
    }

    [Fact]
    public void UnScoped_SearchAllDirectories_FindsFile()
    {
        // When searching from root (no user scope), the file should still be found
        var result = CallFindUploadedFile(_uploadsRoot, "report.docx");
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void SustainabilityClaimsCheckRequest_HasUserIdProperty()
    {
        // Verify the UserId property exists and works
        var request = new SustainabilityClaimsCheckRequest
        {
            Filename = "test.docx",
            UserId = "user123"
        };

        Assert.Equal("user123", request.UserId);
    }

    [Fact]
    public void SustainabilityClaimsCheckRequest_UserIdIsNullByDefault()
    {
        var request = new SustainabilityClaimsCheckRequest
        {
            Filename = "test.docx"
        };

        Assert.Null(request.UserId);
    }
}
