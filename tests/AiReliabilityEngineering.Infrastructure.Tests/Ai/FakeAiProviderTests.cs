using System.Text.Json;
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Infrastructure.Ai;

namespace AiReliabilityEngineering.Infrastructure.Tests.Ai;

public sealed class FakeAiProviderTests
{
    [Fact]
    public async Task GenerateAsync_TextResponseIsDeterministic()
    {
        var provider = new FakeAiProvider();
        var request = AiRequest.FromPrompts(
            "You are a fake provider.",
            "Create documentation.",
            AiOutputFormat.Text,
            AiProviderOptions.DefaultFake);

        var first = await provider.GenerateAsync(request, CancellationToken.None);
        var second = await provider.GenerateAsync(request, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Content, second.Content);
        Assert.Equal("Fake AI response for: Create documentation.", first.Content);
        Assert.Equal("fake", first.Provider);
        Assert.Equal("fake-model", first.Model);
        Assert.NotNull(first.Usage);
    }

    [Fact]
    public async Task GenerateAsync_JsonResponseIsDeterministicAndParseable()
    {
        var provider = new FakeAiProvider();
        var request = AiRequest.FromPrompts(
            "system",
            "user",
            AiOutputFormat.Json,
            AiProviderOptions.DefaultFake);

        var response = await provider.GenerateAsync(request, CancellationToken.None);

        Assert.True(response.Succeeded);
        using var document = JsonDocument.Parse(response.Content);
        Assert.Equal("fake", document.RootElement.GetProperty("provider").GetString());
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GenerateAsync_ChecksCancellationAtStart()
    {
        var provider = new FakeAiProvider();
        var request = AiRequest.FromPrompts(
            "system",
            "user",
            AiOutputFormat.Text,
            AiProviderOptions.DefaultFake);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.GenerateAsync(request, source.Token));
    }

    [Fact]
    public async Task GenerateAsync_NullRequestThrowsProgrammingError()
    {
        var provider = new FakeAiProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.GenerateAsync(null!, CancellationToken.None));
    }
}
