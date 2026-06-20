using AiReliabilityEngineering.Core.Documentation;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Orchestration.Agents;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class DocumentationArtifactWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesReadmeAndPlan()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new DocumentationArtifactWriter();
        var documentation = new ProjectDocumentation("# README", "# PLAN");

        var artifacts = await writer.WriteAsync(documentation, workspace.RunContext, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "README.md")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "PLAN.md")));
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "artifacts/README.md");
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "artifacts/PLAN.md");
    }

    [Fact]
    public async Task WriteAsync_ContentMatchesDocumentation()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new DocumentationArtifactWriter();
        var documentation = new ProjectDocumentation("# README\n\nContent", "# PLAN\n\nContent");

        await writer.WriteAsync(documentation, workspace.RunContext, CancellationToken.None);

        Assert.Equal(
            documentation.ReadmeMarkdown,
            await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "README.md")));
        Assert.Equal(
            documentation.PlanMarkdown,
            await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "PLAN.md")));
    }

    [Fact]
    public async Task WriteAsync_UsesArtifactsDirectory()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new DocumentationArtifactWriter();

        await writer.WriteAsync(new ProjectDocumentation("# README", "# PLAN"), workspace.RunContext, CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(workspace.RunContext.Paths.RootDirectory, "README.md")));
        Assert.False(File.Exists(Path.Combine(workspace.RunContext.Paths.RootDirectory, "PLAN.md")));
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

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
