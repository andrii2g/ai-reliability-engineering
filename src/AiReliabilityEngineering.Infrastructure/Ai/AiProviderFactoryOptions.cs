using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed record AiProviderFactoryOptions(
    AiProviderKind ProviderKind)
{
    public static AiProviderFactoryOptions Default { get; } =
        new(AiProviderKind.Fake);
}
