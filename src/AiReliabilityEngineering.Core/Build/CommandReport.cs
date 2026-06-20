namespace AiReliabilityEngineering.Core.Build;

public sealed record CommandReport
{
    public CommandReport(
        string command,
        string workingDirectory,
        int exitCode,
        string? standardOutput,
        string? standardError,
        TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new ArgumentException("Working directory is required.", nameof(workingDirectory));
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must not be negative.");
        }

        Command = command;
        WorkingDirectory = workingDirectory;
        ExitCode = exitCode;
        StandardOutput = standardOutput ?? string.Empty;
        StandardError = standardError ?? string.Empty;
        Duration = duration;
    }

    public string Command { get; }

    public string WorkingDirectory { get; }

    public int ExitCode { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public TimeSpan Duration { get; }

    public bool Succeeded => ExitCode == 0;
}
