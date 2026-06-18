namespace AiReliabilityEngineering.Orchestration.Logging;

public interface IRunLogger
{
    Task InfoAsync(string message, CancellationToken cancellationToken);

    Task ErrorAsync(string message, Exception? exception, CancellationToken cancellationToken);
}
