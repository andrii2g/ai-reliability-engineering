# PLAN.md - AIRE Step 10.3: Add Workflow Profiles

## Purpose

This plan is for Codex to add workflow profile support to AIRE.

AIRE means AI Reliability Engineering.

Repository conventions:

- Repository name: ai-reliability-engineering
- Solution name: AiReliabilityEngineering.slnx
- CLI project: AiReliabilityEngineering.Cli
- CLI command name: aire
- PRD location: docs/PRD.md

The previous steps should already be implemented:

- stable fake workflow;
- `run` CLI command;
- `cleanup` CLI command;
- AI provider contracts;
- FakeAiProvider;
- AiRequirementsAgent;
- ProjectSpecification;
- RequirementsNormalizer;
- RequirementsArtifactWriter;
- tests for fake workflow, cleanup, AI provider contracts, and AiRequirementsAgent.

The goal of this step is to make pipeline composition selectable through workflow profiles.

This step must keep the system local, deterministic, and stable.

---

## High-Level Goal

Add support for selecting a workflow profile when running AIRE.

Supported profiles in this step:

```text
fake
ai-requirements
```

Default behavior must remain unchanged:

```bash
aire run samples/idea.md
```

should still use the existing fake workflow.

New behavior:

```bash
aire run samples/idea.md --profile ai-requirements
```

should use `AiRequirementsAgent` for the requirements step and fake agents for all later steps.

---

## Why This Step Exists

The current orchestrator likely hardcodes the fake pipeline directly.

That is not scalable because every new agent would require changing the orchestrator itself.

This step introduces a small profile-based pipeline composition layer:

```text
WorkflowProfile -> AgentPipelineFactory -> AgentPipeline
```

This allows later steps to add profiles such as:

```text
ai-documentation
ai-planning
full-local-ai
openai-requirements
ollama-requirements
```

But those profiles are out of scope for this step.

---

## Non-Goals

Do not implement these in this step:

- OpenAI provider
- Anthropic provider
- Gemini provider
- Ollama provider
- OpenRouter provider
- Codex executor
- OpenCode executor
- provider configuration file
- provider selection
- API key loading
- `--provider` CLI option
- `--model` CLI option
- `--api-key` CLI option
- AI-powered DocumentationAgent
- AI-powered PlannerAgent
- AI-powered CodeAgent
- AI-powered TestAgent
- AI-powered ReviewerAgent
- real prompt template system
- real JSON schema validation package
- build/test runner
- retry/fix loop
- Git integration
- dashboard
- SQLite storage

This step only introduces workflow profile selection and uses the already implemented `FakeAiProvider` and `AiRequirementsAgent`.

---

## Design Decision

Use only two profiles in this step:

```text
fake
ai-requirements
```

### fake

The default profile.

Pipeline:

```text
Requirements  -> FakeRequirementsAgent
Documentation -> FakeDocumentationAgent
Planning      -> FakePlannerAgent
Code          -> FakeCodeAgent
Testing       -> FakeTestAgent
Review        -> FakeReviewerAgent
```

### ai-requirements

The first AI-aware profile.

Pipeline:

```text
Requirements  -> AiRequirementsAgent
Documentation -> FakeDocumentationAgent
Planning      -> FakePlannerAgent
Code          -> FakeCodeAgent
Testing       -> FakeTestAgent
Review        -> FakeReviewerAgent
```

Important:

- `ai-requirements` must still use `FakeAiProvider`.
- No API keys are required.
- No network calls are made.
- The run must still complete successfully.
- Later fake agents may overwrite some artifacts if they currently write the same files. If that happens, update fake agents only if necessary to preserve `requirements.md` and a valid `specification.json`. Do not over-refactor.

---

## CLI Contract

Add a `--profile` option to the existing `run` command.

Supported values:

```text
fake
ai-requirements
```

Examples:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements
```

If `./scripts/aire` exists:

```bash
./scripts/aire run samples/idea.md
./scripts/aire run samples/idea.md --profile fake
./scripts/aire run samples/idea.md --profile ai-requirements
```

Default:

```text
--profile omitted -> fake
```

Invalid profile:

```bash
aire run samples/idea.md --profile unknown
```

Expected:

```text
exit code 2
error says profile is unsupported
```

Use the existing CLI exit code constants:

```text
0  success
1  execution failed
2  invalid arguments
3  input file not found
```

Do not introduce new exit codes.

---

## Target Folder Structure

Add or update this structure:

```text
src/
|-- AiReliabilityEngineering.Core/
|   `-- Workflow/
|       |-- WorkflowProfile.cs
|       `-- WorkflowProfileParser.cs
|
|-- AiReliabilityEngineering.Orchestration/
|   `-- Pipeline/
|       `-- AgentPipelineFactory.cs
|
|-- tests/
|   |-- AiReliabilityEngineering.Core.Tests/
|   |   `-- Workflow/
|   |       `-- WorkflowProfileParserTests.cs
|   |
|   |-- AiReliabilityEngineering.Orchestration.Tests/
|   |   `-- Pipeline/
|   |       `-- AgentPipelineFactoryTests.cs
|   |
|   `-- AiReliabilityEngineering.Cli.Tests/
|       `-- RunProfileCommandTests.cs
|
`-- docs/
    `-- workflow-profiles.md
```

If the repository already has a different folder layout, follow the existing style, but keep the same logical coverage.

---

# Required Core Changes

## 1. Add WorkflowProfile enum

Create:

```text
src/AiReliabilityEngineering.Core/Workflow/WorkflowProfile.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Workflow;
```

Required enum:

```csharp
namespace AiReliabilityEngineering.Core.Workflow;

public enum WorkflowProfile
{
    Fake,
    AiRequirements
}
```

Rules:

- `Fake` is the default profile.
- `AiRequirements` means only the requirements step uses `AiRequirementsAgent`.
- Do not add future profiles yet.

---

## 2. Add WorkflowProfileParser

Create:

```text
src/AiReliabilityEngineering.Core/Workflow/WorkflowProfileParser.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Workflow;
```

Purpose:

- convert CLI/config string values into `WorkflowProfile`;
- keep parsing out of CLI command handlers;
- make profile names consistent.

Required supported names:

```text
fake
ai-requirements
```

Suggested implementation shape:

```csharp
namespace AiReliabilityEngineering.Core.Workflow;

public static class WorkflowProfileParser
{
    public static bool TryParse(
        string? value,
        out WorkflowProfile profile)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            profile = WorkflowProfile.Fake;
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "fake":
                profile = WorkflowProfile.Fake;
                return true;

            case "ai-requirements":
                profile = WorkflowProfile.AiRequirements;
                return true;

            default:
                profile = WorkflowProfile.Fake;
                return false;
        }
    }

    public static string ToCliName(WorkflowProfile profile)
    {
        return profile switch
        {
            WorkflowProfile.Fake => "fake",
            WorkflowProfile.AiRequirements => "ai-requirements",
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };
    }

    public static IReadOnlyList<string> SupportedCliNames { get; } =
        new[] { "fake", "ai-requirements" };
}
```

Rules:

- Null, empty, or whitespace profile value maps to `Fake`.
- Unknown profile returns false.
- Names are lowercase kebab-case for CLI.
- Do not throw for unknown user input in `TryParse`.
- Throw only for unsupported enum values in `ToCliName`.

---

## 3. Extend RunRequest

Find the current `RunRequest` model.

It likely looks similar to:

```csharp
public sealed record RunRequest(
    string IdeaFilePath,
    string RunsDirectory);
```

Update it to include profile:

```csharp
using AiReliabilityEngineering.Core.Workflow;

public sealed record RunRequest(
    string IdeaFilePath,
    string RunsDirectory,
    WorkflowProfile Profile = WorkflowProfile.Fake);
```

Rules:

- Default profile must be `WorkflowProfile.Fake`.
- Existing code constructing `RunRequest` with two arguments should continue compiling if possible.
- If the current language/version does not allow this exact default shape, provide an overload or static factory that preserves compatibility.
- Do not change unrelated RunRequest behavior.

---

# Required Orchestration Changes

## 4. Add AgentPipelineFactory

Create:

```text
src/AiReliabilityEngineering.Orchestration/Pipeline/AgentPipelineFactory.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Pipeline;
```

Purpose:

- create pipeline steps for a selected workflow profile;
- remove hardcoded profile composition from `AireOrchestrator`;
- keep `AireOrchestrator` focused on run lifecycle and pipeline execution.

Suggested dependencies:

```csharp
public sealed class AgentPipelineFactory
{
    private readonly Func<IRunLogger, IAiProvider> _aiProviderFactory;
    private readonly TimeProvider _timeProvider;

    public AgentPipelineFactory(
        Func<IRunLogger, IAiProvider> aiProviderFactory,
        TimeProvider? timeProvider = null)
    {
        _aiProviderFactory = aiProviderFactory ?? throw new ArgumentNullException(nameof(aiProviderFactory));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public AgentPipeline Create(
        WorkflowProfile profile,
        IRunLogger logger,
        IRunStateStore stateStore)
    {
        // create pipeline for profile
    }
}
```

If the current code style prefers passing `IAiProvider` directly, that is acceptable:

```csharp
public AgentPipelineFactory(
    IAiProvider aiProvider,
    TimeProvider? timeProvider = null)
```

But avoid creating a real provider inside the factory manually unless the project already has a composition root method for it.

The preferred composition is:

```text
CompositionRoot creates AiProvider
CompositionRoot creates AgentPipelineFactory
AgentPipelineFactory creates agents
```

### fake profile pipeline

For `WorkflowProfile.Fake`, create the same exact pipeline currently used by default:

```csharp
new AgentPipelineStep(WorkflowStep.Requirements, new FakeRequirementsAgent(logger)),
new AgentPipelineStep(WorkflowStep.Documentation, new FakeDocumentationAgent(logger)),
new AgentPipelineStep(WorkflowStep.Planning, new FakePlannerAgent(logger)),
new AgentPipelineStep(WorkflowStep.Code, new FakeCodeAgent(logger)),
new AgentPipelineStep(WorkflowStep.Testing, new FakeTestAgent(logger)),
new AgentPipelineStep(WorkflowStep.Review, new FakeReviewerAgent(logger))
```

### ai-requirements profile pipeline

For `WorkflowProfile.AiRequirements`, create:

```csharp
new AgentPipelineStep(WorkflowStep.Requirements, new AiRequirementsAgent(aiProvider, logger)),
new AgentPipelineStep(WorkflowStep.Documentation, new FakeDocumentationAgent(logger)),
new AgentPipelineStep(WorkflowStep.Planning, new FakePlannerAgent(logger)),
new AgentPipelineStep(WorkflowStep.Code, new FakeCodeAgent(logger)),
new AgentPipelineStep(WorkflowStep.Testing, new FakeTestAgent(logger)),
new AgentPipelineStep(WorkflowStep.Review, new FakeReviewerAgent(logger))
```

The `aiProvider` must be the existing `FakeAiProvider` through the existing provider factory/composition root.

Rules:

- Do not add real providers.
- Do not call network.
- Do not read API keys.
- Do not introduce provider config.
- Keep agent order identical across profiles.
- Keep step names identical across profiles.

---

## 5. Update AireOrchestrator to use AgentPipelineFactory

Find the current hardcoded pipeline creation inside `AireOrchestrator`.

Replace internal hardcoded creation with `AgentPipelineFactory`.

Current conceptual shape may be:

```csharp
private AgentPipeline CreatePipeline(IRunLogger logger, IRunStateStore stateStore)
{
    var steps = new AgentPipelineStep[]
    {
        ...
    };

    return new AgentPipeline(steps, stateStore, logger, _timeProvider);
}
```

Required new behavior:

```csharp
var pipeline = _pipelineFactory.Create(request.Profile, logger, stateStore);
```

Constructor should accept an `AgentPipelineFactory` or a delegate.

Suggested shape:

```csharp
public sealed class AireOrchestrator
{
    private readonly AgentPipelineFactory _pipelineFactory;

    public AireOrchestrator(
        Func<RunContext, IRunLogger> loggerFactory,
        Func<RunContext, IRunStateStore> stateStoreFactory,
        AgentPipelineFactory pipelineFactory,
        TimeProvider? timeProvider = null)
    {
        ...
    }
}
```

If adding `AgentPipelineFactory` as a constructor dependency breaks many tests, provide a default factory in constructor overloads while keeping production composition explicit.

Rules:

- `AireOrchestrator` should not hardcode profile-specific agent lists after this step.
- Run state behavior must not change.
- Failure behavior must not change.
- Existing fake workflow tests must still pass.

---

## 6. Update CompositionRoot

Find the current composition root, likely:

```text
src/AiReliabilityEngineering.Cli/CompositionRoot.cs
```

Update it to create:

- FakeAiProvider through existing AiProviderFactory;
- AgentPipelineFactory;
- AireOrchestrator with AgentPipelineFactory.

Conceptual example:

```csharp
public static IAiProvider CreateAiProvider()
{
    var factory = new AiProviderFactory();
    return factory.Create(AiProviderFactoryOptions.Default);
}

public static AgentPipelineFactory CreateAgentPipelineFactory()
{
    return new AgentPipelineFactory(
        _ => CreateAiProvider());
}

public static AireOrchestrator CreateOrchestrator()
{
    return new AireOrchestrator(
        CreateLogger,
        CreateStateStore,
        CreateAgentPipelineFactory());
}
```

Adapt to the actual existing composition root style.

Rules:

- Keep provider fake.
- No config loading.
- No API key loading.
- No network providers.
- Existing CLI should continue working without extra configuration.

---

# Required CLI Changes

## 7. Add --profile option to run command

Update the `run` command to accept:

```text
--profile <profile>
```

Supported values:

```text
fake
ai-requirements
```

Default:

```text
fake
```

Example:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements
```

Implementation guidance:

1. Add an option with default value `"fake"`.
2. Parse it using `WorkflowProfileParser.TryParse`.
3. If parsing fails:
   - write an error to error output;
   - include supported values;
   - return `CliExitCodes.InvalidArguments`.
4. Pass the parsed profile into `RunRequest`.

Suggested error text:

```text
Unsupported workflow profile: unknown
Supported profiles: fake, ai-requirements
```

Rules:

- Missing `--profile` means `WorkflowProfile.Fake`.
- Unknown profile returns exit code 2.
- Do not add profile abbreviations.
- Do not make profile case-sensitive.
- Do not add profile config file yet.

---

## 8. Update CLI help text

Help output should mention:

```text
--profile
fake
ai-requirements
```

If using System.CommandLine, configure the option description clearly.

Suggested description:

```text
Workflow profile to use. Supported values: fake, ai-requirements. Default: fake.
```

---

# Tests

## 9. Core WorkflowProfileParser tests

Add tests under:

```text
tests/AiReliabilityEngineering.Core.Tests/Workflow/
```

### Test: null or empty profile maps to Fake

Cases:

```text
null
""
"   "
```

Expected:

- TryParse returns true;
- profile is WorkflowProfile.Fake.

### Test: fake parses

Input:

```text
fake
```

Expected:

- TryParse returns true;
- profile is WorkflowProfile.Fake.

### Test: ai-requirements parses

Input:

```text
ai-requirements
```

Expected:

- TryParse returns true;
- profile is WorkflowProfile.AiRequirements.

### Test: parsing is case-insensitive

Inputs:

```text
FAKE
Ai-Requirements
```

Expected:

- correct profiles.

### Test: unknown profile returns false

Input:

```text
unknown
```

Expected:

- TryParse returns false.

### Test: ToCliName returns kebab-case names

Expected:

```text
WorkflowProfile.Fake -> fake
WorkflowProfile.AiRequirements -> ai-requirements
```

---

## 10. RunRequest tests

Add or update tests to verify:

- default profile is Fake when constructing with old/current constructor shape;
- explicit profile is retained.

If there are no RunRequest tests, add them under the most appropriate existing test project.

---

## 11. AgentPipelineFactory tests

Add tests under:

```text
tests/AiReliabilityEngineering.Orchestration.Tests/Pipeline/
```

### Test: fake profile creates fake requirements agent

Arrange:

- AgentPipelineFactory with FakeAiProvider;
- test logger;
- test state store.

Act:

```csharp
var pipeline = factory.Create(WorkflowProfile.Fake, logger, stateStore);
```

Assert:

- pipeline contains six steps if accessible;
- first step is WorkflowStep.Requirements;
- first step agent name/type is FakeRequirementsAgent;
- no AiRequirementsAgent is used.

If `AgentPipeline` does not expose steps, either:

1. Add a read-only `Steps` property for tests and diagnostics; or
2. Test by executing pipeline and inspecting produced artifacts.

Preferred minimal change:

```csharp
public IReadOnlyList<AgentPipelineStep> Steps => _steps;
```

This is useful for diagnostics and tests.

### Test: ai-requirements profile creates AiRequirementsAgent

Assert:

- first step is WorkflowStep.Requirements;
- first step agent is AiRequirementsAgent;
- remaining steps use fake agents.

### Test: both profiles keep same step order

Expected order:

```text
Requirements
Documentation
Planning
Code
Testing
Review
```

### Test: factory rejects unsupported enum value

If possible, cast invalid enum:

```csharp
(WorkflowProfile)999
```

Expected:

- ArgumentOutOfRangeException or AiProviderException, depending on implementation.

Prefer ArgumentOutOfRangeException.

---

## 12. Orchestrator profile tests

Add tests under:

```text
tests/AiReliabilityEngineering.Orchestration.Tests/
```

### Test: default fake profile still completes

Arrange:

- RunRequest with no explicit profile or `WorkflowProfile.Fake`.

Act:

- call orchestrator.

Assert:

- run succeeds;
- run-state status is Completed;
- expected fake artifacts exist;
- default behavior remains unchanged.

### Test: ai-requirements profile completes

Arrange:

- RunRequest with `WorkflowProfile.AiRequirements`.

Act:

- call orchestrator.

Assert:

- run succeeds;
- run-state status is Completed;
- `artifacts/specification.json` exists;
- `artifacts/requirements.md` exists;
- `specification.json` is valid JSON;
- run-state first step agent name is `AiRequirementsAgent`.

### Important artifact overwrite check

If fake documentation or later fake agents overwrite `artifacts/specification.json`, adjust the fake agents or artifact names so `AiRequirementsAgent` output remains available.

The acceptance criteria require `requirements.md` and valid `specification.json` after an `ai-requirements` profile run.

Do not introduce a large artifact versioning system in this step.

---

## 13. CLI profile tests

Add or update tests under:

```text
tests/AiReliabilityEngineering.Cli.Tests/
```

### Test: run without profile succeeds

Command:

```text
run idea.md
```

Expected:

- exit code 0;
- output contains Completed;
- default profile is fake.

### Test: run with fake profile succeeds

Command:

```text
run idea.md --profile fake
```

Expected:

- exit code 0;
- output contains Completed.

### Test: run with ai-requirements profile succeeds

Command:

```text
run idea.md --profile ai-requirements
```

Expected:

- exit code 0;
- output contains Completed;
- generated run contains `artifacts/requirements.md`.

Use temporary directories / isolated current directory as existing CLI tests should already do.

### Test: unknown profile returns invalid arguments

Command:

```text
run idea.md --profile unknown
```

Expected:

- exit code 2;
- error mentions unsupported workflow profile;
- error mentions supported profiles.

### Test: help mentions profile option

Command:

```text
--help
```

or command-specific help if already supported:

```text
run --help
```

Expected:

- output mentions `--profile`;
- output mentions `fake`;
- output mentions `ai-requirements`.

---

# Documentation

## 14. Add docs/workflow-profiles.md

Create:

```text
docs/workflow-profiles.md
```

Content should explain:

```markdown
# Workflow Profiles

A workflow profile selects which agents are used in an AIRE run.

## fake

The default profile.

Uses fake agents for all workflow steps.

Command:

```bash
aire run samples/idea.md
```

or:

```bash
aire run samples/idea.md --profile fake
```

## ai-requirements

Uses AiRequirementsAgent for the requirements step and fake agents for all later steps.

Command:

```bash
aire run samples/idea.md --profile ai-requirements
```

This profile still uses FakeAiProvider. It does not require API keys and does not call the network.
```

---

## 15. Update docs/wiki.md

If `docs/wiki.md` exists, add or update entries:

```markdown
## Workflow Profile

A named pipeline composition that selects which agents are used for a run.

Current profiles:

- fake
- ai-requirements
```

---

## 16. Update docs/ai-requirements-agent.md

Update the document created in the previous step to mention:

```markdown
AiRequirementsAgent can now be used through the `ai-requirements` workflow profile:

```bash
aire run samples/idea.md --profile ai-requirements
```

The profile still uses FakeAiProvider.
```

---

## 17. Update README.md

Add a short usage section:

```markdown
## Workflow Profiles

AIRE supports workflow profiles.

Default fake workflow:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

Explicit fake workflow:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
```

AI requirements workflow:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements
```

The `ai-requirements` profile uses AiRequirementsAgent with FakeAiProvider. It does not require API keys and does not call the network.
```

Keep it brief.

---

# Existing Workflow Must Remain Stable

After this step, these commands must pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements
```

Cleanup command should still work when used against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

Do not run cleanup against repo-local `./runs` if it contains useful manual run history.

---

# Suggested Implementation Order for Codex

Follow this order.

## Task 1: Add WorkflowProfile and parser

Create:

```text
src/AiReliabilityEngineering.Core/Workflow/WorkflowProfile.cs
src/AiReliabilityEngineering.Core/Workflow/WorkflowProfileParser.cs
```

Add parser tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 2: Extend RunRequest

Add `WorkflowProfile Profile = WorkflowProfile.Fake`.

Update tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 3: Add AgentPipelineFactory

Create:

```text
src/AiReliabilityEngineering.Orchestration/Pipeline/AgentPipelineFactory.cs
```

Move profile-specific agent composition into the factory.

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 4: Update AireOrchestrator

Update orchestrator to use `AgentPipelineFactory`.

Keep behavior unchanged for default fake profile.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 5: Update CompositionRoot

Wire FakeAiProvider and AgentPipelineFactory.

No real providers.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 6: Add CLI --profile option

Add profile parsing to run command.

Unknown profile returns exit code 2.

Add CLI tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 7: Add orchestration profile tests

Verify both profiles complete.

Verify `ai-requirements` writes `requirements.md`.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 8: Update documentation

Create/update:

```text
docs/workflow-profiles.md
docs/wiki.md if it exists
docs/ai-requirements-agent.md
README.md
```

Run final verification.

---

# Acceptance Criteria

The task is complete when all criteria are satisfied.

## Core

- WorkflowProfile exists.
- WorkflowProfile includes Fake and AiRequirements.
- WorkflowProfileParser exists.
- WorkflowProfileParser supports fake.
- WorkflowProfileParser supports ai-requirements.
- Unknown profile values return false.
- Missing profile value maps to Fake.
- Parser tests pass.

## Run Request

- RunRequest includes WorkflowProfile.
- RunRequest defaults to Fake.
- Existing construction behavior remains compatible where possible.

## Orchestration

- AgentPipelineFactory exists.
- AgentPipelineFactory creates fake profile pipeline.
- AgentPipelineFactory creates ai-requirements profile pipeline.
- AgentPipelineFactory tests pass.
- AireOrchestrator uses AgentPipelineFactory.
- AireOrchestrator no longer hardcodes the full agent list directly.
- Default fake workflow still completes.
- ai-requirements workflow completes.
- ai-requirements workflow uses AiRequirementsAgent for Requirements step.
- ai-requirements workflow writes artifacts/requirements.md.
- ai-requirements workflow leaves a valid artifacts/specification.json.

## CLI

- `aire run idea.md` still works.
- `aire run idea.md --profile fake` works.
- `aire run idea.md --profile ai-requirements` works.
- Unknown profile returns exit code 2.
- Help mentions `--profile`.
- Help mentions fake.
- Help mentions ai-requirements.
- CLI tests pass.

## Documentation

- docs/workflow-profiles.md exists.
- README mentions workflow profiles.
- docs/wiki.md is updated if it exists.
- docs/ai-requirements-agent.md mentions ai-requirements profile.
- Documentation states ai-requirements still uses FakeAiProvider and requires no API keys.

## Verification

These commands pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements
```

Cleanup passes when run against disposable data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Notes for Codex

- Keep the step focused on workflow profiles.
- Do not add real AI providers.
- Do not add OpenAI SDK.
- Do not add Ollama HTTP code.
- Do not add provider config.
- Do not add API key handling.
- Do not replace fake profile as the default.
- Do not change cleanup behavior.
- Keep outputs deterministic.
- Use temporary directories in tests.
- Preserve existing CLI behavior.
- Save this file as UTF-8 without BOM.
