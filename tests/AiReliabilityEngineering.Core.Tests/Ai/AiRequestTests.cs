using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Core.Tests.Ai;

public sealed class AiRequestTests
{
    [Fact]
    public void FromPrompts_CreatesSystemAndUserMessages()
    {
        var request = AiRequest.FromPrompts(
            "system",
            "user",
            AiOutputFormat.Text,
            AiProviderOptions.DefaultFake);

        Assert.Equal(2, request.Messages.Count);
        Assert.Equal(AiRole.System, request.Messages[0].Role);
        Assert.Equal("system", request.Messages[0].Content);
        Assert.Equal(AiRole.User, request.Messages[1].Role);
        Assert.Equal("user", request.Messages[1].Content);
        Assert.Equal(AiOutputFormat.Text, request.OutputFormat);
        Assert.Equal("fake-model", request.Options.Model);
    }

    [Fact]
    public void Constructor_RejectsNullMessagesList()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AiRequest(null!, AiOutputFormat.Text, AiProviderOptions.DefaultFake));
    }

    [Fact]
    public void Constructor_RejectsEmptyMessagesList()
    {
        Assert.Throws<ArgumentException>(() =>
            new AiRequest(Array.Empty<AiMessage>(), AiOutputFormat.Text, AiProviderOptions.DefaultFake));
    }

    [Fact]
    public void Constructor_RejectsNullMessageEntries()
    {
        Assert.Throws<ArgumentException>(() =>
            new AiRequest([new AiMessage(AiRole.User, "hello"), null!], AiOutputFormat.Text, AiProviderOptions.DefaultFake));
    }

    [Fact]
    public void Constructor_RejectsNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AiRequest(
                [new AiMessage(AiRole.User, "hello")],
                AiOutputFormat.Text,
                null!));
    }

    [Fact]
    public void Constructor_DefensivelyCopiesMessages()
    {
        var messages = new List<AiMessage>
        {
            new(AiRole.User, "hello")
        };

        var request = new AiRequest(messages, AiOutputFormat.Text, AiProviderOptions.DefaultFake);
        messages.Add(new AiMessage(AiRole.Assistant, "changed"));

        Assert.Single(request.Messages);
        Assert.Equal("hello", request.Messages[0].Content);
    }
}
