using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Infrastructure.Logging;

public sealed class CompositeRunLogger(IReadOnlyList<IRunLogger> loggers) : IRunLogger
{
    public async Task InfoAsync(string message, CancellationToken cancellationToken)
    {
        foreach (var logger in loggers)
        {
            await logger.InfoAsync(message, cancellationToken);
        }
    }

    public async Task ErrorAsync(string message, Exception? exception, CancellationToken cancellationToken)
    {
        foreach (var logger in loggers)
        {
            await logger.ErrorAsync(message, exception, cancellationToken);
        }
    }
}
