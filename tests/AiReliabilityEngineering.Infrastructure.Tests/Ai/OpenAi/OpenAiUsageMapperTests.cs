using System.Text.Json;
using AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

namespace AiReliabilityEngineering.Infrastructure.Tests.Ai.OpenAi;

public sealed class OpenAiUsageMapperTests
{
    [Fact]
    public void MapUsage_MapsFullUsage()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "usage": {
                "input_tokens": 10,
                "output_tokens": 5,
                "total_tokens": 15
              }
            }
            """);

        var usage = OpenAiUsageMapper.MapUsage(document);

        Assert.NotNull(usage);
        Assert.Equal(10, usage.InputTokens);
        Assert.Equal(5, usage.OutputTokens);
        Assert.Equal(15, usage.TotalTokens);
    }

    [Fact]
    public void MapUsage_WithMissingUsageReturnsNull()
    {
        using var document = JsonDocument.Parse("{}");

        var usage = OpenAiUsageMapper.MapUsage(document);

        Assert.Null(usage);
    }

    [Fact]
    public void MapUsage_MapsPartialUsage()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "usage": {
                "input_tokens": 10
              }
            }
            """);

        var usage = OpenAiUsageMapper.MapUsage(document);

        Assert.NotNull(usage);
        Assert.Equal(10, usage.InputTokens);
        Assert.Null(usage.OutputTokens);
        Assert.Null(usage.TotalTokens);
    }
}
