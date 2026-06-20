using AiReliabilityEngineering.Core.Git;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class GeneratedFilesReporter
{
    public GeneratedFilesReport Create(RunContext runContext)
    {
        ArgumentNullException.ThrowIfNull(runContext);

        var workspaceDirectory = runContext.Paths.WorkspaceDirectory;
        if (!Directory.Exists(workspaceDirectory))
        {
            return new GeneratedFilesReport([]);
        }

        var workspaceRoot = Path.GetFullPath(workspaceDirectory);
        var files = Directory.EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories)
            .Select(path => CreateEntry(workspaceRoot, path))
            .Where(entry => !TransientWorkspacePathFilter.IsTransient(entry.RelativePath))
            .ToArray();

        return new GeneratedFilesReport(files);
    }

    private static GeneratedFileEntry CreateEntry(string workspaceRoot, string path)
    {
        var relativePath = Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/');
        var length = new FileInfo(path).Length;
        return new GeneratedFileEntry(relativePath, length);
    }
}
