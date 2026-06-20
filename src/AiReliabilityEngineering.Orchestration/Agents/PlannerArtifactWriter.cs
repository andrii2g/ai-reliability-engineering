using System.Text.Json;
using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.Planning;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class PlannerArtifactWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        ImplementationPlan plan,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(runContext);

        Directory.CreateDirectory(runContext.Paths.ArtifactsDirectory);

        var path = Path.Combine(runContext.Paths.ArtifactsDirectory, "tasks.json");
        await using (var stream = File.Create(path))
        {
            await JsonSerializer.SerializeAsync(stream, plan, SerializerOptions, cancellationToken);
        }

        return
        [
            new ArtifactRef(ArtifactType.Tasks, "artifacts/tasks.json", "Generated implementation tasks")
        ];
    }
}
