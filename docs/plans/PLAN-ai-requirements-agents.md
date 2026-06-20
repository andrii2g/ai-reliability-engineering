# PLAN.md - AIRE: Add AiRequirementsAgent Using IAiProvider

## Purpose

This plan is for Codex to add the first AI-aware workflow agent to AIRE.

AIRE means AI Reliability Engineering.

Repository conventions:

- Repository name: ai-reliability-engineering
- Solution name: AiReliabilityEngineering.slnx
- CLI project: AiReliabilityEngineering.Cli
- CLI command name: aire
- PRD location: docs/PRD.md

The previous step should already be implemented:

- provider-neutral AI contracts in AiReliabilityEngineering.Core;
- FakeAiProvider in AiReliabilityEngineering.Infrastructure;
- AiProviderFactory;
- tests for AI contracts and FakeAiProvider;
- no real AI provider integration;
- no API keys;
- no network calls.

The goal of this step is to add `AiRequirementsAgent`, the first agent that depends on `IAiProvider`.

The workflow must remain stable. The existing CLI default workflow must continue using the current fake pipeline unless explicitly changed later.

---

## High-Level Goal

Add an AI-aware requirements agent that can turn an input `idea.md` into structured requirement artifacts.

The new agent should:

- read the copied input idea file from the run context;
- build an `AiRequest`;
- call `IAiProvider`;
- handle AI provider success/failure;
- create a normalized `ProjectSpecification`;
- write `artifacts/specification.json`;
- write `artifacts/requirements.md`;
- return `AgentResult.Success(...)` with artifact references;
- return `AgentResult.Failure(...)` on expected errors.

Important: this step uses `FakeAiProvider` or test providers only. It does not connect to OpenAI, Ollama, Anthropic, Gemini, Codex, or OpenCode.

---

## Non-Goals

Do not implement these in this step:

- OpenAI provider
- Anthropic provider
- Gemini provider
- Ollama provider
- Codex executor
- OpenCode executor
- provider configuration file
- `--provider` CLI option
- `--profile` CLI option
- `--ai` CLI option
- replacing the default CLI workflow
- real prompt engineering framework
- real JSON schema validation package
- prompt template variable engine
- AI-powered DocumentationAgent
- AI-powered PlannerAgent
- AI-powered CodeAgent
- AI-powered TestAgent
- AI-powered ReviewerAgent
- retry/fix loop
- build/test runner
- Git integration
- dashboard

This step is only about adding and testing the first AI-aware agent class.

---

## Design Decision

The default CLI workflow must remain unchanged in this step.

That means:

```text
aire run samples/idea.md
```

should still execute the currently stable fake pipeline unless the repository already has a safe internal test-only way to swap agents.

Do not add new CLI flags in this step.

Do not change the existing `AireOrchestrator` default behavior unless it is strictly necessary and fully covered by tests.

The new `AiRequirementsAgent` should be tested directly.

Later, another step can add workflow profiles, for example:

```text
fake
ai-requirements
openai-requirements
```

But not now.

---

## Design Decision: Provider Is Generic, Agent Owns Domain Shape

`FakeAiProvider` must remain generic.

Do not make `FakeAiProvider` return AIRE-specific `ProjectSpecification` JSON.

The provider is responsible for generic AI text/JSON generation.

`AiRequirementsAgent` is responsible for AIRE-specific output:

```text
idea.md -> ProjectSpecification -> specification.json + requirements.md
```

This keeps the provider reusable for future agents.

---

## Important Model Names From Current Code

Use the actual current model shapes.

The current `AgentContext` shape is:

```csharp
AgentContext(RunContext Run, ...)
```

Therefore, implementation must use:

```csharp
context.Run.CopiedIdeaFilePath
```

Do not use:

```csharp
context.RunContext.CopiedIdeaFilePath
```

The artifact writer tests must construct real `RunContext` and `RunPaths` instances and use:

```csharp
runContext.Paths.ArtifactsDirectory
```

Do not invent a simplified test-only run context shape.

---

## Recommended Behavior for This Step

Because the only available provider is `FakeAiProvider`, `AiRequirementsAgent` should call `IAiProvider` but should produce deterministic requirement artifacts using simple local normalization from `idea.md`.

This gives a real dependency path:

```text
AiRequirementsAgent -> IAiProvider -> FakeAiProvider
```

without making the fake provider domain-specific.

The agent may use the AI response as metadata or a note internally, but the generated `ProjectSpecification` should be deterministic and based on the input idea text.

---

## Target Folder Structure

Add or update this structure:

```text
src/
|-- AiReliabilityEngineering.Core/
|   `-- Requirements/
|       |-- ProjectSpecification.cs
|       `-- ProjectSpecificationDefaults.cs
|
|-- AiReliabilityEngineering.Orchestration/
|   `-- Agents/
|       |-- AiRequirementsAgent.cs
|       |-- RequirementsArtifactWriter.cs
|       `-- RequirementsNormalizer.cs
|
|-- tests/
|   |-- AiReliabilityEngineering.Core.Tests/
|   |   `-- Requirements/
|   |       `-- ProjectSpecificationTests.cs
|   |
|   `-- AiReliabilityEngineering.Orchestration.Tests/
|       `-- Agents/
|           |-- AiRequirementsAgentTests.cs
|           |-- RequirementsArtifactWriterTests.cs
|           `-- RequirementsNormalizerTests.cs
|
`-- docs/
    `-- ai-requirements-agent.md
```

If the repository already has a different folder layout for agents/tests, follow the existing style, but keep the same logical coverage.

---

# Required Core Model

## 1. ProjectSpecification

Create:

```text
src/AiReliabilityEngineering.Core/Requirements/ProjectSpecification.cs
```

Use namespace:

```csharp
namespace AiReliabilityEngineering.Core.Requirements;
```

Suggested implementation shape:

```csharp
namespace AiReliabilityEngineering.Core.Requirements;

public sealed record ProjectSpecification
{
    public ProjectSpecification(
        string projectName,
        string summary,
        IReadOnlyList<string> goals,
        IReadOnlyList<string> nonGoals,
        IReadOnlyList<string> functionalRequirements,
        IReadOnlyList<string> acceptanceCriteria)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required.", nameof(projectName));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Summary is required.", nameof(summary));
        }

        ProjectName = projectName;
        Summary = summary;
        Goals = CopyRequiredList(goals, nameof(goals));
        NonGoals = CopyRequiredList(nonGoals, nameof(nonGoals));
        FunctionalRequirements = CopyRequiredList(functionalRequirements, nameof(functionalRequirements));
        AcceptanceCriteria = CopyRequiredList(acceptanceCriteria, nameof(acceptanceCriteria));
    }

    public string ProjectName { get; }

    public string Summary { get; }

    public IReadOnlyList<string> Goals { get; }

    public IReadOnlyList<string> NonGoals { get; }

    public IReadOnlyList<string> FunctionalRequirements { get; }

    public IReadOnlyList<string> AcceptanceCriteria { get; }

    private static IReadOnlyList<string> CopyRequiredList(
        IReadOnlyList<string> values,
        string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (values.Count == 0)
        {
            throw new ArgumentException("At least one item is required.", parameterName);
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Items must not be null, empty, or whitespace.", parameterName);
        }

        return values.ToArray();
    }
}
```

Rules:

- `ProjectName` is required.
- `Summary` is required.
- All lists are required.
- All lists must have at least one item.
- List items must not be null, empty, or whitespace.
- Store defensive copies of all lists.
- Do not add serialization attributes unless already used in the project.
- Keep the model provider-neutral.

---

## 2. ProjectSpecificationDefaults

Optional but recommended.

Create:

```text
src/AiReliabilityEngineering.Core/Requirements/ProjectSpecificationDefaults.cs
```

Purpose:

- provide deterministic fallback/default values;
- avoid duplicating default lists inside multiple classes.

Suggested shape:

```csharp
namespace AiReliabilityEngineering.Core.Requirements;

public static class ProjectSpecificationDefaults
{
    public static IReadOnlyList<string> DefaultNonGoals { get; } =
        new[]
        {
            "Do not implement real AI provider calls in this step.",
            "Do not modify source repositories outside the run workspace."
        };

    public static IReadOnlyList<string> DefaultFunctionalRequirements { get; } =
        new[]
        {
            "Read the project idea from the copied input Markdown file.",
            "Generate a normalized project specification.",
            "Write specification.json.",
            "Write requirements.md."
        };

    public static IReadOnlyList<string> DefaultAcceptanceCriteria { get; } =
        new[]
        {
            "The generated specification.json is valid JSON.",
            "The generated requirements.md file exists.",
            "The requirements agent returns a successful AgentResult.",
            "The AI provider abstraction is called."
        };
}
```

If this feels unnecessary, inline these defaults in `RequirementsNormalizer`, but avoid duplicating them.

---

# Required Orchestration Components

## 3. RequirementsNormalizer

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/RequirementsNormalizer.cs
```

Use namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- convert raw idea Markdown into a deterministic `ProjectSpecification`;
- keep normalization separate from `AiRequirementsAgent`;
- make the logic easy to test without file I/O or provider calls.

Suggested behavior:

1. Accept raw idea text.
2. Trim whitespace.
3. Extract a project name:
   - use first Markdown H1 if present, e.g. `# Redis TTL Audit Tool`;
   - otherwise use the first non-empty line;
   - otherwise use `Untitled AIRE Project`.
4. Normalize project name:
   - remove leading Markdown heading marks;
   - trim whitespace;
   - limit excessive length if needed.
5. Use summary:
   - first non-empty paragraph or line after the title;
   - if none, use the project name.
6. Goals:
   - create at least one goal from the idea text;
   - for now, a simple deterministic goal is acceptable.
7. NonGoals, FunctionalRequirements, AcceptanceCriteria:
   - use defaults from `ProjectSpecificationDefaults`.

Suggested class:

```csharp
using AiReliabilityEngineering.Core.Requirements;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class RequirementsNormalizer
{
    public ProjectSpecification Normalize(string ideaText)
    {
        // deterministic normalization
    }
}
```

Suggested deterministic output for `# Redis TTL Audit Tool`:

```text
ProjectName: Redis TTL Audit Tool
Summary: Redis TTL Audit Tool
Goals:
- Convert the provided idea into a structured project specification.
```

If the idea has body text, use the first meaningful body line as summary.

Rules:

- Null idea text should throw ArgumentNullException.
- Empty or whitespace idea text should still produce a valid specification with `Untitled AIRE Project`.
- The output must be deterministic.
- Do not call AI here.
- Do not read or write files here.

---

## 4. RequirementsArtifactWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/RequirementsArtifactWriter.cs
```

Use namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- write `ProjectSpecification` to:
  - `artifacts/specification.json`
  - `artifacts/requirements.md`

Suggested behavior:

```csharp
public sealed class RequirementsArtifactWriter
{
    public Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        ProjectSpecification specification,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        // write JSON and Markdown
    }
}
```

Adjust `ArtifactRef` creation to match the existing Core model.

Rules:

- Use `runContext.Paths.ArtifactsDirectory` as the artifact directory.
- Write JSON using `System.Text.Json`.
- Use indented JSON.
- Use `JsonSerializerOptions` with `WriteIndented = true`.
- Use `JsonSerializerOptions` with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`.
- Use stable property names.
- Do not include timestamps.
- Do not include random IDs.
- Markdown output should be deterministic.
- Ensure artifacts directory exists before writing.
- Return artifact references for both files.

Expected JSON path:

```text
artifacts/specification.json
```

Expected Markdown path:

```text
artifacts/requirements.md
```

Expected JSON shape:

```json
{
  "projectName": "Redis TTL Audit Tool",
  "summary": "Redis TTL Audit Tool",
  "goals": [
    "Convert the provided idea into a structured project specification."
  ],
  "nonGoals": [
    "Do not implement real AI provider calls in this step.",
    "Do not modify source repositories outside the run workspace."
  ],
  "functionalRequirements": [
    "Read the project idea from the copied input Markdown file.",
    "Generate a normalized project specification.",
    "Write specification.json.",
    "Write requirements.md."
  ],
  "acceptanceCriteria": [
    "The generated specification.json is valid JSON.",
    "The generated requirements.md file exists.",
    "The requirements agent returns a successful AgentResult.",
    "The AI provider abstraction is called."
  ]
}
```

Expected Markdown shape:

```markdown
# Requirements

## Project Name

Redis TTL Audit Tool

## Summary

Redis TTL Audit Tool

## Goals

- Convert the provided idea into a structured project specification.

## Non-Goals

- Do not implement real AI provider calls in this step.
- Do not modify source repositories outside the run workspace.

## Functional Requirements

- Read the project idea from the copied input Markdown file.
- Generate a normalized project specification.
- Write specification.json.
- Write requirements.md.

## Acceptance Criteria

- The generated specification.json is valid JSON.
- The generated requirements.md file exists.
- The requirements agent returns a successful AgentResult.
- The AI provider abstraction is called.
```

---

## 5. AiRequirementsAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/AiRequirementsAgent.cs
```

Use namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

The agent should implement the existing `IAgent` interface.

Suggested dependencies:

```csharp
public sealed class AiRequirementsAgent : IAgent
{
    private readonly IAiProvider _aiProvider;
    private readonly IRunLogger _logger;
    private readonly RequirementsNormalizer _normalizer;
    private readonly RequirementsArtifactWriter _artifactWriter;

    public AiRequirementsAgent(
        IAiProvider aiProvider,
        IRunLogger logger,
        RequirementsNormalizer? normalizer = null,
        RequirementsArtifactWriter? artifactWriter = null)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _normalizer = normalizer ?? new RequirementsNormalizer();
        _artifactWriter = artifactWriter ?? new RequirementsArtifactWriter();
    }

    public string Name => "AiRequirementsAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // implementation
    }
}
```

Adapt constructor and namespace details to existing project style.

### Required execution flow

1. Check cancellation at the start.
2. Validate `context` is not null.
3. Log start event.
4. Verify `context.Run.CopiedIdeaFilePath` exists.
5. Read the copied idea file from `context.Run.CopiedIdeaFilePath`.
6. Build an `AiRequest`:
   - system message: explain that this is requirements analysis;
   - user message: include the idea text;
   - output format: Text;
   - options: `AiProviderOptions.DefaultFake` for this step.
7. Call `_aiProvider.GenerateAsync(...)`.
8. If AI response failed:
   - log failure;
   - return `AgentResult.Failure(...)`.
9. Normalize idea text into `ProjectSpecification`.
10. Write `specification.json` and `requirements.md`.
11. Log completion.
12. Return `AgentResult.Success(...)` with artifact references.

### Important

The AI response does not need to be parsed in this step.

The AI provider call is used to prove integration and failure behavior.

The domain artifacts are produced by deterministic local normalization.

This is intentional.

### Failure behavior

Return `AgentResult.Failure(...)` when:

- context is invalid;
- `context.Run.CopiedIdeaFilePath` does not exist;
- file reading fails;
- AI provider returns `Succeeded = false`;
- artifact writing fails.

Do not swallow `OperationCanceledException`.

Cancellation may propagate.

---

# Prompt Text

Do not add a complex prompt-template system in this step.

A small inline prompt is acceptable.

Suggested system prompt:

```text
You are AIRE Requirements Agent. Analyze the project idea and help produce a normalized project specification.
```

Suggested user prompt prefix:

```text
Project idea:
```

Later steps may move prompts to a `prompts/` folder.

Do not create prompt templating infrastructure now.

---

# Wiring / Composition

## 6. Composition Root

If the repository has a composition root, add a method to create `AiRequirementsAgent`.

Example:

```csharp
public static AiRequirementsAgent CreateAiRequirementsAgent(IRunLogger logger)
{
    return new AiRequirementsAgent(
        CreateAiProvider(),
        logger);
}
```

Only do this if it is useful and does not change existing default workflow behavior.

Do not replace `FakeRequirementsAgent` in the default pipeline in this step.

---

# Tests

## 7. Core ProjectSpecification tests

Add tests under:

```text
tests/AiReliabilityEngineering.Core.Tests/Requirements/
```

### Test: ProjectSpecification accepts valid values

Assert properties are assigned correctly.

### Test: ProjectSpecification rejects blank project name

Input:

```csharp
new ProjectSpecification("", "summary", goals, nonGoals, functionalRequirements, acceptanceCriteria)
```

Expected:

- ArgumentException.

### Test: ProjectSpecification rejects blank summary

Expected:

- ArgumentException.

### Test: ProjectSpecification rejects null lists

Expected:

- ArgumentNullException.

### Test: ProjectSpecification rejects empty lists

Expected:

- ArgumentException.

### Test: ProjectSpecification rejects blank list items

Expected:

- ArgumentException.

### Test: ProjectSpecification defensively copies lists

Arrange:

- create mutable List<string>;
- pass it into ProjectSpecification;
- mutate original list.

Assert:

- ProjectSpecification list remains unchanged.

---

## 8. RequirementsNormalizer tests

Add tests under:

```text
tests/AiReliabilityEngineering.Orchestration.Tests/Agents/
```

### Test: extracts H1 as project name

Input:

```markdown
# Redis TTL Audit Tool

Scan Redis keys and report keys without TTL.
```

Expected:

```text
ProjectName: Redis TTL Audit Tool
Summary: Scan Redis keys and report keys without TTL.
```

### Test: uses first non-empty line when no H1 exists

Input:

```text
Redis TTL Audit Tool

Scan Redis keys.
```

Expected:

```text
ProjectName: Redis TTL Audit Tool
```

### Test: empty idea text produces Untitled AIRE Project

Input:

```text

```

Expected:

```text
ProjectName: Untitled AIRE Project
```

### Test: null idea text throws ArgumentNullException

Expected:

- ArgumentNullException.

### Test: defaults are populated

Assert:

- goals has at least one item;
- non-goals has at least one item;
- functional requirements has at least one item;
- acceptance criteria has at least one item.

---

## 9. RequirementsArtifactWriter tests

Add tests under:

```text
tests/AiReliabilityEngineering.Orchestration.Tests/Agents/
```

### Test: writes specification.json and requirements.md

Arrange:

- create a temporary root directory;
- construct a real `RunPaths` instance with:
  - RootDirectory = temp root
  - InputDirectory = temp/input
  - WorkspaceDirectory = temp/workspace
  - ArtifactsDirectory = temp/artifacts
  - ReportsDirectory = temp/reports
  - LogsDirectory = temp/logs
  - StateFilePath = temp/run-state.json
- construct a real `RunContext` instance using the current model shape;
- ensure the artifacts directory exists or verify the writer creates it;
- create valid ProjectSpecification.

Act:

- call writer.

Assert:

- `runContext.Paths.ArtifactsDirectory/specification.json` exists;
- `runContext.Paths.ArtifactsDirectory/requirements.md` exists;
- returned artifact refs include both artifacts.

Use the actual `RunContext` and `RunPaths` constructors from `RunModels.cs`. Do not create a fake run-context abstraction.

### Test: specification.json is valid JSON

Parse with System.Text.Json.

Assert:

- projectName property exists;
- summary property exists;
- goals property exists.

### Test: requirements.md contains expected sections

Assert content contains:

```text
# Requirements
## Project Name
## Summary
## Goals
## Non-Goals
## Functional Requirements
## Acceptance Criteria
```

---

## 10. AiRequirementsAgent tests

Add tests under:

```text
tests/AiReliabilityEngineering.Orchestration.Tests/Agents/
```

Create small test doubles if needed.

### RecordingAiProvider

Create a test-only provider:

```csharp
public sealed class RecordingAiProvider : IAiProvider
{
    public string Name => "recording";

    public bool WasCalled { get; private set; }

    public AiRequest? LastRequest { get; private set; }

    public Task<AiResponse> GenerateAsync(
        AiRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WasCalled = true;
        LastRequest = request;

        return Task.FromResult(AiResponse.Success(
            "recorded",
            Name,
            request.Options.Model));
    }
}
```

### FailingAiProvider

Create a test-only provider:

```csharp
public sealed class FailingAiProvider : IAiProvider
{
    public string Name => "failing";

    public Task<AiResponse> GenerateAsync(
        AiRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(AiResponse.Failure(
            "provider failed",
            Name,
            request.Options.Model));
    }
}
```

### Test: agent calls AI provider

Arrange:

- create a real temporary `RunContext` and `RunPaths`;
- create the copied `idea.md` file at `runContext.CopiedIdeaFilePath`;
- RecordingAiProvider;
- test logger.

Act:

- execute agent.

Assert:

- result success;
- provider WasCalled is true;
- provider LastRequest is not null;
- LastRequest contains a system message;
- LastRequest contains a user message with idea text.

### Test: agent writes requirement artifacts

Assert:

- `runContext.Paths.ArtifactsDirectory/specification.json` exists;
- `runContext.Paths.ArtifactsDirectory/requirements.md` exists;
- result artifacts include both files.

### Test: generated specification is valid JSON

Parse `specification.json`.

Assert:

- projectName property exists;
- summary property exists;
- goals array exists.

### Test: missing copied idea file returns failure

Arrange:

- real RunContext points to a missing `CopiedIdeaFilePath`.

Expected:

- AgentResult indicates failure;
- message mentions missing idea file.

### Test: provider failure returns agent failure

Arrange:

- FailingAiProvider.

Expected:

- AgentResult indicates failure;
- message contains provider failure message;
- artifacts are not written, or no success artifact refs are returned.

### Test: cancellation is respected

Arrange:

- already-canceled token.

Act:

```csharp
await agent.ExecuteAsync(context, canceledToken);
```

Assert:

- OperationCanceledException is thrown.

### Test: current fake workflow is unchanged

Keep or add an orchestration test proving the default orchestrator still uses existing fake pipeline and produces the existing successful run.

Do not require the default pipeline to use AiRequirementsAgent in this step.

---

# Documentation

## 11. Add docs/ai-requirements-agent.md

Create:

```text
docs/ai-requirements-agent.md
```

Content should explain:

```markdown
# AI Requirements Agent

AiRequirementsAgent is the first AI-aware workflow agent in AIRE.

It depends on IAiProvider, not on a concrete AI vendor SDK.

In the current step, it is tested with FakeAiProvider or test providers only. It does not call OpenAI, Ollama, Anthropic, Gemini, Codex, or OpenCode.

The agent reads the copied input idea file, calls IAiProvider, normalizes the idea into ProjectSpecification, and writes:

- artifacts/specification.json
- artifacts/requirements.md

The default CLI workflow is not changed in this step.
```

---

## 12. Update docs/wiki.md

If `docs/wiki.md` exists, add short entries:

```markdown
## ProjectSpecification

The normalized requirements contract produced by requirements analysis. Later agents can consume it for documentation, planning, code generation, testing, and review.

## AiRequirementsAgent

The first AI-aware AIRE agent. It calls IAiProvider but currently writes deterministic artifacts using local normalization.
```

If `docs/wiki.md` does not exist, do not create it in this step unless the repository already expects it.

---

## 13. Update README.md

Add a short note:

```markdown
## AI Requirements Agent

The repository now contains `AiRequirementsAgent`, the first AI-aware agent.

It uses the provider-neutral `IAiProvider` contract and is currently tested with fake/test providers only. The default CLI workflow still uses the stable fake pipeline; workflow profile selection will be added later.
```

Keep it brief.

---

# Existing Workflow Must Remain Stable

After this step, these commands must still pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

Cleanup command should still work when used against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

Do not run cleanup against repo-local `./runs` if it contains useful manual run history.

---

# Suggested Implementation Order for Codex

Follow this order.

## Task 1: Add ProjectSpecification model

Create:

```text
src/AiReliabilityEngineering.Core/Requirements/ProjectSpecification.cs
```

Optionally create:

```text
src/AiReliabilityEngineering.Core/Requirements/ProjectSpecificationDefaults.cs
```

Run:

```bash
dotnet build
```

## Task 2: Add Core tests

Create:

```text
tests/AiReliabilityEngineering.Core.Tests/Requirements/ProjectSpecificationTests.cs
```

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 3: Add RequirementsNormalizer

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/RequirementsNormalizer.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 4: Add RequirementsArtifactWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/RequirementsArtifactWriter.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 5: Add AiRequirementsAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/AiRequirementsAgent.cs
```

Add tests with RecordingAiProvider and FailingAiProvider.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 6: Optional composition root helper

Add a creation helper only if useful.

Do not change default pipeline behavior.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 7: Documentation updates

Create/update:

```text
docs/ai-requirements-agent.md
docs/wiki.md if it exists
README.md
```

Run:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

---

# Acceptance Criteria

The task is complete when all criteria are satisfied.

## Core

- ProjectSpecification exists.
- ProjectSpecification validates required fields.
- ProjectSpecification validates required lists.
- ProjectSpecification defensively copies lists.
- ProjectSpecification tests pass.

## Orchestration

- RequirementsNormalizer exists.
- RequirementsNormalizer produces deterministic ProjectSpecification output.
- RequirementsArtifactWriter exists.
- RequirementsArtifactWriter uses real RunContext and RunPaths.
- RequirementsArtifactWriter writes specification.json.
- RequirementsArtifactWriter writes requirements.md.
- AiRequirementsAgent exists.
- AiRequirementsAgent implements IAgent.
- AiRequirementsAgent uses `context.Run.CopiedIdeaFilePath`.
- AiRequirementsAgent calls IAiProvider.
- AiRequirementsAgent handles provider failure as AgentResult failure.
- AiRequirementsAgent writes artifacts on success.
- AiRequirementsAgent respects cancellation.
- The default CLI workflow remains unchanged.

## Tests

- Core requirements tests pass.
- RequirementsNormalizer tests pass.
- RequirementsArtifactWriter tests pass.
- RequirementsArtifactWriter tests use real RunContext and RunPaths.
- AiRequirementsAgent tests pass.
- RecordingAiProvider test double implements IAiProvider.Name.
- Existing CLI/orchestration/cleanup/AI provider tests still pass.

## Documentation

- docs/ai-requirements-agent.md exists.
- README mentions AiRequirementsAgent.
- docs/wiki.md is updated if it exists.
- Documentation says the default CLI workflow is not changed in this step.
- Documentation says real providers are not integrated in this step.

## Verification

These commands pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

Cleanup passes when run against disposable data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Notes for Codex

- Keep this step small and focused.
- Do not integrate real AI providers.
- Do not add OpenAI SDK.
- Do not add Ollama HTTP code.
- Do not add CLI flags.
- Do not replace the default fake pipeline.
- Do not add workflow profile selection yet.
- Do not create prompt templating infrastructure yet.
- Keep outputs deterministic.
- Use temporary directories in tests.
- Use the actual current model names:
  - `AgentContext.Run`
  - `RunContext`
  - `RunPaths`
- Preserve the existing stable CLI behavior.
- Save this file as UTF-8 without BOM.
