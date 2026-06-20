using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed class FakeAiProvider : IAiProvider
{
    public string Name => "fake";

    public Task<AiResponse> GenerateAsync(
        AiRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        var content = request.OutputFormat switch
        {
            AiOutputFormat.Text => CreateTextResponse(request),
            AiOutputFormat.Json => CreateJsonResponse(),
            _ => throw new AiProviderException($"Unsupported AI output format: {request.OutputFormat}")
        };

        var usage = new AiUsage(
            InputTokens: EstimateTokens(request.Messages.Sum(message => message.Content.Length)),
            OutputTokens: EstimateTokens(content.Length),
            TotalTokens: null);

        return Task.FromResult(AiResponse.Success(
            content,
            Name,
            request.Options.Model,
            usage));
    }

    private static string CreateTextResponse(AiRequest request)
    {
        var userMessage = request.Messages.LastOrDefault(message => message.Role == AiRole.User)?.Content ?? string.Empty;

        return $"Fake AI response for: {userMessage}";
    }

    private static string CreateJsonResponse()
    {
        return """
        {
          "provider": "fake",
          "status": "ok",
          "message": "Fake AI JSON response"
        }
        """.Trim();
    }

    private static int EstimateTokens(int characters)
    {
        return Math.Max(1, characters / 4);
    }
}
