namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiResponse
{
    private AiResponse(
        bool succeeded,
        string content,
        string? errorMessage,
        AiUsage? usage,
        string provider,
        string model)
    {
        Succeeded = succeeded;
        Content = content;
        ErrorMessage = errorMessage;
        Usage = usage;
        Provider = provider;
        Model = model;
    }

    public bool Succeeded { get; }

    public string Content { get; }

    public string? ErrorMessage { get; }

    public AiUsage? Usage { get; }

    public string Provider { get; }

    public string Model { get; }

    public static AiResponse Success(
        string content,
        string provider,
        string model,
        AiUsage? usage = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(model);

        return new AiResponse(
            true,
            content,
            null,
            usage,
            provider,
            model);
    }

    public static AiResponse Failure(
        string errorMessage,
        string provider,
        string model)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("AI error message is required.", nameof(errorMessage));
        }

        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(model);

        return new AiResponse(
            false,
            string.Empty,
            errorMessage,
            null,
            provider,
            model);
    }
}
