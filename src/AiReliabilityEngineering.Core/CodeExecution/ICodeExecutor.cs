namespace AiReliabilityEngineering.Core.CodeExecution;

public interface ICodeExecutor
{
    string Name { get; }

    Task<CodeExecutionResult> ExecuteAsync(
        CodeExecutionRequest request,
        CancellationToken cancellationToken);
}
