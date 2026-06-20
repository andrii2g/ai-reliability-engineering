using System.Text.Json;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Orchestration.Agents;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class ProjectSpecificationReaderTests
{
    [Fact]
    public async Task ReadAsync_ReadsValidSpecificationJson()
    {
        using var workspace = TestRunWorkspace.Create();
        await WriteSpecificationAsync(workspace.RunContext, CreateSpecification(), CancellationToken.None);
        var reader = new ProjectSpecificationReader();

        var specification = await reader.ReadAsync(workspace.RunContext, CancellationToken.None);

        Assert.Equal("Redis TTL Audit Tool", specification.ProjectName);
        Assert.Equal("Scan Redis keys.", specification.Summary);
        Assert.Equal(["Find keys without TTL"], specification.Goals);
    }

    [Fact]
    public async Task ReadAsync_WithMissingSpecificationThrowsFileNotFoundException()
    {
        using var workspace = TestRunWorkspace.Create();
        var reader = new ProjectSpecificationReader();

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            reader.ReadAsync(workspace.RunContext, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_WithInvalidJsonThrowsInvalidOperationException()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.ArtifactsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "specification.json"),
            "not json");
        var reader = new ProjectSpecificationReader();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadAsync(workspace.RunContext, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_WithInvalidShapeThrowsInvalidOperationException()
    {
        using var workspace = TestRunWorkspace.Create();
        Directory.CreateDirectory(workspace.RunContext.Paths.ArtifactsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.ArtifactsDirectory, "specification.json"),
            """{ "projectName": "", "summary": "Summary" }""");
        var reader = new ProjectSpecificationReader();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadAsync(workspace.RunContext, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_ReadsFromArtifactsDirectory()
    {
        using var workspace = TestRunWorkspace.Create();
        await WriteSpecificationAsync(workspace.RunContext, CreateSpecification(), CancellationToken.None);
        Directory.CreateDirectory(workspace.RunContext.Paths.WorkspaceDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(workspace.RunContext.Paths.WorkspaceDirectory, "specification.json"),
            """{ "projectName": "Wrong" }""");
        var reader = new ProjectSpecificationReader();

        var specification = await reader.ReadAsync(workspace.RunContext, CancellationToken.None);

        Assert.Equal("Redis TTL Audit Tool", specification.ProjectName);
    }

    private static ProjectSpecification CreateSpecification()
        => new(
            "Redis TTL Audit Tool",
            "Scan Redis keys.",
            ["Find keys without TTL"],
            ["Do not modify keys"],
            ["Generate a report"],
            ["Report is generated"]);

    private static async Task WriteSpecificationAsync(
        RunContext runContext,
        ProjectSpecification specification,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(runContext.Paths.ArtifactsDirectory);
        var path = Path.Combine(runContext.Paths.ArtifactsDirectory, "specification.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            specification,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            },
            cancellationToken);
    }

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
