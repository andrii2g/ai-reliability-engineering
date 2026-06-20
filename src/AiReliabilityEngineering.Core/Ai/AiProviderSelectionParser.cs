namespace AiReliabilityEngineering.Core.Ai;

public static class AiProviderSelectionParser
{
    public static IReadOnlyList<string> SupportedCliNames { get; } =
        ["fake", "openai"];

    public static bool TryParseProviderKind(
        string? value,
        out AiProviderKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            kind = AiProviderKind.Fake;
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "fake":
                kind = AiProviderKind.Fake;
                return true;

            case "openai":
                kind = AiProviderKind.OpenAi;
                return true;

            default:
                kind = AiProviderKind.Fake;
                return false;
        }
    }

    public static string ToCliName(AiProviderKind kind)
    {
        return kind switch
        {
            AiProviderKind.Fake => "fake",
            AiProviderKind.OpenAi => "openai",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}
