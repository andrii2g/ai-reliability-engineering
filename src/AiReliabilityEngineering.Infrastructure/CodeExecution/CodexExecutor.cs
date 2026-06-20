using AiReliabilityEngineering.Core.CodeExecution;
using AiReliabilityEngineering.Core.Tools;

namespace AiReliabilityEngineering.Infrastructure.CodeExecution;

public sealed class CodexExecutor : ICodeExecutor
{
    private readonly IToolExecutor _toolExecutor;

    public CodexExecutor(IToolExecutor toolExecutor)
    {
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
    }

    public string Name => "codex";

    public async Task<CodeExecutionResult> ExecuteAsync(
        CodeExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var result = await _toolExecutor.ExecuteAsync(CreateToolRequest(request), cancellationToken);
            return new CodeExecutionResult(
                result.Succeeded,
                result.ExitCode,
                result.StandardOutput,
                result.StandardError,
                result.FinishedAtUtc - result.StartedAtUtc);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new CodeExecutionResult(
                false,
                1,
                string.Empty,
                exception.Message,
                DateTimeOffset.UtcNow - startedAt);
        }
    }

    public ToolExecutionRequest CreateToolRequest(CodeExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ToolExecutionRequest(
            "codex",
            ["exec", request.PromptFilePath],
            request.WorkspaceDirectory,
            request.Timeout);
    }
}
