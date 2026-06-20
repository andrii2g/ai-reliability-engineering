namespace AiReliabilityEngineering.Core.Review;

public sealed record RequiredArtifactCheck
{
    public RequiredArtifactCheck(
        string relativePath,
        bool exists,
        string category)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path is required.", nameof(relativePath));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        RelativePath = relativePath.Replace('\\', '/');
        Exists = exists;
        Category = category;
    }

    public string RelativePath { get; }

    public bool Exists { get; }

    public string Category { get; }
}
