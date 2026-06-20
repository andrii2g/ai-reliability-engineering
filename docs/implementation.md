# Implementation Notes

This document tracks what has been implemented in AIRE and how the current skeleton works.

## Current Milestone

Milestone 1 implements a local-first fake workflow. It does not call real AI services or external coding agents.

The implemented command is:

```bash
./scripts/aire run samples/idea.md
```

The Bash script wraps:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

The command creates a new run folder under `runs/`, executes fake agents in order, writes placeholder artifacts, writes logs, writes `run-state.json`, and prints a final summary.

Generated runs can be cleaned with:

```bash
./scripts/aire cleanup
```

Cleanup removes generated folders and files under `runs/`, preserves `runs/`, recreates `runs/.gitkeep`, and does not delete anything outside `runs/`.

## Projects

### `AiReliabilityEngineering.Cli`

Contains the command-line entry point.

Current responsibilities:

- define the `run <idea-file>` command using `System.CommandLine`;
- define the `cleanup` command for removing generated run outputs;
- validate the idea file path;
- compose the orchestrator with infrastructure implementations;
- print the final summary;
- return process exit codes.

### `AiReliabilityEngineering.Core`

Contains dependency-free domain contracts and models.

Current areas:

- agents;
- artifacts;
- runs;
- workflow steps;
- tool execution contracts.

### `AiReliabilityEngineering.Orchestration`

Coordinates the fake workflow.

Current responsibilities:

- create run directories;
- clean generated run folders and files;
- copy input files;
- create and update run state;
- execute fake agents sequentially;
- stop on failed agents;
- return run results.

### `AiReliabilityEngineering.Infrastructure`

Contains concrete infrastructure implementations.

Current implementations:

- console run logger;
- file run logger;
- composite run logger;
- JSON run state store;
- fake tool executor;
- shell tool executor.

## Workflow

The current pipeline order is:

```text
Requirements -> Documentation -> Planning -> Code -> Testing -> Review
```

Each fake agent writes one or more placeholder files and returns artifact references using relative paths.

## Run Folder Contract

Each run is created under:

```text
runs/{run-id}/
```

Expected files:

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

The final `run-state.json` should have status `Completed` and six successful workflow steps.

## Verification

Use these commands from the repository root:

```bash
dotnet build AiReliabilityEngineering.slnx
dotnet test AiReliabilityEngineering.slnx
./scripts/aire run samples/idea.md
./scripts/aire cleanup
```

Current verified test count:

```text
23 tests passing
```

## Known Deferrals

The following are intentionally not implemented yet:

- real AI calls;
- Codex or OpenCode integration;
- Git operations;
- Docker sandboxing;
- dashboard or web UI;
- real source-code generation;
- build/test fix loop.
