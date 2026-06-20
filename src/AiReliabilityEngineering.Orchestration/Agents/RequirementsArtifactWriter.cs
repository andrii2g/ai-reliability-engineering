using System.Text;
using System.Text.Json;
using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class RequirementsArtifactWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        ProjectSpecification specification,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(runContext);

        Directory.CreateDirectory(runContext.Paths.ArtifactsDirectory);

        var specificationPath = Path.Combine(runContext.Paths.ArtifactsDirectory, "specification.json");
        await using (var stream = File.Create(specificationPath))
        {
            await JsonSerializer.SerializeAsync(stream, specification, SerializerOptions, cancellationToken);
        }

        var requirementsPath = Path.Combine(runContext.Paths.ArtifactsDirectory, "requirements.md");
        await File.WriteAllTextAsync(requirementsPath, ToMarkdown(specification), Encoding.UTF8, cancellationToken);

        return
        [
            new ArtifactRef(ArtifactType.Specification, "artifacts/specification.json", "Normalized project specification"),
            new ArtifactRef(ArtifactType.Documentation, "artifacts/requirements.md", "Normalized requirements")
        ];
    }

    private static string ToMarkdown(ProjectSpecification specification)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Requirements");
        builder.AppendLine();
        builder.AppendLine("## Project Name");
        builder.AppendLine();
        builder.AppendLine(specification.ProjectName);
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine(specification.Summary);
        builder.AppendLine();
        AppendList(builder, "Goals", specification.Goals);
        AppendList(builder, "Non-Goals", specification.NonGoals);
        AppendList(builder, "Functional Requirements", specification.FunctionalRequirements);
        AppendList(builder, "Acceptance Criteria", specification.AcceptanceCriteria);
        return builder.ToString();
    }

    private static void AppendList(StringBuilder builder, string heading, IReadOnlyList<string> values)
    {
        builder.AppendLine($"## {heading}");
        builder.AppendLine();
        foreach (var value in values)
        {
            builder.AppendLine($"- {value}");
        }

        builder.AppendLine();
    }
}
