namespace AiReliabilityEngineering.Orchestration.RunManagement;

public sealed class RunCleanupService
{
    public async Task<RunCleanupResult> CleanupAsync(string runsDirectory, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runsDirectory))
        {
            return new RunCleanupResult(false, string.Empty, 0, "Runs directory is required.");
        }

        var fullRunsDirectory = Path.GetFullPath(runsDirectory);
        Directory.CreateDirectory(fullRunsDirectory);

        var deletedEntries = 0;
        foreach (var entry in Directory.EnumerateFileSystemEntries(fullRunsDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullEntryPath = Path.GetFullPath(entry);
            if (!IsInsideDirectory(fullEntryPath, fullRunsDirectory))
            {
                return new RunCleanupResult(false, fullRunsDirectory, deletedEntries, $"Refusing to delete outside runs directory: {fullEntryPath}");
            }

            if (string.Equals(Path.GetFileName(fullEntryPath), ".gitkeep", StringComparison.Ordinal))
            {
                continue;
            }

            if (Directory.Exists(fullEntryPath))
            {
                Directory.Delete(fullEntryPath, recursive: true);
            }
            else if (File.Exists(fullEntryPath))
            {
                File.Delete(fullEntryPath);
            }

            deletedEntries++;
        }

        var gitkeepPath = Path.Combine(fullRunsDirectory, ".gitkeep");
        await File.WriteAllTextAsync(gitkeepPath, string.Empty, cancellationToken);

        var message = deletedEntries == 0
            ? "Runs cleanup completed. Nothing to clean."
            : $"Runs cleanup completed. Deleted {deletedEntries} entries.";

        return new RunCleanupResult(true, fullRunsDirectory, deletedEntries, message);
    }

    private static bool IsInsideDirectory(string childPath, string parentDirectory)
    {
        var normalizedParent = parentDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return childPath.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
    }
}
