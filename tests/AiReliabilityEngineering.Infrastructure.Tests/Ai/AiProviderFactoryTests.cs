using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Infrastructure.Ai;
using AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

namespace AiReliabilityEngineering.Infrastructure.Tests.Ai;

public sealed class AiProviderFactoryTests
{
    [Fact]
    public void Create_DefaultFactoryOptionsCreatesFakeAiProvider()
    {
        var factory = new AiProviderFactory();

        var provider = factory.Create(AiProviderFactoryOptions.Default);

        Assert.NotNull(provider);
        Assert.Equal("fake", provider.Name);
        Assert.IsType<FakeAiProvider>(provider);
    }

    [Fact]
    public void Create_RejectsNullOptions()
    {
        var factory = new AiProviderFactory();

        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    [Fact]
    public void Create_OpenAiSelectionCreatesOpenAiProvider()
    {
        var factory = new AiProviderFactory();
        var options = new AiProviderFactoryOptions(new AiProviderSelection(AiProviderKind.OpenAi, "test-model"));

        var provider = factory.Create(options);

        Assert.NotNull(provider);
        Assert.Equal("openai", provider.Name);
        Assert.IsType<OpenAiProvider>(provider);
    }
}
