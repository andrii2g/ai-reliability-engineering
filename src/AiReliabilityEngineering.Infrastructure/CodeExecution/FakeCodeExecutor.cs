using AiReliabilityEngineering.Core.CodeExecution;

namespace AiReliabilityEngineering.Infrastructure.CodeExecution;

public sealed class FakeCodeExecutor : ICodeExecutor
{
    public string Name => "fake-code-executor";

    public Task<CodeExecutionResult> ExecuteAsync(
        CodeExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new CodeExecutionResult(
            true,
            0,
            "Fake code execution succeeded.",
            string.Empty,
            TimeSpan.Zero));
    }
}
