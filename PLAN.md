# PLAN.md - Current CLI and Run Skeleton Alignment

## Project

Repository: `ai-reliability-engineering`
Solution: `AiReliabilityEngineering.slnx`
CLI project: `src/AiReliabilityEngineering.Cli`
CLI command name: `aire`

## Goal

Keep the current AIRE skeleton aligned, documented, buildable, testable, and runnable.
Implement the first working command-line interface for AIRE.

This step must keep the system simple. The CLI should understand basic commands, validate arguments, call placeholder services, print clear output, and return stable exit codes.

## Current CLI Contract

Primary local command through the Bash launcher:

```bash
./scripts/aire run samples/idea.md
./scripts/aire -cleanup
```

Equivalent development commands through `dotnet run`:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- -cleanup
```

Help is provided by `System.CommandLine`:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- --help
dotnet run --project src/AiReliabilityEngineering.Cli -- -h
```

## Scope Rules

### Preserve

- Keep `System.CommandLine` as the CLI framework.
- Keep `run <idea-file>` wired to the current fake workflow.
- Keep `-cleanup` as the cleanup command shape.
- Keep cleanup safe: delete only generated entries under `runs/`, preserve `runs/`, and recreate `runs/.gitkeep`.
- Keep the Bash launcher as the user-facing local entry point.
- Keep documentation paths under `docs/`, including `docs/PRD.md`.
- Keep Markdown files UTF-8 without BOM.

### Do Not Regress

- Do not replace the fake workflow with a placeholder-only service.
- Do not remove run folder creation.
- Do not remove `run-state.json`.
- Do not remove fake agents.
- Do not remove the orchestration pipeline.
- Do not remove real cleanup behavior.
- Do not switch back to manual CLI routing.
- Do not introduce new CLI frameworks.
- Do not add advanced cleanup options yet, such as `--dry-run`, `--older-than`, `--keep-last`, or `--runs-dir`.

## Expected CLI Behavior

### 1. Run With Existing File

Command:

```bash
./scripts/aire run samples/idea.md
```

Expected behavior:

- Validate that `samples/idea.md` exists.
- Create a new run folder under `runs/`.
- Copy the input file to `input/idea.md`.
- Execute fake agents in order:
  - Requirements
  - Documentation
  - Planning
  - Code
  - Testing
  - Review
- Write placeholder artifacts, reports, logs, workspace files, and `run-state.json`.
- Print a final summary containing:
  - `Run ID:`
  - `Run directory:`
  - `Status: Completed`
- Return exit code `0`.

### 2. Run With Missing File

Command:

```bash
./scripts/aire run missing.md
```

Expected behavior:

- Print a friendly missing-file error.
- Do not create a run folder.
- Return non-zero.

### 3. Invalid Arguments

Examples:

```bash
./scripts/aire
./scripts/aire unknown
./scripts/aire run
./scripts/aire run samples/idea.md extra
```

Expected behavior:

- Print usage or a friendly parse error.
- Return non-zero.

### 4. Cleanup

Command:

```bash
./scripts/aire -cleanup
```

Expected behavior:

- Remove generated folders and files directly under `runs/`.
- Preserve the `runs/` directory.
- Preserve or recreate `runs/.gitkeep`.
- Do not delete anything outside `runs/`.
- Return exit code `0` when cleanup succeeds or there is nothing to clean.

Example output:

```text
Runs cleanup completed. Deleted 1 entries.
Runs directory: /path/to/repo/runs
Deleted entries: 1
```

## Current Implementation Shape

CLI:

- `Program.cs` stays thin.
- `CliCommandHandler` defines the `System.CommandLine` root command, `run <idea-file>`, and `-cleanup`.
- `CompositionRoot` wires the orchestrator and cleanup service.

Orchestration:

- `AireOrchestrator` owns the fake workflow run.
- `RunDirectoryFactory` creates the run folder layout and copies input.
- `AgentPipeline` executes fake agents sequentially and stops on failure.
- `RunCleanupService` safely cleans generated run outputs.

Infrastructure:

- Console, file, and composite run loggers.
- JSON run state store.
- Fake and shell tool executors.

## Run Folder Contract

Each successful run creates:

```text
runs/{run-id}/
|-- input/
|-- workspace/
|-- artifacts/
|-- reports/
|-- logs/
`-- run-state.json
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

Final `run-state.json` must show `Completed` and six successful steps.

## Tests

Keep tests for:

- Core model behavior.
- CLI invalid arguments and missing input.
- CLI successful `run` invocation.
- CLI `-cleanup` invocation.
- Orchestration run folder creation.
- Input copy to `input/idea.md`.
- Expected artifact, report, log, workspace, and state files.
- Completed state with six successful steps.
- Pipeline stops on failed agent.
- Cleanup deletes only generated entries under `runs/`.
- Cleanup preserves or recreates `runs/.gitkeep`.
- Cleanup does not delete anything outside `runs/`.

## Documentation

Keep these docs consistent with the current CLI contract:

```bash
./scripts/aire run samples/idea.md
./scripts/aire -cleanup
```

Relevant files:

- `README.md`
- `docs/PRD.md`
- `docs/wiki.md`
- `docs/QUICKSTART.md`
- `docs/implementation.md`

Do not refer to a root-level `PRD.md`; the product requirements document lives at `docs/PRD.md`.

## Verification Commands

Run:

```bash
dotnet build AiReliabilityEngineering.slnx
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- -cleanup
```

On Linux, WSL, or a Bash-capable environment, also verify:

```bash
chmod +x scripts/aire
./scripts/aire run samples/idea.md
./scripts/aire -cleanup
```

## Acceptance Criteria

This step is complete when:

- `dotnet build AiReliabilityEngineering.slnx` passes.
- `dotnet test AiReliabilityEngineering.slnx` passes.
- `run <idea-file>` creates a completed fake workflow run.
- `-cleanup` cleans generated run outputs safely.
- `runs/.gitkeep` exists after cleanup.
- Documentation references `docs/PRD.md`, not root `PRD.md`.
- Markdown files touched by this step are UTF-8 without BOM.
- No mojibake characters remain in `PLAN.md`.
