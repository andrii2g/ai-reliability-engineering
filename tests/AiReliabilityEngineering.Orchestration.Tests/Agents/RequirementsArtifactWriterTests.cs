using System.Text.Json;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Orchestration.Agents;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class RequirementsArtifactWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesSpecificationJsonAndRequirementsMd()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new RequirementsArtifactWriter();

        var artifacts = await writer.WriteAsync(CreateSpecification(), workspace.RunContext, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "specification.json")));
        Assert.True(File.Exists(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "requirements.md")));
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "artifacts/specification.json");
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "artifacts/requirements.md");
    }

    [Fact]
    public async Task WriteAsync_WritesCamelCaseValidJson()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new RequirementsArtifactWriter();

        await writer.WriteAsync(CreateSpecification(), workspace.RunContext, CancellationToken.None);

        await using var stream = File.OpenRead(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "specification.json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);
        Assert.True(document.RootElement.TryGetProperty("projectName", out _));
        Assert.True(document.RootElement.TryGetProperty("summary", out _));
        Assert.True(document.RootElement.TryGetProperty("goals", out _));
        Assert.False(document.RootElement.TryGetProperty("ProjectName", out _));
    }

    [Fact]
    public async Task WriteAsync_RequirementsMarkdownContainsExpectedSections()
    {
        using var workspace = TestRunWorkspace.Create();
        var writer = new RequirementsArtifactWriter();

        await writer.WriteAsync(CreateSpecification(), workspace.RunContext, CancellationToken.None);

        var content = await File.ReadAllTextAsync(Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "requirements.md"), CancellationToken.None);
        Assert.Contains("# Requirements", content);
        Assert.Contains("## Project Name", content);
        Assert.Contains("## Summary", content);
        Assert.Contains("## Goals", content);
        Assert.Contains("## Non-Goals", content);
        Assert.Contains("## Functional Requirements", content);
        Assert.Contains("## Acceptance Criteria", content);
    }

    private static ProjectSpecification CreateSpecification()
        => new(
            "Redis TTL Audit Tool",
            "Redis TTL Audit Tool",
            ["Convert the provided idea into a structured project specification."],
            ProjectSpecificationDefaults.DefaultNonGoals,
            ProjectSpecificationDefaults.DefaultFunctionalRequirements,
            ProjectSpecificationDefaults.DefaultAcceptanceCriteria);

    private sealed class TestRunWorkspace : IDisposable
    {
        private TestRunWorkspace(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            var paths = new RunPaths(
                rootDirectory,
                Path.Combine(rootDirectory, "input"),
                Path.Combine(rootDirectory, "workspace"),
                Path.Combine(rootDirectory, "artifacts"),
                Path.Combine(rootDirectory, "reports"),
                Path.Combine(rootDirectory, "logs"),
                Path.Combine(rootDirectory, "run-state.json"));
            RunContext = new RunContext(
                new RunId("test-run"),
                Path.Combine(rootDirectory, "original-idea.md"),
                Path.Combine(paths.InputDirectory, "idea.md"),
                paths,
                DateTimeOffset.UnixEpoch);
        }

        public string RootDirectory { get; }

        public RunContext RunContext { get; }

        public static TestRunWorkspace Create()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "aire-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return new TestRunWorkspace(rootDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
