using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.Build;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class BuildTestReportWriter
{
    public async Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        BuildTestReport report,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(runContext);

        Directory.CreateDirectory(runContext.Paths.ReportsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ReportsDirectory, "build.md"),
            RenderCommandReport("Build", report.Build),
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ReportsDirectory, "tests.md"),
            report.Test is null
                ? "# Tests\n\nTests were not run because build failed.\n"
                : RenderCommandReport("Tests", report.Test),
            cancellationToken);

        return
        [
            new ArtifactRef(ArtifactType.Report, "reports/build.md", "Build command report"),
            new ArtifactRef(ArtifactType.Tests, "reports/tests.md", "Test command report")
        ];
    }

    private static string RenderCommandReport(string title, CommandReport report) =>
        $"""
        # {title}

        Command: `{report.Command}`

        Working directory: `{report.WorkingDirectory}`

        Exit code: `{report.ExitCode}`

        Duration: `{report.Duration}`

        ## stdout

        ```text
        {report.StandardOutput}
        ```

        ## stderr

        ```text
        {report.StandardError}
        ```
        """;
}
