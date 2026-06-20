using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed class AiProviderFactory
{
    public IAiProvider Create(AiProviderFactoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.ProviderKind switch
        {
            AiProviderKind.Fake => new FakeAiProvider(),
            _ => throw new AiProviderException(
                $"Unsupported AI provider kind: {options.ProviderKind}")
        };
    }
}
