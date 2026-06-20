# BuildTestAgent

`BuildTestAgent` validates the generated .NET workspace project.

It runs hardcoded commands inside the run workspace:

```bash
dotnet build src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
dotnet test tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
```

Each command has a 2-minute timeout.

The agent captures stdout, stderr, exit code, and duration, then writes:

```text
reports/build.md
reports/tests.md
```

If build fails, tests are skipped and the run fails. If tests fail, the run fails.

`BuildTestAgent` depends only on `IToolExecutor`. `ShellToolExecutor` is wired from the CLI composition root.
