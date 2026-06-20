using System.Text.Json;
using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Infrastructure.Ai;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class AiPlannerAgentTests
{
    [Fact]
    public async Task ExecuteAsync_CallsProviderWithJsonRequestAndSpecificationContent()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(CancellationToken.None);
        var provider = new RecordingAiProvider(ValidPlanningJson);
        var agent = new AiPlannerAgent(provider, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(provider.WasCalled);
        Assert.NotNull(provider.LastRequest);
        Assert.Equal(AiOutputFormat.Json, provider.LastRequest.OutputFormat);
        Assert.Contains(provider.LastRequest.Messages, message => message.Content.Contains("Redis TTL Audit Tool"));
    }

    [Fact]
    public async Task ExecuteAsync_WritesTasksJson()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(CancellationToken.None);
        var agent = new AiPlannerAgent(new RecordingAiProvider(ValidPlanningJson), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "tasks.json")));
        Assert.Contains(result.Artifacts, artifact => artifact.RelativePath == "artifacts/tasks.json");
    }

    [Fact]
    public async Task ExecuteAsync_ProviderFailureReturnsAgentFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(CancellationToken.None);
        var agent = new AiPlannerAgent(new FailingAiProvider(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("provider failed", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJsonProviderResponseReturnsFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(CancellationToken.None);
        var agent = new AiPlannerAgent(new RecordingAiProvider("not json"), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Planning response JSON is invalid", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpecificationReturnsFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        var agent = new AiPlannerAgent(new RecordingAiProvider(ValidPlanningJson), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Project specification artifact was not found", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFakeProviderGeneratesDeterministicTasks()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(CancellationToken.None);
        var agent = new AiPlannerAgent(new FakeAiProvider(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await using var stream = File.OpenRead(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "tasks.json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);
        Assert.Equal("T001", document.RootElement.GetProperty("tasks")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyCanceledTokenThrows()
    {
        using var workspace = TestRunWorkspace.Create();
        await workspace.WriteSpecificationAsync(CancellationToken.None);
        var agent = new AiPlannerAgent(new RecordingAiProvider(ValidPlanningJson), new InMemoryRunLogger());
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            agent.ExecuteAsync(workspace.CreateAgentContext(), source.Token));
    }

    private const string ValidPlanningJson = """
        {
          "tasks": [
            {
              "id": "T001",
              "title": "Create skeleton",
              "description": "Create the first implementation skeleton.",
              "acceptanceCriteria": [
                "Build passes",
                "Tests pass"
              ]
            }
          ]
        }
        """;

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
