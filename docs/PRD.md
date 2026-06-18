# AIRE — AI Reliability Engineering

For common naming and terminology, see [AIRE Wiki](wiki.md).

## High-Level Description

AIRE is a local-first development orchestrator for building reliable AI-assisted software workflows.

The goal is to help a developer transform an initial project idea or requirements document into working software artifacts step by step: documentation, implementation plan, source code, tests, review reports, and eventually Git commits or pull requests.

AIRE is not intended to be a fully autonomous “magic developer”. The developer remains in control. The system should automate routine work, keep every step observable, and make AI-generated changes safer by using logs, validation, tests, review gates, and repeatable workflows.

## Main Goal

Given an input description such as:

```markdown
Create a Redis TTL audit tool.
It should scan Redis keys, detect keys without TTL, group them by prefix, and generate a Markdown report.
```

AIRE should create a structured run:

```text
runs/{run-id}/
├─ input/
├─ workspace/
├─ artifacts/
├─ reports/
├─ logs/
└─ run-state.json
```

The first version does not need real AI integration. It should prove that the workflow skeleton works reliably.

## Core Principle

The system must always stay working.

Every implementation step must leave the application runnable, testable, and demonstrable. Early implementations may use fake agents that only write logs and placeholder files, but the full workflow should exist from the beginning.

Example first demo behavior:

```text
RequirementsAgent started
RequirementsAgent completed
DocumentationAgent started
DocumentationAgent completed
PlannerAgent started
PlannerAgent completed
CodeAgent started
CodeAgent completed
TestAgent started
TestAgent completed
ReviewerAgent started
ReviewerAgent completed
```

## Initial Input

The initial supported input is a Markdown file:

```text
idea.md
```

Later inputs may include:

```text
requirements.md
existing repository path
GitHub repository URL
template name
target technology stack
execution mode
```

## Main Output

AIRE should produce:

```text
specification.json
README.md
PLAN.md
tasks.json
tests.md
review.md
logs
run-state.json
```

Later versions may also produce:

```text
source code changes
test results
build reports
Git diff summary
Git commits
GitHub pull requests
```

## Main System Parts

### CLI

The first user-facing interface.

Initial command:

```bash
aire run idea.md
```

Future commands:

```bash
aire status <run-id>
aire report <run-id>
aire logs <run-id>
aire artifacts <run-id>
```

### Orchestrator

Coordinates the workflow.

Responsibilities:

* create run folders;
* copy input files;
* execute workflow steps;
* call agents;
* update run state;
* write logs;
* stop on failure;
* produce final summary.

### Agents

Agents are small workflow components with clear responsibilities.

Initial agents:

```text
RequirementsAgent
DocumentationAgent
PlannerAgent
CodeAgent
TestAgent
ReviewerAgent
```

In the first milestone these agents may be fake/stubbed. Each fake agent should log its execution and write a placeholder artifact.

Later, fake agents will be replaced one by one with real implementations.

### Tool Executors

Tool executors wrap external tools and commands.

Initial executors:

```text
FakeToolExecutor
ShellToolExecutor
```

Future executors:

```text
CodexExecutor
OpenCodeExecutor
GitExecutor
DockerExecutor
DotnetExecutor
PythonExecutor
```

Codex and OpenCode should be treated as pluggable executors, not as the core architecture.

### Run State

Each run should have a persistent state file:

```text
runs/{run-id}/run-state.json
```

Initial states:

```text
Created
Running
RequirementsCompleted
DocumentationCompleted
PlanningCompleted
CodeCompleted
TestingCompleted
ReviewCompleted
Completed
Failed
```

### Logging

Logging is required from the first version.

Minimum events:

```text
RunStarted
RunCompleted
RunFailed
StepStarted
StepCompleted
StepFailed
ArtifactWritten
```

Logs should be written to console and to files under:

```text
runs/{run-id}/logs/
```

## Initial .NET Structure

Repository:

```text
ai-reliability-engineering/
├─ AiReliabilityEngineering.slnx
├─ src/
│  ├─ AiReliabilityEngineering.Cli/
│  ├─ AiReliabilityEngineering.Core/
│  ├─ AiReliabilityEngineering.Orchestration/
│  └─ AiReliabilityEngineering.Infrastructure/
├─ tests/
│  ├─ AiReliabilityEngineering.Core.Tests/
│  └─ AiReliabilityEngineering.Orchestration.Tests/
├─ samples/
│  └─ idea.md
├─ docs/
├─ README.md
└─ PLAN.md
```

CLI command name:

```text
aire
```

Formal .NET project name:

```text
AiReliabilityEngineering.Cli
```

Solution name:

```text
AiReliabilityEngineering.slnx
```

## First Milestone

The first milestone is a working fake workflow.

Command:

```bash
aire run samples/idea.md
```

Expected result:

```text
Run folder is created
Input file is copied
All fake agents are executed in order
Placeholder artifacts are generated
Logs are written
run-state.json is written
Final summary is printed
Process exits with code 0
```

Expected generated artifacts:

```text
artifacts/specification.json
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json
artifacts/review.md
reports/tests.md
logs/orchestrator.log
run-state.json
```

## Epic Implementation Steps

### Step 1: Review High-Level Description

Review and approve the system purpose, naming, workflow, and first milestone.

### Step 2: Create Repository Skeleton

Create the solution, projects, tests, sample input file, README, and PLAN.

Result:

```text
dotnet build works
dotnet test works
CLI starts
```

### Step 3: Implement Basic CLI

Implement:

```bash
aire run <idea-file>
```

Initial behavior:

```text
validate input file
create run ID
create run folder
print run summary
```

### Step 4: Implement Run Folders and Run State

Create the standard run directory structure and write `run-state.json`.

### Step 5: Implement Logging

Add console and file logging for run and step lifecycle events.

### Step 6: Implement Agent Abstractions

Define shared contracts for agents, agent context, agent result, artifacts, and run context.

### Step 7: Implement Fake Agents

Implement fake versions of all initial agents.

Each fake agent should:

```text
log started
write placeholder artifact
log completed
return success
```

### Step 8: Implement Orchestrator Pipeline

Execute agents in order:

```text
Requirements
Documentation
Planning
Code
Testing
Review
```

Stop on failure and update run state.

### Step 9: Add Tests for the Fake Workflow

Protect the skeleton with tests:

```text
run folder is created
input is copied
run-state.json is written
agents execute in order
expected artifacts are created
failed agent stops the pipeline
```

### Step 10: Replace Fake Parts One by One

Replace fake logic incrementally:

```text
real requirements extraction
real documentation generation
real planning
real project generation
real test execution
real review
```

### Step 11: Add Tool Execution

Add safe command execution through tool executor abstractions.

### Step 12: Add Build/Test Runner

Support commands such as:

```text
dotnet build
dotnet test
pytest
go test
```

### Step 13: Add Codex/OpenCode Integration

Add Codex and OpenCode as optional coding executors behind the same abstraction.

### Step 14: Add Fix Loop

If build or tests fail, allow a limited retry loop:

```text
implement
build/test
fix if failed
build/test again
review
```

### Step 15: Add Git Integration

Add optional Git support:

```text
git status
git diff
git add
git commit
push
pull request
```

## Definition of Done for Every Step

Every step must satisfy:

```text
dotnet build passes
dotnet test passes
CLI still runs
existing workflow still works
new behavior is covered by tests
logs remain useful
previous artifact contracts are not broken accidentally
```

## Summary

AIRE should be built as a reliable orchestrator first and an AI-powered system second.

The first version should prove the workflow:

```text
input → run → agents → artifacts → reports → logs → final status
```

After that, real AI integrations and coding tools can be safely added behind stable abstractions.
