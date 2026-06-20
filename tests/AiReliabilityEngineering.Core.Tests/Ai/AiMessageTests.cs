using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Core.Tests.Ai;

public sealed class AiMessageTests
{
    [Fact]
    public void Constructor_RejectsNullContent()
    {
        Assert.Throws<ArgumentNullException>(() => new AiMessage(AiRole.User, null!));
    }

    [Fact]
    public void Constructor_AllowsEmptyContent()
    {
        var message = new AiMessage(AiRole.User, string.Empty);

        Assert.Equal(string.Empty, message.Content);
    }
}
