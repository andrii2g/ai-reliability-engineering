namespace AiReliabilityEngineering.Core.Git;

public sealed record GitWorkspaceSnapshot
{
    public GitWorkspaceSnapshot(
        GeneratedFilesReport generatedFiles,
        IReadOnlyList<GitStatusEntry>? statusEntries)
    {
        GeneratedFiles = generatedFiles ?? throw new ArgumentNullException(nameof(generatedFiles));
        StatusEntries = (statusEntries ?? Array.Empty<GitStatusEntry>())
            .Where(entry => entry is not null)
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToArray();
    }

    public GeneratedFilesReport GeneratedFiles { get; }

    public IReadOnlyList<GitStatusEntry> StatusEntries { get; }
}
