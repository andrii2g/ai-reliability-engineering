namespace AiReliabilityEngineering.Core.Git;

public sealed record GeneratedFileEntry
{
    public GeneratedFileEntry(string relativePath, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path is required.", nameof(relativePath));
        }

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        RelativePath = relativePath.Replace('\\', '/');
        SizeBytes = sizeBytes;
    }

    public string RelativePath { get; }

    public long SizeBytes { get; }
}
