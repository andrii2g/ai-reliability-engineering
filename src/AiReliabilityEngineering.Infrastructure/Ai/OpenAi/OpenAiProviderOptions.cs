namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

public sealed record OpenAiProviderOptions
{
    public OpenAiProviderOptions(
        string model,
        string apiKeyEnvironmentVariable = "OPENAI_API_KEY",
        Uri? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("OpenAI model is required.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(apiKeyEnvironmentVariable))
        {
            throw new ArgumentException("API key environment variable name is required.", nameof(apiKeyEnvironmentVariable));
        }

        Model = model;
        ApiKeyEnvironmentVariable = apiKeyEnvironmentVariable;
        Endpoint = endpoint ?? new Uri("https://api.openai.com/v1/responses");
    }

    public string Model { get; }

    public string ApiKeyEnvironmentVariable { get; }

    public Uri Endpoint { get; }
}
