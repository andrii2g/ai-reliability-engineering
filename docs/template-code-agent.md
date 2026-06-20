# TemplateCodeAgent

`TemplateCodeAgent` creates a deterministic .NET CLI project inside the run workspace.

It writes only under:

```text
runs/{run-id}/workspace/
```

Generated files include:

```text
workspace/Directory.Packages.props
workspace/GeneratedTool.slnx
workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
workspace/src/GeneratedTool.Cli/Program.cs
workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs
```

The workspace-local `Directory.Packages.props` keeps generated test restores independent from the repository root.

Source code is not AI-generated in this step. The agent uses a fixed template so AIRE has a stable baseline before Codex/OpenCode code agents are introduced.
