using System.Text;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class SessionIndexTitleRepositoryTests
{
    [Fact]
    public void LoadLatestTitles_KeepsLastValidRenameForEachThread()
    {
        using TemporarySessionIndex index = TemporarySessionIndex.Create(
            """
            {"id":"thread-a","thread_name":"Original title","updated_at":"2026-07-16T02:49:33Z"}
            {"id":"thread-b","thread_name":" Other task ","updated_at":"2026-07-16T03:00:00Z"}
            {not-json}
            {"id":"thread-a","thread_name":"Renamed sample task","updated_at":"2026-07-16T04:11:59Z"}
            {"id":"thread-a","thread_name":"   ","updated_at":"2026-07-16T05:00:00Z"}
            {"id":"","thread_name":"Ignored"}
            {"id":"missing-title"}
            {"thread_name":"Missing id"}
            """);

        TitleLoadResult result = new SessionIndexTitleRepository(index.Path).LoadLatestTitles();

        Assert.Equal(SessionIndexStatus.Healthy, result.Status);
        Assert.Equal("Renamed sample task", result.Titles["thread-a"]);
        Assert.Equal("Other task", result.Titles["thread-b"]);
        Assert.Equal(2, result.Titles.Count);
    }

    [Fact]
    public void LoadLatestTitles_ReturnsMissingWhenFileDoesNotExist()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jsonl");

        TitleLoadResult result = new SessionIndexTitleRepository(path).LoadLatestTitles();

        Assert.Equal(SessionIndexStatus.Missing, result.Status);
        Assert.Empty(result.Titles);
    }

    [Fact]
    public void LoadLatestTitles_ReturnsIncompatibleForInvalidUtf8()
    {
        using TemporarySessionIndex index = TemporarySessionIndex.Create([0xC3, 0x28]);

        TitleLoadResult result = new SessionIndexTitleRepository(index.Path).LoadLatestTitles();

        Assert.Equal(SessionIndexStatus.Incompatible, result.Status);
        Assert.Empty(result.Titles);
    }

    [Fact]
    public void LoadLatestTitles_CanReadWhileAnotherProcessHasFileOpenForWriting()
    {
        using TemporarySessionIndex index = TemporarySessionIndex.Create(
            """
            {"id":"thread-a","thread_name":"Renamed task"}
            """);
        using var writer = new FileStream(
            index.Path,
            FileMode.Open,
            FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete);

        TitleLoadResult result = new SessionIndexTitleRepository(index.Path).LoadLatestTitles();

        Assert.Equal(SessionIndexStatus.Healthy, result.Status);
        Assert.Equal("Renamed task", result.Titles["thread-a"]);
    }

    private sealed class TemporarySessionIndex : IDisposable
    {
        private TemporarySessionIndex(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporarySessionIndex Create(string contents) =>
            Create(Encoding.UTF8.GetBytes(contents));

        public static TemporarySessionIndex Create(byte[] contents)
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"{Guid.NewGuid():N}.jsonl");
            File.WriteAllBytes(path, contents);
            return new TemporarySessionIndex(path);
        }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
