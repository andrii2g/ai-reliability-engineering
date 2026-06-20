using AiReliabilityEngineering.Core.CodeExecution;

namespace AiReliabilityEngineering.Core.Tests.CodeExecution;

public sealed class CodeExecutionTests
{
    [Fact]
    public void CodeExecutorSelection_DefaultFakeUsesFakeKind()
    {
        Assert.Equal(CodeExecutorKind.Fake, CodeExecutorSelection.Fake.Kind);
        Assert.Equal(CodeExecutorKind.Codex, new CodeExecutorSelection(CodeExecutorKind.Codex).Kind);
    }

    [Fact]
    public void CodeExecutionRequest_ValidatesRequiredValues()
    {
        var request = new CodeExecutionRequest("workspace", "workspace/.aire/prompt.md", TimeSpan.FromSeconds(1));

        Assert.Equal("workspace", request.WorkspaceDirectory);
        Assert.Equal("workspace/.aire/prompt.md", request.PromptFilePath);
        Assert.Equal(TimeSpan.FromSeconds(1), request.Timeout);
        Assert.Throws<ArgumentException>(() => new CodeExecutionRequest("", "prompt", TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentException>(() => new CodeExecutionRequest("workspace", "", TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CodeExecutionRequest("workspace", "prompt", TimeSpan.Zero));
    }

    [Fact]
    public void CodeExecutionResult_NormalizesNullOutputAndValidatesDuration()
    {
        var result = new CodeExecutionResult(true, 0, null, null, TimeSpan.FromSeconds(1));

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CodeExecutionResult(false, 1, null, null, TimeSpan.FromSeconds(-1)));
    }
}
