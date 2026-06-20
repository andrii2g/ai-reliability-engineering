namespace AiReliabilityEngineering.Orchestration.Agents;

public static class TransientWorkspacePathFilter
{
    private static readonly HashSet<string> TransientSegments =
        new(StringComparer.Ordinal)
        {
            ".git",
            "bin",
            "obj",
            ".vs",
            ".idea"
        };

    public static bool IsTransient(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var normalized = relativePath.Replace('\\', '/');
        return normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => TransientSegments.Contains(segment));
    }
}
