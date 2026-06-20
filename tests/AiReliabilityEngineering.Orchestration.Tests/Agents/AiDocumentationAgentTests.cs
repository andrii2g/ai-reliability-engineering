using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Infrastructure.Ai;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class AiDocumentationAgentTests
{
    [Fact]
    public async Task ExecuteAsync_CallsProviderWithSpecificationContent()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(TestContext.Current.CancellationToken);
        var provider = new RecordingAiProvider("""
            ---README---
            # Test README

            Generated README.

            ---PLAN---
            # Test PLAN

            Generated plan.
            """);
        var agent = new AiDocumentationAgent(provider, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(provider.WasCalled);
        Assert.NotNull(provider.LastRequest);
        Assert.Contains(provider.LastRequest.Messages, message => message.Content.Contains("Redis TTL Audit Tool"));
    }

    [Fact]
    public async Task ExecuteAsync_WritesReadmeAndPlanArtifacts()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(TestContext.Current.CancellationToken);
        var agent = new AiDocumentationAgent(new RecordingAiProvider("""
            ---README---
            # Test README

            ---PLAN---
            # Test PLAN
            """), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "README.md")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "PLAN.md")));
        Assert.Contains(result.Artifacts, artifact => artifact.RelativePath == "artifacts/README.md");
        Assert.Contains(result.Artifacts, artifact => artifact.RelativePath == "artifacts/PLAN.md");
    }

    [Fact]
    public async Task ExecuteAsync_ProviderFailureReturnsAgentFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(TestContext.Current.CancellationToken);
        var agent = new AiDocumentationAgent(new FailingAiProvider(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Contains("provider failed", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpecificationReturnsFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        var agent = new AiDocumentationAgent(new RecordingAiProvider("unused"), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Contains("Project specification artifact was not found", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidProviderResponseWithoutMarkersReturnsFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(TestContext.Current.CancellationToken);
        var agent = new AiDocumentationAgent(new RecordingAiProvider("plain markdown"), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Contains("required README and PLAN markers", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFakeProviderGeneratesDeterministicDocumentation()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(TestContext.Current.CancellationToken);
        var agent = new AiDocumentationAgent(new FakeAiProvider(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var readme = await File.ReadAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "README.md"),
            TestContext.Current.CancellationToken);
        var plan = await File.ReadAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "PLAN.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains("# Redis TTL Audit Tool", readme);
        Assert.Contains("# Plan: Redis TTL Audit Tool", plan);
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyCanceledTokenThrows()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(TestContext.Current.CancellationToken);
        var agent = new AiDocumentationAgent(new RecordingAiProvider("unused"), new InMemoryRunLogger());
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            agent.ExecuteAsync(workspace.CreateAgentContext(), source.Token));
    }

    private sealed class RecordingAiProvider(string content) : IAiProvider
    {
        public string Name => "recording";

        public bool WasCalled { get; private set; }

        public AiRequest? LastRequest { get; private set; }

        public Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;
            LastRequest = request;
            return Task.FromResult(AiResponse.Success(content, Name, request.Options.Model));
        }
    }

    private sealed class FailingAiProvider : IAiProvider
    {
        public string Name => "failing";

        public Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken)
            => Task.FromResult(AiResponse.Failure("provider failed", Name, request.Options.Model));
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

        public Task WriteSpecificationAsync(CancellationToken cancellationToken)
            => new RequirementsArtifactWriter().WriteAsync(
                new ProjectSpecification(
                    "Redis TTL Audit Tool",
                    "Scan Redis keys.",
                    ["Find keys without TTL"],
                    ["Do not modify keys"],
                    ["Generate a report"],
                    ["Report is generated"]),
                RunContext,
                cancellationToken);

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
