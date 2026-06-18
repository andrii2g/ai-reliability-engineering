using AiReliabilityEngineering.Orchestration.RunManagement;
using System.CommandLine;

namespace AiReliabilityEngineering.Cli;

public sealed class CliCommandHandler(
    Func<RunRequest, CancellationToken, Task<RunResult>> runAsync,
    TextWriter output,
    TextWriter error)
{
    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
    {
        var command = BuildCommand(cancellationToken);
        var parseResult = command.Parse(args);
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
        return new CliCommandHandler(orchestrator.RunAsync, output, error);
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

        runCommand.SetAction(async parseResult =>
        {
            var ideaFile = parseResult.GetValue(ideaFileArgument);
            if (ideaFile is null || !ideaFile.Exists)
            {
                await error.WriteLineAsync($"Idea file not found: {ideaFile?.FullName ?? "(null)"}");
                return 1;
            }

            var runsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "runs");
            var result = await runAsync(new RunRequest(ideaFile.FullName, runsDirectory), cancellationToken);

            await output.WriteLineAsync();
            await output.WriteLineAsync("Final summary");
            await output.WriteLineAsync($"Run ID: {result.RunId ?? "(none)"}");
            await output.WriteLineAsync($"Run directory: {result.RunDirectory ?? "(none)"}");
            await output.WriteLineAsync(result.Message);

            return result.Succeeded ? 0 : 1;
        });

        var rootCommand = new RootCommand("AIRE - AI Reliability Engineering")
        {
            runCommand
        };

        return rootCommand;
    }
}
