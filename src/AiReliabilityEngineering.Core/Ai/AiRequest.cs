namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiRequest
{
    public AiRequest(
        IReadOnlyList<AiMessage> messages,
        AiOutputFormat outputFormat,
        AiProviderOptions options,
        string? jsonSchema = null)
    {
        if (messages is null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        if (messages.Count == 0)
        {
            throw new ArgumentException("AI request must contain at least one message.", nameof(messages));
        }

        if (messages.Any(message => message is null))
        {
            throw new ArgumentException("AI request messages must not contain null entries.", nameof(messages));
        }

        Messages = messages.ToArray();
        OutputFormat = outputFormat;
        Options = options ?? throw new ArgumentNullException(nameof(options));
        JsonSchema = jsonSchema;
    }

    public IReadOnlyList<AiMessage> Messages { get; }

    public AiOutputFormat OutputFormat { get; }

    public AiProviderOptions Options { get; }

    public string? JsonSchema { get; }

    public static AiRequest FromPrompts(
        string systemPrompt,
        string userPrompt,
        AiOutputFormat outputFormat,
        AiProviderOptions options,
        string? jsonSchema = null)
    {
        return new AiRequest(
            new[]
            {
                new AiMessage(AiRole.System, systemPrompt),
                new AiMessage(AiRole.User, userPrompt)
            },
            outputFormat,
            options,
            jsonSchema);
    }
}
