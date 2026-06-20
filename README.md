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
