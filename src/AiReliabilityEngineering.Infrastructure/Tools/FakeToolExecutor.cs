using AiReliabilityEngineering.Core.Tools;

namespace AiReliabilityEngineering.Infrastructure.Tools;

public sealed class FakeToolExecutor : IToolExecutor
{
    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult(new ToolExecutionResult(0, "Fake tool execution succeeded.", string.Empty, now, now));
    }
}
