using System.Text;
using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.Documentation;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class DocumentationArtifactWriter
{
    public async Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        ProjectDocumentation documentation,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documentation);
        ArgumentNullException.ThrowIfNull(runContext);

        Directory.CreateDirectory(runContext.Paths.ArtifactsDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ArtifactsDirectory, "README.md"),
            documentation.ReadmeMarkdown,
            Encoding.UTF8,
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ArtifactsDirectory, "PLAN.md"),
            documentation.PlanMarkdown,
            Encoding.UTF8,
            cancellationToken);

        return
        [
            new ArtifactRef(ArtifactType.Documentation, "artifacts/README.md", "Generated README"),
            new ArtifactRef(ArtifactType.Plan, "artifacts/PLAN.md", "Generated implementation plan")
        ];
    }
}
