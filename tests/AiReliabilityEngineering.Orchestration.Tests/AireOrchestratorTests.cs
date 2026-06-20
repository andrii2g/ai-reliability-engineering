using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Tools;
using AiReliabilityEngineering.Core.Workflow;
using AiReliabilityEngineering.Infrastructure.Ai;
using AiReliabilityEngineering.Infrastructure.Logging;
using AiReliabilityEngineering.Infrastructure.Serialization;
using AiReliabilityEngineering.Orchestration;
using AiReliabilityEngineering.Orchestration.Pipeline;
using AiReliabilityEngineering.Orchestration.RunManagement;
using System.Text.Json;

namespace AiReliabilityEngineering.Orchestration.Tests;

public sealed class AireOrchestratorTests
{
    [Fact]
    public async Task RunAsync_WithValidIdeaFile_CreatesRunDirectory()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), TestContext.Current.CancellationToken);

        Assert.True(Directory.Exists(result.RunDirectory));
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_CopiesInputFileToInputIdeaMd()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(result.RunDirectory!, "input", "idea.md")));
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_WritesRunStateJson()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), TestContext.Current.CancellationToken);

        var statePath = Path.Combine(result.RunDirectory!, "run-state.json");
        Assert.True(File.Exists(statePath));
        Assert.Contains("\"status\": \"Completed\"", await File.ReadAllTextAsync(statePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_WritesCompletedStateWithSixSuccessfulSteps()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), TestContext.Current.CancellationToken);
        var statePath = Path.Combine(result.RunDirectory!, "run-state.json");
        await using var stream = File.OpenRead(statePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
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
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), TestContext.Current.CancellationToken);
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
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), TestContext.Current.CancellationToken);

        var logPath = Path.Combine(result.RunDirectory!, "logs", "orchestrator.log");
        Assert.True(File.Exists(logPath));
        Assert.Contains("RunCompleted", await File.ReadAllTextAsync(logPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_WithValidIdeaFile_ReturnsSuccess()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains("Completed", result.Message);
    }

    [Fact]
    public async Task RunAsync_WithAiRequirementsProfile_CompletesAndWritesRequirementsArtifacts()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(WorkflowProfile.AiRequirements), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(Path.Combine(result.RunDirectory!, "artifacts", "requirements.md")));
        var specificationPath = Path.Combine(result.RunDirectory!, "artifacts", "specification.json");
        Assert.True(File.Exists(specificationPath));
        await using var stream = File.OpenRead(specificationPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(document.RootElement.TryGetProperty("projectName", out _));
    }

    [Fact]
    public async Task RunAsync_WithAiRequirementsProfile_RecordsAiRequirementsAgent()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(WorkflowProfile.AiRequirements), TestContext.Current.CancellationToken);

        var statePath = Path.Combine(result.RunDirectory!, "run-state.json");
        await using var stream = File.OpenRead(statePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var firstStep = document.RootElement.GetProperty("steps")[0];

        Assert.Equal("Requirements", firstStep.GetProperty("step").GetString());
        Assert.Equal("AiRequirementsAgent", firstStep.GetProperty("agentName").GetString());
        Assert.Equal("Succeeded", firstStep.GetProperty("status").GetString());
    }

    [Fact]
    public async Task RunAsync_WithAiDemoProfile_CompletesAndWritesAiArtifacts()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(WorkflowProfile.AiDemo), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains("Completed", result.Message);
        Assert.True(File.Exists(Path.Combine(result.RunDirectory!, "artifacts", "specification.json")));
        Assert.True(File.Exists(Path.Combine(result.RunDirectory!, "artifacts", "requirements.md")));
        Assert.True(File.Exists(Path.Combine(result.RunDirectory!, "artifacts", "README.md")));
        Assert.True(File.Exists(Path.Combine(result.RunDirectory!, "artifacts", "PLAN.md")));
        Assert.True(File.Exists(Path.Combine(result.RunDirectory!, "artifacts", "tasks.json")));

        await using var tasksStream = File.OpenRead(Path.Combine(result.RunDirectory!, "artifacts", "tasks.json"));
        using var tasksDocument = await JsonDocument.ParseAsync(tasksStream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(tasksDocument.RootElement.TryGetProperty("tasks", out _));

        await using var stateStream = File.OpenRead(Path.Combine(result.RunDirectory!, "run-state.json"));
        using var stateDocument = await JsonDocument.ParseAsync(stateStream, cancellationToken: TestContext.Current.CancellationToken);
        var steps = stateDocument.RootElement.GetProperty("steps");
        Assert.Equal("Completed", stateDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal("AiRequirementsAgent", steps[0].GetProperty("agentName").GetString());
        Assert.Equal("AiDocumentationAgent", steps[1].GetProperty("agentName").GetString());
        Assert.Equal("AiPlannerAgent", steps[2].GetProperty("agentName").GetString());
    }

    [Fact]
    public async Task RunAsync_WithAiDemoDotnetProfile_CompletesAndWritesWorkspaceAndReports()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(WorkflowProfile.AiDemoDotnet), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains("Completed", result.Message);

        string[] expectedFiles =
        [
            "artifacts/specification.json",
            "artifacts/requirements.md",
            "artifacts/README.md",
            "artifacts/PLAN.md",
            "artifacts/tasks.json",
            "workspace/Directory.Packages.props",
            "workspace/GeneratedTool.slnx",
            "workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj",
            "workspace/src/GeneratedTool.Cli/Program.cs",
            "workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj",
            "workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs",
            "reports/build.md",
            "reports/tests.md"
        ];

        foreach (var expectedFile in expectedFiles)
        {
            Assert.True(File.Exists(Path.Combine(result.RunDirectory!, expectedFile)), expectedFile);
        }

        await using var stateStream = File.OpenRead(Path.Combine(result.RunDirectory!, "run-state.json"));
        using var stateDocument = await JsonDocument.ParseAsync(stateStream, cancellationToken: TestContext.Current.CancellationToken);
        var steps = stateDocument.RootElement.GetProperty("steps");
        Assert.Equal("Completed", stateDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal("AiRequirementsAgent", steps[0].GetProperty("agentName").GetString());
        Assert.Equal("AiDocumentationAgent", steps[1].GetProperty("agentName").GetString());
        Assert.Equal("AiPlannerAgent", steps[2].GetProperty("agentName").GetString());
        Assert.Equal("TemplateCodeAgent", steps[3].GetProperty("agentName").GetString());
        Assert.Equal("BuildTestAgent", steps[4].GetProperty("agentName").GetString());
    }

    [Fact]
    public async Task RunAsync_WithAiDemoDotnetReviewProfile_CompletesAndWritesWorkspaceReportsAndFinalReview()
    {
        using var test = TestWorkspace.Create();
        var result = await test.Orchestrator.RunAsync(test.CreateRequest(WorkflowProfile.AiDemoDotnetReview), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains("Completed", result.Message);

        string[] expectedFiles =
        [
            "artifacts/specification.json",
            "artifacts/requirements.md",
            "artifacts/README.md",
            "artifacts/PLAN.md",
            "artifacts/tasks.json",
            "workspace/Directory.Packages.props",
            "workspace/GeneratedTool.slnx",
            "workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj",
            "workspace/src/GeneratedTool.Cli/Program.cs",
            "workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj",
            "workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs",
            "reports/build.md",
            "reports/tests.md",
            "reports/final-review.md",
            "reports/workspace-summary.md"
        ];

        foreach (var expectedFile in expectedFiles)
        {
            Assert.True(File.Exists(Path.Combine(result.RunDirectory!, expectedFile)), expectedFile);
        }

        await using var stateStream = File.OpenRead(Path.Combine(result.RunDirectory!, "run-state.json"));
        using var stateDocument = await JsonDocument.ParseAsync(stateStream, cancellationToken: TestContext.Current.CancellationToken);
        var steps = stateDocument.RootElement.GetProperty("steps");
        Assert.Equal("Completed", stateDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal("AiRequirementsAgent", steps[0].GetProperty("agentName").GetString());
        Assert.Equal("AiDocumentationAgent", steps[1].GetProperty("agentName").GetString());
        Assert.Equal("AiPlannerAgent", steps[2].GetProperty("agentName").GetString());
        Assert.Equal("TemplateCodeAgent", steps[3].GetProperty("agentName").GetString());
        Assert.Equal("BuildTestAgent", steps[4].GetProperty("agentName").GetString());
        Assert.Equal("ArtifactReviewAgent", steps[5].GetProperty("agentName").GetString());
    }

    [Fact]
    public async Task RunAsync_WithAiRequirementsProfilePassesProviderSelectionToPipelineFactory()
    {
        AiProviderSelection? receivedSelection = null;
        using var test = TestWorkspace.Create(selection => receivedSelection = selection);
        var expectedSelection = new AiProviderSelection(AiProviderKind.OpenAi, "test-model");

        var result = await test.Orchestrator.RunAsync(
            test.CreateRequest(WorkflowProfile.AiRequirements, expectedSelection),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Same(expectedSelection, receivedSelection);
    }

    [Fact]
    public async Task RunAsync_WithMissingIdeaFile_ReturnsFailureWithoutRunId()
    {
        using var test = TestWorkspace.Create();

        var result = await test.Orchestrator.RunAsync(new RunRequest(Path.Combine(test.RootDirectory, "missing.md"), test.RunsDirectory), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Null(result.RunId);
        Assert.Contains("Idea file not found", result.Message);
    }

    [Fact]
    public async Task RunAsync_WithMissingRunsDirectory_ReturnsFailureWithoutRunId()
    {
        using var test = TestWorkspace.Create();

        var result = await test.Orchestrator.RunAsync(new RunRequest(test.IdeaFilePath, " "), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Null(result.RunId);
        Assert.Contains("Runs directory is required", result.Message);
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(
            string rootDirectory,
            Action<AiProviderSelection>? onProviderSelection = null)
        {
            RootDirectory = rootDirectory;
            RunsDirectory = Path.Combine(rootDirectory, "runs");
            IdeaFilePath = Path.Combine(rootDirectory, "idea.md");
            File.WriteAllText(IdeaFilePath, "# Idea\n\nCreate a test app.\n");
            Orchestrator = new AireOrchestrator(
                runContext => new CompositeRunLogger([new FileRunLogger(Path.Combine(runContext.Paths.LogsDirectory, "orchestrator.log"))]),
                runContext => new JsonRunStateStore(runContext.Paths.StateFilePath),
                new AgentPipelineFactory(selection =>
                {
                    onProviderSelection?.Invoke(selection);
                    return new FakeAiProvider();
                }, () => new SuccessfulToolExecutor()));
        }

        public string RootDirectory { get; }

        public string RunsDirectory { get; }

        public string IdeaFilePath { get; }

        public AireOrchestrator Orchestrator { get; }

        public static TestWorkspace Create(Action<AiProviderSelection>? onProviderSelection = null)
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "aire-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return new TestWorkspace(rootDirectory, onProviderSelection);
        }

        public RunRequest CreateRequest(
            WorkflowProfile profile = WorkflowProfile.Fake,
            AiProviderSelection? aiProviderSelection = null)
            => new(IdeaFilePath, RunsDirectory, profile, aiProviderSelection);

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }

    private sealed class SuccessfulToolExecutor : IToolExecutor
    {
        public Task<ToolExecutionResult> ExecuteAsync(
            ToolExecutionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new ToolExecutionResult(0, "ok", string.Empty, now, now));
        }
    }
}
