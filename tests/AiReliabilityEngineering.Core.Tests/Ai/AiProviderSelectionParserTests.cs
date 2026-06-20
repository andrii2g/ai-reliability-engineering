using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Core.Tests.Ai;

public sealed class AiProviderSelectionParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseProviderKind_WithEmptyValueMapsToFake(string? value)
    {
        var parsed = AiProviderSelectionParser.TryParseProviderKind(value, out var kind);

        Assert.True(parsed);
        Assert.Equal(AiProviderKind.Fake, kind);
    }

    [Theory]
    [InlineData("fake", AiProviderKind.Fake)]
    [InlineData("FAKE", AiProviderKind.Fake)]
    [InlineData("openai", AiProviderKind.OpenAi)]
    [InlineData("OpenAI", AiProviderKind.OpenAi)]
    public void TryParseProviderKind_WithSupportedValueReturnsKind(
        string value,
        AiProviderKind expectedKind)
    {
        var parsed = AiProviderSelectionParser.TryParseProviderKind(value, out var kind);

        Assert.True(parsed);
        Assert.Equal(expectedKind, kind);
    }

    [Fact]
    public void TryParseProviderKind_WithUnknownValueReturnsFalse()
    {
        var parsed = AiProviderSelectionParser.TryParseProviderKind("unknown", out var kind);

        Assert.False(parsed);
        Assert.Equal(AiProviderKind.Fake, kind);
    }

    [Fact]
    public void ToCliName_ReturnsLowercaseNames()
    {
        Assert.Equal("fake", AiProviderSelectionParser.ToCliName(AiProviderKind.Fake));
        Assert.Equal("openai", AiProviderSelectionParser.ToCliName(AiProviderKind.OpenAi));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AiProviderSelection_RejectsBlankModel(string? model)
    {
        Assert.Throws<ArgumentException>(() => new AiProviderSelection(AiProviderKind.Fake, model!));
    }
}
