using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class WorkspaceArtifactReader
{
    public async Task<string> ReadSpecificationJsonAsync(
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runContext);

        var path = Path.Combine(runContext.Paths.ArtifactsDirectory, "specification.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Project specification artifact was not found.", path);
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task<string?> TryReadTasksJsonAsync(
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runContext);

        var path = Path.Combine(runContext.Paths.ArtifactsDirectory, "tasks.json");
        return File.Exists(path)
            ? await File.ReadAllTextAsync(path, cancellationToken)
            : null;
    }
}
