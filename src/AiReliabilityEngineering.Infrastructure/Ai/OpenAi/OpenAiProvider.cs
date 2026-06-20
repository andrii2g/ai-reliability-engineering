using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

public sealed class OpenAiProvider : IAiProvider
{
    private const int MaxErrorBodyLength = 2000;
    private readonly HttpClient _httpClient;
    private readonly OpenAiProviderOptions _options;
    private readonly Func<string, string?> _environmentVariableReader;

    public OpenAiProvider(
        HttpClient httpClient,
        OpenAiProviderOptions options,
        Func<string, string?>? environmentVariableReader = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
    }

    public string Name => "openai";

    public async Task<AiResponse> GenerateAsync(
        AiRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        var apiKey = _environmentVariableReader(_options.ApiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AiResponse.Failure(
                $"{_options.ApiKeyEnvironmentVariable} environment variable is not set.",
                Name,
                _options.Model);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(
            BuildRequestJson(request),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return AiResponse.Failure(
                $"OpenAI request failed with status {(int)response.StatusCode} {response.StatusCode}: {Truncate(body)}",
                Name,
                _options.Model);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return AiResponse.Failure("Failed to parse OpenAI response JSON.", Name, _options.Model);
        }

        using (document)
        {
            var text = OpenAiResponseTextExtractor.ExtractText(document);
            if (string.IsNullOrWhiteSpace(text))
            {
                return AiResponse.Failure("OpenAI response did not contain output text.", Name, _options.Model);
            }

            var usage = OpenAiUsageMapper.MapUsage(document);
            return AiResponse.Success(text, Name, _options.Model, usage);
        }
    }

    private string BuildRequestJson(AiRequest request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["input"] = BuildInput(request)
        };

        if (request.Options.Temperature is not null)
        {
            payload["temperature"] = request.Options.Temperature;
        }

        return JsonSerializer.Serialize(payload);
    }

    private static string BuildInput(AiRequest request)
    {
        var builder = new StringBuilder();
        foreach (var message in request.Messages)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append('[');
            builder.Append(message.Role.ToString().ToUpperInvariant());
            builder.AppendLine("]");
            builder.Append(message.Content);
        }

        if (request.OutputFormat == AiOutputFormat.Json)
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("Return valid JSON only.");
        }

        return builder.ToString();
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaxErrorBodyLength)
        {
            return value;
        }

        return value[..MaxErrorBodyLength];
    }
}
