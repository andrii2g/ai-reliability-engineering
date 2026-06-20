# AIRE - AI Reliability Engineering

AIRE is a local-first development orchestrator for building reliable AI-assisted software workflows.

The first milestone proves the workflow skeleton with fake agents, inspectable run folders, logs, state, reports, and placeholder artifacts.

## Quickstart

See [docs/QUICKSTART.md](docs/QUICKSTART.md).

## Documentation

- [PRD](docs/PRD.md) - product requirements and high-level description
- [PLAN](PLAN.md) - Codex-ready implementation plan
- [AIRE Wiki](docs/wiki.md) - project terms, naming, and common statements

## Build

```bash
dotnet build AiReliabilityEngineering.slnx
```

## Test

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Run the Fake Workflow

```bash
./scripts/aire run samples/idea.md
```

## Clean Generated Runs

```bash
./scripts/aire cleanup
```

This removes generated folders and files under `runs/`, preserves `runs/`, and recreates `runs/.gitkeep`.
Do not run cleanup against a `runs/` directory that contains useful manual history.

The Bash script wraps the underlying .NET command:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

The command creates a new folder under `runs/` containing the copied input file, placeholder artifacts, simulated reports, logs, workspace files, and `run-state.json`.

## AI Provider Layer

AIRE now contains the first provider-neutral AI abstraction.

The default implementation is `FakeAiProvider`. It is deterministic, local-only, and does not require API keys or network access.

AI request contracts validate invalid shapes at construction time.

`OpenAiProvider` is available for manual real-provider demos behind the same `IAiProvider` contract.

## AI Requirements Agent

The repository now contains `AiRequirementsAgent`, the first AI-aware agent.

It uses the provider-neutral `IAiProvider` contract and is currently tested with fake/test providers only. The default CLI workflow still uses the stable fake pipeline, and the agent can be selected with the `ai-requirements` workflow profile.

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

The `ai-requirements` profile uses `AiRequirementsAgent` with `FakeAiProvider`. It does not require API keys and does not call the network.

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

- `artifacts/specification.json`
- `artifacts/requirements.md`
- `artifacts/README.md`
- `artifacts/PLAN.md`
- `artifacts/tasks.json`

Source code generation is not implemented yet.

## Generated .NET Workspace Demo

Generate requirements, documentation, planning artifacts, a deterministic .NET CLI project, and build/test reports:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
```

The generated project is written under the run workspace:

- `workspace/Directory.Packages.props`
- `workspace/GeneratedTool.slnx`
- `workspace/src/GeneratedTool.Cli/`
- `workspace/tests/GeneratedTool.Cli.Tests/`

Build and test reports are written to:

- `reports/build.md`
- `reports/tests.md`

This demo does not use AI to write source code yet. It uses a deterministic template so the build/test validation path is stable.

## Final Review Demo

Run the most complete local demo:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```

The run produces:

- requirements artifacts
- documentation artifacts
- planning artifacts
- generated .NET workspace
- build/test reports
- final review reports

Review:

- `reports/final-review.md`
- `reports/workspace-summary.md`

## Real Provider Demo

AIRE can run the AI requirements workflow with OpenAI:

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

The default provider remains fake and does not require API keys. Do not pass API keys through CLI arguments.
