using System.Text.Json;
using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class AiRequirementsAgentTests
{
    [Fact]
    public async Task ExecuteAsync_CallsAiProvider()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteCopiedIdeaAsync("# Redis TTL Audit Tool\n\nScan Redis keys.", CancellationToken.None);
        var provider = new RecordingAiProvider();
        var agent = new AiRequirementsAgent(provider, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(provider.WasCalled);
        Assert.NotNull(provider.LastRequest);
        Assert.Contains(provider.LastRequest.Messages, message => message.Role == AiRole.System);
        Assert.Contains(provider.LastRequest.Messages, message => message.Role == AiRole.User && message.Content.Contains("Scan Redis keys."));
    }

    [Fact]
    public async Task ExecuteAsync_WritesRequirementArtifacts()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteCopiedIdeaAsync("# Redis TTL Audit Tool", CancellationToken.None);
        var agent = new AiRequirementsAgent(new RecordingAiProvider(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "specification.json")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "requirements.md")));
        Assert.Contains(result.Artifacts, artifact => artifact.RelativePath == "artifacts/specification.json");
        Assert.Contains(result.Artifacts, artifact => artifact.RelativePath == "artifacts/requirements.md");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedSpecificationIsValidJson()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteCopiedIdeaAsync("# Redis TTL Audit Tool", CancellationToken.None);
        var agent = new AiRequirementsAgent(new RecordingAiProvider(), new InMemoryRunLogger());

        await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        await using var stream = File.OpenRead(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "specification.json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);
        Assert.True(document.RootElement.TryGetProperty("projectName", out _));
        Assert.True(document.RootElement.TryGetProperty("summary", out _));
        Assert.True(document.RootElement.TryGetProperty("goals", out var goals));
        Assert.Equal(JsonValueKind.Array, goals.ValueKind);
    }

    [Fact]
    public async Task ExecuteAsync_MissingCopiedIdeaFileReturnsFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        var agent = new AiRequirementsAgent(new RecordingAiProvider(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Copied idea file not found", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ProviderFailureReturnsAgentFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteCopiedIdeaAsync("# Redis TTL Audit Tool", CancellationToken.None);
        var agent = new AiRequirementsAgent(new FailingAiProvider(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("provider failed", result.Message);
        Assert.Empty(result.Artifacts);
        Assert.False(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "specification.json")));
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyCanceledTokenThrows()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteCopiedIdeaAsync("# Redis TTL Audit Tool", CancellationToken.None);
        var agent = new AiRequirementsAgent(new RecordingAiProvider(), new InMemoryRunLogger());
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            agent.ExecuteAsync(workspace.CreateAgentContext(), source.Token));
    }

    private sealed class RecordingAiProvider : IAiProvider
    {
        public string Name => "recording";

        public bool WasCalled { get; private set; }

        public AiRequest? LastRequest { get; private set; }

        public Task<AiResponse> GenerateAsync(
            AiRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WasCalled = true;
            LastRequest = request;

            return Task.FromResult(AiResponse.Success(
                "recorded",
                Name,
                request.Options.Model));
        }
    }

    private sealed class FailingAiProvider : IAiProvider
    {
        public string Name => "failing";

        public Task<AiResponse> GenerateAsync(
            AiRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(AiResponse.Failure(
                "provider failed",
                Name,
                request.Options.Model));
        }
    }

    private sealed class InMemoryRunLogger : IRunLogger
    {
        public Task InfoAsync(string message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ErrorAsync(string message, Exception? exception, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestRunWorkspace : IDisposable
    {
        private TestRunWorkspace(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            var paths = new RunPaths(
                rootDirectory,
                Path.Combine(rootDirectory, "input"),
                Path.Combine(rootDirectory, "workspace"),
                Path.Combine(rootDirectory, "artifacts"),
                Path.Combine(rootDirectory, "reports"),
                Path.Combine(rootDirectory, "logs"),
                Path.Combine(rootDirectory, "run-state.json"));
            Directory.CreateDirectory(paths.InputDirectory);
            Directory.CreateDirectory(paths.ArtifactsDirectory);
            RunContext = new RunContext(
                new RunId("test-run"),
                Path.Combine(rootDirectory, "original-idea.md"),
                Path.Combine(paths.InputDirectory, "idea.md"),
                paths,
                DateTimeOffset.UnixEpoch);
        }

        public string RootDirectory { get; }

        public RunContext RunContext { get; }

        public static TestRunWorkspace Create()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "aire-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return new TestRunWorkspace(rootDirectory);
        }

        public AgentContext CreateAgentContext() => new(RunContext, new Dictionary<string, string>());

        public Task WriteCopiedIdeaAsync(string content, CancellationToken cancellationToken)
            => File.WriteAllTextAsync(RunContext.CopiedIdeaFilePath, content, cancellationToken);

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
