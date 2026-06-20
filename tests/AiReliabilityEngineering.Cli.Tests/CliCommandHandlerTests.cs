using AiReliabilityEngineering.Core.Workflow;
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

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
        Assert.Contains("Usage:", output.ToString());
        Assert.Contains("aire run <idea-file> [--profile <profile>]", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownCommand_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["unknown", "idea.md"], CancellationToken.None);

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
        Assert.Contains("Unrecognized command or argument", error.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingIdeaFile_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["run", "missing.md"], CancellationToken.None);

        Assert.Equal(CliExitCodes.InputFileNotFound, exitCode);
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
                Assert.Equal(WorkflowProfile.Fake, request.Profile);
                return Task.FromResult(new RunResult(true, "test-run", Path.Combine(tempDirectory, "runs", "test-run"), "Status: Completed"));
            },
            CleanupNotCalled,
            output,
            error);

        try
        {
            var exitCode = await handler.ExecuteAsync(["run", ideaFilePath], CancellationToken.None);

            Assert.Equal(CliExitCodes.Success, exitCode);
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
    public async Task ExecuteAsync_WithFakeProfile_PassesFakeProfile()
    {
        await ExecuteRunProfileTestAsync("fake", WorkflowProfile.Fake);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiRequirementsProfile_PassesAiRequirementsProfile()
    {
        await ExecuteRunProfileTestAsync("ai-requirements", WorkflowProfile.AiRequirements);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownProfile_ReturnsInvalidArguments()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n");
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        try
        {
            var exitCode = await handler.ExecuteAsync(["run", ideaFilePath, "--profile", "unknown"], CancellationToken.None);

            Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
            Assert.Contains("Unsupported workflow profile: unknown", error.ToString());
            Assert.Contains("Supported profiles: fake, ai-requirements", error.ToString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCleanupCommand_InvokesCleanupAndReturnsZero()
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

        var exitCode = await handler.ExecuteAsync(["cleanup"], CancellationToken.None);

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.True(invoked);
        Assert.Contains("Runs cleanup completed", output.ToString());
        Assert.Contains("Deleted entries: 2", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task ExecuteAsync_WithHelpOption_ReturnsZeroAndMentionsCommands(string helpOption)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync([helpOption], CancellationToken.None);

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Contains("AIRE", output.ToString());
        Assert.Contains("run", output.ToString());
        Assert.Contains("cleanup", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithRunHelpOption_ReturnsZeroAndMentionsProfile()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["run", "--help"], CancellationToken.None);

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Contains("--profile", output.ToString());
        Assert.Contains("fake", output.ToString());
        Assert.Contains("ai-requirements", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithRunMissingArgument_ReturnsInvalidArguments()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["run"], CancellationToken.None);

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task ExecuteAsync_WithCleanupUnexpectedArgument_ReturnsInvalidArguments()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["cleanup", "unexpected"], CancellationToken.None);

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task ExecuteAsync_WithDeprecatedCleanupOption_ReturnsInvalidArguments()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["-cleanup"], CancellationToken.None);

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
    }

    private static Task<RunResult> NotCalled(RunRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("The run delegate should not be called.");

    private static Task<RunCleanupResult> CleanupNotCalled(string runsDirectory, CancellationToken cancellationToken)
        => throw new InvalidOperationException("The cleanup delegate should not be called.");

    private static async Task ExecuteRunProfileTestAsync(string profileValue, WorkflowProfile expectedProfile)
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
                Assert.Equal(expectedProfile, request.Profile);
                return Task.FromResult(new RunResult(true, "test-run", Path.Combine(tempDirectory, "runs", "test-run"), "Status: Completed"));
            },
            CleanupNotCalled,
            output,
            error);

        try
        {
            var exitCode = await handler.ExecuteAsync(["run", ideaFilePath, "--profile", profileValue], CancellationToken.None);

            Assert.Equal(CliExitCodes.Success, exitCode);
            Assert.True(invoked);
            Assert.Contains("Status: Completed", output.ToString());
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
