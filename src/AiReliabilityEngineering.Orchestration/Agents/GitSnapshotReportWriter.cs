using System.Text.Json;
using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.Git;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class GitSnapshotReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        GitWorkspaceSnapshot snapshot,
        RunContext runContext,
        string? gitFailureMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(runContext);

        Directory.CreateDirectory(runContext.Paths.ReportsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ReportsDirectory, "generated-files.md"),
            RenderGeneratedFiles(snapshot.GeneratedFiles),
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ReportsDirectory, "generated-files.json"),
            JsonSerializer.Serialize(snapshot.GeneratedFiles, JsonOptions),
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ReportsDirectory, "git-status.md"),
            RenderGitStatus(snapshot.StatusEntries, gitFailureMessage),
            cancellationToken);

        return
        [
            new ArtifactRef(ArtifactType.Report, "reports/generated-files.md", "Generated workspace files report"),
            new ArtifactRef(ArtifactType.Report, "reports/generated-files.json", "Generated workspace files JSON report"),
            new ArtifactRef(ArtifactType.Report, "reports/git-status.md", "Workspace git status report")
        ];
    }

    private static string RenderGeneratedFiles(GeneratedFilesReport report)
    {
        var rows = report.Files.Count == 0
            ? "| | |"
            : string.Join(Environment.NewLine, report.Files.Select(file => $"| {file.RelativePath} | {file.SizeBytes} |"));

        return
            $"""
            # Generated Files

            Count: `{report.Count}`

            Total size bytes: `{report.TotalSizeBytes}`

            | Path | Size bytes |
            |---|---:|
            {rows}
            """;
    }

    private static string RenderGitStatus(IReadOnlyList<GitStatusEntry> entries, string? gitFailureMessage)
    {
        var statusText = string.IsNullOrWhiteSpace(gitFailureMessage)
            ? "Git status collected."
            : $"Git command failed: {gitFailureMessage}";
        var rows = entries.Count == 0
            ? "| | |"
            : string.Join(Environment.NewLine, entries.Select(entry => $"| {entry.Status} | {entry.Path} |"));

        return
            $"""
            # Git Status

            {statusText}

            | Status | Path |
            |---|---|
            {rows}
            """;
    }
}
