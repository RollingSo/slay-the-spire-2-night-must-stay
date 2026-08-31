using System.Diagnostics;
using System.Text.Json;
using Steamworks;

const uint appId = 2868840;
const ulong publishedFileId = 3791558981;

if (args.Length == 0)
{
    Console.Error.WriteLine("Pass one or more Workshop metadata JSON files.");
    return 2;
}

Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());
Environment.SetEnvironmentVariable("SteamGameId", appId.ToString());

ESteamAPIInitResult initResult = SteamAPI.InitEx(out string initError);
if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
{
    Console.Error.WriteLine($"SteamAPI initialization failed: {initResult}: {initError}");
    return 3;
}

try
{
    foreach (string metadataPath in args)
    {
        WorkshopMetadata metadata = JsonSerializer.Deserialize<WorkshopMetadata>(
            File.ReadAllText(metadataPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Invalid metadata file: {metadataPath}");

        UpdateWorkshopMetadata(metadata);
    }
}
finally
{
    SteamAPI.Shutdown();
}

return 0;

static void UpdateWorkshopMetadata(WorkshopMetadata metadata)
{
    UGCUpdateHandle_t handle = SteamUGC.StartItemUpdate(
        new AppId_t(appId),
        new PublishedFileId_t(publishedFileId));

    Require(SteamUGC.SetItemUpdateLanguage(handle, metadata.Language), "language");
    if (!string.IsNullOrWhiteSpace(metadata.Title))
        Require(SteamUGC.SetItemTitle(handle, metadata.Title), "title");
    if (!string.IsNullOrWhiteSpace(metadata.Description))
        Require(SteamUGC.SetItemDescription(handle, metadata.Description), "description");
    if (!string.IsNullOrWhiteSpace(metadata.ContentPath))
    {
        string contentPath = Path.GetFullPath(metadata.ContentPath);
        if (!Directory.Exists(contentPath))
            throw new DirectoryNotFoundException($"Workshop content directory does not exist: {contentPath}");

        Require(SteamUGC.SetItemContent(handle, contentPath), "content directory");
    }
    if (!string.IsNullOrWhiteSpace(metadata.MinBranch)
        || !string.IsNullOrWhiteSpace(metadata.MaxBranch))
    {
        Require(
            SteamUGC.SetRequiredGameVersions(
                handle,
                metadata.MinBranch ?? string.Empty,
                metadata.MaxBranch ?? string.Empty),
            "supported game branches");
    }

    bool completed = false;
    SubmitItemUpdateResult_t result = default;
    bool ioFailure = false;

    using CallResult<SubmitItemUpdateResult_t> callResult =
        CallResult<SubmitItemUpdateResult_t>.Create((value, failed) =>
        {
            result = value;
            ioFailure = failed;
            completed = true;
        });

    callResult.Set(SteamUGC.SubmitItemUpdate(handle, metadata.ChangeNote));

    Stopwatch timeout = Stopwatch.StartNew();
    Stopwatch progressTimer = Stopwatch.StartNew();
    while (!completed && timeout.Elapsed < TimeSpan.FromMinutes(20))
    {
        SteamAPI.RunCallbacks();
        if (progressTimer.Elapsed >= TimeSpan.FromSeconds(5))
        {
            EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(
                handle,
                out ulong processed,
                out ulong total);
            double percent = total == 0 ? 0 : processed * 100d / total;
            Console.WriteLine($"{metadata.Language}: {status}, {processed}/{total} bytes ({percent:F1}%).");
            progressTimer.Restart();
        }
        Thread.Sleep(50);
    }

    if (!completed)
        throw new TimeoutException($"Timed out updating Workshop language {metadata.Language}.");
    if (ioFailure)
        throw new InvalidOperationException($"Steam I/O failure updating {metadata.Language}.");
    if (result.m_eResult != EResult.k_EResultOK)
        throw new InvalidOperationException($"Steam rejected {metadata.Language}: {result.m_eResult}.");

    Console.WriteLine(
        $"Updated {metadata.Language}: {metadata.Title ?? "(content only)"} " +
        $"(legal agreement required: {result.m_bUserNeedsToAcceptWorkshopLegalAgreement}).");
}

static void Require(bool success, string field)
{
    if (!success)
        throw new InvalidOperationException($"Steam rejected Workshop {field} before submission.");
}

internal sealed record WorkshopMetadata(
    string Language,
    string ChangeNote,
    string? Title = null,
    string? Description = null,
    string? MinBranch = null,
    string? MaxBranch = null,
    string? ContentPath = null);
