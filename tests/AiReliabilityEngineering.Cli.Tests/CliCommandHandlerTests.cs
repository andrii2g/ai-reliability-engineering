using AiReliabilityEngineering.Orchestration.RunManagement;

namespace AiReliabilityEngineering.Cli.Tests;

public sealed class CliCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_WithMissingArguments_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, output, error);

        var exitCode = await handler.ExecuteAsync([], CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Usage:", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownCommand_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["unknown", "idea.md"], CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Usage:", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingIdeaFile_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["run", "missing.md"], CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Idea file not found", error.ToString());
    }

    private static Task<RunResult> NotCalled(RunRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("The run delegate should not be called.");
}
