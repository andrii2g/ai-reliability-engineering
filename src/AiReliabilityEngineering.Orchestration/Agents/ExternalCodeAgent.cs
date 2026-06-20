using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.CodeExecution;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class ExternalCodeAgent : IAgent
{
    private static readonly TimeSpan CodeExecutionTimeout = TimeSpan.FromMinutes(10);

    private readonly ICodeExecutor _codeExecutor;
    private readonly IRunLogger _logger;
    private readonly WorkspaceArtifactReader _artifactReader;
    private readonly DotnetTemplateProjectWriter _projectWriter;
    private readonly CodeExecutionPromptBuilder _promptBuilder;
    private readonly CodeExecutionReportWriter _reportWriter;

    public ExternalCodeAgent(
        ICodeExecutor codeExecutor,
        IRunLogger logger,
        WorkspaceArtifactReader? artifactReader = null,
        DotnetTemplateProjectWriter? projectWriter = null,
        CodeExecutionPromptBuilder? promptBuilder = null,
        CodeExecutionReportWriter? reportWriter = null)
    {
        _codeExecutor = codeExecutor ?? throw new ArgumentNullException(nameof(codeExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artifactReader = artifactReader ?? new WorkspaceArtifactReader();
        _projectWriter = projectWriter ?? new DotnetTemplateProjectWriter();
        _promptBuilder = promptBuilder ?? new CodeExecutionPromptBuilder();
        _reportWriter = reportWriter ?? new CodeExecutionReportWriter();
    }

    public string Name => "ExternalCodeAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        await _logger.InfoAsync("ExternalCodeAgentStarted", cancellationToken);
        try
        {
            var specificationJson = await _artifactReader.ReadSpecificationJsonAsync(context.Run, cancellationToken);
            var tasksJson = await _artifactReader.TryReadTasksJsonAsync(context.Run, cancellationToken);
            await _projectWriter.WriteAsync(context.Run, specificationJson, tasksJson, cancellationToken);

            var prompt = await _promptBuilder.BuildAsync(context.Run, cancellationToken);
            var promptFilePath = await WritePromptFileAsync(context.Run.Paths.WorkspaceDirectory, prompt, cancellationToken);
            var request = new CodeExecutionRequest(
                context.Run.Paths.WorkspaceDirectory,
                promptFilePath,
                CodeExecutionTimeout);

            var result = await ExecuteCodeAsync(request, cancellationToken);
            var artifacts = await _reportWriter.WriteAsync(_codeExecutor.Name, result, context.Run, cancellationToken);
            if (!result.Succeeded)
            {
                await _logger.InfoAsync("ExternalCodeAgentFailed", cancellationToken);
                return AgentResult.Failure("External code executor failed.", artifacts);
            }

            await _logger.InfoAsync("ExternalCodeAgentCompleted", cancellationToken);
            return AgentResult.Success("External code executor completed.", artifacts);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync("ExternalCodeAgentFailed", exception, cancellationToken);
            var result = new CodeExecutionResult(false, 1, string.Empty, exception.Message, TimeSpan.Zero);
            var artifacts = await _reportWriter.WriteAsync(_codeExecutor.Name, result, context.Run, cancellationToken);
            return AgentResult.Failure(exception.Message, artifacts);
        }
    }

    private async Task<CodeExecutionResult> ExecuteCodeAsync(
        CodeExecutionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _codeExecutor.ExecuteAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new CodeExecutionResult(false, 1, string.Empty, exception.Message, TimeSpan.Zero);
        }
    }

    private static async Task<string> WritePromptFileAsync(
        string workspaceDirectory,
        string prompt,
        CancellationToken cancellationToken)
    {
        var aireDirectory = Path.Combine(workspaceDirectory, ".aire");
        Directory.CreateDirectory(aireDirectory);
        var promptFilePath = Path.Combine(aireDirectory, "code-execution-prompt.md");
        EnsureInsideWorkspace(workspaceDirectory, promptFilePath);
        await File.WriteAllTextAsync(promptFilePath, prompt, cancellationToken);
        return promptFilePath;
    }

    private static void EnsureInsideWorkspace(string workspaceDirectory, string path)
    {
        var workspaceRoot = Path.GetFullPath(workspaceDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Prompt file path must be inside the run workspace.");
        }
    }
}
