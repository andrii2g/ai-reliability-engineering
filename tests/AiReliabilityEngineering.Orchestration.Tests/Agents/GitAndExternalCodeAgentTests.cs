using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.CodeExecution;
using AiReliabilityEngineering.Core.Documentation;
using AiReliabilityEngineering.Core.Git;
using AiReliabilityEngineering.Core.Planning;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Core.Tools;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class GitAndExternalCodeAgentTests
{
    [Theory]
    [InlineData("src/App/bin/Debug/app.dll")]
    [InlineData("src/App/obj/project.assets.json")]
    [InlineData(".git/config")]
    [InlineData(".vs/state.json")]
    [InlineData(".idea/workspace.xml")]
    public void TransientWorkspacePathFilter_MatchesTransientSegments(string path)
    {
        Assert.True(TransientWorkspacePathFilter.IsTransient(path));
    }

    [Fact]
    public void GitStatusParser_ParsesCommonStatusLines()
    {
        var entries = new GitStatusParser().Parse(
            """
             M src/App/Program.cs
            ?? tests/App.Tests/SmokeTests.cs
            A  README.md
            R  old.txt -> new.txt
            """);

        Assert.Equal(["src/App/Program.cs", "tests/App.Tests/SmokeTests.cs", "README.md", "new.txt"], entries.Select(entry => entry.Path));
        Assert.Equal(["M", "??", "A", "R"], entries.Select(entry => entry.Status));
    }

    [Fact]
    public void GeneratedFilesReporter_ReturnsSortedRelativeFilesAndExcludesTransientPaths()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "src", "App", "bin"));
        Directory.CreateDirectory(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "src", "App"));
        File.WriteAllText(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "z.txt"), "z");
        File.WriteAllText(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "src", "App", "a.txt"), "a");
        File.WriteAllText(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "src", "App", "bin", "app.dll"), "dll");

        var report = new GeneratedFilesReporter().Create(workspace.RunContext);

        Assert.Equal(["src/App/a.txt", "z.txt"], report.Files.Select(file => file.RelativePath));
    }

    [Fact]
    public async Task GitSnapshotReportWriter_WritesMarkdownAndJson()
    {
        using var workspace = TestRunWorkspace.Create();
        var snapshot = new GitWorkspaceSnapshot(
            new GeneratedFilesReport([new GeneratedFileEntry("src/App/Program.cs", 10)]),
            [new GitStatusEntry("??", "src/App/Program.cs")]);

        var artifacts = await new GitSnapshotReportWriter().WriteAsync(
            snapshot,
            workspace.RunContext,
            null,
            TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "generated-files.md")));
        var json = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "generated-files.json"), TestContext.Current.CancellationToken);
        Assert.Contains("\"totalSizeBytes\"", json);
        Assert.Contains("reports/git-status.md", artifacts.Select(artifact => artifact.RelativePath));
    }

    [Fact]
    public async Task GitWorkspaceSnapshotAgent_WritesReportsAndFiltersTransientStatus()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "src", "App", "bin"));
        File.WriteAllText(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "src", "App", "bin", "skip.dll"), "skip");
        var executor = new RecordingToolExecutor([
            CreateToolResult(0, "initialized"),
            CreateToolResult(0, "?? keep.txt\n?? src/App/bin/skip.dll\n")
        ]);
        var agent = new GitWorkspaceSnapshotAgent(executor, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, executor.Requests.Count);
        Assert.Equal(["init"], executor.Requests[0].Arguments);
        Assert.Equal(["status", "--short"], executor.Requests[1].Arguments);
        var gitStatus = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "git-status.md"), TestContext.Current.CancellationToken);
        Assert.Contains("keep.txt", gitStatus);
        Assert.DoesNotContain("skip.dll", gitStatus);
    }

    [Fact]
    public async Task GitWorkspaceSnapshotAgent_SucceedsWhenToolThrows()
    {
        using var workspace = TestRunWorkspace.Create();
        var agent = new GitWorkspaceSnapshotAgent(new ThrowingToolExecutor(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var gitStatus = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "git-status.md"), TestContext.Current.CancellationToken);
        Assert.Contains("Git command failed", gitStatus);
    }

    [Fact]
    public async Task CodeExecutionPromptBuilder_IncludesArtifactsAndTruncatesLongSections()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.ArtifactsDirectory);
        await File.WriteAllTextAsync(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "specification.json"), new string('a', 7000), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "tasks.json"), "tasks", TestContext.Current.CancellationToken);

        var prompt = await new CodeExecutionPromptBuilder().BuildAsync(workspace.RunContext, TestContext.Current.CancellationToken);

        Assert.Contains("# AIRE Coding Task", prompt);
        Assert.Contains("[truncated]", prompt);
        Assert.Contains("Do not change the AIRE repository root.", prompt);
    }

    [Fact]
    public async Task CodeExecutionReportWriter_WritesReport()
    {
        using var workspace = TestRunWorkspace.Create();
        await new CodeExecutionReportWriter().WriteAsync(
            "fake",
            new CodeExecutionResult(false, 1, "out", "err", TimeSpan.FromSeconds(1)),
            workspace.RunContext,
            TestContext.Current.CancellationToken);

        var report = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "code-execution.md"), TestContext.Current.CancellationToken);
        Assert.Contains("Executor: `fake`", report);
        Assert.Contains("Exit code: `1`", report);
    }

    [Fact]
    public async Task ExternalCodeAgent_CreatesTemplatePromptAndCallsExecutor()
    {
        using var workspace = TestRunWorkspace.Create();
        await WriteSpecificationAndTasksAsync(workspace, TestContext.Current.CancellationToken);
        var executor = new RecordingCodeExecutor(new CodeExecutionResult(true, 0, "ok", string.Empty, TimeSpan.FromSeconds(1)));
        var agent = new ExternalCodeAgent(executor, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(executor.Request);
        Assert.Equal(workspace.RunContext.Paths.WorkspaceDirectory, executor.Request.WorkspaceDirectory);
        Assert.StartsWith(Path.GetFullPath(workspace.RunContext.Paths.WorkspaceDirectory), Path.GetFullPath(executor.Request.PromptFilePath), StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, ".aire", "code-execution-prompt.md")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "GeneratedTool.slnx")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "code-execution.md")));
    }

    [Fact]
    public async Task ExternalCodeAgent_ReturnsFailureOnExecutorFailure()
    {
        using var workspace = TestRunWorkspace.Create();
        await WriteSpecificationAndTasksAsync(workspace, TestContext.Current.CancellationToken);
        var agent = new ExternalCodeAgent(
            new RecordingCodeExecutor(new CodeExecutionResult(false, 1, string.Empty, "failed", TimeSpan.Zero)),
            new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "code-execution.md")));
    }

    [Fact]
    public async Task ExternalCodeAgent_CatchesExecutorExceptionAndWritesReport()
    {
        using var workspace = TestRunWorkspace.Create();
        await WriteSpecificationAndTasksAsync(workspace, TestContext.Current.CancellationToken);
        var agent = new ExternalCodeAgent(new ThrowingCodeExecutor(), new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        var report = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "code-execution.md"), TestContext.Current.CancellationToken);
        Assert.Contains("executor failed", report);
    }

    private static async Task WriteSpecificationAndTasksAsync(TestRunWorkspace workspace, CancellationToken cancellationToken)
    {
        var specification = new ProjectSpecification(
            "Redis TTL Audit Tool",
            "Audit Redis keys for missing TTL values.",
            ["Scan keys"],
            ["Do not modify Redis data"],
            ["Report missing TTL"],
            ["Missing TTL values are reported"]);
        await new RequirementsArtifactWriter().WriteAsync(specification, workspace.RunContext, cancellationToken);
        await new PlannerArtifactWriter().WriteAsync(
            new ImplementationPlan(
                [new ImplementationTask("T001", "Create CLI", "Add command-line entry point.", ["CLI command exists"])]),
            workspace.RunContext,
            cancellationToken);
        await new DocumentationArtifactWriter().WriteAsync(
            new ProjectDocumentation("# Redis TTL Audit Tool\n\nOverview", "# PLAN\n\n- Create CLI"),
            workspace.RunContext,
            cancellationToken);
    }

    private static ToolExecutionResult CreateToolResult(int exitCode, string standardOutput = "", string standardError = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new ToolExecutionResult(exitCode, standardOutput, standardError, now, now.AddMilliseconds(10));
    }

    private sealed class RecordingToolExecutor(Queue<ToolExecutionResult> results) : IToolExecutor
    {
        public RecordingToolExecutor(IEnumerable<ToolExecutionResult> results)
            : this(new Queue<ToolExecutionResult>(results))
        {
        }

        public List<ToolExecutionRequest> Requests { get; } = [];

        public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(results.Dequeue());
        }
    }

    private sealed class ThrowingToolExecutor : IToolExecutor
    {
        public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("git missing");
    }

    private sealed class RecordingCodeExecutor(CodeExecutionResult result) : ICodeExecutor
    {
        public string Name => "recording";

        public CodeExecutionRequest? Request { get; private set; }

        public Task<CodeExecutionResult> ExecuteAsync(CodeExecutionRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingCodeExecutor : ICodeExecutor
    {
        public string Name => "throwing";

        public Task<CodeExecutionResult> ExecuteAsync(CodeExecutionRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("executor failed");
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
            Directory.CreateDirectory(rootDirectory);
            RunContext = new RunContext(
                new RunId("test-run"),
                Path.Combine(rootDirectory, "idea.md"),
                Path.Combine(rootDirectory, "input", "idea.md"),
                new RunPaths(
                    rootDirectory,
                    Path.Combine(rootDirectory, "input"),
                    Path.Combine(rootDirectory, "workspace"),
                    Path.Combine(rootDirectory, "artifacts"),
                    Path.Combine(rootDirectory, "reports"),
                    Path.Combine(rootDirectory, "logs"),
                    Path.Combine(rootDirectory, "state.json")),
                DateTimeOffset.UtcNow);
        }

        public string RootDirectory { get; }

        public RunContext RunContext { get; }

        public static TestRunWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "aire-tests", Guid.NewGuid().ToString("N"));
            return new TestRunWorkspace(root);
        }

        public AgentContext CreateAgentContext() => new(RunContext, new Dictionary<string, string>());

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
