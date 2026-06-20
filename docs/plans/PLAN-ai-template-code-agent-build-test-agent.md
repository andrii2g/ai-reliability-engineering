# PLAN.md - AIRE: TemplateCodeAgent and BuildTestAgent

## Purpose

This plan is for Codex to implement the next AIRE milestone: generating a real runnable demo project and validating it with build/test commands.

AIRE means AI Reliability Engineering.

Repository conventions:

- Repository name: ai-reliability-engineering
- Solution name: AiReliabilityEngineering.slnx
- CLI project: AiReliabilityEngineering.Cli
- CLI command name: aire
- PRD location: docs/PRD.md

Previous steps should already be implemented:

- stable fake workflow;
- cleanup command;
- workflow profiles;
- `fake` profile;
- `ai-requirements` profile;
- `ai-demo` profile;
- AI provider contracts;
- FakeAiProvider;
- OpenAiProvider;
- provider selection through `--provider fake|openai`;
- model selection through `--model`;
- AiRequirementsAgent;
- AiDocumentationAgent;
- AiPlannerAgent;
- ProjectSpecification;
- ProjectDocumentation;
- ImplementationPlan / ImplementationTask;
- requirements, documentation, and planning artifacts;
- samples/redis-ttl-audit.md.

The goal of this step is to move from "artifact generation" to "generated runnable project demo".

This step should produce a deterministic .NET CLI project inside the run workspace and validate it with build/test commands.

---

## High-Level Goal

Add a new workflow profile:

```text
ai-demo-dotnet
```

Expected pipeline:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> TemplateCodeAgent
Testing       -> BuildTestAgent
Review        -> FakeReviewerAgent
```

Expected command:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
```

Expected outputs:

```text
artifacts/specification.json
artifacts/requirements.md
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json

workspace/Directory.Packages.props
workspace/GeneratedTool.slnx
workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
workspace/src/GeneratedTool.Cli/Program.cs
workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs

reports/build.md
reports/tests.md
```

This workflow should complete successfully with `--provider fake`.

With `--provider openai`, the earlier AI agents may call OpenAI, but code generation remains template-based.

---

## Non-Goals

Do not implement these in this step:

- real Redis TTL scanner implementation;
- project-specific code generation from AI;
- Codex executor;
- OpenCode executor;
- Git commit/push/PR;
- Docker;
- Kubernetes;
- multiple templates;
- custom template engine;
- runtime package publishing;
- NuGet packaging;
- real ReviewerAgent;
- retry/fix loop;
- SQLite;
- dashboard;
- OpenAI structured outputs;
- source code modification by LLM;
- network access from generated project tests.

This step is only about deterministic template project generation and build/test validation.

---

## Design Decision: Deterministic Template First

TemplateCodeAgent must not ask AI to write source code.

It should generate a small deterministic .NET CLI project from the artifacts already produced by earlier agents.

This keeps the system safe and gives us a stable validation path before Codex/OpenCode integration.

Future direction:

```text
TemplateCodeAgent -> deterministic baseline
CodexCodeAgent    -> future external coding agent
OpenCodeAgent     -> future external coding agent
```

BuildTestAgent should be reusable by all future code agents.

---

## Design Decision: One Template Only

Support only one generated project type in this step:

```text
dotnet-cli
```

Do not add template selection yet.

The new profile name `ai-demo-dotnet` implies this template.

---

## Design Decision: Generated Project Name

Use a stable generated project name:

```text
GeneratedTool
```

Do not try to infer project/package names from the idea yet.

Reason:

- avoids invalid .NET identifier problems;
- keeps tests deterministic;
- keeps this step small.

Later steps can add project name normalization.

---

## Design Decision: Build/Test Runner Uses Existing Tool Executor

If the repository already has:

```text
IToolExecutor
ToolExecutionRequest
ToolExecutionResult
ShellToolExecutor
FakeToolExecutor
```

reuse them.

BuildTestAgent must depend only on `IToolExecutor`.

BuildTestAgent must not depend on `ShellToolExecutor`, `Process`, or any infrastructure-specific type.

Shell execution is an infrastructure concern and must be wired only from the CLI composition root.

Correct dependency direction:

```text
AiReliabilityEngineering.Orchestration
  -> IToolExecutor abstraction only

AiReliabilityEngineering.Cli
  -> CompositionRoot
  -> ShellToolExecutor from AiReliabilityEngineering.Infrastructure
```

Tests should use fake/recording tool executors.

If the existing ShellToolExecutor is incomplete, minimally fix it so it can run:

```bash
dotnet build
dotnet test
```

inside the generated workspace.

Do not introduce a new process execution abstraction if one already exists.

---

## Important Safety Rules

BuildTestAgent may run shell commands.

Therefore:

- commands must run only inside `runContext.Paths.WorkspaceDirectory`;
- do not run commands in repository root;
- do not accept arbitrary user commands in this step;
- only run hardcoded commands:
  - `dotnet build`
  - `dotnet test`
- capture stdout/stderr;
- write reports under `runContext.Paths.ReportsDirectory`;
- fail the agent if command exit code is non-zero;
- do not hide failures.

---

## Important Timeout Rules

The existing `ToolExecutionRequest` requires a timeout.

BuildTestAgent must use a fixed timeout of:

```text
2 minutes per command
```

Required timeouts:

```text
dotnet build -> 2 minutes
dotnet test  -> 2 minutes
```

If a command times out:

- treat it as a failed command;
- write the failure to the corresponding report;
- use a non-zero exit code in the `CommandReport`;
- include a clear message such as `Command timed out after 00:02:00.`;
- do not continue to tests if build timed out.

If ShellToolExecutor currently throws on timeout, BuildTestAgent should catch the timeout exception and convert it into a failed `CommandReport`, unless the existing ToolExecutionResult already represents timeouts. Do not let normal command timeout crash the entire process without a report.

Do not swallow `OperationCanceledException` from the workflow cancellation token.

---

## Target Folder Structure

Add or update this structure:

```text
src/
|-- AiReliabilityEngineering.Core/
|   `-- Build/
|       |-- CommandReport.cs
|       `-- BuildTestReport.cs
|
|-- AiReliabilityEngineering.Orchestration/
|   `-- Agents/
|       |-- TemplateCodeAgent.cs
|       |-- DotnetTemplateProjectWriter.cs
|       |-- BuildTestAgent.cs
|       |-- BuildTestReportWriter.cs
|       `-- WorkspaceArtifactReader.cs
|
|-- tests/
|   |-- AiReliabilityEngineering.Core.Tests/
|   |   `-- Build/
|   |       |-- CommandReportTests.cs
|   |       `-- BuildTestReportTests.cs
|   |
|   `-- AiReliabilityEngineering.Orchestration.Tests/
|       `-- Agents/
|           |-- TemplateCodeAgentTests.cs
|           |-- DotnetTemplateProjectWriterTests.cs
|           |-- BuildTestAgentTests.cs
|           |-- BuildTestReportWriterTests.cs
|           `-- WorkspaceArtifactReaderTests.cs
|
`-- docs/
    |-- template-code-agent.md
    |-- build-test-agent.md
    `-- demo-dotnet-workspace.md
```

If the repository already uses a different folder layout, follow the existing style, but keep the same logical coverage.

---

# Required Core Models

## 1. CommandReport

Create:

```text
src/AiReliabilityEngineering.Core/Build/CommandReport.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Build;
```

Purpose:

- represent one executed command and its result;
- used to generate Markdown reports.

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Build;

public sealed record CommandReport
{
    public CommandReport(
        string command,
        string workingDirectory,
        int exitCode,
        string standardOutput,
        string standardError,
        TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new ArgumentException("Working directory is required.", nameof(workingDirectory));
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must not be negative.");
        }

        Command = command;
        WorkingDirectory = workingDirectory;
        ExitCode = exitCode;
        StandardOutput = standardOutput ?? string.Empty;
        StandardError = standardError ?? string.Empty;
        Duration = duration;
    }

    public string Command { get; }

    public string WorkingDirectory { get; }

    public int ExitCode { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public TimeSpan Duration { get; }

    public bool Succeeded => ExitCode == 0;
}
```

---

## 2. BuildTestReport

Create:

```text
src/AiReliabilityEngineering.Core/Build/BuildTestReport.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Build;
```

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Build;

public sealed record BuildTestReport
{
    public BuildTestReport(
        CommandReport build,
        CommandReport? test)
    {
        Build = build ?? throw new ArgumentNullException(nameof(build));
        Test = test;
    }

    public CommandReport Build { get; }

    public CommandReport? Test { get; }

    public bool Succeeded => Build.Succeeded && (Test?.Succeeded ?? true);
}
```

Rules:

- Test report may be null if build failed and tests were not run.
- If build fails, BuildTestAgent should not run tests.

---

# Required Orchestration Components

## 3. WorkspaceArtifactReader

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/WorkspaceArtifactReader.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- read previously generated artifacts needed by TemplateCodeAgent;
- keep file reading separate from code generation logic.

Suggested responsibilities:

- read `artifacts/specification.json`;
- optionally read `artifacts/tasks.json`;
- optionally read `artifacts/README.md`;
- return simple strings or typed models.

Minimum required behavior for this step:

```csharp
public sealed class WorkspaceArtifactReader
{
    public Task<string> ReadSpecificationJsonAsync(
        RunContext runContext,
        CancellationToken cancellationToken);

    public Task<string?> TryReadTasksJsonAsync(
        RunContext runContext,
        CancellationToken cancellationToken);
}
```

Rules:

- Use `runContext.Paths.ArtifactsDirectory`.
- Throw FileNotFoundException if specification.json is missing.
- Return null for missing tasks.json.
- Do not read from workspace.
- Use actual current `RunContext` and `RunPaths` model names.

---

## 4. DotnetTemplateProjectWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/DotnetTemplateProjectWriter.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- write deterministic .NET CLI project into the run workspace.

Suggested signature:

```csharp
public sealed class DotnetTemplateProjectWriter
{
    public Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        RunContext runContext,
        string specificationJson,
        string? tasksJson,
        CancellationToken cancellationToken)
    {
        // write generated project files
    }
}
```

Rules:

- Use `runContext.Paths.WorkspaceDirectory`.
- Ensure directories exist.
- Delete existing generated project files only inside workspace if needed.
- Do not write outside workspace.
- Do not call `dotnet new`; write files directly for deterministic output.
- Return artifact refs for generated workspace files.

### Required generated structure

```text
workspace/
|-- Directory.Packages.props
|-- GeneratedTool.slnx
|-- src/
|   `-- GeneratedTool.Cli/
|       |-- GeneratedTool.Cli.csproj
|       `-- Program.cs
`-- tests/
    `-- GeneratedTool.Cli.Tests/
        |-- GeneratedTool.Cli.Tests.csproj
        `-- SmokeTests.cs
```

### Required workspace-local Directory.Packages.props

Generated workspaces may be created under any current directory, not necessarily under the repository root.

Therefore, DotnetTemplateProjectWriter must write a workspace-local:

```text
workspace/Directory.Packages.props
```

This prevents restore failures when the generated test project omits versions and no ancestor `Directory.Packages.props` exists.

Required content should include at least the packages needed by the generated test project.

Use versions consistent with the current repository if easy to determine. If not, use stable explicit versions.

Example:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
    <PackageVersion Include="xunit.v3" Version="3.2.2" />
  </ItemGroup>
</Project>
```

If the repository already uses newer versions, prefer matching the repository.

The generated test csproj should omit package versions and rely on this workspace-local file.

### Required GeneratedTool.Cli.csproj

Use a minimal console app project.

Suggested content:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

If the current repository target framework is not `net10.0`, use the same target framework as the repository to keep build stable.

### Required Program.cs

Suggested behavior:

- `--help` prints help and exits 0;
- no args prints a short message and exits 0;
- `--version` prints `0.1.0` and exits 0;
- unknown args print message and exit 0 or 2, but tests must match.

Suggested simple content:

```csharp
Console.WriteLine("Generated tool from AIRE");
Console.WriteLine("This is a deterministic demo project generated in the run workspace.");
```

Better minimal argument handling is acceptable, but keep it simple.

### Required test project

Use xUnit and match the repo style.

GeneratedTool.Cli.Tests.csproj should use package references without versions and include Xunit globally through the `Using` item pattern.

Required content shape:

```xml
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

</Project>
```

If the current repository target framework is not `net10.0`, use the same target framework as the repository.

SmokeTests.cs should compile with the generated test project.

Required SmokeTests.cs content shape:

```csharp
namespace GeneratedTool.Cli.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Smoke_test_passes()
    {
        Assert.True(true);
    }
}
```

Because the generated test csproj includes:

```xml
<Using Include="Xunit" />
```

the test file does not need `using Xunit;`.

If Codex chooses not to include `<Using Include="Xunit" />`, then SmokeTests.cs must include:

```csharp
using Xunit;
```

Preferred approach: use `<Using Include="Xunit" />` to match the repository style.

### Required GeneratedTool.slnx

The generated workspace must contain a valid minimal `.slnx` file referencing both generated projects.

Do not write arbitrary placeholder text.

Use a valid `.slnx` XML shape consistent with the repository's existing simple `.slnx` format.

Required intent:

```text
GeneratedTool.slnx references:
- src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
- tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
```

If the repository already has a simple `.slnx` XML example, mirror that style.

If unsure, use this minimal XML structure and adjust only if the repo uses a different valid SLNX shape:

```xml
<Solution>
  <Project Path="src/GeneratedTool.Cli/GeneratedTool.Cli.csproj" />
  <Project Path="tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj" />
</Solution>
```

BuildTestAgent should still build/test explicit project files, not the solution file.

---

## 5. TemplateCodeAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/TemplateCodeAgent.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

The agent should implement existing `IAgent`.

Suggested dependencies:

```csharp
public sealed class TemplateCodeAgent : IAgent
{
    private readonly IRunLogger _logger;
    private readonly WorkspaceArtifactReader _artifactReader;
    private readonly DotnetTemplateProjectWriter _projectWriter;

    public TemplateCodeAgent(
        IRunLogger logger,
        WorkspaceArtifactReader? artifactReader = null,
        DotnetTemplateProjectWriter? projectWriter = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artifactReader = artifactReader ?? new WorkspaceArtifactReader();
        _projectWriter = projectWriter ?? new DotnetTemplateProjectWriter();
    }

    public string Name => "TemplateCodeAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // implementation
    }
}
```

### Required execution flow

1. Check cancellation at the start.
2. Validate `context` is not null.
3. Log start event.
4. Read specification JSON.
5. Try read tasks JSON.
6. Generate deterministic .NET CLI workspace project.
7. Log completion.
8. Return AgentResult.Success with generated workspace artifact refs.

Failure behavior:

- missing specification.json returns AgentResult.Failure;
- file/write errors return AgentResult.Failure;
- do not swallow OperationCanceledException.

---

## 6. BuildTestReportWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/BuildTestReportWriter.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- write:
  - `reports/build.md`
  - `reports/tests.md`

Suggested signature:

```csharp
public sealed class BuildTestReportWriter
{
    public Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        BuildTestReport report,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        // write markdown reports
    }
}
```

Rules:

- Use `runContext.Paths.ReportsDirectory`.
- Ensure reports directory exists.
- Write deterministic Markdown.
- Include:
  - command;
  - working directory;
  - exit code;
  - duration;
  - stdout fenced block;
  - stderr fenced block.
- If tests were not run because build failed, `tests.md` should say so.

---

## 7. BuildTestAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/BuildTestAgent.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

The agent should implement existing `IAgent`.

Suggested dependencies:

```csharp
public sealed class BuildTestAgent : IAgent
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(2);

    private readonly IToolExecutor _toolExecutor;
    private readonly IRunLogger _logger;
    private readonly BuildTestReportWriter _reportWriter;

    public BuildTestAgent(
        IToolExecutor toolExecutor,
        IRunLogger logger,
        BuildTestReportWriter? reportWriter = null)
    {
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reportWriter = reportWriter ?? new BuildTestReportWriter();
    }

    public string Name => "BuildTestAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // implementation
    }
}
```

### Required execution flow

1. Check cancellation at the start.
2. Validate context is not null.
3. Log start event.
4. Verify workspace directory exists.
5. Run build command with timeout = 2 minutes.
6. If build fails:
   - write build.md;
   - write tests.md saying tests were not run;
   - return AgentResult.Failure.
7. Run test command with timeout = 2 minutes.
8. Write build.md and tests.md.
9. If tests fail:
   - return AgentResult.Failure.
10. Return AgentResult.Success with report artifact refs.

### Required commands

Use hardcoded commands:

```bash
dotnet build src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
dotnet test tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
```

Working directory:

```text
runContext.Paths.WorkspaceDirectory
```

If current ToolExecutionRequest expects command and arguments separately, represent them accordingly.

Required timeout for both commands:

```text
00:02:00
```

Do not run arbitrary user-provided commands.

---

# Tool Executor Requirements

If existing `ShellToolExecutor` does not support the required data, minimally update it.

Required ToolExecutionRequest data:

- command;
- arguments if applicable;
- working directory;
- timeout.

Required ToolExecutionResult information:

- exit code;
- stdout;
- stderr;
- duration if available;
- command text or enough data to build command text;
- timeout failure if supported.

If duration is not already available, measure duration inside BuildTestAgent around executor call.

Timeout failures must be represented as non-zero command failures in reports.

Do not over-refactor tool execution in this step.

---

# Workflow Profile Changes

## 8. Extend WorkflowProfile

Add:

```text
ai-demo-dotnet
```

Enum value:

```csharp
AiDemoDotnet
```

Current enum likely has:

```csharp
Fake,
AiRequirements,
AiDemo
```

Update to:

```csharp
Fake,
AiRequirements,
AiDemo,
AiDemoDotnet
```

---

## 9. Update WorkflowProfileParser

Support CLI name:

```text
ai-demo-dotnet
```

Required supported names after this step:

```text
fake
ai-requirements
ai-demo
ai-demo-dotnet
```

Rules:

- Missing profile maps to Fake.
- Unknown profile returns false.
- ToCliName maps AiDemoDotnet to `ai-demo-dotnet`.
- SupportedCliNames includes `ai-demo-dotnet`.

---

## 10. Update AgentPipelineFactory

For `WorkflowProfile.AiDemoDotnet`, create pipeline:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> TemplateCodeAgent
Testing       -> BuildTestAgent
Review        -> FakeReviewerAgent
```

Use selected provider for AI agents.

Use `IToolExecutor` for BuildTestAgent.

For production CLI composition, the `IToolExecutor` implementation should be `ShellToolExecutor` from Infrastructure.

For tests, use fake/recording tool executors.

Rules:

- Fake profile remains unchanged.
- AiRequirements profile remains unchanged.
- AiDemo profile remains unchanged.
- AiDemoDotnet adds real template code and build/test validation.
- Orchestration project must remain infrastructure-agnostic.

---

## 11. Update CompositionRoot

Update only the CLI composition root to provide ShellToolExecutor.

The CLI project already references Infrastructure and is the correct place to wire infrastructure implementations.

Conceptual shape:

```csharp
public static AgentPipelineFactory CreateAgentPipelineFactory()
{
    return new AgentPipelineFactory(
        CreateAiProvider,
        CreateToolExecutor);
}

public static IToolExecutor CreateToolExecutor()
{
    return new ShellToolExecutor();
}
```

Adapt to existing constructors and project style.

Rules:

- BuildTestAgent depends only on IToolExecutor.
- AgentPipelineFactory accepts IToolExecutor or a factory/delegate that returns IToolExecutor.
- ShellToolExecutor is constructed only in AiReliabilityEngineering.Cli composition root or Infrastructure tests.
- AiReliabilityEngineering.Orchestration must not reference AiReliabilityEngineering.Infrastructure.
- Do not introduce a DI container if the project does not already use one.

---

# Tests

## 12. Core build model tests

Add tests under:

```text
tests/AiReliabilityEngineering.Core.Tests/Build/
```

Tests for CommandReport:

- accepts valid values;
- rejects blank command;
- rejects blank working directory;
- rejects negative duration;
- Succeeded is true when exit code is 0;
- Succeeded is false when exit code is non-zero.

Tests for BuildTestReport:

- rejects null build report;
- Succeeded is true when build succeeds and test is null;
- Succeeded is true when both build/test succeed;
- Succeeded is false when build fails;
- Succeeded is false when test fails.

---

## 13. WorkspaceArtifactReader tests

Use real RunContext and RunPaths.

Required tests:

- reads specification.json from artifacts directory;
- returns tasks.json when present;
- returns null when tasks.json missing;
- throws FileNotFoundException when specification.json missing.

---

## 14. DotnetTemplateProjectWriter tests

Use real RunContext and RunPaths with temp directories.

Required tests:

- creates workspace directories;
- writes Directory.Packages.props;
- Directory.Packages.props contains Microsoft.NET.Test.Sdk and xunit.v3 versions;
- writes valid minimal GeneratedTool.slnx;
- GeneratedTool.slnx references CLI and test csproj paths;
- writes GeneratedTool.Cli.csproj;
- writes Program.cs;
- writes GeneratedTool.Cli.Tests.csproj;
- GeneratedTool.Cli.Tests.csproj references xunit.v3, not xunit or xunit.runner.visualstudio;
- GeneratedTool.Cli.Tests.csproj contains `<OutputType>Exe</OutputType>`;
- GeneratedTool.Cli.Tests.csproj contains `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`;
- GeneratedTool.Cli.Tests.csproj contains `<Using Include="Xunit" />`;
- writes SmokeTests.cs;
- SmokeTests.cs compiles with the generated test project style;
- returns artifact refs including Directory.Packages.props and GeneratedTool.slnx;
- never writes outside workspace.

Optional but useful:

- generated Program.cs contains `Generated tool from AIRE`;
- generated test file contains a passing smoke test.

---

## 15. TemplateCodeAgent tests

Use real RunContext and RunPaths.

Required tests:

- succeeds when specification.json exists;
- creates generated workspace project files;
- returns artifact refs;
- missing specification.json returns AgentResult failure;
- cancellation propagates OperationCanceledException.

---

## 16. BuildTestReportWriter tests

Required tests:

- writes reports/build.md;
- writes reports/tests.md;
- reports contain command, exit code, stdout, stderr;
- tests.md says tests were not run when test report is null;
- returned artifact refs include both files.

---

## 17. BuildTestAgent tests

Use fake/recording tool executor.

Do not require real dotnet execution for unit tests.

Required tests:

- runs build command first;
- uses timeout of 2 minutes for build command;
- uses timeout of 2 minutes for test command;
- does not run test command when build fails;
- runs test command when build succeeds;
- writes build.md and tests.md;
- returns success when build and test exit code 0;
- returns failure when build fails;
- returns failure when tests fail;
- timeout is reported as a failed command;
- uses workspace directory as working directory;
- cancellation propagates OperationCanceledException.

If adding one integration test with real dotnet is acceptable in repository style, mark it clearly and ensure it is reliable. Otherwise do not add real process integration tests.

---

## 18. Workflow profile tests

Update WorkflowProfileParser tests:

- parses `ai-demo-dotnet`;
- ToCliName maps AiDemoDotnet to `ai-demo-dotnet`;
- SupportedCliNames includes `ai-demo-dotnet`.

Update AgentPipelineFactory tests:

- AiDemoDotnet pipeline contains:
  - AiRequirementsAgent
  - AiDocumentationAgent
  - AiPlannerAgent
  - TemplateCodeAgent
  - BuildTestAgent
  - FakeReviewerAgent
- step order remains:
  - Requirements
  - Documentation
  - Planning
  - Code
  - Testing
  - Review
- Orchestration tests use fake/recording IToolExecutor, not ShellToolExecutor.

---

## 19. Orchestrator ai-demo-dotnet tests

Add test:

```text
ai-demo-dotnet profile completes with fake provider and fake tool executor
```

Arrange:

- RunRequest with WorkflowProfile.AiDemoDotnet and Fake provider.
- Use a test pipeline factory or composition that injects a fake tool executor returning successful build/test results.
- Run orchestrator.

Assert:

- run succeeds;
- final status is Completed;
- artifacts/specification.json exists;
- artifacts/requirements.md exists;
- artifacts/README.md exists;
- artifacts/PLAN.md exists;
- artifacts/tasks.json exists;
- workspace/Directory.Packages.props exists;
- workspace/GeneratedTool.slnx exists;
- workspace generated project files exist;
- reports/build.md exists;
- reports/tests.md exists;
- run-state step agents include:
  - AiRequirementsAgent
  - AiDocumentationAgent
  - AiPlannerAgent
  - TemplateCodeAgent
  - BuildTestAgent

Do not make this orchestration unit test depend on real dotnet process execution.

---

## 20. CLI tests

Update CLI tests:

- help mentions `ai-demo-dotnet`;
- `run idea.md --profile ai-demo-dotnet --provider fake` succeeds if the real dotnet tool is available in the test environment OR use existing CLI test strategy to avoid full shell execution;
- generated run contains:
  - workspace/Directory.Packages.props;
  - workspace files;
  - reports/build.md;
  - reports/tests.md.

If CLI tests cannot safely run real build/test commands, limit CLI tests to parser/help/profile validation and cover full behavior in orchestration tests with fake tool executor.

Do not make automated tests flaky by depending on external environment unexpectedly.

---

# Documentation

## 21. Add docs/template-code-agent.md

Create:

```text
docs/template-code-agent.md
```

Explain:

- TemplateCodeAgent creates deterministic .NET CLI project;
- it writes only inside run workspace;
- it writes workspace-local Directory.Packages.props;
- it writes a valid minimal GeneratedTool.slnx;
- it does not use AI to write source code;
- it is a safe baseline before Codex/OpenCode integration.

---

## 22. Add docs/build-test-agent.md

Create:

```text
docs/build-test-agent.md
```

Explain:

- BuildTestAgent runs hardcoded build/test commands in workspace;
- each command has a 2-minute timeout;
- captures stdout/stderr/exit code;
- writes build.md and tests.md;
- fails the run if build/test fails;
- future code agents will reuse this validation path;
- ShellToolExecutor is wired only in the CLI composition root.

---

## 23. Add docs/demo-dotnet-workspace.md

Create:

```text
docs/demo-dotnet-workspace.md
```

Include command:

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

Explain that source code is template-based and not AI-generated yet.

---

## 24. Update docs/workflow-profiles.md

Add:

```markdown
## ai-demo-dotnet

Runs AI-aware requirements, documentation, and planning agents, then generates a deterministic .NET CLI workspace project and validates it with build/test commands.

```bash
aire run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
```
```

---

## 25. Update docs/wiki.md

If exists, add:

```markdown
## TemplateCodeAgent

Creates a deterministic .NET CLI project in the run workspace.

## BuildTestAgent

Runs build and test commands in the run workspace and writes build/test reports. Each command has a 2-minute timeout.

## ai-demo-dotnet Profile

Runs requirements, documentation, planning, template code generation, build/test validation, and fake review.
```

---

## 26. Update README.md

Add a short section:

```markdown
## Generated .NET Workspace Demo

Generate requirements, documentation, planning artifacts, a deterministic .NET CLI project, and build/test reports:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
```

The generated project is written under the run workspace:

- workspace/Directory.Packages.props
- workspace/GeneratedTool.slnx
- workspace/src/GeneratedTool.Cli/
- workspace/tests/GeneratedTool.Cli.Tests/

Build and test reports are written to:

- reports/build.md
- reports/tests.md

This demo does not use AI to write source code yet. It uses a deterministic template so the build/test validation path is stable.
```

---

# Existing Workflow Must Remain Stable

These commands must still pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
```

OpenAI manual demo should still work:

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider openai --model <model-name>
```

Cleanup command should still work when used against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Suggested Implementation Order for Codex

Follow this order.

## Task 1: Add Core build models

Create:

```text
src/AiReliabilityEngineering.Core/Build/CommandReport.cs
src/AiReliabilityEngineering.Core/Build/BuildTestReport.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 2: Add WorkspaceArtifactReader

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/WorkspaceArtifactReader.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 3: Add DotnetTemplateProjectWriter and TemplateCodeAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/DotnetTemplateProjectWriter.cs
src/AiReliabilityEngineering.Orchestration/Agents/TemplateCodeAgent.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 4: Add BuildTestReportWriter and BuildTestAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/BuildTestReportWriter.cs
src/AiReliabilityEngineering.Orchestration/Agents/BuildTestAgent.cs
```

Add tests with fake tool executor.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 5: Add ai-demo-dotnet workflow profile

Update:

```text
WorkflowProfile
WorkflowProfileParser
AgentPipelineFactory
CompositionRoot
CLI help/tests if needed
```

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 6: Add orchestration and CLI tests for ai-demo-dotnet

Prefer fake tool executor for orchestration tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 7: Add docs and README updates

Create/update docs.

Run final verification.

---

# Acceptance Criteria

The task is complete when all criteria are satisfied.

## Core

- CommandReport exists and validates inputs.
- BuildTestReport exists and validates inputs.
- Core build tests pass.

## Template Code Agent

- WorkspaceArtifactReader exists.
- DotnetTemplateProjectWriter exists.
- TemplateCodeAgent exists.
- TemplateCodeAgent implements IAgent.
- TemplateCodeAgent writes generated .NET CLI project under workspace.
- TemplateCodeAgent writes workspace/Directory.Packages.props with required test package versions.
- TemplateCodeAgent writes valid minimal workspace/GeneratedTool.slnx.
- TemplateCodeAgent writes GeneratedTool.Cli.Tests.csproj with `<Using Include="Xunit" />`.
- TemplateCodeAgent does not write outside workspace.
- TemplateCodeAgent returns generated artifact refs including Directory.Packages.props and GeneratedTool.slnx.
- Tests pass.

## Build/Test Agent

- BuildTestReportWriter exists.
- BuildTestAgent exists.
- BuildTestAgent implements IAgent.
- BuildTestAgent uses IToolExecutor only.
- BuildTestAgent does not reference ShellToolExecutor directly.
- BuildTestAgent runs build before tests.
- BuildTestAgent uses a 2-minute timeout for build.
- BuildTestAgent uses a 2-minute timeout for tests.
- BuildTestAgent skips tests if build fails.
- BuildTestAgent writes reports/build.md.
- BuildTestAgent writes reports/tests.md.
- BuildTestAgent returns failure on build/test failure.
- Timeout failures are reported as non-zero command failures.
- Tests pass.

## Composition

- ShellToolExecutor is wired only from AiReliabilityEngineering.Cli composition root.
- AiReliabilityEngineering.Orchestration remains infrastructure-agnostic.
- Tests use fake/recording IToolExecutor implementations.

## Workflow

- WorkflowProfile includes AiDemoDotnet.
- CLI profile name is `ai-demo-dotnet`.
- ai-demo-dotnet profile uses:
  - AiRequirementsAgent
  - AiDocumentationAgent
  - AiPlannerAgent
  - TemplateCodeAgent
  - BuildTestAgent
  - FakeReviewerAgent
- fake profile remains unchanged.
- ai-requirements profile remains unchanged.
- ai-demo profile remains unchanged.

## Demo

- `--profile ai-demo-dotnet --provider fake` completes successfully.
- The run contains:
  - artifacts/specification.json
  - artifacts/requirements.md
  - artifacts/README.md
  - artifacts/PLAN.md
  - artifacts/tasks.json
  - workspace/Directory.Packages.props
  - workspace/GeneratedTool.slnx
  - workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
  - workspace/src/GeneratedTool.Cli/Program.cs
  - workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
  - workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs
  - reports/build.md
  - reports/tests.md

## Documentation

- docs/template-code-agent.md exists.
- docs/build-test-agent.md exists.
- docs/demo-dotnet-workspace.md exists.
- docs/workflow-profiles.md mentions ai-demo-dotnet.
- README includes generated .NET workspace demo.
- Docs explain source code is template-based and not AI-generated yet.

## Verification

These commands pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
```

Cleanup passes against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Notes for Codex

- Keep this step focused on deterministic workspace generation and validation.
- Do not implement real AI code generation.
- Do not add Codex or OpenCode.
- Do not add Docker.
- Do not add Kubernetes.
- Do not add Git integration.
- Do not add multiple templates.
- Do not accept arbitrary commands.
- BuildTestAgent must only run hardcoded dotnet build/test commands.
- Build/test commands must run only inside the run workspace.
- Build/test commands must use a 2-minute timeout.
- Use temporary directories in tests.
- Preserve existing stable CLI behavior.
- Save this file as UTF-8 without BOM.
