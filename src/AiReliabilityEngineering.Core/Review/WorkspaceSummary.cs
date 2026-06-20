namespace AiReliabilityEngineering.Core.Review;

public sealed record WorkspaceSummary
{
    public WorkspaceSummary(
        string workspaceRoot,
        IReadOnlyList<string> files)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
        }

        ArgumentNullException.ThrowIfNull(files);

        if (files.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Workspace files must not contain empty entries.", nameof(files));
        }

        WorkspaceRoot = workspaceRoot;
        Files = files
            .Select(path => path.Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    public string WorkspaceRoot { get; }

    public IReadOnlyList<string> Files { get; }
}
