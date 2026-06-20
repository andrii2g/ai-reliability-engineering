# AIRE Wiki

This page explains the common names, terms, files, folders, and statements used inside AIRE.
It is intentionally lightweight and should evolve together with the project.

## Naming

| Name | Meaning |
| --- | --- |
| AIRE | Product name and short form of AI Reliability Engineering |
| AI Reliability Engineering | The discipline/practice behind the project: making AI-assisted development observable, testable, reviewable, and reliable |
| aire | CLI command name |
| ai-reliability-engineering | GitHub repository name |
| AiReliabilityEngineering.slnx | .NET solution file |
| AiReliabilityEngineering.Cli | .NET CLI project |

## Core Terms

| Term | Meaning |
| --- | --- |
| Run | One execution of the AIRE workflow |
| Run ID | Unique identifier of a workflow execution |
| Run Folder | Folder under `runs/` containing input, artifacts, reports, logs, workspace, and run state |
| Agent | Workflow component responsible for one specific part of the process |
| Fake Agent | Stub implementation used by the skeleton to keep the full pipeline working |
| Orchestrator | Component that coordinates the workflow and executes agents in order |
| Artifact | Generated file such as `specification.json`, `README.md`, `PLAN.md`, `tasks.json`, or `review.md` |
| Report | Generated diagnostic/output file such as `tests.md` |
| Workspace | Folder where generated or modified repository files are placed |
| Tool Executor | Abstraction for running tools or commands such as shell, Codex, OpenCode, Git, Docker, or dotnet |
| AI Provider | Adapter that converts AIRE's provider-neutral AI request into a concrete model call |

## Workflow Terms

| Step | Meaning |
| --- | --- |
| Requirements | Convert the input idea into a structured specification |
| Documentation | Generate initial documentation files |
| Planning | Generate implementation tasks |
| Code | Create or update source files |
| Testing | Generate or run tests and write test reports |
| Review | Review generated artifacts and workflow result |

## Run Folder Contract

```text
runs/{run-id}/
|-- input/
|-- workspace/
|-- artifacts/
|-- reports/
|-- logs/
`-- run-state.json
```

| Path | Purpose |
| --- | --- |
| `input/` | Copied input files for the run |
| `workspace/` | Generated or modified repository content |
| `artifacts/` | Main generated files |
| `reports/` | Build, test, review, and summary reports |
| `logs/` | Run and agent logs |
| `run-state.json` | Persistent state of the run |

## Project Statements

- AIRE is local-first.
- AIRE should be reliable before it becomes smart.
- The developer stays in control.
- Every step must keep the system runnable.
- Fake agents are valid until replaced by real implementations.
- Codex and OpenCode are tool executors, not the core architecture.
- Logs, artifacts, reports, and run state are part of the product.
- Build and tests must not be hidden or ignored.

## AI Provider

Current providers:

- `FakeAiProvider`: deterministic local provider used by default for tests and early development.
- `OpenAiProvider`: real provider for manual AI requirements demos.

AI request contracts validate invalid shapes at construction time.

Provider selection chooses which `IAiProvider` implementation is used by AI-aware agents.

Current provider CLI names:

- `fake`
- `openai`

OpenAI usage requires `OPENAI_API_KEY` in the environment and an explicit `--model` value. API keys must not be passed through CLI arguments.

Future providers may include Ollama, Anthropic, and Gemini.

## ProjectSpecification

The normalized requirements contract produced by requirements analysis. Later agents can consume it for documentation, planning, code generation, testing, and review.

## AiRequirementsAgent

The first AI-aware AIRE agent. It calls `IAiProvider` but currently writes deterministic artifacts using local normalization.

## AiDocumentationAgent

Generates `README.md` and `PLAN.md` from `specification.json`.

## AiPlannerAgent

Generates `tasks.json` from `specification.json`.

## Workflow Profile

A named pipeline composition that selects which agents are used for a run.

Current profiles:

- `fake`
- `ai-requirements`
- `ai-demo`

## ai-demo Profile

Runs `AiRequirementsAgent`, `AiDocumentationAgent`, and `AiPlannerAgent` before fake code/test/review steps.

## Documentation Map

| File | Purpose |
| --- | --- |
| `README.md` | User-facing overview and quick start |
| `docs/PRD.md` | Product requirements and high-level system description |
| `PLAN.md` | Codex-ready implementation plan |
| `docs/wiki.md` | Naming, terms, and project statements |
| `docs/QUICKSTART.md` | Quickstart for running the current skeleton |
| `docs/implementation.md` | Notes about the current implementation |
| `docs/ai-providers.md` | AI provider abstraction and fake provider notes |
| `docs/openai-provider.md` | OpenAI provider usage and scope |
| `docs/demo-openai-requirements.md` | Manual OpenAI requirements demo |
| `docs/ai-requirements-agent.md` | AI requirements agent notes |
| `docs/ai-documentation-agent.md` | AI documentation agent notes |
| `docs/ai-planner-agent.md` | AI planner agent notes |
| `docs/demo-ai-artifacts.md` | AI artifacts demo |
| `docs/workflow-profiles.md` | Workflow profile usage |
| `docs/architecture.md` | Deeper architecture notes when needed |
| `docs/workflow.md` | Detailed workflow notes when needed |
