namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiMessage
{
    public AiMessage(AiRole role, string content)
    {
        Role = role;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public AiRole Role { get; }

    public string Content { get; }
}
