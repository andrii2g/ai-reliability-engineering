namespace AiReliabilityEngineering.Core.Tools;

public interface IToolExecutor
{
    Task<ToolExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        CancellationToken cancellationToken);
}

public sealed record ToolExecutionRequest(
    string Command,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan Timeout);

public sealed record ToolExecutionResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc)
{
    public bool Succeeded => ExitCode == 0;
}
