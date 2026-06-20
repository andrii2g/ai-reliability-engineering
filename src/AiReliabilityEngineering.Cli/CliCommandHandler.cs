using AiReliabilityEngineering.Orchestration.RunManagement;
using System.CommandLine;

namespace AiReliabilityEngineering.Cli;

public sealed class CliCommandHandler(
    Func<RunRequest, CancellationToken, Task<RunResult>> runAsync,
    Func<string, CancellationToken, Task<RunCleanupResult>> cleanupRunsAsync,
    TextWriter output,
    TextWriter error)
{
    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
    {
        var command = BuildCommand(cancellationToken);
        var parseResult = command.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var parseError in parseResult.Errors)
            {
                await error.WriteLineAsync(parseError.Message);
            }

            await output.WriteLineAsync("Usage:");
            await output.WriteLineAsync("  aire run <idea-file>");
            await output.WriteLineAsync("  aire cleanup");
            return CliExitCodes.InvalidArguments;
        }

        var configuration = new InvocationConfiguration
        {
            Output = output,
            Error = error
        };

        return await parseResult.InvokeAsync(configuration, cancellationToken);
    }

    public static CliCommandHandler CreateDefault(TextWriter output, TextWriter error)
    {
        var orchestrator = CompositionRoot.CreateOrchestrator();
        var cleanupService = CompositionRoot.CreateRunCleanupService();
        return new CliCommandHandler(orchestrator.RunAsync, cleanupService.CleanupAsync, output, error);
    }

    private RootCommand BuildCommand(CancellationToken cancellationToken)
    {
        var ideaFileArgument = new Argument<FileInfo>("idea-file")
        {
            Description = "Path to the Markdown idea file."
        };
        var runCommand = new Command("run", "Run the fake AIRE workflow.")
        {
            ideaFileArgument
        };
        var cleanupCommand = new Command("cleanup", "Remove generated run folders and files under runs.");

        cleanupCommand.SetAction(async parseResult =>
        {
            var runsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "runs");
            var result = await cleanupRunsAsync(runsDirectory, cancellationToken);
            var writer = result.Succeeded ? output : error;

            await writer.WriteLineAsync(result.Message);
            await writer.WriteLineAsync($"Runs directory: {result.RunsDirectory}");
            await writer.WriteLineAsync($"Deleted entries: {result.DeletedEntries}");

            return result.Succeeded ? CliExitCodes.Success : CliExitCodes.ExecutionFailed;
        });

        runCommand.SetAction(async parseResult =>
        {
            var ideaFile = parseResult.GetValue(ideaFileArgument);
            if (ideaFile is null || !ideaFile.Exists)
            {
                await error.WriteLineAsync($"Idea file not found: {ideaFile?.FullName ?? "(null)"}");
                return CliExitCodes.InputFileNotFound;
            }

            var runsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "runs");
            var result = await runAsync(new RunRequest(ideaFile.FullName, runsDirectory), cancellationToken);

            await output.WriteLineAsync();
            await output.WriteLineAsync("Final summary");
            await output.WriteLineAsync($"Run ID: {result.RunId ?? "(none)"}");
            await output.WriteLineAsync($"Run directory: {result.RunDirectory ?? "(none)"}");
            await output.WriteLineAsync(result.Message);

            return result.Succeeded ? CliExitCodes.Success : CliExitCodes.ExecutionFailed;
        });

        var rootCommand = new RootCommand("AIRE - AI Reliability Engineering")
        {
            runCommand,
            cleanupCommand
        };

        rootCommand.SetAction(async parseResult =>
        {
            await output.WriteLineAsync("Usage:");
            await output.WriteLineAsync("  aire run <idea-file>");
            await output.WriteLineAsync("  aire cleanup");
            return CliExitCodes.InvalidArguments;
        });

        return rootCommand;
    }
}
