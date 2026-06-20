namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiProviderSelection
{
    public AiProviderSelection(
        AiProviderKind kind,
        string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("AI provider model is required.", nameof(model));
        }

        Kind = kind;
        Model = model;
    }

    public AiProviderKind Kind { get; }

    public string Model { get; }

    public static AiProviderSelection DefaultFake { get; } =
        new(AiProviderKind.Fake, AiProviderOptions.DefaultFake.Model);
}
