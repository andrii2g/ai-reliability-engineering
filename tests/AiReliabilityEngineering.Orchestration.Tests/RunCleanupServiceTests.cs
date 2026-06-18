using AiReliabilityEngineering.Orchestration.RunManagement;

namespace AiReliabilityEngineering.Orchestration.Tests;

public sealed class RunCleanupServiceTests
{
    [Fact]
    public async Task CleanupAsync_RemovesGeneratedRunFoldersAndFilesUnderRuns()
    {
        using var workspace = CleanupWorkspace.Create();
        var runDirectory = Path.Combine(workspace.RunsDirectory, "20260618-101530-abc12345");
        Directory.CreateDirectory(runDirectory);
        await File.WriteAllTextAsync(Path.Combine(runDirectory, "run-state.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(workspace.RunsDirectory, "orphan.tmp"), "delete me");
        var service = new RunCleanupService();

        var result = await service.CleanupAsync(workspace.RunsDirectory, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.DeletedEntries);
        Assert.False(Directory.Exists(runDirectory));
        Assert.False(File.Exists(Path.Combine(workspace.RunsDirectory, "orphan.tmp")));
    }

    [Fact]
    public async Task CleanupAsync_PreservesRunsDirectoryAndRecreatesGitkeep()
    {
        using var workspace = CleanupWorkspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.RunsDirectory, "run-to-delete"));
        var gitkeepPath = Path.Combine(workspace.RunsDirectory, ".gitkeep");
        File.Delete(gitkeepPath);
        var service = new RunCleanupService();

        var result = await service.CleanupAsync(workspace.RunsDirectory, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(Directory.Exists(workspace.RunsDirectory));
        Assert.True(File.Exists(gitkeepPath));
    }

    [Fact]
    public async Task CleanupAsync_WithMissingRunsDirectory_CreatesRunsDirectoryAndGitkeep()
    {
        using var workspace = CleanupWorkspace.Create(createRunsDirectory: false);
        var service = new RunCleanupService();

        var result = await service.CleanupAsync(workspace.RunsDirectory, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.DeletedEntries);
        Assert.True(Directory.Exists(workspace.RunsDirectory));
        Assert.True(File.Exists(Path.Combine(workspace.RunsDirectory, ".gitkeep")));
    }

    [Fact]
    public async Task CleanupAsync_DoesNotDeleteAnythingOutsideRuns()
    {
        using var workspace = CleanupWorkspace.Create();
        var outsideFilePath = Path.Combine(workspace.RootDirectory, "outside.txt");
        await File.WriteAllTextAsync(outsideFilePath, "keep me");
        Directory.CreateDirectory(Path.Combine(workspace.RunsDirectory, "run-to-delete"));
        var service = new RunCleanupService();

        var result = await service.CleanupAsync(workspace.RunsDirectory, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(outsideFilePath));
    }

    private sealed class CleanupWorkspace : IDisposable
    {
        private CleanupWorkspace(string rootDirectory, bool createRunsDirectory)
        {
            RootDirectory = rootDirectory;
            RunsDirectory = Path.Combine(rootDirectory, "runs");

            if (createRunsDirectory)
            {
                Directory.CreateDirectory(RunsDirectory);
                File.WriteAllText(Path.Combine(RunsDirectory, ".gitkeep"), string.Empty);
            }
        }

        public string RootDirectory { get; }

        public string RunsDirectory { get; }

        public static CleanupWorkspace Create(bool createRunsDirectory = true)
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "aire-cleanup-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return new CleanupWorkspace(rootDirectory, createRunsDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
