using AiReliabilityEngineering.Orchestration.RunManagement;

namespace AiReliabilityEngineering.Cli.Tests;

public sealed class CliCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_WithMissingArguments_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync([], CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Usage:", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownCommand_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["unknown", "idea.md"], CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Usage:", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingIdeaFile_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["run", "missing.md"], CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Idea file not found", error.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRunCommand_InvokesRunAndPrintsSummary()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n");
        using var output = new StringWriter();
        using var error = new StringWriter();
        var invoked = false;
        var handler = new CliCommandHandler(
            (request, cancellationToken) =>
            {
                invoked = true;
                Assert.Equal(Path.GetFullPath(ideaFilePath), request.IdeaFilePath);
                Assert.EndsWith("runs", request.RunsDirectory);
                return Task.FromResult(new RunResult(true, "test-run", Path.Combine(tempDirectory, "runs", "test-run"), "Status: Completed"));
            },
            CleanupNotCalled,
            output,
            error);

        try
        {
            var exitCode = await handler.ExecuteAsync(["run", ideaFilePath], CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.True(invoked);
            Assert.Contains("Run ID: test-run", output.ToString());
            Assert.Contains("Status: Completed", output.ToString());
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCleanupOption_InvokesCleanupAndReturnsZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var invoked = false;
        var handler = new CliCommandHandler(
            NotCalled,
            (runsDirectory, cancellationToken) =>
            {
                invoked = true;
                Assert.EndsWith("runs", runsDirectory);
                return Task.FromResult(new RunCleanupResult(true, runsDirectory, 2, "Runs cleanup completed. Deleted 2 entries."));
            },
            output,
            error);

        var exitCode = await handler.ExecuteAsync(["-cleanup"], CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.True(invoked);
        Assert.Contains("Runs cleanup completed", output.ToString());
        Assert.Contains("Deleted entries: 2", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    private static Task<RunResult> NotCalled(RunRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("The run delegate should not be called.");

    private static Task<RunCleanupResult> CleanupNotCalled(string runsDirectory, CancellationToken cancellationToken)
        => throw new InvalidOperationException("The cleanup delegate should not be called.");
}
