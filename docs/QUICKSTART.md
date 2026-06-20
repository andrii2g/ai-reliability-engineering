# Quickstart

This guide runs the current AIRE skeleton with fake workflow logic.

## Prerequisites

- Linux, macOS, WSL, or another environment with Bash.
- .NET 10 SDK installed.

Check the SDK:

```bash
dotnet --version
```

## Make the Launcher Executable

From the repository root:

```bash
chmod +x scripts/aire
```

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

The script wraps:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli/AiReliabilityEngineering.Cli.csproj -- run samples/idea.md
```

## Expected Output

The command prints a final summary:

```text
Run ID: ...
Run directory: ...
Status: Completed
```

It also creates a new run folder:

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

The final `run-state.json` should show `Completed` with six successful fake workflow steps.

## Clean Generated Runs

After test runs, clean generated run folders and files:

```bash
./scripts/aire cleanup
```

Cleanup preserves the `runs/` directory and recreates `runs/.gitkeep`.
Do not run cleanup against a `runs/` directory that contains useful manual history.
