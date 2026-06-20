using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Review;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class ArtifactReviewAgentTests
{
    [Fact]
    public void RequiredArtifactChecker_ReturnsAllExpectedChecksAndMarksExistingFiles()
    {
        using var workspace = TestRunWorkspace.Create();
        workspace.WriteRequiredFiles();
        var checker = new RequiredArtifactChecker();

        var checks = checker.Check(workspace.RunContext);

        Assert.Equal(13, checks.Count);
        Assert.All(checks, check => Assert.True(check.Exists, check.RelativePath));
        Assert.Contains(checks, check => check.Category == "artifact");
        Assert.Contains(checks, check => check.Category == "workspace");
        Assert.Contains(checks, check => check.Category == "report");
        Assert.All(checks, check => Assert.DoesNotContain('\\', check.RelativePath));
    }

    [Fact]
    public void RequiredArtifactChecker_MarksMissingFilesWithoutThrowing()
    {
        using var workspace = TestRunWorkspace.Create();
        var checker = new RequiredArtifactChecker();

        var checks = checker.Check(workspace.RunContext);

        Assert.Equal(13, checks.Count);
        Assert.All(checks, check => Assert.False(check.Exists));
    }

    [Fact]
    public void WorkspaceSummaryBuilder_ReturnsEmptyListWhenWorkspaceIsMissing()
    {
        using var workspace = TestRunWorkspace.Create();
        var builder = new WorkspaceSummaryBuilder();

        var summary = builder.Build(workspace.RunContext);

        Assert.Empty(summary.Files);
    }

    [Fact]
    public void WorkspaceSummaryBuilder_ListsFilesRecursivelyAndExcludesBuildOutputs()
    {
        using var workspace = TestRunWorkspace.Create();
        workspace.WriteWorkspaceFile("src/GeneratedTool.Cli/Program.cs");
        workspace.WriteWorkspaceFile("tests/GeneratedTool.Cli.Tests/SmokeTests.cs");
        workspace.WriteWorkspaceFile("src/GeneratedTool.Cli/bin/Debug/output.dll");
        workspace.WriteWorkspaceFile("src/GeneratedTool.Cli/obj/project.assets.json");
        var builder = new WorkspaceSummaryBuilder();

        var summary = builder.Build(workspace.RunContext);

        Assert.Equal(
            ["src/GeneratedTool.Cli/Program.cs", "tests/GeneratedTool.Cli.Tests/SmokeTests.cs"],
            summary.Files);
    }

    [Fact]
    public async Task ReviewReportWriter_WritesReportsAndPreservesBuildReports()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.ReportsDirectory);
        await File.WriteAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "build.md"), "build", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "tests.md"), "tests", TestContext.Current.CancellationToken);
        var writer = new ReviewReportWriter();
        var result = new ArtifactReviewResult(
            [
                new RequiredArtifactCheck("artifacts/specification.json", true, "artifact"),
                new RequiredArtifactCheck("reports/build.md", false, "report")
            ],
            new WorkspaceSummary(workspace.RunContext.Paths.WorkspaceDirectory, ["src/Program.cs", "tests/SmokeTests.cs"]),
            ["Missing required report file: reports/build.md"]);

        var artifacts = await writer.WriteAsync(result, workspace.RunContext, TestContext.Current.CancellationToken);

        var finalReview = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "final-review.md"), TestContext.Current.CancellationToken);
        var workspaceSummary = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "workspace-summary.md"), TestContext.Current.CancellationToken);
        Assert.Contains("# Final Review", finalReview);
        Assert.Contains("## Required Artifacts", finalReview);
        Assert.Contains("| report | reports/build.md | Missing |", finalReview);
        Assert.Contains("Missing required report file: reports/build.md", finalReview);
        Assert.Contains("# Workspace Summary", workspaceSummary);
        Assert.Contains("## Workspace Root", workspaceSummary);
        Assert.Contains("- src/Program.cs", workspaceSummary);
        Assert.Equal("build", await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "build.md"), TestContext.Current.CancellationToken));
        Assert.Equal("tests", await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "tests.md"), TestContext.Current.CancellationToken));
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "reports/final-review.md");
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "reports/workspace-summary.md");
    }

    [Fact]
    public async Task ArtifactReviewAgent_SucceedsAndWritesReportsWhenRequiredFilesExist()
    {
        using var workspace = TestRunWorkspace.Create();
        workspace.WriteRequiredFiles();
        var agent = new ArtifactReviewAgent(new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "final-review.md")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "workspace-summary.md")));
        Assert.Contains(result.Artifacts, artifact => artifact.RelativePath == "reports/final-review.md");
    }

    [Fact]
    public async Task ArtifactReviewAgent_SucceedsAndWarnsWhenRequiredFilesAreMissing()
    {
        using var workspace = TestRunWorkspace.Create();
        workspace.WriteWorkspaceFile("src/GeneratedTool.Cli/Program.cs");
        var agent = new ArtifactReviewAgent(new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var finalReview = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "final-review.md"), TestContext.Current.CancellationToken);
        Assert.Contains("Missing required artifact file: artifacts/specification.json", finalReview);
        Assert.Contains("| artifacts/specification.json | Missing |", finalReview);
    }

    [Fact]
    public async Task ArtifactReviewAgent_PropagatesCancellation()
    {
        using var workspace = TestRunWorkspace.Create();
        var agent = new ArtifactReviewAgent(new InMemoryRunLogger());
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            agent.ExecuteAsync(workspace.CreateAgentContext(), source.Token));
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
            var rootDirectory = Path.Combine(Path.GetTempPath(), "aire-review-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return new TestRunWorkspace(rootDirectory);
        }

        public AgentContext CreateAgentContext() => new(RunContext, new Dictionary<string, string>());

        public void WriteRequiredFiles()
        {
            WriteRootFile("artifacts/specification.json");
            WriteRootFile("artifacts/requirements.md");
            WriteRootFile("artifacts/README.md");
            WriteRootFile("artifacts/PLAN.md");
            WriteRootFile("artifacts/tasks.json");
            WriteRootFile("workspace/Directory.Packages.props");
            WriteRootFile("workspace/GeneratedTool.slnx");
            WriteRootFile("workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj");
            WriteRootFile("workspace/src/GeneratedTool.Cli/Program.cs");
            WriteRootFile("workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj");
            WriteRootFile("workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs");
            WriteRootFile("reports/build.md");
            WriteRootFile("reports/tests.md");
        }

        public void WriteWorkspaceFile(string relativePath)
            => WriteFile(Path.Combine(RunContext.Paths.WorkspaceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private void WriteRootFile(string relativePath)
            => WriteFile(Path.Combine(RootDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static void WriteFile(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "content");
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
