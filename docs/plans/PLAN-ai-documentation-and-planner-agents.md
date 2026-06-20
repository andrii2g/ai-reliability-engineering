# PLAN.md - AIRE: Real Demo with AI Documentation Agent and AI Planner Agent

## Purpose

This plan is for Codex to implement the next practical AIRE milestone.

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
- fake profile;
- ai-requirements profile;
- AI provider contracts;
- FakeAiProvider;
- OpenAiProvider;
- provider selection through --provider fake|openai;
- model selection through --model;
- AiRequirementsAgent;
- ProjectSpecification;
- RequirementsArtifactWriter;
- samples/redis-ttl-audit.md.

The goal of this step is to make AIRE visibly useful by adding:

- AiDocumentationAgent;
- AiPlannerAgent;
- ai-demo workflow profile;
- generated documentation artifacts;
- generated planning artifacts;
- docs explaining the demo;
- tests for the new agents and profile.

The demo should produce useful artifacts from an idea file:

```text
idea.md
  -> specification.json
  -> requirements.md
  -> README.md
  -> PLAN.md
  -> tasks.json
```

This step still does not generate or modify source code.

---

## High-Level Goal

Add an end-to-end AI-aware demo workflow:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
```

and a real provider variant:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider openai --model <model-name>
```

Expected demo pipeline:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> FakeCodeAgent
Testing       -> FakeTestAgent
Review        -> FakeReviewerAgent
```

Expected output artifacts:

```text
artifacts/specification.json
artifacts/requirements.md
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json
```

The workflow must complete successfully with --provider fake.

With --provider openai, it should call OpenAI through the already implemented provider. The OpenAI demo is valid manual usage, but the run may fail through the normal failed-run path if OpenAI output does not match the required documentation marker format or planner JSON shape.

---

## Non-Goals

Do not implement these in this step:

- real CodeAgent;
- real TestAgent;
- real ReviewerAgent;
- Codex executor;
- OpenCode executor;
- Git integration;
- Docker;
- Kubernetes;
- SQLite;
- dashboard;
- streaming;
- OpenAI structured outputs enforcement;
- JSON schema response format;
- prompt template engine;
- provider config file;
- retry/fix loop;
- build/test runner;
- repository template system.

This step is only about generating documentation and planning artifacts.

---

## Design Decisions

### Add one demo profile

Add one new workflow profile:

```text
ai-demo
```

The profile means:

```text
Run AI-aware requirements, documentation, and planning agents, then finish with fake code/test/review agents.
```

Do not rename existing profiles.

Supported profiles after this step:

```text
fake
ai-requirements
ai-demo
```

Default profile remains:

```text
fake
```

### Keep FakeAiProvider generic

Do not make FakeAiProvider domain-specific.

It is acceptable for AiDocumentationAgent and AiPlannerAgent to have deterministic fallback behavior when provider.Name == "fake". That allows the local demo to work without API keys while keeping FakeAiProvider reusable.

### AI provider output handling

AiDocumentationAgent and AiPlannerAgent should call IAiProvider.

For real providers:

- AiDocumentationAgent should use provider Markdown output if it matches the expected marker format.
- AiPlannerAgent should parse provider JSON output into tasks.json.
- If real provider output does not match the required format, return AgentResult.Failure and let the run fail normally.

For fake provider:

- AiDocumentationAgent may call provider and then generate deterministic README/PLAN locally.
- AiPlannerAgent may call provider and then generate deterministic tasks locally.
- Deterministic fallback behavior is only for provider.Name == "fake".

This ensures:

- fake-provider demo works;
- OpenAI provider can be used manually and may fail normally on malformed model output;
- no source code generation occurs yet;
- tests stay deterministic.

---

## Target Folder Structure

Add or update this structure:

```text
src/
|-- AiReliabilityEngineering.Core/
|   |-- Documentation/
|   |   `-- ProjectDocumentation.cs
|   |
|   `-- Planning/
|       |-- ImplementationTask.cs
|       `-- ImplementationPlan.cs
|
|-- AiReliabilityEngineering.Orchestration/
|   `-- Agents/
|       |-- AiDocumentationAgent.cs
|       |-- AiPlannerAgent.cs
|       |-- DocumentationArtifactWriter.cs
|       |-- PlannerArtifactWriter.cs
|       |-- ProjectSpecificationReader.cs
|       `-- PlanningResponseParser.cs
|
|-- tests/
|   |-- AiReliabilityEngineering.Core.Tests/
|   |   |-- Documentation/
|   |   |   `-- ProjectDocumentationTests.cs
|   |   `-- Planning/
|   |       |-- ImplementationTaskTests.cs
|   |       `-- ImplementationPlanTests.cs
|   |
|   `-- AiReliabilityEngineering.Orchestration.Tests/
|       `-- Agents/
|           |-- AiDocumentationAgentTests.cs
|           |-- AiPlannerAgentTests.cs
|           |-- DocumentationArtifactWriterTests.cs
|           |-- PlannerArtifactWriterTests.cs
|           |-- ProjectSpecificationReaderTests.cs
|           `-- PlanningResponseParserTests.cs
|
`-- docs/
    |-- ai-documentation-agent.md
    |-- ai-planner-agent.md
    `-- demo-ai-artifacts.md
```

Follow existing repository layout if it differs, but keep the same logical coverage.

---

# Required Core Models

## 1. ProjectDocumentation

Create:

```text
src/AiReliabilityEngineering.Core/Documentation/ProjectDocumentation.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Documentation;
```

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Documentation;

public sealed record ProjectDocumentation
{
    public ProjectDocumentation(string readmeMarkdown, string planMarkdown)
    {
        if (string.IsNullOrWhiteSpace(readmeMarkdown))
        {
            throw new ArgumentException("README markdown is required.", nameof(readmeMarkdown));
        }

        if (string.IsNullOrWhiteSpace(planMarkdown))
        {
            throw new ArgumentException("PLAN markdown is required.", nameof(planMarkdown));
        }

        ReadmeMarkdown = readmeMarkdown;
        PlanMarkdown = planMarkdown;
    }

    public string ReadmeMarkdown { get; }

    public string PlanMarkdown { get; }
}
```

Rules:

- README markdown is required.
- PLAN markdown is required.
- No provider-specific fields.
- No serialization attributes unless already used in the project.

---

## 2. ImplementationTask

Create:

```text
src/AiReliabilityEngineering.Core/Planning/ImplementationTask.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Planning;
```

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Planning;

public sealed record ImplementationTask
{
    public ImplementationTask(
        string id,
        string title,
        string description,
        IReadOnlyList<string> acceptanceCriteria)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Task id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Task description is required.", nameof(description));
        }

        if (acceptanceCriteria is null)
        {
            throw new ArgumentNullException(nameof(acceptanceCriteria));
        }

        if (acceptanceCriteria.Count == 0)
        {
            throw new ArgumentException("At least one acceptance criterion is required.", nameof(acceptanceCriteria));
        }

        if (acceptanceCriteria.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Acceptance criteria must not contain empty items.", nameof(acceptanceCriteria));
        }

        Id = id;
        Title = title;
        Description = description;
        AcceptanceCriteria = acceptanceCriteria.ToArray();
    }

    public string Id { get; }

    public string Title { get; }

    public string Description { get; }

    public IReadOnlyList<string> AcceptanceCriteria { get; }
}
```

---

## 3. ImplementationPlan

Create:

```text
src/AiReliabilityEngineering.Core/Planning/ImplementationPlan.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Planning;
```

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Planning;

public sealed record ImplementationPlan
{
    public ImplementationPlan(IReadOnlyList<ImplementationTask> tasks)
    {
        if (tasks is null)
        {
            throw new ArgumentNullException(nameof(tasks));
        }

        if (tasks.Count == 0)
        {
            throw new ArgumentException("At least one implementation task is required.", nameof(tasks));
        }

        if (tasks.Any(task => task is null))
        {
            throw new ArgumentException("Tasks must not contain null entries.", nameof(tasks));
        }

        Tasks = tasks.ToArray();
    }

    public IReadOnlyList<ImplementationTask> Tasks { get; }
}
```

---

# Required Orchestration Components

## 4. ProjectSpecificationReader

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/ProjectSpecificationReader.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- read artifacts/specification.json;
- deserialize it into ProjectSpecification;
- provide a shared reader for documentation and planning agents.

Suggested signature:

```csharp
public sealed class ProjectSpecificationReader
{
    public async Task<ProjectSpecification> ReadAsync(
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        // read runContext.Paths.ArtifactsDirectory/specification.json
    }
}
```

Rules:

- Use runContext.Paths.ArtifactsDirectory.
- Expected file path: artifacts/specification.json.
- If file does not exist, throw FileNotFoundException.
- Always deserialize through a reader-local DTO that matches the camelCase specification.json contract.
- Construct ProjectSpecification from the DTO so existing constructor validation remains authoritative.
- If JSON is invalid, throw InvalidOperationException with a clear message.
- If the deserialized specification shape is invalid, throw InvalidOperationException with a clear message.
- Do not read from workspace.

---

## 5. DocumentationArtifactWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/DocumentationArtifactWriter.cs
```

Purpose:

- write artifacts/README.md;
- write artifacts/PLAN.md.

Suggested signature:

```csharp
public sealed class DocumentationArtifactWriter
{
    public Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        ProjectDocumentation documentation,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        // write README.md and PLAN.md to artifacts
    }
}
```

Rules:

- Use runContext.Paths.ArtifactsDirectory.
- Ensure artifacts directory exists.
- Do not write to repository root README.md or PLAN.md.
- Do not include timestamps.
- Do not include random IDs.
- Return artifact references for both files.

---

## 6. AiDocumentationAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/AiDocumentationAgent.cs
```

The agent must implement existing IAgent.

Suggested dependencies:

```csharp
public sealed class AiDocumentationAgent : IAgent
{
    private readonly IAiProvider _aiProvider;
    private readonly IRunLogger _logger;
    private readonly ProjectSpecificationReader _specificationReader;
    private readonly DocumentationArtifactWriter _artifactWriter;

    public AiDocumentationAgent(
        IAiProvider aiProvider,
        IRunLogger logger,
        ProjectSpecificationReader? specificationReader = null,
        DocumentationArtifactWriter? artifactWriter = null)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _specificationReader = specificationReader ?? new ProjectSpecificationReader();
        _artifactWriter = artifactWriter ?? new DocumentationArtifactWriter();
    }

    public string Name => "AiDocumentationAgent";

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
4. Read ProjectSpecification using ProjectSpecificationReader.
5. Build an AI request from the specification.
6. Call IAiProvider.GenerateAsync.
7. If provider response failed, return AgentResult.Failure.
8. Convert provider content into ProjectDocumentation.
9. Write artifacts/README.md and artifacts/PLAN.md.
10. Log completion.
11. Return AgentResult.Success with artifact refs.

### Documentation response format

For non-fake providers, ask for Markdown response with markers:

```text
---README---
<README markdown>

---PLAN---
<PLAN markdown>
```

Parser behavior:

- Find ---README---.
- Find ---PLAN---.
- Text between markers becomes README.
- Text after ---PLAN--- becomes PLAN.
- If markers are missing, return AgentResult.Failure with clear message.

### Fake provider behavior

If provider.Name == "fake":

- still call the provider to prove integration;
- ignore generic fake provider content;
- generate deterministic README and PLAN locally from ProjectSpecification;
- write both artifacts;
- return success.

Do not modify FakeAiProvider.

For non-fake providers, do not use deterministic fallback if the response is malformed. Return AgentResult.Failure with a clear message.

---

## 7. PlanningResponseParser

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/PlanningResponseParser.cs
```

Purpose:

- parse JSON returned by provider into ImplementationPlan.

Expected provider JSON shape:

```json
{
  "tasks": [
    {
      "id": "T001",
      "title": "Create CLI skeleton",
      "description": "Create the basic command line structure.",
      "acceptanceCriteria": [
        "CLI starts",
        "Help command works"
      ]
    }
  ]
}
```

Suggested signature:

```csharp
public sealed class PlanningResponseParser
{
    public ImplementationPlan Parse(string json)
    {
        // implementation
    }
}
```

Rules:

- Null input throws ArgumentNullException.
- Empty/whitespace input throws ArgumentException.
- Invalid JSON throws InvalidOperationException.
- Missing tasks throws InvalidOperationException.
- Empty tasks throws InvalidOperationException or constructor ArgumentException.
- Missing/blank task fields cause failure.
- Use reader-local DTOs.
- Do not add JSON schema package.

---

## 8. PlannerArtifactWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/PlannerArtifactWriter.cs
```

Purpose:

- write artifacts/tasks.json.

Suggested signature:

```csharp
public sealed class PlannerArtifactWriter
{
    public Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        ImplementationPlan plan,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        // write tasks.json
    }
}
```

Rules:

- Use runContext.Paths.ArtifactsDirectory.
- Ensure artifacts directory exists.
- Write indented JSON.
- Use stable property names: tasks, id, title, description, acceptanceCriteria.
- Do not include timestamps.
- Do not include random IDs.
- Return artifact ref for artifacts/tasks.json.

---

## 9. AiPlannerAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/AiPlannerAgent.cs
```

The agent must implement existing IAgent.

Suggested dependencies:

```csharp
public sealed class AiPlannerAgent : IAgent
{
    private readonly IAiProvider _aiProvider;
    private readonly IRunLogger _logger;
    private readonly ProjectSpecificationReader _specificationReader;
    private readonly PlanningResponseParser _parser;
    private readonly PlannerArtifactWriter _artifactWriter;

    public AiPlannerAgent(
        IAiProvider aiProvider,
        IRunLogger logger,
        ProjectSpecificationReader? specificationReader = null,
        PlanningResponseParser? parser = null,
        PlannerArtifactWriter? artifactWriter = null)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _specificationReader = specificationReader ?? new ProjectSpecificationReader();
        _parser = parser ?? new PlanningResponseParser();
        _artifactWriter = artifactWriter ?? new PlannerArtifactWriter();
    }

    public string Name => "AiPlannerAgent";

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
4. Read ProjectSpecification using ProjectSpecificationReader.
5. Build an AI request from the specification.
6. Call IAiProvider.GenerateAsync.
7. If provider response failed, return AgentResult.Failure.
8. Parse provider content into ImplementationPlan.
9. Write artifacts/tasks.json.
10. Log completion.
11. Return AgentResult.Success with artifact refs.

### Fake provider behavior

If provider.Name == "fake":

- still call the provider to prove integration;
- ignore generic fake provider content;
- generate deterministic ImplementationPlan locally from ProjectSpecification;
- write tasks.json;
- return success.

Do not modify FakeAiProvider.

For non-fake providers, do not use deterministic fallback if JSON parsing fails or the parsed plan is invalid. Return AgentResult.Failure with a clear message.

### Planner AI request

Use AiOutputFormat.Json.

Prompt should ask for JSON only and include required JSON shape.

Do not use provider-level structured outputs yet.

---

# Workflow Profile Changes

## 10. Extend WorkflowProfile

Add:

```csharp
AiDemo
```

Supported CLI name:

```text
ai-demo
```

---

## 11. Update WorkflowProfileParser

Supported names after this step:

```text
fake
ai-requirements
ai-demo
```

Rules:

- Missing profile maps to Fake.
- Unknown profile returns false.
- ToCliName maps AiDemo to ai-demo.
- SupportedCliNames includes ai-demo.

---

## 12. Update AgentPipelineFactory

For WorkflowProfile.AiDemo, create pipeline:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> FakeCodeAgent
Testing       -> FakeTestAgent
Review        -> FakeReviewerAgent
```

Use the selected IAiProvider for all AI-aware agents.

Important:

- AgentPipelineFactory.Create(...) must create the selected IAiProvider once for WorkflowProfile.AiDemo.
- Pass the same selected provider instance to AiRequirementsAgent, AiDocumentationAgent, and AiPlannerAgent.
- Fake profile remains unchanged.
- AiRequirements profile remains unchanged.

---

# Tests

## 13. Core model tests

Add tests for:

```text
ProjectDocumentation
ImplementationTask
ImplementationPlan
```

Required tests:

- accepts valid values;
- rejects null/blank required strings;
- rejects null/empty lists;
- rejects blank list items;
- defensively copies lists.

---

## 14. ProjectSpecificationReader tests

Required tests:

- reads valid specification.json;
- throws FileNotFoundException when missing;
- throws InvalidOperationException for invalid JSON;
- reads from runContext.Paths.ArtifactsDirectory.

Use real RunContext and RunPaths.

---

## 15. DocumentationArtifactWriter tests

Required tests:

- writes artifacts/README.md;
- writes artifacts/PLAN.md;
- returned artifact refs include both files;
- output content matches ProjectDocumentation;
- uses runContext.Paths.ArtifactsDirectory.

Use real RunContext and RunPaths.

---

## 16. AiDocumentationAgent tests

Use test providers.

### DocumentationRecordingProvider

Test provider must implement IAiProvider and include Name.

Example response:

```text
---README---
# Test README

Generated README.

---PLAN---
# Test PLAN

Generated plan.
```

Required tests:

- calls provider;
- request includes project specification content;
- writes README.md and PLAN.md;
- returns success artifact refs;
- provider failure returns AgentResult failure;
- missing specification.json returns failure;
- invalid provider response without markers returns failure;
- fake provider path generates deterministic local docs;
- cancellation propagates OperationCanceledException.

---

## 17. PlanningResponseParser tests

Required tests:

- parses valid tasks JSON;
- rejects null input;
- rejects blank input;
- rejects invalid JSON;
- rejects missing tasks;
- rejects empty tasks;
- rejects blank task fields;
- rejects blank acceptance criteria.

---

## 18. PlannerArtifactWriter tests

Required tests:

- writes artifacts/tasks.json;
- output JSON contains tasks array;
- output JSON contains id/title/description/acceptanceCriteria;
- returned artifact refs include tasks.json;
- uses runContext.Paths.ArtifactsDirectory.

Use real RunContext and RunPaths.

---

## 19. AiPlannerAgent tests

Use test providers.

### PlannerRecordingProvider

Test provider must implement IAiProvider and include Name.

Example response:

```json
{
  "tasks": [
    {
      "id": "T001",
      "title": "Create skeleton",
      "description": "Create the first implementation skeleton.",
      "acceptanceCriteria": [
        "Build passes",
        "Tests pass"
      ]
    }
  ]
}
```

Required tests:

- calls provider;
- request output format is Json;
- request includes project specification content;
- writes tasks.json;
- returns success artifact refs;
- provider failure returns AgentResult failure;
- invalid JSON provider response returns AgentResult failure;
- missing specification.json returns failure;
- fake provider path generates deterministic local tasks;
- cancellation propagates OperationCanceledException.

---

## 20. Workflow profile tests

Update WorkflowProfileParser tests:

- parses ai-demo;
- ToCliName maps AiDemo to ai-demo;
- SupportedCliNames includes ai-demo.

Update AgentPipelineFactory tests:

- AiDemo pipeline contains:
  - AiRequirementsAgent;
  - AiDocumentationAgent;
  - AiPlannerAgent;
  - FakeCodeAgent;
  - FakeTestAgent;
  - FakeReviewerAgent.
- step order remains:
  - Requirements;
  - Documentation;
  - Planning;
  - Code;
  - Testing;
  - Review.

---

## 21. Orchestrator ai-demo tests

Add test:

```text
ai-demo profile completes with fake provider
```

Arrange:

- RunRequest with WorkflowProfile.AiDemo and fake provider.
- Use sample idea.
- Run orchestrator.

Assert:

- run succeeds;
- final status is Completed;
- artifacts/specification.json exists;
- artifacts/requirements.md exists;
- artifacts/README.md exists;
- artifacts/PLAN.md exists;
- artifacts/tasks.json exists;
- tasks.json is valid JSON;
- run-state step agents include:
  - AiRequirementsAgent;
  - AiDocumentationAgent;
  - AiPlannerAgent.

---

## 22. CLI tests

Update CLI tests:

- help mentions ai-demo;
- run idea.md --profile ai-demo --provider fake succeeds;
- generated run contains artifacts/README.md;
- generated run contains artifacts/PLAN.md;
- generated run contains artifacts/tasks.json;
- run idea.md --profile ai-demo --provider openai without model fails with exit code 2;
- unknown profile still fails with exit code 2.

Do not add CLI tests that call real OpenAI.

CLI tests that inspect generated run artifacts must execute from a temporary current directory and restore the previous current directory in finally. They must not write to repo-local runs/. Orchestrator tests remain the primary place for full artifact assertions.

---

# Demo and Documentation

## 23. Update samples/redis-ttl-audit.md

Ensure it exists and is useful.

Suggested content:

```markdown
# Redis TTL Audit Tool

Create a small CLI tool that scans Redis keys and reports keys that have no TTL.

The tool should group keys by prefix, estimate how many keys never expire, and generate a Markdown report.

The tool must be read-only and must not delete or modify Redis keys.
```

---

## 24. Add docs/ai-documentation-agent.md

Create:

```text
docs/ai-documentation-agent.md
```

Explain:

- purpose;
- reads specification.json;
- calls IAiProvider;
- writes README.md and PLAN.md;
- fake-provider behavior;
- no source code changes.

---

## 25. Add docs/ai-planner-agent.md

Create:

```text
docs/ai-planner-agent.md
```

Explain:

- purpose;
- reads specification.json;
- calls IAiProvider;
- expects planning JSON;
- writes tasks.json;
- fake-provider behavior;
- no source code changes.

---

## 26. Add docs/demo-ai-artifacts.md

Create:

```text
docs/demo-ai-artifacts.md
```

Include demo commands:

Fake/local demo:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
```

OpenAI demo:

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider openai --model <model-name>
```

This command calls OpenAI. It may fail normally if the model response does not include the required documentation markers or valid planner JSON.

Expected artifacts:

```text
artifacts/specification.json
artifacts/requirements.md
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json
```

Explain that source code generation is not implemented yet.

---

## 27. Update docs/workflow-profiles.md

Add ai-demo profile:

```markdown
## ai-demo

Uses AI-aware requirements, documentation, and planning agents, then fake code/test/review agents.

```bash
aire run samples/redis-ttl-audit.md --profile ai-demo --provider fake
```

or:

```bash
aire run samples/redis-ttl-audit.md --profile ai-demo --provider openai --model <model-name>
```
```

---

## 28. Update docs/wiki.md

If exists, add:

```markdown
## AiDocumentationAgent

Generates README.md and PLAN.md from specification.json.

## AiPlannerAgent

Generates tasks.json from specification.json.

## ai-demo Profile

Runs AiRequirementsAgent, AiDocumentationAgent, and AiPlannerAgent before fake code/test/review steps.
```

---

## 29. Update README.md

Add a short demo section:

```markdown
## AI Artifacts Demo

Generate requirements, documentation, and planning artifacts locally with the fake provider:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
```

Run the same profile with OpenAI:

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider openai --model <model-name>
```

The demo produces:

- artifacts/specification.json
- artifacts/requirements.md
- artifacts/README.md
- artifacts/PLAN.md
- artifacts/tasks.json

Source code generation is not implemented yet.
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
```

OpenAI demo remains manual:

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider openai --model <model-name>
```

The manual OpenAI demo can fail through the normal failed-run path if provider output is malformed.

Cleanup command should still work when used against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Suggested Implementation Order for Codex

Follow this order.

## Task 1: Add Core documentation and planning models

Create:

```text
src/AiReliabilityEngineering.Core/Documentation/ProjectDocumentation.cs
src/AiReliabilityEngineering.Core/Planning/ImplementationTask.cs
src/AiReliabilityEngineering.Core/Planning/ImplementationPlan.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 2: Add ProjectSpecificationReader

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/ProjectSpecificationReader.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 3: Add DocumentationArtifactWriter and AiDocumentationAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/DocumentationArtifactWriter.cs
src/AiReliabilityEngineering.Orchestration/Agents/AiDocumentationAgent.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 4: Add planning parser, writer, and AiPlannerAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/PlanningResponseParser.cs
src/AiReliabilityEngineering.Orchestration/Agents/PlannerArtifactWriter.cs
src/AiReliabilityEngineering.Orchestration/Agents/AiPlannerAgent.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 5: Add ai-demo workflow profile

Update:

```text
WorkflowProfile
WorkflowProfileParser
AgentPipelineFactory
CLI help/tests if needed
```

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 6: Add orchestration and CLI tests for ai-demo

Verify fake provider demo works.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 7: Add demo docs and README updates

Create/update docs and sample.

Run final verification.

---

# Acceptance Criteria

The task is complete when all criteria are satisfied.

## Core

- ProjectDocumentation exists and validates content.
- ImplementationTask exists and validates content.
- ImplementationPlan exists and validates task list.
- Core tests pass.

## Documentation Agent

- ProjectSpecificationReader exists.
- DocumentationArtifactWriter exists.
- AiDocumentationAgent exists.
- AiDocumentationAgent implements IAgent.
- AiDocumentationAgent calls IAiProvider.
- AiDocumentationAgent writes README.md.
- AiDocumentationAgent writes PLAN.md.
- Provider failure returns AgentResult failure.
- Invalid provider response returns AgentResult failure.
- Fake provider demo path succeeds.
- Tests pass.

## Planner Agent

- PlanningResponseParser exists.
- PlannerArtifactWriter exists.
- AiPlannerAgent exists.
- AiPlannerAgent implements IAgent.
- AiPlannerAgent calls IAiProvider.
- AiPlannerAgent writes tasks.json.
- Invalid JSON returns AgentResult failure.
- Provider failure returns AgentResult failure.
- Fake provider demo path succeeds.
- Tests pass.

## Workflow

- WorkflowProfile includes AiDemo.
- CLI profile name is ai-demo.
- ai-demo profile uses:
  - AiRequirementsAgent;
  - AiDocumentationAgent;
  - AiPlannerAgent;
  - FakeCodeAgent;
  - FakeTestAgent;
  - FakeReviewerAgent.
- fake profile remains unchanged.
- ai-requirements profile remains unchanged.

## Demo

- samples/redis-ttl-audit.md exists.
- --profile ai-demo --provider fake completes successfully.
- The run contains:
  - artifacts/specification.json;
  - artifacts/requirements.md;
  - artifacts/README.md;
  - artifacts/PLAN.md;
  - artifacts/tasks.json.

## Documentation

- docs/ai-documentation-agent.md exists.
- docs/ai-planner-agent.md exists.
- docs/demo-ai-artifacts.md exists.
- docs/workflow-profiles.md mentions ai-demo.
- README includes AI artifacts demo.
- Docs explain source code generation is not implemented yet.

## Verification

These commands pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
```

Manual OpenAI demo:

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider openai --model <model-name>
```

Cleanup passes against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Notes for Codex

- Keep this step focused on documentation/planning artifacts.
- Do not implement code generation.
- Do not implement Docker.
- Do not implement Kubernetes.
- Do not add new providers.
- Do not add config files.
- Do not accept API keys in CLI.
- Do not change default profile.
- Do not change default provider.
- Keep FakeAiProvider generic.
- It is acceptable for AiDocumentationAgent and AiPlannerAgent to have fake-provider deterministic fallback behavior.
- Use temporary directories in tests.
- Preserve existing stable CLI behavior.
- Save this file as UTF-8 without BOM.
