using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class DotnetTemplateProjectWriter
{
    public async Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        RunContext runContext,
        string specificationJson,
        string? tasksJson,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runContext);
        ArgumentNullException.ThrowIfNull(specificationJson);

        var workspaceDirectory = runContext.Paths.WorkspaceDirectory;
        Directory.CreateDirectory(workspaceDirectory);
        Directory.CreateDirectory(Path.Combine(workspaceDirectory, "src", "GeneratedTool.Cli"));
        Directory.CreateDirectory(Path.Combine(workspaceDirectory, "tests", "GeneratedTool.Cli.Tests"));

        var files = new Dictionary<string, string>
        {
            ["Directory.Packages.props"] = DirectoryPackagesProps(),
            ["GeneratedTool.slnx"] = SolutionFile(),
            ["src/GeneratedTool.Cli/GeneratedTool.Cli.csproj"] = CliProjectFile(),
            ["src/GeneratedTool.Cli/Program.cs"] = ProgramFile(),
            ["tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj"] = TestProjectFile(),
            ["tests/GeneratedTool.Cli.Tests/SmokeTests.cs"] = SmokeTestsFile()
        };

        foreach (var file in files)
        {
            var path = Path.Combine(workspaceDirectory, file.Key.Replace('/', Path.DirectorySeparatorChar));
            await File.WriteAllTextAsync(path, file.Value, cancellationToken);
        }

        await File.WriteAllTextAsync(
            Path.Combine(workspaceDirectory, "specification.snapshot.json"),
            specificationJson,
            cancellationToken);

        if (tasksJson is not null)
        {
            await File.WriteAllTextAsync(
                Path.Combine(workspaceDirectory, "tasks.snapshot.json"),
                tasksJson,
                cancellationToken);
        }

        var artifacts = files.Keys
            .Select(path => new ArtifactRef(ArtifactType.Code, $"workspace/{path}", "Generated .NET workspace file"))
            .ToList();
        artifacts.Add(new ArtifactRef(ArtifactType.Code, "workspace/specification.snapshot.json", "Specification snapshot"));

        if (tasksJson is not null)
        {
            artifacts.Add(new ArtifactRef(ArtifactType.Code, "workspace/tasks.snapshot.json", "Tasks snapshot"));
        }

        return artifacts;
    }

    private static string DirectoryPackagesProps() =>
        """
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
          </PropertyGroup>

          <ItemGroup>
            <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
            <PackageVersion Include="xunit.v3" Version="3.2.2" />
          </ItemGroup>
        </Project>
        """;

    private static string SolutionFile() =>
        """
        <Solution>
          <Project Path="src/GeneratedTool.Cli/GeneratedTool.Cli.csproj" />
          <Project Path="tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj" />
        </Solution>
        """;

    private static string CliProjectFile() =>
        """
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>

        </Project>
        """;

    private static string ProgramFile() =>
        """
        if (args.Length > 0 && args[0] == "--version")
        {
            Console.WriteLine("0.1.0");
            return;
        }

        if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
        {
            Console.WriteLine("GeneratedTool");
            Console.WriteLine("Usage: GeneratedTool [--help] [--version]");
            return;
        }

        Console.WriteLine("Generated tool from AIRE");
        Console.WriteLine("This is a deterministic demo project generated in the run workspace.");
        """;

    private static string TestProjectFile() =>
        """
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <IsPackable>false</IsPackable>
            <OutputType>Exe</OutputType>
            <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Microsoft.NET.Test.Sdk" />
            <PackageReference Include="xunit.v3" />
          </ItemGroup>

          <ItemGroup>
            <Using Include="Xunit" />
          </ItemGroup>

          <ItemGroup>
            <ProjectReference Include="..\..\src\GeneratedTool.Cli\GeneratedTool.Cli.csproj" />
          </ItemGroup>

        </Project>
        """;

    private static string SmokeTestsFile() =>
        """
        namespace GeneratedTool.Cli.Tests;

        public sealed class SmokeTests
        {
            [Fact]
            public void Smoke_test_passes()
            {
                Assert.True(true);
            }
        }
        """;
}
