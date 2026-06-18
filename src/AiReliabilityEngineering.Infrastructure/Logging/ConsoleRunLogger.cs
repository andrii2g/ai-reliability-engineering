using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Infrastructure.Logging;

public sealed class ConsoleRunLogger : IRunLogger
{
    public Task InfoAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine(Format("INFO", message, null));
        return Task.CompletedTask;
    }

    public Task ErrorAsync(string message, Exception? exception, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.Error.WriteLine(Format("ERROR", message, exception));
        return Task.CompletedTask;
    }

    internal static string Format(string level, string message, Exception? exception)
    {
        var exceptionText = exception is null ? string.Empty : $" {exception}";
        return $"{DateTimeOffset.UtcNow:O} [{level}] {message}{exceptionText}";
    }
}
