using System.Text.Json;

namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

public static class OpenAiResponseTextExtractor
{
    public static string ExtractText(JsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = document.RootElement;
        if (TryGetNonEmptyString(root, "output_text", out var outputText))
        {
            return outputText;
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var fragments = new List<string>();
        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (TryGetNonEmptyString(contentItem, "text", out var text))
                {
                    fragments.Add(text);
                }
            }
        }

        return string.Join(Environment.NewLine, fragments);
    }

    private static bool TryGetNonEmptyString(
        JsonElement element,
        string propertyName,
        out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }
}
