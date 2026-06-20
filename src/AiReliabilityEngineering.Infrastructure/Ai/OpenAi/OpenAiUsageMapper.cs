using System.Text.Json;
using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

public static class OpenAiUsageMapper
{
    public static AiUsage? MapUsage(JsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.RootElement.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new AiUsage(
            TryGetInt(usage, "input_tokens"),
            TryGetInt(usage, "output_tokens"),
            TryGetInt(usage, "total_tokens"));
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.TryGetInt32(out var value) ? value : null;
    }
}
