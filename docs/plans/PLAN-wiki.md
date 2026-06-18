# PLAN.md - Add Simple AIRE Wiki Page

## Goal

Add a small, simple wiki-style documentation page to explain the naming used in the AIRE system.

This is a documentation-only step. Do not redesign the system. Do not add new runtime features.

The goal is to make the repository easier to understand while keeping the current skeleton working.

## Context

Project naming:

- Repository: `ai-reliability-engineering`
- Product name: `AIRE`
- Full meaning: `AI Reliability Engineering`
- CLI command: `aire`
- Solution: `AiReliabilityEngineering.slnx`
- Main CLI project: `AiReliabilityEngineering.Cli`

Current project rule:

Every change must keep the system buildable, testable, and runnable.

## Required Changes

### 1. Create `docs/wiki.md`

Create a new Markdown file:

```text
docs/wiki.md
```

The page should be compact and practical.

Suggested title:

```markdown
# AIRE Wiki
```

Suggested purpose paragraph:

```markdown
This page explains the common names, terms, files, folders, and statements used inside AIRE.
It is intentionally lightweight and should evolve together with the project.
```

### 2. Add Naming Section

Add a section:

```markdown
## Naming
```

Include the following entries:

```markdown
| Name | Meaning |
| --- | --- |
| AIRE | Product name and short form of AI Reliability Engineering |
| AI Reliability Engineering | The discipline/practice behind the project: making AI-assisted development observable, testable, reviewable, and reliable |
| aire | CLI command name |
| ai-reliability-engineering | GitHub repository name |
| AiReliabilityEngineering.slnx | .NET solution file |
| AiReliabilityEngineering.Cli | .NET CLI project |
```

### 3. Add Core Terms Section

Add a section:

```markdown
## Core Terms
```

Include these terms:

```markdown
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
```

### 4. Add Workflow Terms Section

Add a section:

```markdown
## Workflow Terms
```

Include:

```markdown
| Step | Meaning |
| --- | --- |
| Requirements | Convert the input idea into a structured specification |
| Documentation | Generate initial documentation files |
| Planning | Generate implementation tasks |
| Code | Create or update source files |
| Testing | Generate or run tests and write test reports |
| Review | Review generated artifacts and workflow result |
```

### 5. Add Folder Contract Section

Add a section:

```markdown
## Run Folder Contract
```

Include the expected run folder shape:

```text
runs/{run-id}/
├─ input/
├─ workspace/
├─ artifacts/
├─ reports/
├─ logs/
└─ run-state.json
```

Briefly explain each folder:

```markdown
| Path | Purpose |
| --- | --- |
| `input/` | Copied input files for the run |
| `workspace/` | Generated or modified repository content |
| `artifacts/` | Main generated files |
| `reports/` | Build, test, review, and summary reports |
| `logs/` | Run and agent logs |
| `run-state.json` | Persistent state of the run |
```

### 6. Add Project Statements Section

Add a section:

```markdown
## Project Statements
```

Include these statements:

```markdown
- AIRE is local-first.
- AIRE should be reliable before it becomes smart.
- The developer stays in control.
- Every step must keep the system runnable.
- Fake agents are valid until replaced by real implementations.
- Codex and OpenCode are tool executors, not the core architecture.
- Logs, artifacts, reports, and run state are part of the product.
- Build and tests must not be hidden or ignored.
```

### 7. Add Documentation Map Section

Add a section:

```markdown
## Documentation Map
```

Include:

```markdown
| File | Purpose |
| --- | --- |
| `README.md` | User-facing overview and quick start |
| `docs/PRD.md` | Product requirements and high-level system description |
| `PLAN.md` | Codex-ready implementation plan |
| `docs/wiki.md` | Naming, terms, and project statements |
| `docs/QUICKSTART.md` | Quickstart for running the current skeleton |
| `docs/implementation.md` | Notes about the current implementation |
| `docs/architecture.md` | Deeper architecture notes when needed |
| `docs/workflow.md` | Detailed workflow notes when needed |
```

If some files do not exist yet, do not create them unless they are already planned. The wiki can mention them as intended documentation locations.

### 8. Update `README.md`

Add a small documentation section to `README.md`.

Example:

```markdown
## Documentation

- [PRD](docs/PRD.md) - product requirements and high-level description
- [PLAN](PLAN.md) - Codex-ready implementation plan
- [AIRE Wiki](docs/wiki.md) - project terms, naming, and common statements
```

Keep it short.

### 9. Optional: Link Wiki from `docs/PRD.md`

If `docs/PRD.md` already exists, add one short link to `docs/wiki.md`.

Example:

```markdown
For common naming and terminology, see [AIRE Wiki](wiki.md).
```

Do not rewrite `docs/PRD.md`.

## File Encoding

Write new and updated Markdown files as UTF-8 without BOM.

## Non-Goals

Do not:

- add a real GitHub Wiki;
- add a documentation generator;
- add MkDocs, Docusaurus, or static site tooling;
- add new CLI commands;
- change the orchestration workflow;
- rename existing projects;
- modify runtime behavior;
- introduce new packages.

## Verification

Run:

```bash
dotnet build
dotnet test
```

If the CLI already works, also run:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

The commands must still pass.

## Expected Result

After this step, the repository should contain:

```text
docs/wiki.md
README.md updated with link to docs/wiki.md
```

The project must still build and test successfully.

## Definition of Done

This step is complete when:

- `docs/wiki.md` exists;
- the wiki explains AIRE naming and core terms;
- `README.md` links to the wiki;
- `docs/PRD.md` links to the wiki if `docs/PRD.md` exists;
- no runtime behavior is changed;
- `dotnet build` passes;
- `dotnet test` passes;
- existing CLI workflow still works.
