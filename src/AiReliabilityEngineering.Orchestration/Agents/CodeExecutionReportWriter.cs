using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.CodeExecution;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class CodeExecutionReportWriter
{
    public async Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        string executorName,
        CodeExecutionResult result,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executorName);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(runContext);

        Directory.CreateDirectory(runContext.Paths.ReportsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(runContext.Paths.ReportsDirectory, "code-execution.md"),
            Render(executorName, result),
            cancellationToken);

        return [new ArtifactRef(ArtifactType.Report, "reports/code-execution.md", "External code execution report")];
    }

    private static string Render(string executorName, CodeExecutionResult result) =>
        $"""
        # Code Execution

        Executor: `{executorName}`

        Success: `{result.Succeeded}`

        Exit code: `{result.ExitCode}`

        Duration: `{result.Duration}`

        ## stdout

        ```text
        {result.StandardOutput}
        ```

        ## stderr

        ```text
        {result.StandardError}
        ```
        """;
}
