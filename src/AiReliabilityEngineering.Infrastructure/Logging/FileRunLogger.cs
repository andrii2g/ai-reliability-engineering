using System.Text;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Infrastructure.Logging;

public sealed class FileRunLogger(string logFilePath) : IRunLogger
{
    public Task InfoAsync(string message, CancellationToken cancellationToken)
        => AppendAsync(ConsoleRunLogger.Format("INFO", message, null), cancellationToken);

    public Task ErrorAsync(string message, Exception? exception, CancellationToken cancellationToken)
        => AppendAsync(ConsoleRunLogger.Format("ERROR", message, exception), cancellationToken);

    private async Task AppendAsync(string line, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.AppendAllTextAsync(logFilePath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
    }
}
