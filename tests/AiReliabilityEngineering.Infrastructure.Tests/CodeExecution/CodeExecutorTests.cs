using AiReliabilityEngineering.Core.CodeExecution;
using AiReliabilityEngineering.Core.Tools;
using AiReliabilityEngineering.Infrastructure.CodeExecution;

namespace AiReliabilityEngineering.Infrastructure.Tests.CodeExecution;

public sealed class CodeExecutorTests
{
    [Fact]
    public async Task FakeCodeExecutor_ReturnsSuccess()
    {
        var result = await new FakeCodeExecutor().ExecuteAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Fake code execution succeeded", result.StandardOutput);
    }

    [Fact]
    public void OpenCodeExecutor_CreatesExpectedToolRequest()
    {
        var request = CreateRequest();
        var toolRequest = new OpenCodeExecutor(new RecordingToolExecutor(CreateToolResult(0))).CreateToolRequest(request);

        Assert.Equal("opencode", toolRequest.Command);
        Assert.Equal(["run", request.PromptFilePath], toolRequest.Arguments);
        Assert.Equal(request.WorkspaceDirectory, toolRequest.WorkingDirectory);
        Assert.Equal(request.Timeout, toolRequest.Timeout);
    }

    [Fact]
    public void CodexExecutor_CreatesExpectedToolRequest()
    {
        var request = CreateRequest();
        var toolRequest = new CodexExecutor(new RecordingToolExecutor(CreateToolResult(0))).CreateToolRequest(request);

        Assert.Equal("codex", toolRequest.Command);
        Assert.Equal(["exec", request.PromptFilePath], toolRequest.Arguments);
        Assert.Equal(request.WorkspaceDirectory, toolRequest.WorkingDirectory);
        Assert.Equal(request.Timeout, toolRequest.Timeout);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task OpenCodeExecutor_MapsToolResult(int exitCode, bool expectedSucceeded)
    {
        var executor = new OpenCodeExecutor(new RecordingToolExecutor(CreateToolResult(exitCode, "out", "err")));

        var result = await executor.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(expectedSucceeded, result.Succeeded);
        Assert.Equal(exitCode, result.ExitCode);
        Assert.Equal("out", result.StandardOutput);
        Assert.Equal("err", result.StandardError);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task CodexExecutor_MapsToolResult(int exitCode, bool expectedSucceeded)
    {
        var executor = new CodexExecutor(new RecordingToolExecutor(CreateToolResult(exitCode, "out", "err")));

        var result = await executor.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(expectedSucceeded, result.Succeeded);
        Assert.Equal(exitCode, result.ExitCode);
        Assert.Equal("out", result.StandardOutput);
        Assert.Equal("err", result.StandardError);
    }

    [Fact]
    public async Task OpenCodeExecutor_CatchesToolExecutorException()
    {
        var result = await new OpenCodeExecutor(new ThrowingToolExecutor()).ExecuteAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("tool failed", result.StandardError);
    }

    [Fact]
    public async Task CodexExecutor_CatchesToolExecutorException()
    {
        var result = await new CodexExecutor(new ThrowingToolExecutor()).ExecuteAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("tool failed", result.StandardError);
    }

    private static CodeExecutionRequest CreateRequest()
        => new("workspace", "workspace/.aire/code-execution-prompt.md", TimeSpan.FromMinutes(10));

    private static ToolExecutionResult CreateToolResult(int exitCode, string standardOutput = "", string standardError = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new ToolExecutionResult(exitCode, standardOutput, standardError, now, now.AddMilliseconds(10));
    }

    private sealed class RecordingToolExecutor(ToolExecutionResult result) : IToolExecutor
    {
        public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class ThrowingToolExecutor : IToolExecutor
    {
        public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("tool failed");
    }
}
