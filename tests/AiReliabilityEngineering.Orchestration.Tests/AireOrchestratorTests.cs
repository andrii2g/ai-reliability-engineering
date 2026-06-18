using AiReliabilityEngineering.Infrastructure.Logging;
using AiReliabilityEngineering.Infrastructure.Serialization;
using AiReliabilityEngineering.Orchestration;
using AiReliabilityEngineering.Orchestration.RunManagement;
using System.Text.Json;

namespace AiReliabilityEngineering.Orchestration.Tests;

public sealed class AireOrchestratorTests
{
    [Fact]
    public async Task RunAsync_WithValidIdeaFile_CreatesRunDirectory()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), CancellationToken.None);

        Assert.True(Directory.Exists(result.RunDirectory));
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_CopiesInputFileToInputIdeaMd()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(result.RunDirectory!, "input", "idea.md")));
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_WritesRunStateJson()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), CancellationToken.None);

        var statePath = Path.Combine(result.RunDirectory!, "run-state.json");
        Assert.True(File.Exists(statePath));
        Assert.Contains("\"status\": \"Completed\"", await File.ReadAllTextAsync(statePath, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_WritesCompletedStateWithSixSuccessfulSteps()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), CancellationToken.None);
        var statePath = Path.Combine(result.RunDirectory!, "run-state.json");
        await using var stream = File.OpenRead(statePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);
        var root = document.RootElement;

        Assert.Equal("Completed", root.GetProperty("status").GetString());
        var steps = root.GetProperty("steps");
        Assert.Equal(6, steps.GetArrayLength());

        string[] expectedSteps = ["Requirements", "Documentation", "Planning", "Code", "Testing", "Review"];
        for (var index = 0; index < expectedSteps.Length; index++)
        {
            Assert.Equal(expectedSteps[index], steps[index].GetProperty("step").GetString());
            Assert.Equal("Succeeded", steps[index].GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_ExecutesAllFakeAgentsAndCreatesArtifacts()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), CancellationToken.None);
        var root = result.RunDirectory!;

        string[] expectedFiles =
        [
            "input/idea.md",
            "run-state.json",
            "artifacts/specification.json",
            "artifacts/README.md",
            "artifacts/PLAN.md",
            "artifacts/tasks.json",
            "artifacts/review.md",
            "logs/orchestrator.log",
            "reports/tests.md",
            "workspace/README.md",
            "workspace/src/placeholder.txt"
        ];

        foreach (var expectedFile in expectedFiles)
        {
            Assert.True(File.Exists(Path.Combine(root, expectedFile)), expectedFile);
        }
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_WritesLogs()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), CancellationToken.None);

        var logPath = Path.Combine(result.RunDirectory!, "logs", "orchestrator.log");
        Assert.True(File.Exists(logPath));
        Assert.Contains("RunCompleted", await File.ReadAllTextAsync(logPath, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_ReturnsSuccess()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains("Completed", result.Message);
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            RunsDirectory = Path.Combine(rootDirectory, "runs");
            IdeaFilePath = Path.Combine(rootDirectory, "idea.md");
            File.WriteAllText(IdeaFilePath, "# Idea\n\nCreate a test app.\n");
            Orchestrator = new AireOrchestrator(
                runContext => new CompositeRunLogger([new FileRunLogger(Path.Combine(runContext.Paths.LogsDirectory, "orchestrator.log"))]),
                runContext => new JsonRunStateStore(runContext.Paths.StateFilePath));
        }

        public string RootDirectory { get; }

        public string RunsDirectory { get; }

        public string IdeaFilePath { get; }

        public AireOrchestrator Orchestrator { get; }

        public static TestWorkspace Create()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "aire-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return new TestWorkspace(rootDirectory);
        }

        public RunRequest CreateRequest() => new(IdeaFilePath, RunsDirectory);

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
