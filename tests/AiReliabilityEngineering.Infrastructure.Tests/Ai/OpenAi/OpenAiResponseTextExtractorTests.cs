using System.Text.Json;
using AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

namespace AiReliabilityEngineering.Infrastructure.Tests.Ai.OpenAi;

public sealed class OpenAiResponseTextExtractorTests
{
    [Fact]
    public void ExtractText_PrefersTopLevelOutputText()
    {
        using var document = JsonDocument.Parse("""{ "output_text": "hello" }""");

        var text = OpenAiResponseTextExtractor.ExtractText(document);

        Assert.Equal("hello", text);
    }

    [Fact]
    public void ExtractText_ExtractsOutputContentText()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "output": [
                {
                  "type": "message",
                  "content": [
                    {
                      "type": "output_text",
                      "text": "hello"
                    }
                  ]
                }
              ]
            }
            """);

        var text = OpenAiResponseTextExtractor.ExtractText(document);

        Assert.Equal("hello", text);
    }

    [Fact]
    public void ExtractText_JoinsMultipleFragments()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "output": [
                {
                  "content": [
                    { "text": "first" },
                    { "text": "second" }
                  ]
                }
              ]
            }
            """);

        var text = OpenAiResponseTextExtractor.ExtractText(document);

        Assert.Equal($"first{Environment.NewLine}second", text);
    }

    [Fact]
    public void ExtractText_WithMissingTextReturnsEmptyString()
    {
        using var document = JsonDocument.Parse("{}");

        var text = OpenAiResponseTextExtractor.ExtractText(document);

        Assert.Equal(string.Empty, text);
    }
}
