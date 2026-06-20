# Generated .NET Workspace Demo

Run the deterministic .NET workspace demo:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
```

Expected generated files:

```text
workspace/Directory.Packages.props
workspace/GeneratedTool.slnx
workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
workspace/src/GeneratedTool.Cli/Program.cs
workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs
reports/build.md
reports/tests.md
```

The source code is template-based and not AI-generated yet. This keeps the build/test validation path stable while the workflow grows.
