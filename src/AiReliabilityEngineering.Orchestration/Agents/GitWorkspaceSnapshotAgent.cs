using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Git;
using AiReliabilityEngineering.Core.Tools;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class GitWorkspaceSnapshotAgent : IAgent
{
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    private readonly IToolExecutor _toolExecutor;
    private readonly IRunLogger _logger;
    private readonly GeneratedFilesReporter _generatedFilesReporter;
    private readonly GitStatusParser _gitStatusParser;
    private readonly GitSnapshotReportWriter _reportWriter;

    public GitWorkspaceSnapshotAgent(
        IToolExecutor toolExecutor,
        IRunLogger logger,
        GeneratedFilesReporter? generatedFilesReporter = null,
        GitStatusParser? gitStatusParser = null,
        GitSnapshotReportWriter? reportWriter = null)
    {
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _generatedFilesReporter = generatedFilesReporter ?? new GeneratedFilesReporter();
        _gitStatusParser = gitStatusParser ?? new GitStatusParser();
        _reportWriter = reportWriter ?? new GitSnapshotReportWriter();
    }

    public string Name => "GitWorkspaceSnapshotAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        await _logger.InfoAsync("GitWorkspaceSnapshotAgentStarted", cancellationToken);
        Directory.CreateDirectory(context.Run.Paths.WorkspaceDirectory);

        string? gitFailureMessage = null;
        if (!Directory.Exists(Path.Combine(context.Run.Paths.WorkspaceDirectory, ".git")))
        {
            var init = await RunGitAsync(["init"], context.Run.Paths.WorkspaceDirectory, cancellationToken);
            if (!init.Succeeded)
            {
                gitFailureMessage = $"git init failed with exit code {init.ExitCode}. {init.StandardError}".Trim();
            }
        }

        var statusOutput = string.Empty;
        if (gitFailureMessage is null)
        {
            var status = await RunGitAsync(["status", "--short"], context.Run.Paths.WorkspaceDirectory, cancellationToken);
            if (status.Succeeded)
            {
                statusOutput = status.StandardOutput;
            }
            else
            {
                gitFailureMessage = $"git status failed with exit code {status.ExitCode}. {status.StandardError}".Trim();
            }
        }

        var generatedFiles = _generatedFilesReporter.Create(context.Run);
        var statusEntries = _gitStatusParser.Parse(statusOutput)
            .Where(entry => !TransientWorkspacePathFilter.IsTransient(entry.Path))
            .ToArray();
        var snapshot = new GitWorkspaceSnapshot(generatedFiles, statusEntries);
        var artifacts = await _reportWriter.WriteAsync(snapshot, context.Run, gitFailureMessage, cancellationToken);

        await _logger.InfoAsync("GitWorkspaceSnapshotAgentCompleted", cancellationToken);
        return AgentResult.Success("Git workspace snapshot reports written.", artifacts);
    }

    private async Task<ToolExecutionResult> RunGitAsync(
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            return await _toolExecutor.ExecuteAsync(
                new ToolExecutionRequest("git", arguments, workingDirectory, GitTimeout),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var finishedAt = DateTimeOffset.UtcNow;
            return new ToolExecutionResult(-1, string.Empty, exception.Message, startedAt, finishedAt);
        }
    }
}
