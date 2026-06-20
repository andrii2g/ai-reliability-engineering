using System.Text.Json;
using AiReliabilityEngineering.Core.Planning;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Orchestration.Agents;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class PlannerArtifactWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesTasksJson()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new PlannerArtifactWriter();

        var artifacts = await writer.WriteAsync(CreatePlan(), workspace.RunContext, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "tasks.json")));
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "artifacts/tasks.json");
    }

    [Fact]
    public async Task WriteAsync_WritesExpectedJsonShape()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new PlannerArtifactWriter();

        await writer.WriteAsync(CreatePlan(), workspace.RunContext, CancellationToken.None);

        await using var stream = File.OpenRead(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "tasks.json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);
        var task = document.RootElement.GetProperty("tasks")[0];
        Assert.Equal("T001", task.GetProperty("id").GetString());
        Assert.Equal("Create skeleton", task.GetProperty("title").GetString());
        Assert.Equal("Create the first implementation skeleton.", task.GetProperty("description").GetString());
        Assert.Equal("Build passes", task.GetProperty("acceptanceCriteria")[0].GetString());
    }

    [Fact]
    public async Task WriteAsync_UsesArtifactsDirectory()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new PlannerArtifactWriter();

        await writer.WriteAsync(CreatePlan(), workspace.RunContext, CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(workspace.RunContext.Paths.RootDirectory, "tasks.json")));
    }

    private static ImplementationPlan CreatePlan()
        => new(
            [
                new ImplementationTask(
                    "T001",
                    "Create skeleton",
                    "Create the first implementation skeleton.",
                    ["Build passes"])
            ]);

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

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
