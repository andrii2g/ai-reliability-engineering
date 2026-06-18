using System.Diagnostics;
using AiReliabilityEngineering.Core.Tools;

namespace AiReliabilityEngineering.Infrastructure.Tools;

public sealed class ShellToolExecutor : IToolExecutor
{
    public async Task<ToolExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Command);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkingDirectory);

        if (request.Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Timeout must be greater than zero.");
        }

        if (!Directory.Exists(request.WorkingDirectory))
        {
            throw new DirectoryNotFoundException($"Working directory does not exist: {request.WorkingDirectory}");
        }

        var startInfo = new ProcessStartInfo(request.Command)
        {
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var startedAtUtc = DateTimeOffset.UtcNow;
        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(request.Timeout);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await KillProcessTreeAsync(process, CancellationToken.None);
            var timedOutAtUtc = DateTimeOffset.UtcNow;
            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;
            return new ToolExecutionResult(
                -1,
                standardOutput,
                $"Timed out after {request.Timeout}. {standardError}".Trim(),
                startedAtUtc,
                timedOutAtUtc);
        }

        var finishedAtUtc = DateTimeOffset.UtcNow;
        return new ToolExecutionResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask,
            startedAtUtc,
            finishedAtUtc);
    }

    private static async Task KillProcessTreeAsync(Process process, CancellationToken cancellationToken)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync(cancellationToken);
    }
}
