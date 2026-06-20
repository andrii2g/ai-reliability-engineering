using AiReliabilityEngineering.Core.Git;

namespace AiReliabilityEngineering.Core.Tests.Git;

public sealed class GitWorkspaceSnapshotTests
{
    [Fact]
    public void GeneratedFileEntry_ValidatesAndNormalizesPath()
    {
        var entry = new GeneratedFileEntry(@"src\Tool\Program.cs", 12);

        Assert.Equal("src/Tool/Program.cs", entry.RelativePath);
        Assert.Equal(12, entry.SizeBytes);
        Assert.Throws<ArgumentException>(() => new GeneratedFileEntry("", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeneratedFileEntry("file", -1));
    }

    [Fact]
    public void GeneratedFilesReport_SortsAndSummarizes()
    {
        var report = new GeneratedFilesReport(
            [
                new GeneratedFileEntry("b.txt", 2),
                new GeneratedFileEntry("a.txt", 1)
            ]);

        Assert.Equal(["a.txt", "b.txt"], report.Files.Select(file => file.RelativePath));
        Assert.Equal(2, report.Count);
        Assert.Equal(3, report.TotalSizeBytes);
    }

    [Fact]
    public void GitStatusEntry_ValidatesAndNormalizesPath()
    {
        var entry = new GitStatusEntry(" M", @"src\Tool\Program.cs");

        Assert.Equal("M", entry.Status);
        Assert.Equal("src/Tool/Program.cs", entry.Path);
        Assert.Throws<ArgumentException>(() => new GitStatusEntry("", "file"));
        Assert.Throws<ArgumentException>(() => new GitStatusEntry("M", ""));
    }

    [Fact]
    public void GitWorkspaceSnapshot_SortsStatusEntries()
    {
        var snapshot = new GitWorkspaceSnapshot(
            new GeneratedFilesReport([]),
            [
                new GitStatusEntry("M", "b.txt"),
                new GitStatusEntry("??", "a.txt")
            ]);

        Assert.Equal(["a.txt", "b.txt"], snapshot.StatusEntries.Select(entry => entry.Path));
    }
}
