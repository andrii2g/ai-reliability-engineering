using AiReliabilityEngineering.Core.Review;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class WorkspaceSummaryBuilder
{
    private static readonly string[] ExcludedDirectoryNames = ["bin", "obj", ".vs", ".idea"];

    public WorkspaceSummary Build(RunContext runContext)
    {
        ArgumentNullException.ThrowIfNull(runContext);

        var workspaceDirectory = runContext.Paths.WorkspaceDirectory;
        if (!Directory.Exists(workspaceDirectory))
        {
            return new WorkspaceSummary(workspaceDirectory, []);
        }

        var files = Directory
            .EnumerateFiles(workspaceDirectory, "*", SearchOption.AllDirectories)
            .Where(file => !IsUnderExcludedDirectory(workspaceDirectory, file))
            .Select(file => Path.GetRelativePath(workspaceDirectory, file))
            .ToArray();

        return new WorkspaceSummary(workspaceDirectory, files);
    }

    private static bool IsUnderExcludedDirectory(string workspaceDirectory, string filePath)
    {
        var relativePath = Path.GetRelativePath(workspaceDirectory, filePath);
        var parts = relativePath.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(part => ExcludedDirectoryNames.Contains(part, StringComparer.OrdinalIgnoreCase));
    }
}
