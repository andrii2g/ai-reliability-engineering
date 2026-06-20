using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.Review;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class ReviewReportWriter
{
    public async Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        ArtifactReviewResult result,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(runContext);

        Directory.CreateDirectory(runContext.Paths.ReportsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ReportsDirectory, "final-review.md"),
            RenderFinalReview(result),
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ReportsDirectory, "workspace-summary.md"),
            RenderWorkspaceSummary(result.WorkspaceSummary),
            cancellationToken);

        return
        [
            new ArtifactRef(ArtifactType.Review, "reports/final-review.md", "Deterministic final review"),
            new ArtifactRef(ArtifactType.Report, "reports/workspace-summary.md", "Generated workspace summary")
        ];
    }

    private static string RenderFinalReview(ArtifactReviewResult result)
    {
        var tableRows = string.Join(
            Environment.NewLine,
            result.Checks.Select(check => $"| {check.Category} | {check.RelativePath} | {(check.Exists ? "OK" : "Missing")} |"));
        var warnings = result.Warnings.Count == 0
            ? "No warnings."
            : string.Join(Environment.NewLine, result.Warnings.Select(warning => $"- {warning}"));

        return
            $"""
            # Final Review

            ## Run Output

            AIRE produced deterministic run artifacts, workspace files, and reports for review.

            ## Required Artifacts

            | Category | Path | Status |
            |---|---|---|
            {tableRows}

            ## Requirements

            Inspect `artifacts/specification.json` and `artifacts/requirements.md`.

            ## Documentation

            Inspect `artifacts/README.md` and `artifacts/PLAN.md`.

            ## Planning

            Inspect `artifacts/tasks.json`.

            ## Workspace

            Inspect generated files under `workspace/`.

            ## Build and Test Reports

            Inspect `reports/build.md` and `reports/tests.md`.

            ## Warnings

            {warnings}

            ## Suggested Next Steps

            - Review generated artifacts.
            - Inspect build/test reports.
            - Inspect generated workspace.
            - Consider Git snapshot or Codex/OpenCode integration in a future step.
            """;
    }

    private static string RenderWorkspaceSummary(WorkspaceSummary summary)
    {
        var generatedFiles = RenderList(summary.Files);
        var projectFiles = RenderList(summary.Files.Where(file => file.StartsWith("src/", StringComparison.Ordinal)));
        var testFiles = RenderList(summary.Files.Where(file => file.StartsWith("tests/", StringComparison.Ordinal)));

        return
            $"""
            # Workspace Summary

            ## Workspace Root

            `{summary.WorkspaceRoot}`

            ## Generated Files

            {generatedFiles}

            ## Project Files

            {projectFiles}

            ## Test Files

            {testFiles}

            ## Reports

            - reports/build.md
            - reports/tests.md
            - reports/final-review.md
            - reports/workspace-summary.md
            """;
    }

    private static string RenderList(IEnumerable<string> values)
    {
        var items = values.ToArray();
        return items.Length == 0
            ? "No files."
            : string.Join(Environment.NewLine, items.Select(value => $"- {value}"));
    }
}
