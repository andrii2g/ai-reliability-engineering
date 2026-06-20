using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Build;
using AiReliabilityEngineering.Core.Planning;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Core.Tools;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;
using System.Text.Json;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class TemplateBuildAgentTests
{
    [Fact]
    public async Task WorkspaceArtifactReader_ReadsSpecificationAndOptionalTasks()
    {
        using var workspace = TestRunWorkspace.Create();
        await WriteSpecificationAndTasksAsync(workspace, TestContext.Current.CancellationToken);
        var reader = new WorkspaceArtifactReader();

        var specificationJson = await reader.ReadSpecificationJsonAsync(workspace.RunContext, TestContext.Current.CancellationToken);
        var tasksJson = await reader.TryReadTasksJsonAsync(workspace.RunContext, TestContext.Current.CancellationToken);

        Assert.Contains("Redis TTL Audit Tool", specificationJson);
        Assert.NotNull(tasksJson);
        Assert.Contains("T001", tasksJson);
    }

    [Fact]
    public async Task WorkspaceArtifactReader_ReturnsNullWhenTasksAreMissing()
    {
        using var workspace = TestRunWorkspace.Create();
        await new RequirementsArtifactWriter().WriteAsync(CreateSpecification(), workspace.RunContext, TestContext.Current.CancellationToken);
        var reader = new WorkspaceArtifactReader();

        var tasksJson = await reader.TryReadTasksJsonAsync(workspace.RunContext, TestContext.Current.CancellationToken);

        Assert.Null(tasksJson);
    }

    [Fact]
    public async Task WorkspaceArtifactReader_ThrowsWhenSpecificationIsMissing()
    {
        using var workspace = TestRunWorkspace.Create();
        var reader = new WorkspaceArtifactReader();

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            reader.ReadSpecificationJsonAsync(workspace.RunContext, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DotnetTemplateProjectWriter_WritesDeterministicWorkspaceFiles()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new DotnetTemplateProjectWriter();

        var artifacts = await writer.WriteAsync(
            workspace.RunContext,
            """{ "projectName": "Redis TTL Audit Tool" }""",
            """{ "tasks": [] }""",
            TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "Directory.Packages.props")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "GeneratedTool.slnx")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "src", "GeneratedTool.Cli", "GeneratedTool.Cli.csproj")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "src", "GeneratedTool.Cli", "Program.cs")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "tests", "GeneratedTool.Cli.Tests", "GeneratedTool.Cli.Tests.csproj")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "tests", "GeneratedTool.Cli.Tests", "SmokeTests.cs")));
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "workspace/Directory.Packages.props");
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "workspace/GeneratedTool.slnx");

        var packages = await File.ReadAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "Directory.Packages.props"),
            TestContext.Current.CancellationToken);
        Assert.Contains("Microsoft.NET.Test.Sdk", packages);
        Assert.Contains("xunit.v3", packages);
        Assert.DoesNotContain("xunit.runner.visualstudio", packages);

        var solution = await File.ReadAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "GeneratedTool.slnx"),
            TestContext.Current.CancellationToken);
        Assert.Contains("src/GeneratedTool.Cli/GeneratedTool.Cli.csproj", solution);
        Assert.Contains("tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj", solution);

        var testProject = await File.ReadAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "tests", "GeneratedTool.Cli.Tests", "GeneratedTool.Cli.Tests.csproj"),
            TestContext.Current.CancellationToken);
        Assert.Contains("<PackageReference Include=\"xunit.v3\" />", testProject);
        Assert.Contains("<OutputType>Exe</OutputType>", testProject);
        Assert.Contains("<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>", testProject);
        Assert.Contains("<Using Include=\"Xunit\" />", testProject);

        var smokeTests = await File.ReadAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "tests", "GeneratedTool.Cli.Tests", "SmokeTests.cs"),
            TestContext.Current.CancellationToken);
        Assert.Contains("[Fact]", smokeTests);
        Assert.Contains("Assert.True(true)", smokeTests);
        Assert.False(File.Exists(Path.Combine(workspace.RunContext.Paths.RootDirectory, "GeneratedTool.slnx")));
    }

    [Fact]
    public async Task TemplateCodeAgent_SucceedsWhenSpecificationExists()
    {
        using var workspace = TestRunWorkspace.Create();
        await WriteSpecificationAndTasksAsync(workspace, TestContext.Current.CancellationToken);
        var agent = new TemplateCodeAgent(new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "GeneratedTool.slnx")));
        Assert.Contains(result.Artifacts, artifact => artifact.RelativePath == "workspace/Directory.Packages.props");
    }

    [Fact]
    public async Task TemplateCodeAgent_ReturnsFailureWhenSpecificationIsMissing()
    {
        using var workspace = TestRunWorkspace.Create();
        var agent = new TemplateCodeAgent(new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Contains("Project specification artifact was not found", result.Message);
    }

    [Fact]
    public async Task TemplateCodeAgent_PropagatesCancellation()
    {
        using var workspace = TestRunWorkspace.Create();
        await WriteSpecificationAndTasksAsync(workspace, TestContext.Current.CancellationToken);
        var agent = new TemplateCodeAgent(new InMemoryRunLogger());
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            agent.ExecuteAsync(workspace.CreateAgentContext(), source.Token));
    }

    [Fact]
    public async Task BuildTestReportWriter_WritesBuildAndTestReports()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new BuildTestReportWriter();

        var artifacts = await writer.WriteAsync(
            new BuildTestReport(
                new CommandReport("dotnet build", "workspace", 0, "build out", "build err", TimeSpan.FromSeconds(1)),
                new CommandReport("dotnet test", "workspace", 0, "test out", "test err", TimeSpan.FromSeconds(2))),
            workspace.RunContext,
            TestContext.Current.CancellationToken);

        var build = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "build.md"), TestContext.Current.CancellationToken);
        var tests = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "tests.md"), TestContext.Current.CancellationToken);
        Assert.Contains("dotnet build", build);
        Assert.Contains("Exit code: `0`", build);
        Assert.Contains("build out", build);
        Assert.Contains("test err", tests);
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "reports/build.md");
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "reports/tests.md");
    }

    [Fact]
    public async Task BuildTestReportWriter_ExplainsWhenTestsWereNotRun()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new BuildTestReportWriter();

        await writer.WriteAsync(
            new BuildTestReport(new CommandReport("dotnet build", "workspace", 1, null, "failed", TimeSpan.Zero), null),
            workspace.RunContext,
            TestContext.Current.CancellationToken);

        var tests = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "tests.md"), TestContext.Current.CancellationToken);
        Assert.Contains("Tests were not run because build failed.", tests);
    }

    [Fact]
    public async Task BuildTestAgent_RunsBuildThenTestsWithWorkspaceAndTimeout()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.WorkspaceDirectory);
        var executor = new RecordingToolExecutor([
            CreateToolResult(0, "build ok"),
            CreateToolResult(0, "test ok")
        ]);
        var agent = new BuildTestAgent(executor, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, executor.Requests.Count);
        Assert.Equal("dotnet", executor.Requests[0].Command);
        Assert.Equal(["build", "src/GeneratedTool.Cli/GeneratedTool.Cli.csproj"], executor.Requests[0].Arguments);
        Assert.Equal(["test", "tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj"], executor.Requests[1].Arguments);
        Assert.Equal(TimeSpan.FromMinutes(2), executor.Requests[0].Timeout);
        Assert.Equal(TimeSpan.FromMinutes(2), executor.Requests[1].Timeout);
        Assert.Equal(workspace.RunContext.Paths.WorkspaceDirectory, executor.Requests[0].WorkingDirectory);
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "build.md")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "tests.md")));
    }

    [Fact]
    public async Task BuildTestAgent_DoesNotRunTestsWhenBuildFails()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.WorkspaceDirectory);
        var executor = new RecordingToolExecutor([CreateToolResult(1, standardError: "build failed")]);
        var agent = new BuildTestAgent(executor, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Single(executor.Requests);
        var tests = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "tests.md"), TestContext.Current.CancellationToken);
        Assert.Contains("Tests were not run", tests);
    }

    [Fact]
    public async Task BuildTestAgent_ReturnsFailureWhenTestsFail()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.WorkspaceDirectory);
        var executor = new RecordingToolExecutor([
            CreateToolResult(0, "build ok"),
            CreateToolResult(1, standardError: "tests failed")
        ]);
        var agent = new BuildTestAgent(executor, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, executor.Requests.Count);
        var tests = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "tests.md"), TestContext.Current.CancellationToken);
        Assert.Contains("tests failed", tests);
    }

    [Fact]
    public async Task BuildTestAgent_ReportsTimeoutAsFailedCommand()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.WorkspaceDirectory);
        var executor = new RecordingToolExecutor([CreateToolResult(-1, standardError: "Timed out after 00:02:00.")]);
        var agent = new BuildTestAgent(executor, new InMemoryRunLogger());

        var result = await agent.ExecuteAsync(workspace.CreateAgentContext(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        var build = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ReportsDirectory, "build.md"), TestContext.Current.CancellationToken);
        Assert.Contains("Exit code: `-1`", build);
        Assert.Contains("Timed out after 00:02:00.", build);
    }

    [Fact]
    public async Task BuildTestAgent_PropagatesCancellation()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.WorkspaceDirectory);
        var agent = new BuildTestAgent(new RecordingToolExecutor([]), new InMemoryRunLogger());
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            agent.ExecuteAsync(workspace.CreateAgentContext(), source.Token));
    }

    private static async Task WriteSpecificationAndTasksAsync(
        TestRunWorkspace workspace,
        CancellationToken cancellationToken)
    {
        await new RequirementsArtifactWriter().WriteAsync(CreateSpecification(), workspace.RunContext, cancellationToken);
        await new PlannerArtifactWriter().WriteAsync(
            new ImplementationPlan(
                [
                    new ImplementationTask(
                        "T001",
                        "Create skeleton",
                        "Create the generated skeleton.",
                        ["Build passes"])
                ]),
            workspace.RunContext,
            cancellationToken);
    }

    private static ProjectSpecification CreateSpecification()
        => new(
            "Redis TTL Audit Tool",
            "Scan Redis keys.",
            ["Find keys without TTL"],
            ["Do not modify keys"],
            ["Generate a report"],
            ["Report is generated"]);

    private static ToolExecutionResult CreateToolResult(
        int exitCode,
        string standardOutput = "",
        string standardError = "")
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

        public Task<ToolExecutionResult> ExecuteAsync(
            ToolExecutionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return Task.FromResult(results.Dequeue());
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
            var rootDirectory = Path.Combine(Path.GetTempPath(), "aire-template-build-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return new TestRunWorkspace(rootDirectory);
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
