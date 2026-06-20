using AiReliabilityEngineering.Core.Ai;
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

        var exitCode = await handler.ExecuteAsync([], TestContext.Current.CancellationToken);

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
        Assert.Contains("Usage:", output.ToString());
        Assert.Contains("aire run <idea-file> [--profile <profile>] [--provider <provider>] [--model <model>]", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownCommand_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["unknown", "idea.md"], TestContext.Current.CancellationToken);

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
        Assert.Contains("Unrecognized command or argument", error.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingIdeaFile_ReturnsNonZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["run", "missing.md"], TestContext.Current.CancellationToken);

        Assert.Equal(CliExitCodes.InputFileNotFound, exitCode);
        Assert.Contains("Idea file not found", error.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRunCommand_InvokesRunAndPrintsSummary()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n", TestContext.Current.CancellationToken);
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
                Assert.Equal(AiProviderKind.Fake, request.EffectiveAiProvider.Kind);
                Assert.Equal(AiProviderOptions.DefaultFake.Model, request.EffectiveAiProvider.Model);
                return Task.FromResult(new RunResult(true, "test-run", Path.Combine(tempDirectory, "runs", "test-run"), "Status: Completed"));
            },
            CleanupNotCalled,
            output,
            error);

        try
        {
            var exitCode = await handler.ExecuteAsync(["run", ideaFilePath], TestContext.Current.CancellationToken);

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
    public async Task ExecuteAsync_WithAiDemoProfile_PassesAiDemoProfile()
    {
        await ExecuteRunProfileTestAsync("ai-demo", WorkflowProfile.AiDemo);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiDemoDotnetProfile_PassesAiDemoDotnetProfile()
    {
        await ExecuteRunProfileTestAsync("ai-demo-dotnet", WorkflowProfile.AiDemoDotnet);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiDemoDotnetReviewProfile_PassesAiDemoDotnetReviewProfile()
    {
        await ExecuteRunProfileTestAsync("ai-demo-dotnet-review", WorkflowProfile.AiDemoDotnetReview);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownProfile_ReturnsInvalidArguments()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n", TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        try
        {
            var exitCode = await handler.ExecuteAsync(["run", ideaFilePath, "--profile", "unknown"], TestContext.Current.CancellationToken);

            Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
            Assert.Contains("Unsupported workflow profile: unknown", error.ToString());
            Assert.Contains("Supported profiles: fake, ai-requirements, ai-demo", error.ToString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultProviderPassesFakeProvider()
    {
        await ExecuteRunProviderTestAsync(
            ["--profile", "ai-requirements"],
            AiProviderKind.Fake,
            AiProviderOptions.DefaultFake.Model);
    }

    [Fact]
    public async Task ExecuteAsync_WithExplicitFakeProviderPassesFakeProvider()
    {
        await ExecuteRunProviderTestAsync(
            ["--profile", "ai-requirements", "--provider", "fake"],
            AiProviderKind.Fake,
            AiProviderOptions.DefaultFake.Model);
    }

    [Fact]
    public async Task ExecuteAsync_WithExplicitFakeModelPassesCustomFakeModel()
    {
        await ExecuteRunProviderTestAsync(
            ["--profile", "ai-requirements", "--provider", "fake", "--model", "custom-fake-model"],
            AiProviderKind.Fake,
            "custom-fake-model");
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenAiProviderAndModelPassesOpenAiSelection()
    {
        await ExecuteRunProviderTestAsync(
            ["--profile", "ai-requirements", "--provider", "openai", "--model", "test-model"],
            AiProviderKind.OpenAi,
            "test-model");
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenAiProviderWithoutModelReturnsInvalidArguments()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n", TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        try
        {
            var exitCode = await handler.ExecuteAsync(
                ["run", ideaFilePath, "--profile", "ai-requirements", "--provider", "openai"],
                TestContext.Current.CancellationToken);

            Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
            Assert.Contains("--model is required when --provider openai is used", error.ToString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithAiDemoOpenAiProviderWithoutModelReturnsInvalidArguments()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n", TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        try
        {
            var exitCode = await handler.ExecuteAsync(
                ["run", ideaFilePath, "--profile", "ai-demo", "--provider", "openai"],
                TestContext.Current.CancellationToken);

            Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
            Assert.Contains("--model is required when --provider openai is used", error.ToString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownProviderReturnsInvalidArguments()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n", TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        try
        {
            var exitCode = await handler.ExecuteAsync(["run", ideaFilePath, "--provider", "unknown"], TestContext.Current.CancellationToken);

            Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
            Assert.Contains("Unsupported AI provider: unknown", error.ToString());
            Assert.Contains("Supported providers: fake, openai", error.ToString());
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

        var exitCode = await handler.ExecuteAsync(["cleanup"], TestContext.Current.CancellationToken);

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

        var exitCode = await handler.ExecuteAsync([helpOption], TestContext.Current.CancellationToken);

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Contains("AIRE", output.ToString());
        Assert.Contains("run", output.ToString());
        Assert.Contains("cleanup", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithRunHelpOption_ReturnsZeroAndMentionsProfileProviderAndModel()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["run", "--help"], TestContext.Current.CancellationToken);

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Contains("--profile", output.ToString());
        Assert.Contains("--provider", output.ToString());
        Assert.Contains("--model", output.ToString());
        Assert.Contains("fake", output.ToString());
        Assert.Contains("ai-requirements", output.ToString());
        Assert.Contains("ai-demo", output.ToString());
        Assert.Contains("ai-demo-dotnet", output.ToString());
        Assert.Contains("ai-demo-dotnet-review", output.ToString());
        Assert.Contains("openai", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithAiDemoFakeProvider_GeneratesRunArtifactsInTemporaryCurrentDirectory()
    {
        var previousCurrentDirectory = Directory.GetCurrentDirectory();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Redis TTL Audit Tool\n\nScan Redis keys.\n", TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = CliCommandHandler.CreateDefault(output, error);

        try
        {
            Directory.SetCurrentDirectory(tempDirectory);

            var exitCode = await handler.ExecuteAsync(
                ["run", ideaFilePath, "--profile", "ai-demo", "--provider", "fake"],
                TestContext.Current.CancellationToken);

            Assert.Equal(CliExitCodes.Success, exitCode);
            var runsDirectory = Path.Combine(tempDirectory, "runs");
            var runDirectory = Directory.GetDirectories(runsDirectory).Single();
            Assert.True(File.Exists(Path.Combine(runDirectory, "artifacts", "README.md")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "artifacts", "PLAN.md")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "artifacts", "tasks.json")));
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCurrentDirectory);
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithRunMissingArgument_ReturnsInvalidArguments()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["run"], TestContext.Current.CancellationToken);

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task ExecuteAsync_WithCleanupUnexpectedArgument_ReturnsInvalidArguments()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["cleanup", "unexpected"], TestContext.Current.CancellationToken);

        Assert.Equal(CliExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task ExecuteAsync_WithDeprecatedCleanupOption_ReturnsInvalidArguments()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var handler = new CliCommandHandler(NotCalled, CleanupNotCalled, output, error);

        var exitCode = await handler.ExecuteAsync(["-cleanup"], TestContext.Current.CancellationToken);

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
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n", TestContext.Current.CancellationToken);
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
            var exitCode = await handler.ExecuteAsync(["run", ideaFilePath, "--profile", profileValue], TestContext.Current.CancellationToken);

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

    private static async Task ExecuteRunProviderTestAsync(
        string[] runOptions,
        AiProviderKind expectedKind,
        string expectedModel)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aire-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var ideaFilePath = Path.Combine(tempDirectory, "idea.md");
        await File.WriteAllTextAsync(ideaFilePath, "# Idea\n", TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var invoked = false;
        var handler = new CliCommandHandler(
            (request, cancellationToken) =>
            {
                invoked = true;
                Assert.Equal(expectedKind, request.EffectiveAiProvider.Kind);
                Assert.Equal(expectedModel, request.EffectiveAiProvider.Model);
                return Task.FromResult(new RunResult(true, "test-run", Path.Combine(tempDirectory, "runs", "test-run"), "Status: Completed"));
            },
            CleanupNotCalled,
            output,
            error);

        try
        {
            var args = new[] { "run", ideaFilePath }.Concat(runOptions).ToArray();
            var exitCode = await handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

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
