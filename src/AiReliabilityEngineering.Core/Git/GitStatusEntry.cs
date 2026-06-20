namespace AiReliabilityEngineering.Core.Git;

public sealed record GitStatusEntry
{
    public GitStatusEntry(string status, string path)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Git status is required.", nameof(status));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Git path is required.", nameof(path));
        }

        Status = status.Trim();
        Path = path.Replace('\\', '/');
    }

    public string Status { get; }

    public string Path { get; }
}
