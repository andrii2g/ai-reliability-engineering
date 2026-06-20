namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiProviderOptions
{
    public AiProviderOptions(
        string model,
        double? temperature = null,
        int? maxOutputTokens = null)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("AI provider model is required.", nameof(model));
        }

        Model = model;
        Temperature = temperature;
        MaxOutputTokens = maxOutputTokens;
    }

    public string Model { get; }

    public double? Temperature { get; }

    public int? MaxOutputTokens { get; }

    public static AiProviderOptions DefaultFake { get; } =
        new("fake-model", temperature: 0, maxOutputTokens: null);
}
