using System.Net;
using System.Text;
using System.Text.Json;
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

namespace AiReliabilityEngineering.Infrastructure.Tests.Ai.OpenAi;

public sealed class OpenAiProviderTests
{
    [Fact]
    public async Task GenerateAsync_WithMissingApiKeyReturnsFailureWithoutSendingRequest()
    {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var provider = CreateProvider(handler, _ => null);

        var response = await provider.GenerateAsync(CreateRequest(), CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Contains("OPENAI_API_KEY environment variable is not set", response.ErrorMessage);
        Assert.Equal("openai", response.Provider);
        Assert.Equal("test-model", response.Model);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task GenerateAsync_SendsExpectedResponsesApiRequest()
    {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(
            """
            {
              "output_text": "hello"
            }
            """));
        var provider = CreateProvider(handler);

        var response = await provider.GenerateAsync(CreateRequest(), CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(1, handler.RequestCount);
        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("/v1/responses", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("fake-secret", handler.Request.Headers.Authorization.Parameter);

        using var document = JsonDocument.Parse(handler.RequestBody);
        var root = document.RootElement;
        Assert.Equal("test-model", root.GetProperty("model").GetString());
        var input = root.GetProperty("input").GetString();
        Assert.Contains("[SYSTEM]", input);
        Assert.Contains("System prompt", input);
        Assert.Contains("[USER]", input);
        Assert.Contains("User prompt", input);
    }

    [Fact]
    public async Task GenerateAsync_WithSuccessfulResponseMapsTextAndUsage()
    {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(
            """
            {
              "output_text": "hello from openai",
              "usage": {
                "input_tokens": 10,
                "output_tokens": 5,
                "total_tokens": 15
              }
            }
            """));
        var provider = CreateProvider(handler);

        var response = await provider.GenerateAsync(CreateRequest(), CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal("hello from openai", response.Content);
        Assert.Equal("openai", response.Provider);
        Assert.Equal("test-model", response.Model);
        Assert.NotNull(response.Usage);
        Assert.Equal(10, response.Usage.InputTokens);
        Assert.Equal(5, response.Usage.OutputTokens);
        Assert.Equal(15, response.Usage.TotalTokens);
    }

    [Fact]
    public async Task GenerateAsync_WithNonSuccessHttpResponseReturnsFailure()
    {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(
            """{ "error": { "message": "bad request" } }""",
            HttpStatusCode.BadRequest));
        var provider = CreateProvider(handler);

        var response = await provider.GenerateAsync(CreateRequest(), CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Contains("400", response.ErrorMessage);
        Assert.Contains("bad request", response.ErrorMessage);
        Assert.DoesNotContain("fake-secret", response.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidJsonReturnsFailure()
    {
        var handler = new RecordingHttpMessageHandler(_ => TextResponse("not json"));
        var provider = CreateProvider(handler);

        var response = await provider.GenerateAsync(CreateRequest(), CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Contains("Failed to parse OpenAI response JSON", response.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_WithMissingOutputTextReturnsFailure()
    {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("{}"));
        var provider = CreateProvider(handler);

        var response = await provider.GenerateAsync(CreateRequest(), CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Contains("OpenAI response did not contain output text", response.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_WithCanceledTokenThrowsAtStart()
    {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("{}"));
        var provider = CreateProvider(handler);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => provider.GenerateAsync(CreateRequest(), source.Token));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task GenerateAsync_WithNullRequestThrows()
    {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("{}"));
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.GenerateAsync(null!, CancellationToken.None));
    }

    private static OpenAiProvider CreateProvider(
        RecordingHttpMessageHandler handler,
        Func<string, string?>? environmentVariableReader = null)
    {
        var httpClient = new HttpClient(handler);
        var options = new OpenAiProviderOptions(
            "test-model",
            endpoint: new Uri("https://api.openai.com/v1/responses"));
        return new OpenAiProvider(httpClient, options, environmentVariableReader ?? (_ => "fake-secret"));
    }

    private static AiRequest CreateRequest()
        => AiRequest.FromPrompts(
            "System prompt",
            "User prompt",
            AiOutputFormat.Text,
            AiProviderOptions.DefaultFake);

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage TextResponse(string text)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(text, Encoding.UTF8, "text/plain")
        };

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Request = request;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}
