using AiReliabilityEngineering.Core.Review;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class RequiredArtifactChecker
{
    private static readonly IReadOnlyList<(string Path, string Category)> RequiredFiles =
    [
        ("artifacts/specification.json", "artifact"),
        ("artifacts/requirements.md", "artifact"),
        ("artifacts/README.md", "artifact"),
        ("artifacts/PLAN.md", "artifact"),
        ("artifacts/tasks.json", "artifact"),
        ("workspace/Directory.Packages.props", "workspace"),
        ("workspace/GeneratedTool.slnx", "workspace"),
        ("workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj", "workspace"),
        ("workspace/src/GeneratedTool.Cli/Program.cs", "workspace"),
        ("workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj", "workspace"),
        ("workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs", "workspace"),
        ("reports/build.md", "report"),
        ("reports/tests.md", "report")
    ];

    public IReadOnlyList<RequiredArtifactCheck> Check(RunContext runContext)
    {
        ArgumentNullException.ThrowIfNull(runContext);

        return RequiredFiles
            .Select(file =>
            {
                var fullPath = Path.Combine(
                    runContext.Paths.RootDirectory,
                    file.Path.Replace('/', Path.DirectorySeparatorChar));
                return new RequiredArtifactCheck(file.Path, File.Exists(fullPath), file.Category);
            })
            .ToArray();
    }
}
