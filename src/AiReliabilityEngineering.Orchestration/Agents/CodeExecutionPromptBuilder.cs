using System.Text;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class CodeExecutionPromptBuilder
{
    private const int SectionLimit = 6000;

    public async Task<string> BuildAsync(
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runContext);

        var builder = new StringBuilder();
        builder.AppendLine("# AIRE Coding Task");
        builder.AppendLine();
        builder.AppendLine("You are operating inside the current generated workspace only.");
        builder.AppendLine("Do not delete unrelated files.");
        builder.AppendLine("Keep the generated project buildable.");
        builder.AppendLine("Run no network commands unless required by the tool itself.");
        builder.AppendLine("Implement only small safe improvements.");
        builder.AppendLine("Do not change the AIRE repository root.");
        builder.AppendLine();

        await AppendArtifactAsync(builder, "Project Specification", runContext.Paths.ArtifactsDirectory, "specification.json", cancellationToken);
        await AppendArtifactAsync(builder, "Implementation Tasks", runContext.Paths.ArtifactsDirectory, "tasks.json", cancellationToken);
        await AppendArtifactAsync(builder, "README", runContext.Paths.ArtifactsDirectory, "README.md", cancellationToken);
        await AppendArtifactAsync(builder, "PLAN", runContext.Paths.ArtifactsDirectory, "PLAN.md", cancellationToken);

        return builder.ToString();
    }

    private static async Task AppendArtifactAsync(
        StringBuilder builder,
        string title,
        string artifactsDirectory,
        string fileName,
        CancellationToken cancellationToken)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();

        var path = Path.Combine(artifactsDirectory, fileName);
        if (!File.Exists(path))
        {
            builder.AppendLine($"{fileName} was not generated.");
            builder.AppendLine();
            return;
        }

        var text = await File.ReadAllTextAsync(path, cancellationToken);
        builder.AppendLine(Truncate(text));
        builder.AppendLine();
    }

    private static string Truncate(string text) =>
        text.Length <= SectionLimit
            ? text
            : string.Concat(text.AsSpan(0, SectionLimit), Environment.NewLine, "[truncated]");
}
