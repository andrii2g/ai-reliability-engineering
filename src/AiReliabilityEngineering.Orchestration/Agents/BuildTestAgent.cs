using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Build;
using AiReliabilityEngineering.Core.Tools;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class BuildTestAgent : IAgent
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(2);

    private readonly IToolExecutor _toolExecutor;
    private readonly IRunLogger _logger;
    private readonly BuildTestReportWriter _reportWriter;

    public BuildTestAgent(
        IToolExecutor toolExecutor,
        IRunLogger logger,
        BuildTestReportWriter? reportWriter = null)
    {
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reportWriter = reportWriter ?? new BuildTestReportWriter();
    }

    public string Name => "BuildTestAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        var workspaceDirectory = context.Run.Paths.WorkspaceDirectory;
        if (!Directory.Exists(workspaceDirectory))
        {
            return AgentResult.Failure($"Workspace directory does not exist: {workspaceDirectory}");
        }

        await _logger.InfoAsync("BuildTestAgentStarted", cancellationToken);
        var build = await RunCommandAsync(
            "dotnet",
            ["build", "src/GeneratedTool.Cli/GeneratedTool.Cli.csproj"],
            workspaceDirectory,
            cancellationToken);

        if (!build.Succeeded)
        {
            var failedReport = new BuildTestReport(build, null);
            var artifacts = await _reportWriter.WriteAsync(failedReport, context.Run, cancellationToken);
            await _logger.InfoAsync("BuildTestAgentBuildFailed", cancellationToken);
            return AgentResult.Failure("Build command failed.", artifacts);
        }

        var test = await RunCommandAsync(
            "dotnet",
            ["test", "tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj"],
            workspaceDirectory,
            cancellationToken);
        var report = new BuildTestReport(build, test);
        var reportArtifacts = await _reportWriter.WriteAsync(report, context.Run, cancellationToken);

        if (!test.Succeeded)
        {
            await _logger.InfoAsync("BuildTestAgentTestsFailed", cancellationToken);
            return AgentResult.Failure("Test command failed.", reportArtifacts);
        }

        await _logger.InfoAsync("BuildTestAgentCompleted", cancellationToken);
        return AgentResult.Success("Build and tests passed.", reportArtifacts);
    }

    private async Task<CommandReport> RunCommandAsync(
        string command,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var commandText = string.Join(" ", new[] { command }.Concat(arguments));
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            var result = await _toolExecutor.ExecuteAsync(
                new ToolExecutionRequest(command, arguments, workingDirectory, CommandTimeout),
                cancellationToken);
            return new CommandReport(
                commandText,
                workingDirectory,
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
            return new CommandReport(
                commandText,
                workingDirectory,
                -1,
                string.Empty,
                exception.Message,
                DateTimeOffset.UtcNow - startedAt);
        }
    }
}
