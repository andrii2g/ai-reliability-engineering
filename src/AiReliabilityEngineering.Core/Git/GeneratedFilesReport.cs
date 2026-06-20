namespace AiReliabilityEngineering.Core.Git;

public sealed record GeneratedFilesReport
{
    public GeneratedFilesReport(IReadOnlyList<GeneratedFileEntry> files)
    {
        Files = (files ?? throw new ArgumentNullException(nameof(files)))
            .Where(file => file is not null)
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<GeneratedFileEntry> Files { get; }

    public int Count => Files.Count;

    public long TotalSizeBytes => Files.Sum(file => file.SizeBytes);
}
