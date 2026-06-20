namespace AiReliabilityEngineering.Core.CodeExecution;

public sealed record CodeExecutionResult
{
    public CodeExecutionResult(
        bool succeeded,
        int exitCode,
        string? standardOutput,
        string? standardError,
        TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Succeeded = succeeded;
        ExitCode = exitCode;
        StandardOutput = standardOutput ?? string.Empty;
        StandardError = standardError ?? string.Empty;
        Duration = duration;
    }

    public bool Succeeded { get; }

    public int ExitCode { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public TimeSpan Duration { get; }
}
