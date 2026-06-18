using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.RunManagement;

public sealed class RunDirectoryFactory
{
    private readonly string _runsRootDirectory;
    private readonly TimeProvider _timeProvider;

    public RunDirectoryFactory(string runsRootDirectory, TimeProvider? timeProvider = null)
    {
        _runsRootDirectory = string.IsNullOrWhiteSpace(runsRootDirectory)
            ? throw new ArgumentException("Runs directory is required.", nameof(runsRootDirectory))
            : runsRootDirectory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<RunContext> CreateAsync(string ideaFilePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(ideaFilePath))
        {
            throw new FileNotFoundException("Idea file was not found.", ideaFilePath);
        }

        var createdAtUtc = _timeProvider.GetUtcNow();
        var runId = RunId.Create(createdAtUtc);
        var rootDirectory = Path.Combine(_runsRootDirectory, runId.Value);
        var paths = new RunPaths(
            rootDirectory,
            Path.Combine(rootDirectory, "input"),
            Path.Combine(rootDirectory, "workspace"),
            Path.Combine(rootDirectory, "artifacts"),
            Path.Combine(rootDirectory, "reports"),
            Path.Combine(rootDirectory, "logs"),
            Path.Combine(rootDirectory, "run-state.json"));

        Directory.CreateDirectory(paths.InputDirectory);
        Directory.CreateDirectory(paths.WorkspaceDirectory);
        Directory.CreateDirectory(paths.ArtifactsDirectory);
        Directory.CreateDirectory(paths.ReportsDirectory);
        Directory.CreateDirectory(paths.LogsDirectory);

        var copiedIdeaFilePath = Path.Combine(paths.InputDirectory, "idea.md");
        await using (var source = File.OpenRead(ideaFilePath))
        await using (var destination = File.Create(copiedIdeaFilePath))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        return new RunContext(
            runId,
            Path.GetFullPath(ideaFilePath),
            copiedIdeaFilePath,
            paths,
            createdAtUtc);
    }
}
