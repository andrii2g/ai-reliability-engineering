namespace AiReliabilityEngineering.Core.Ai;

public interface IAiProvider
{
    string Name { get; }

    Task<AiResponse> GenerateAsync(
        AiRequest request,
        CancellationToken cancellationToken);
}
