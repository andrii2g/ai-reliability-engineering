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
./scripts/aire -cleanup
```

This removes generated folders and files under `runs/`, preserves `runs/`, and recreates `runs/.gitkeep`.

The Bash script wraps the underlying .NET command:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

The command creates a new folder under `runs/` containing the copied input file, placeholder artifacts, simulated reports, logs, workspace files, and `run-state.json`.
