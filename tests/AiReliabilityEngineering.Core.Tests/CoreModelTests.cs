using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Core.Tools;

namespace AiReliabilityEngineering.Core.Tests;

public sealed class CoreModelTests
{
    [Fact]
    public void RunIdCreate_ReturnsNonEmptyId()
    {
        var runId = RunId.Create(new DateTimeOffset(2026, 6, 18, 10, 15, 30, TimeSpan.Zero));

        Assert.False(string.IsNullOrWhiteSpace(runId.Value));
    }

    [Fact]
    public void RunIdCreate_IncludesTimestampLikePrefix()
    {
        var runId = RunId.Create(new DateTimeOffset(2026, 6, 18, 10, 15, 30, TimeSpan.Zero));

        Assert.StartsWith("20260618-101530-", runId.Value);
    }

    [Fact]
    public void AgentResultSuccess_SetsIsSuccessTrue()
    {
        var result = AgentResult.Success("ok");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AgentResultFailure_SetsIsSuccessFalse()
    {
        var result = AgentResult.Failure("failed");

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void ToolExecutionResultSucceeded_ReflectsExitCode(int exitCode, bool expected)
    {
        var now = DateTimeOffset.UtcNow;
        var result = new ToolExecutionResult(exitCode, string.Empty, string.Empty, now, now);

        Assert.Equal(expected, result.Succeeded);
    }
}
