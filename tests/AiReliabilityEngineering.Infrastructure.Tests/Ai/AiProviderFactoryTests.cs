using AiReliabilityEngineering.Infrastructure.Ai;

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
}
