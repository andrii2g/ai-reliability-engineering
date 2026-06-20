using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed record AiProviderFactoryOptions
{
    public AiProviderFactoryOptions(AiProviderSelection selection)
    {
        Selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public AiProviderSelection Selection { get; }

    public static AiProviderFactoryOptions Default { get; } =
        new(AiProviderSelection.DefaultFake);
}
