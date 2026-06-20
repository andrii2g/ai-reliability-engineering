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

The current implementation includes only a `FakeAiProvider`. It is deterministic, local-only, and does not require API keys or network access.

AI request contracts validate invalid shapes at construction time.

Real providers such as OpenAI, Ollama, Anthropic, and Gemini will be added later behind the same `IAiProvider` contract.

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
