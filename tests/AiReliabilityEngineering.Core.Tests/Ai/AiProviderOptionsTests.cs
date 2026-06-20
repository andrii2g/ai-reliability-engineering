using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Core.Tests.Ai;

public sealed class AiProviderOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidModel(string? model)
    {
        Assert.Throws<ArgumentException>(() => new AiProviderOptions(model!));
    }

    [Fact]
    public void DefaultFake_IsUsable()
    {
        var options = AiProviderOptions.DefaultFake;

        Assert.Equal("fake-model", options.Model);
        Assert.Equal(0, options.Temperature);
        Assert.Null(options.MaxOutputTokens);
    }
}
