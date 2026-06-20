using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed class AiProviderFactory
{
    private readonly HttpClient _httpClient;
    private readonly Func<string, string?> _environmentVariableReader;

    public AiProviderFactory(
        HttpClient? httpClient = null,
        Func<string, string?>? environmentVariableReader = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
    }

    public IAiProvider Create(AiProviderFactoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Selection.Kind switch
        {
            AiProviderKind.Fake => new FakeAiProvider(),
            AiProviderKind.OpenAi => new OpenAiProvider(
                _httpClient,
                new OpenAiProviderOptions(options.Selection.Model),
                _environmentVariableReader),
            _ => throw new AiProviderException(
                $"Unsupported AI provider kind: {options.Selection.Kind}")
        };
    }
}
