# PLAN.md - AIRE Skeleton

## Project

Repository name: `ai-reliability-engineering`

Product name: `AIRE - AI Reliability Engineering`

CLI command name: `aire`

.NET solution file: `AiReliabilityEngineering.slnx`

Main CLI project: `AiReliabilityEngineering.Cli`

## Purpose

Create the first working skeleton of AIRE.

AIRE is a local-first development orchestrator for building reliable AI-assisted software workflows. The first implementation must not integrate real AI yet. It must create the solution, projects, abstractions, fake agents, orchestration pipeline, run folder layout, logs, state file, placeholder artifacts, and tests.

The first milestone proves that the core workflow works end-to-end:

```text
input idea.md -> run folder -> fake agents -> artifacts -> reports -> logs -> run-state.json -> final summary
```

The system must remain runnable and testable after this implementation.

## Non-negotiable requirements

1. Use .NET 10 and target `net10.0`.
2. Use `AiReliabilityEngineering.slnx` as the solution file.
3. Use `AiReliabilityEngineering.*` as project and namespace prefix.
4. The CLI command/project must expose the command shape `run <idea-file>`.
5. Do not implement real AI calls in this milestone.
6. Do not add Codex/OpenCode integration in this milestone.
7. Do not run commands outside the configured workspace/run directory except normal build/test commands.
8. Keep `AiReliabilityEngineering.Core` clean from infrastructure dependencies.
9. Every generated run must be inspectable through files.
10. `dotnet build` and `dotnet test` must pass.

## Expected final command

The following command must work from the repository root:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

It must create a new run folder under `runs/` and print a final summary.

Later, this project may be packaged as a global tool exposing `aire`, but that is not required in this milestone.

## Expected repository structure

Create this structure:

```text
ai-reliability-engineering/
├─ AiReliabilityEngineering.slnx
├─ Directory.Build.props
├─ Directory.Packages.props
├─ global.json
├─ README.md
├─ PLAN.md
├─ .gitignore
│
├─ src/
│  ├─ AiReliabilityEngineering.Cli/
│  │  ├─ AiReliabilityEngineering.Cli.csproj
│  │  └─ Program.cs
│  │
│  ├─ AiReliabilityEngineering.Core/
│  │  ├─ AiReliabilityEngineering.Core.csproj
│  │  ├─ Agents/
│  │  ├─ Artifacts/
│  │  ├─ Runs/
│  │  ├─ Steps/
│  │  └─ Tools/
│  │
│  ├─ AiReliabilityEngineering.Orchestration/
│  │  ├─ AiReliabilityEngineering.Orchestration.csproj
│  │  ├─ Agents/
│  │  ├─ Pipeline/
│  │  ├─ RunManagement/
│  │  └─ State/
│  │
│  └─ AiReliabilityEngineering.Infrastructure/
│     ├─ AiReliabilityEngineering.Infrastructure.csproj
│     ├─ FileSystem/
│     ├─ Logging/
│     ├─ Serialization/
│     └─ Tools/
│
├─ tests/
│  ├─ AiReliabilityEngineering.Core.Tests/
│  │  └─ AiReliabilityEngineering.Core.Tests.csproj
│  │
│  ├─ AiReliabilityEngineering.Orchestration.Tests/
│  │  └─ AiReliabilityEngineering.Orchestration.Tests.csproj
│  │
│  └─ AiReliabilityEngineering.Cli.Tests/
│     └─ AiReliabilityEngineering.Cli.Tests.csproj
│
├─ samples/
│  └─ idea.md
│
├─ docs/
│  ├─ architecture.md
│  ├─ workflow.md
│  └─ decisions/
│     └─ 0001-local-first-cli.md
│
└─ runs/
   └─ .gitkeep
```

## Project responsibilities

### `AiReliabilityEngineering.Cli`

Console application.

Responsibilities:

- parse the command line;
- support `run <idea-file>`;
- validate that the input file exists;
- create an orchestration request;
- call the orchestrator;
- print final summary;
- return exit code `0` for success and non-zero for failure.

This project must not contain orchestration business logic.

References:

- `AiReliabilityEngineering.Core`
- `AiReliabilityEngineering.Orchestration`
- `AiReliabilityEngineering.Infrastructure`

### `AiReliabilityEngineering.Core`

Class library.

Responsibilities:

- define core models;
- define agent contracts;
- define tool executor contracts;
- define run state models;
- define artifact models.

This project must not reference infrastructure packages, file system implementations, logging sinks, shell execution, Codex, OpenCode, Docker, Git, or AI APIs.

### `AiReliabilityEngineering.Orchestration`

Class library.

Responsibilities:

- execute the workflow;
- create and update run context;
- execute agents in order;
- stop on failure;
- update run state;
- return workflow result.

This project depends on `Core`.

It may depend on abstractions for state writing and logging, but concrete file system implementations should live in `Infrastructure`.

### `AiReliabilityEngineering.Infrastructure`

Class library.

Responsibilities:

- create run directories;
- copy input files;
- write JSON state files;
- write placeholder artifacts;
- write console/file logs;
- provide fake and shell tool executors.

This project can depend on `Core` and `Orchestration`.

Do not add Codex/OpenCode executors yet.

## Recommended dependencies

Keep dependencies minimal.

Use:

- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Logging.Console`
- `System.Text.Json`
- `xunit`
- `xunit.runner.visualstudio`
- `FluentAssertions`
- `Microsoft.NET.Test.Sdk`

Do not add:

- MediatR
- EF Core
- SQLite
- OpenAI SDK
- Docker SDK
- GitHub SDK
- System.CommandLine unless strongly necessary

Manual CLI parsing is acceptable for this milestone.

## Solution and project creation

Create the solution and projects using .NET CLI.

Required solution file name:

```text
AiReliabilityEngineering.slnx
```

If the installed SDK creates `AiReliabilityEngineering.slnx` automatically, use it.

If it creates `AiReliabilityEngineering.sln`, migrate or convert it to `.slnx` and keep only `AiReliabilityEngineering.slnx` in the repository.

Create projects:

```bash
dotnet new sln -n AiReliabilityEngineering

dotnet new console -n AiReliabilityEngineering.Cli -o src/AiReliabilityEngineering.Cli --framework net10.0

dotnet new classlib -n AiReliabilityEngineering.Core -o src/AiReliabilityEngineering.Core --framework net10.0

dotnet new classlib -n AiReliabilityEngineering.Orchestration -o src/AiReliabilityEngineering.Orchestration --framework net10.0

dotnet new classlib -n AiReliabilityEngineering.Infrastructure -o src/AiReliabilityEngineering.Infrastructure --framework net10.0

dotnet new xunit -n AiReliabilityEngineering.Core.Tests -o tests/AiReliabilityEngineering.Core.Tests --framework net10.0

dotnet new xunit -n AiReliabilityEngineering.Orchestration.Tests -o tests/AiReliabilityEngineering.Orchestration.Tests --framework net10.0

dotnet new xunit -n AiReliabilityEngineering.Cli.Tests -o tests/AiReliabilityEngineering.Cli.Tests --framework net10.0
```

Add project references:

```bash
dotnet add src/AiReliabilityEngineering.Orchestration/AiReliabilityEngineering.Orchestration.csproj reference src/AiReliabilityEngineering.Core/AiReliabilityEngineering.Core.csproj

dotnet add src/AiReliabilityEngineering.Infrastructure/AiReliabilityEngineering.Infrastructure.csproj reference src/AiReliabilityEngineering.Core/AiReliabilityEngineering.Core.csproj

dotnet add src/AiReliabilityEngineering.Infrastructure/AiReliabilityEngineering.Infrastructure.csproj reference src/AiReliabilityEngineering.Orchestration/AiReliabilityEngineering.Orchestration.csproj

dotnet add src/AiReliabilityEngineering.Cli/AiReliabilityEngineering.Cli.csproj reference src/AiReliabilityEngineering.Core/AiReliabilityEngineering.Core.csproj

dotnet add src/AiReliabilityEngineering.Cli/AiReliabilityEngineering.Cli.csproj reference src/AiReliabilityEngineering.Orchestration/AiReliabilityEngineering.Orchestration.csproj

dotnet add src/AiReliabilityEngineering.Cli/AiReliabilityEngineering.Cli.csproj reference src/AiReliabilityEngineering.Infrastructure/AiReliabilityEngineering.Infrastructure.csproj

dotnet add tests/AiReliabilityEngineering.Core.Tests/AiReliabilityEngineering.Core.Tests.csproj reference src/AiReliabilityEngineering.Core/AiReliabilityEngineering.Core.csproj

dotnet add tests/AiReliabilityEngineering.Orchestration.Tests/AiReliabilityEngineering.Orchestration.Tests.csproj reference src/AiReliabilityEngineering.Core/AiReliabilityEngineering.Core.csproj

dotnet add tests/AiReliabilityEngineering.Orchestration.Tests/AiReliabilityEngineering.Orchestration.Tests.csproj reference src/AiReliabilityEngineering.Orchestration/AiReliabilityEngineering.Orchestration.csproj

dotnet add tests/AiReliabilityEngineering.Orchestration.Tests/AiReliabilityEngineering.Orchestration.Tests.csproj reference src/AiReliabilityEngineering.Infrastructure/AiReliabilityEngineering.Infrastructure.csproj

dotnet add tests/AiReliabilityEngineering.Cli.Tests/AiReliabilityEngineering.Cli.Tests.csproj reference src/AiReliabilityEngineering.Cli/AiReliabilityEngineering.Cli.csproj
```

Add all projects to the solution:

```bash
dotnet sln AiReliabilityEngineering.slnx add src/AiReliabilityEngineering.Cli/AiReliabilityEngineering.Cli.csproj

dotnet sln AiReliabilityEngineering.slnx add src/AiReliabilityEngineering.Core/AiReliabilityEngineering.Core.csproj

dotnet sln AiReliabilityEngineering.slnx add src/AiReliabilityEngineering.Orchestration/AiReliabilityEngineering.Orchestration.csproj

dotnet sln AiReliabilityEngineering.slnx add src/AiReliabilityEngineering.Infrastructure/AiReliabilityEngineering.Infrastructure.csproj

dotnet sln AiReliabilityEngineering.slnx add tests/AiReliabilityEngineering.Core.Tests/AiReliabilityEngineering.Core.Tests.csproj

dotnet sln AiReliabilityEngineering.slnx add tests/AiReliabilityEngineering.Orchestration.Tests/AiReliabilityEngineering.Orchestration.Tests.csproj

dotnet sln AiReliabilityEngineering.slnx add tests/AiReliabilityEngineering.Cli.Tests/AiReliabilityEngineering.Cli.Tests.csproj
```

## Repository configuration files

### `global.json`

Create a `global.json` that targets .NET 10.

Example:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

If the local SDK version differs but is .NET 10, use the installed .NET 10 SDK version.

### `Directory.Build.props`

Create a shared props file:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
    <AnalysisLevel>latest</AnalysisLevel>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

Do not enable `AnalysisMode=AllEnabledByDefault` in this skeleton. Keep analyzers strict enough without turning the repository into a warning trap.

### `Directory.Packages.props`

Use central package management.

Include test and logging packages.

Example package versions can be adjusted to installed/restored versions if needed.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="FluentAssertions" Version="8.8.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Console" Version="10.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>
</Project>
```

If package restore fails because a version is unavailable in the execution environment, choose the latest compatible stable version. Do not switch frameworks.

### `.gitignore`

Create `.gitignore`:

```gitignore
bin/
obj/
.vs/
.idea/
*.user
*.suo

runs/*
!runs/.gitkeep

.env
*.tmp
```

Do not ignore `PLAN.md`, `README.md`, `samples/idea.md`, or docs.

## Core models and interfaces

Implement the following models in `AiReliabilityEngineering.Core`.

### Agents

Folder:

```text
src/AiReliabilityEngineering.Core/Agents/
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Agents;

public interface IAgent
{
    string Name { get; }

    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken);
}
```

```csharp
namespace AiReliabilityEngineering.Core.Agents;

public sealed record AgentContext(
    RunContext Run,
    IReadOnlyDictionary<string, string> Properties);
```

```csharp
namespace AiReliabilityEngineering.Core.Agents;

public sealed record AgentResult(
    AgentStatus Status,
    string Message,
    IReadOnlyList<ArtifactRef> Artifacts)
{
    public bool IsSuccess => Status == AgentStatus.Succeeded;

    public static AgentResult Success(string message, IReadOnlyList<ArtifactRef>? artifacts = null)
        => new(AgentStatus.Succeeded, message, artifacts ?? Array.Empty<ArtifactRef>());

    public static AgentResult Failure(string message, IReadOnlyList<ArtifactRef>? artifacts = null)
        => new(AgentStatus.Failed, message, artifacts ?? Array.Empty<ArtifactRef>());
}
```

```csharp
namespace AiReliabilityEngineering.Core.Agents;

public enum AgentStatus
{
    Succeeded = 0,
    Failed = 1,
    Skipped = 2
}
```

### Artifacts

Folder:

```text
src/AiReliabilityEngineering.Core/Artifacts/
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Artifacts;

public sealed record ArtifactRef(
    ArtifactType Type,
    string RelativePath,
    string Description);
```

```csharp
namespace AiReliabilityEngineering.Core.Artifacts;

public enum ArtifactType
{
    Specification = 0,
    Documentation = 1,
    Plan = 2,
    Tasks = 3,
    Code = 4,
    Tests = 5,
    Review = 6,
    Log = 7,
    Report = 8,
    Other = 100
}
```

### Runs

Folder:

```text
src/AiReliabilityEngineering.Core/Runs/
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Runs;

public sealed record RunId(string Value)
{
    public override string ToString() => Value;

    public static RunId Create(DateTimeOffset utcNow)
    {
        var timestamp = utcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new RunId($"{timestamp}-{suffix}");
    }
}
```

Remember to import `System.Globalization`.

Create:

```csharp
namespace AiReliabilityEngineering.Core.Runs;

public sealed record RunPaths(
    string RootDirectory,
    string InputDirectory,
    string WorkspaceDirectory,
    string ArtifactsDirectory,
    string ReportsDirectory,
    string LogsDirectory,
    string StateFilePath);
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Runs;

public sealed record RunContext(
    RunId Id,
    string OriginalIdeaFilePath,
    string CopiedIdeaFilePath,
    RunPaths Paths,
    DateTimeOffset CreatedAtUtc);
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Runs;

public enum RunStatus
{
    Created = 0,
    Running = 1,
    RequirementsCompleted = 2,
    DocumentationCompleted = 3,
    PlanningCompleted = 4,
    CodeCompleted = 5,
    TestingCompleted = 6,
    ReviewCompleted = 7,
    Completed = 8,
    Failed = 9
}
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Runs;

public sealed record RunState(
    string RunId,
    RunStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<WorkflowStepResult> Steps,
    string? FailureMessage);
```

### Steps

Folder:

```text
src/AiReliabilityEngineering.Core/Steps/
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Steps;

public enum WorkflowStep
{
    Requirements = 0,
    Documentation = 1,
    Planning = 2,
    Code = 3,
    Testing = 4,
    Review = 5
}
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Steps;

public enum WorkflowStepStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4
}
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Steps;

public sealed record WorkflowStepResult(
    WorkflowStep Step,
    string AgentName,
    WorkflowStepStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string Message,
    IReadOnlyList<ArtifactRef> Artifacts);
```

### Tools

Folder:

```text
src/AiReliabilityEngineering.Core/Tools/
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Tools;

public interface IToolExecutor
{
    Task<ToolExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        CancellationToken cancellationToken);
}
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Tools;

public sealed record ToolExecutionRequest(
    string Command,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan Timeout);
```

Create:

```csharp
namespace AiReliabilityEngineering.Core.Tools;

public sealed record ToolExecutionResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc)
{
    public bool Succeeded => ExitCode == 0;
}
```

## Infrastructure abstractions and implementations

### Run logging

Create an interface for run logs.

Preferred location:

```text
src/AiReliabilityEngineering.Infrastructure/Logging/IRunLogger.cs
```

```csharp
namespace AiReliabilityEngineering.Infrastructure.Logging;

public interface IRunLogger
{
    Task InfoAsync(string message, CancellationToken cancellationToken);

    Task ErrorAsync(string message, Exception? exception, CancellationToken cancellationToken);
}
```

Create implementations:

- `ConsoleRunLogger`
- `FileRunLogger`
- `CompositeRunLogger`

`FileRunLogger` must append UTF-8 log lines to a configured log file.

Log format:

```text
2026-06-18T10:15:30.1234567+00:00 [INFO] Message
2026-06-18T10:15:31.1234567+00:00 [ERROR] Message Exception details
```

### Run state store

Create interface in orchestration:

```text
src/AiReliabilityEngineering.Orchestration/State/IRunStateStore.cs
```

```csharp
namespace AiReliabilityEngineering.Orchestration.State;

public interface IRunStateStore
{
    Task SaveAsync(RunState state, CancellationToken cancellationToken);
}
```

Create JSON implementation in infrastructure:

```text
src/AiReliabilityEngineering.Infrastructure/Serialization/JsonRunStateStore.cs
```

`JsonRunStateStore` must write indented JSON using `System.Text.Json`.

Use enum strings in JSON if practical. If not, numeric enum values are acceptable for this milestone, but string enum values are preferred.

### Run directory factory

Create:

```text
src/AiReliabilityEngineering.Orchestration/RunManagement/RunDirectoryFactory.cs
```

Responsibilities:

- create run ID;
- create folder layout;
- copy input file to `input/idea.md`;
- return `RunContext`.

Constructor inputs:

- runs root directory;
- clock abstraction or `TimeProvider`.

Use `TimeProvider.System` or a small custom `IClock` abstraction. Prefer `TimeProvider` if simple.

Required folder layout:

```text
runs/{run-id}/
├─ input/
├─ artifacts/
├─ reports/
├─ logs/
├─ workspace/
└─ run-state.json
```

The copied input file path must be:

```text
runs/{run-id}/input/idea.md
```

Even if the original file has another name, normalize it to `idea.md` in the run folder.

## Fake agents

Create fake agents in:

```text
src/AiReliabilityEngineering.Orchestration/Agents/
```

Agents:

- `FakeRequirementsAgent`
- `FakeDocumentationAgent`
- `FakePlannerAgent`
- `FakeCodeAgent`
- `FakeTestAgent`
- `FakeReviewerAgent`

All fake agents must implement `IAgent`.

Each fake agent must:

1. write a start log line;
2. create one or more placeholder files;
3. write a completed log line;
4. return `AgentResult.Success` with artifact references.

Use simple file writes. Do not call external tools.

### `FakeRequirementsAgent`

Name:

```text
RequirementsAgent
```

Writes:

```text
artifacts/specification.json
```

Content example:

```json
{
  "name": "placeholder-project",
  "summary": "Generated by fake requirements agent.",
  "requirements": [
    "Read initial idea file",
    "Generate placeholder workflow artifacts"
  ]
}
```

Artifact type:

```text
Specification
```

### `FakeDocumentationAgent`

Name:

```text
DocumentationAgent
```

Writes:

```text
artifacts/README.md
artifacts/PLAN.md
```

`README.md` content must mention that it is generated by the fake documentation agent.

`PLAN.md` content must mention that it is a placeholder plan.

Artifact types:

- `Documentation`
- `Plan`

### `FakePlannerAgent`

Name:

```text
PlannerAgent
```

Writes:

```text
artifacts/tasks.json
```

Content example:

```json
[
  {
    "id": "T001",
    "title": "Implement skeleton",
    "status": "placeholder"
  }
]
```

Artifact type:

```text
Tasks
```

### `FakeCodeAgent`

Name:

```text
CodeAgent
```

Writes:

```text
workspace/README.md
workspace/src/placeholder.txt
```

Create `workspace/src/` if needed.

Artifact type:

```text
Code
```

### `FakeTestAgent`

Name:

```text
TestAgent
```

Writes:

```text
reports/tests.md
```

Content should say tests are simulated and passed.

Artifact type:

```text
Tests
```

### `FakeReviewerAgent`

Name:

```text
ReviewerAgent
```

Writes:

```text
artifacts/review.md
```

Content should include:

```markdown
# Review

Status: Passed

This is a fake review report for the first AIRE skeleton.
```

Artifact type:

```text
Review
```

## Pipeline

Create pipeline classes in:

```text
src/AiReliabilityEngineering.Orchestration/Pipeline/
```

Suggested classes:

- `AgentPipeline`
- `AgentPipelineResult`
- `AgentPipelineStep`

The pipeline order must be:

```text
Requirements -> Documentation -> Planning -> Code -> Testing -> Review
```

Each pipeline step maps a `WorkflowStep` to an `IAgent`.

Rules:

1. Execute agents sequentially.
2. Before each agent starts, append/update a running step result.
3. Save run state after each step starts.
4. Save run state after each step completes.
5. If an agent fails, stop the pipeline.
6. Mark final run status as `Failed` if any agent fails.
7. Mark final run status as `Completed` only if all agents succeed.
8. Do not swallow exceptions. Convert exceptions to failed workflow results and log them.

## Orchestrator

Create:

```text
src/AiReliabilityEngineering.Orchestration/AireOrchestrator.cs
```

Create request/response models:

```text
src/AiReliabilityEngineering.Orchestration/RunManagement/RunRequest.cs
src/AiReliabilityEngineering.Orchestration/RunManagement/RunResult.cs
```

`RunRequest` fields:

```text
IdeaFilePath
RunsDirectory
```

`RunResult` fields:

```text
Succeeded
RunId
RunDirectory
Message
```

`AireOrchestrator` responsibilities:

1. validate request;
2. create run folder;
3. create loggers;
4. initialize run state as `Created`;
5. set run state to `Running`;
6. execute fake pipeline;
7. set final run state;
8. write final summary log;
9. return `RunResult`.

## CLI behavior

Implement manual parsing in `Program.cs`.

Supported commands:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

Behavior:

- If no arguments are provided, print usage and return non-zero.
- If command is not `run`, print usage and return non-zero.
- If idea file is missing, print a clear error and return non-zero.
- If idea file exists, call `AireOrchestrator`.
- Print final summary.

Usage text:

```text
AIRE - AI Reliability Engineering

Usage:
  aire run <idea-file>

Examples:
  aire run samples/idea.md
```

The executable file itself does not need to be named `aire` yet. The command shape should be documented as `aire`.

## Sample idea file

Create:

```text
samples/idea.md
```

Content:

```markdown
# Project Idea

Create a Redis TTL audit tool.

The tool should scan Redis keys, detect keys without TTL, group them by prefix, and generate a Markdown report.
```

## README.md

Create a compact README with:

- project name;
- purpose;
- first milestone description;
- how to build;
- how to test;
- how to run fake pipeline;
- expected output folder.

Required commands:

```bash
dotnet build AiReliabilityEngineering.slnx

dotnet test AiReliabilityEngineering.slnx

dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

## Documentation files

Create:

```text
docs/architecture.md
docs/workflow.md
docs/decisions/0001-local-first-cli.md
```

Keep them compact.

### `docs/architecture.md`

Must describe:

- CLI;
- Core;
- Orchestration;
- Infrastructure;
- fake agents;
- run folder contract.

### `docs/workflow.md`

Must describe:

```text
idea.md -> run folder -> fake pipeline -> artifacts -> reports -> logs -> final status
```

### `docs/decisions/0001-local-first-cli.md`

Must state:

- AIRE starts as a local-first CLI;
- no real AI integration in milestone 1;
- fake agents are intentional;
- every step must keep the system working.

## Tests

Tests must be meaningful and must pass.

### Core tests

Create tests for:

1. `RunId.Create` returns non-empty ID.
2. `RunId.Create` includes timestamp-like prefix.
3. `AgentResult.Success` sets `IsSuccess` to true.
4. `AgentResult.Failure` sets `IsSuccess` to false.
5. `ToolExecutionResult.Succeeded` is true for exit code `0` and false for non-zero.

### Orchestration tests

Create tests for the fake workflow.

Use a temporary directory per test.

Required tests:

1. `RunAsync_WithValidIdeaFile_CreatesRunDirectory`.
2. `RunAsync_WithValidIdeaFile_CopiesInputFileToInputIdeaMd`.
3. `RunAsync_WithValidIdeaFile_WritesRunStateJson`.
4. `RunAsync_WithValidIdeaFile_ExecutesAllFakeAgentsAndCreatesArtifacts`.
5. `RunAsync_WithValidIdeaFile_WritesLogs`.
6. `RunAsync_WithValidIdeaFile_ReturnsSuccess`.
7. Pipeline test: if one agent fails, pipeline stops and final result fails.

Expected artifacts:

```text
artifacts/specification.json
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json
artifacts/review.md
reports/tests.md
workspace/README.md
workspace/src/placeholder.txt
logs/orchestrator.log
run-state.json
```

### CLI tests

Implement at least basic argument behavior tests.

Preferred approach:

- extract command handling into a small testable class, for example `CliCommandHandler`;
- keep `Program.cs` thin.

Required tests:

1. Missing arguments returns non-zero.
2. Unknown command returns non-zero.
3. Missing idea file returns non-zero.

Do not make CLI tests brittle by requiring exact full console output. Check key substrings if needed.

## File writing rules

All generated text files must be written as UTF-8 without BOM if practical.

Use `Directory.CreateDirectory` before writing files.

Use relative paths in artifact references, never absolute paths.

Run output must never write outside the selected run directory, except console output.

## Run state JSON expectations

`run-state.json` must include at least:

```json
{
  "runId": "20260618-101530-abc12345",
  "status": "Completed",
  "createdAtUtc": "2026-06-18T10:15:30.0000000+00:00",
  "updatedAtUtc": "2026-06-18T10:15:31.0000000+00:00",
  "steps": [],
  "failureMessage": null
}
```

The property naming policy should use camelCase.

Enums should be serialized as strings if practical.

## Error handling requirements

1. Missing input file must fail before creating a run folder.
2. Failed agent must stop the pipeline.
3. Exceptions from agents must be logged and converted to failed run results.
4. A failed run must still write `run-state.json` if the run folder was created.
5. CLI must return non-zero for failed runs.

## Acceptance criteria

The implementation is complete when all criteria are met.

### Build/test criteria

```bash
dotnet build AiReliabilityEngineering.slnx

dotnet test AiReliabilityEngineering.slnx
```

Both commands must pass.

### Run criteria

This command must succeed:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

It must print a final summary containing:

```text
Run ID:
Run directory:
Status: Completed
```

### File output criteria

A new folder must appear under `runs/`.

It must contain:

```text
input/idea.md
artifacts/specification.json
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json
artifacts/review.md
reports/tests.md
logs/orchestrator.log
workspace/README.md
workspace/src/placeholder.txt
run-state.json
```

### State criteria

Final `run-state.json` must show:

```text
status = Completed
```

It must contain six successful steps:

```text
Requirements
Documentation
Planning
Code
Testing
Review
```

### Failure criteria

Tests must verify that a failed agent stops the pipeline and produces a failed result.

## Implementation order

Follow this order:

1. Create solution and projects.
2. Add repository config files.
3. Add sample input and docs.
4. Implement core models and interfaces.
5. Implement logging and JSON state store.
6. Implement run directory factory.
7. Implement fake agents.
8. Implement pipeline.
9. Implement orchestrator.
10. Wire CLI to orchestrator.
11. Add core tests.
12. Add orchestration tests.
13. Add CLI tests.
14. Run build and tests.
15. Run the sample command and verify generated files.
16. Update README if commands differ from implementation.

## Important exclusions for this milestone

Do not implement these yet:

- real AI model calls;
- OpenAI Responses API integration;
- Codex executor;
- OpenCode executor;
- GitHub repository creation;
- Git commit/push;
- Docker sandbox;
- SQLite storage;
- dashboard;
- Web API;
- Blazor UI;
- advanced CLI parser;
- fix loop;
- real generated source project templates.

These will be added in later milestones behind stable abstractions.

## Quality rules

1. Prefer small classes with clear responsibilities.
2. Keep namespaces consistent.
3. Avoid static global state except `Program` entry point.
4. Use cancellation tokens in async methods.
5. Avoid hardcoded absolute paths.
6. Make tests independent and use temporary directories.
7. Keep generated placeholder content deterministic enough for tests.
8. Do not make tests depend on exact timestamps.
9. Do not hide exceptions without logging.
10. Do not remove or weaken the existing architecture while implementing.

## Final verification checklist

Before finishing, verify:

```bash
dotnet --version

dotnet build AiReliabilityEngineering.slnx

dotnet test AiReliabilityEngineering.slnx

dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

Then inspect the newest folder under `runs/` and confirm all expected files are present.

## Expected final note from implementer

When implementation is complete, report:

- solution/projects created;
- command used to run the fake workflow;
- number of tests passing;
- path of one generated run folder;
- any deviation from this plan.
