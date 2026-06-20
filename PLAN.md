# PLAN.md - AIRE Step 10.8-10.10: Git Snapshot, Generated Files Report, Code Executor Abstraction, and OpenCode/Codex Integration

## Purpose

This plan is for Codex to implement the next AIRE milestone after the local generated .NET demo and deterministic final review.

AIRE means AI Reliability Engineering.

Repository conventions:

- Repository name: `ai-reliability-engineering`
- Solution name: `AiReliabilityEngineering.slnx`
- CLI project: `AiReliabilityEngineering.Cli`
- CLI command name: `aire`
- PRD location: `docs/PRD.md`

Previous steps should already be implemented:

- stable fake workflow;
- cleanup command;
- workflow profiles;
- `fake`;
- `ai-requirements`;
- `ai-demo`;
- `ai-demo-dotnet`;
- `ai-demo-dotnet-review`;
- AI provider contracts;
- FakeAiProvider;
- OpenAiProvider;
- AiRequirementsAgent;
- AiDocumentationAgent;
- AiPlannerAgent;
- TemplateCodeAgent;
- BuildTestAgent;
- ArtifactReviewAgent;
- generated .NET workspace demo;
- build/test reports;
- final-review.md and workspace-summary.md;
- samples/redis-ttl-audit.md.

The goal of this plan is to add the final local foundations needed before real external coding agents become useful:

1. Git workspace snapshot.
2. Generated files report.
3. Code executor abstraction.
4. Fake code executor.
5. OpenCode executor integration.
6. Codex executor integration.
7. New guarded workflow profiles that keep execution inside the run workspace.

This plan intentionally keeps OpenCode/Codex execution optional and disabled unless explicitly selected through a workflow profile.

---

## High-Level Goal

Add these new profiles:

```text
ai-demo-dotnet-review-git
ai-demo-dotnet-opencode
ai-demo-dotnet-codex
```

Expected profile behavior:

```text
ai-demo-dotnet-review-git
  Requirements  -> AiRequirementsAgent
  Documentation -> AiDocumentationAgent
  Planning      -> AiPlannerAgent
  Code          -> TemplateCodeAgent
  Testing       -> BuildTestAgent
  Review        -> ArtifactReviewAgent
  Finalize      -> GitWorkspaceSnapshotAgent

ai-demo-dotnet-opencode
  Requirements  -> AiRequirementsAgent
  Documentation -> AiDocumentationAgent
  Planning      -> AiPlannerAgent
  Code          -> ExternalCodeAgent(OpenCodeExecutor)
  Testing       -> BuildTestAgent
  Review        -> ArtifactReviewAgent
  Finalize      -> GitWorkspaceSnapshotAgent

ai-demo-dotnet-codex
  Requirements  -> AiRequirementsAgent
  Documentation -> AiDocumentationAgent
  Planning      -> AiPlannerAgent
  Code          -> ExternalCodeAgent(CodexExecutor)
  Testing       -> BuildTestAgent
  Review        -> ArtifactReviewAgent
  Finalize      -> GitWorkspaceSnapshotAgent
```

Important: `ExternalCodeAgent` must create the deterministic template workspace first, then run the selected external executor against that workspace. This preserves a working baseline before any external coding agent modifies files.

---

## Non-Goals

Do not implement these in this step:

- Git commit;
- Git push;
- GitHub PR creation;
- remote repository operations;
- Docker;
- Kubernetes;
- multi-template selection;
- real issue tracker integration;
- persistent queue/worker mode;
- UI/dashboard;
- automatic credential handling for OpenCode/Codex;
- installing OpenCode or Codex;
- assuming OpenCode/Codex are installed;
- running OpenCode/Codex by default;
- allowing arbitrary external commands from user input;
- executing outside run workspace.

---

## Important Safety Rules

External code executors are powerful. They may read, write, and run code depending on their own configuration.

AIRE must enforce these rules:

1. External executors run only inside `runContext.Paths.WorkspaceDirectory`.
2. Do not pass repository root as working directory.
3. Do not pass secrets to executor prompts.
4. Do not include `OPENAI_API_KEY` or other provider keys in executor prompts.
5. Do not write executor logs containing secrets.
6. Use fixed timeouts.
7. Capture stdout/stderr.
8. Write code execution reports.
9. Run BuildTestAgent after external executor.
10. Use GitWorkspaceSnapshotAgent after review/finalization to summarize changed files.

OpenCode/Codex profiles are opt-in only.

Default profile remains `fake`.

---

## Design Decision: Add Finalize Workflow Step

Add a new workflow step:

```text
Finalize
```

This step is for non-review finalization work such as:

- Git snapshot reports;
- generated files reports;
- future packaging reports;
- future PR preparation.

Do not overload the Review step with Git snapshot responsibilities.

The pipeline should support both old six-step profiles and new seven-step profiles.

Existing profiles do not need to include Finalize.

---

## Design Decision: Git Snapshot Is Local and Reporting-Only

GitWorkspaceSnapshotAgent should be local-only and safe.

It may initialize Git inside the run workspace if no `.git` directory exists.

It must not:

- commit;
- push;
- create branches;
- create pull requests;
- modify files outside workspace.

Required reports:

```text
reports/generated-files.md
reports/generated-files.json
reports/git-status.md
```

Optional but useful:

```text
reports/git-diff-summary.md
```

Do not require a commit in this step.

---

## Design Decision: ExternalCodeAgent Uses Baseline Template First

ExternalCodeAgent must:

1. Create the deterministic .NET template project using the same DotnetTemplateProjectWriter/TemplateCodeAgent path.
2. Build a coding task prompt from artifacts/tasks.json, artifacts/README.md, and artifacts/PLAN.md.
3. Run selected ICodeExecutor inside workspace.
4. Write reports/code-execution.md.
5. Return success if executor exits successfully.

BuildTestAgent remains responsible for validation after code execution.

This keeps OpenCode/Codex integration behind a stable baseline.

---

## Target Folder Structure

Add or update this structure:

```text
src/
|-- AiReliabilityEngineering.Core/
|   |-- Git/
|   |   |-- GeneratedFileEntry.cs
|   |   |-- GeneratedFilesReport.cs
|   |   |-- GitStatusEntry.cs
|   |   `-- GitWorkspaceSnapshot.cs
|   |
|   |-- CodeExecution/
|   |   |-- CodeExecutorKind.cs
|   |   |-- CodeExecutionRequest.cs
|   |   |-- CodeExecutionResult.cs
|   |   |-- ICodeExecutor.cs
|   |   `-- CodeExecutorSelection.cs
|   |
|   `-- Workflow/
|       |-- WorkflowProfile.cs
|       `-- WorkflowProfileParser.cs
|
|-- AiReliabilityEngineering.Orchestration/
|   |-- Agents/
|   |   |-- GitWorkspaceSnapshotAgent.cs
|   |   |-- GeneratedFilesReporter.cs
|   |   |-- GitStatusParser.cs
|   |   |-- GitSnapshotReportWriter.cs
|   |   |-- ExternalCodeAgent.cs
|   |   |-- CodeExecutionPromptBuilder.cs
|   |   `-- CodeExecutionReportWriter.cs
|   |
|   `-- Pipeline/
|       `-- AgentPipelineFactory.cs
|
|-- AiReliabilityEngineering.Infrastructure/
|   |-- Git/
|   |   `-- GitCommandRunner.cs
|   |
|   `-- CodeExecution/
|       |-- FakeCodeExecutor.cs
|       |-- OpenCodeExecutor.cs
|       `-- CodexExecutor.cs
|
|-- tests/
|   |-- AiReliabilityEngineering.Core.Tests/
|   |-- AiReliabilityEngineering.Orchestration.Tests/
|   `-- AiReliabilityEngineering.Infrastructure.Tests/
|
`-- docs/
    |-- git-workspace-snapshot.md
    |-- generated-files-report.md
    |-- code-executor-abstraction.md
    |-- opencode-codex-integration.md
    `-- demo-external-code-executors.md
```

Follow existing folder conventions if they differ.

---

# Required Core Changes

## 1. Extend WorkflowStep

Find the current `WorkflowStep` enum.

Add:

```csharp
Finalize
```

Expected step order:

```text
Requirements
Documentation
Planning
Code
Testing
Review
Finalize
```

Rules:

- Existing six-step profiles continue working.
- Run state serialization must still work.
- If RunStatus has per-step completed states, add `FinalizationCompleted` or equivalent.
- If RunStatus does not require per-step states, just record the step result.

---

## 2. Extend WorkflowProfile

Add:

```csharp
AiDemoDotnetReviewGit
AiDemoDotnetOpenCode
AiDemoDotnetCodex
```

CLI names:

```text
ai-demo-dotnet-review-git
ai-demo-dotnet-opencode
ai-demo-dotnet-codex
```

Update WorkflowProfileParser:

- parse all new names;
- include them in SupportedCliNames;
- ToCliName maps them correctly;
- missing profile still maps to Fake;
- unknown profile still fails.

---

## 3. Add Git Core Models

Create:

```text
src/AiReliabilityEngineering.Core/Git/GeneratedFileEntry.cs
src/AiReliabilityEngineering.Core/Git/GeneratedFilesReport.cs
src/AiReliabilityEngineering.Core/Git/GitStatusEntry.cs
src/AiReliabilityEngineering.Core/Git/GitWorkspaceSnapshot.cs
```

### GeneratedFileEntry

Suggested shape:

```csharp
public sealed record GeneratedFileEntry
{
    public GeneratedFileEntry(string relativePath, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path is required.", nameof(relativePath));
        }

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        RelativePath = relativePath.Replace('\\\\', '/');
        SizeBytes = sizeBytes;
    }

    public string RelativePath { get; }

    public long SizeBytes { get; }
}
```

### GeneratedFilesReport

```csharp
public sealed record GeneratedFilesReport
{
    public GeneratedFilesReport(IReadOnlyList<GeneratedFileEntry> files)
    {
        Files = (files ?? throw new ArgumentNullException(nameof(files)))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<GeneratedFileEntry> Files { get; }

    public int Count => Files.Count;

    public long TotalSizeBytes => Files.Sum(file => file.SizeBytes);
}
```

### GitStatusEntry

```csharp
public sealed record GitStatusEntry
{
    public GitStatusEntry(string status, string path)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Git status is required.", nameof(status));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Git path is required.", nameof(path));
        }

        Status = status.Trim();
        Path = path.Replace('\\\\', '/');
    }

    public string Status { get; }

    public string Path { get; }
}
```

### GitWorkspaceSnapshot

```csharp
public sealed record GitWorkspaceSnapshot
{
    public GitWorkspaceSnapshot(
        GeneratedFilesReport generatedFiles,
        IReadOnlyList<GitStatusEntry> statusEntries)
    {
        GeneratedFiles = generatedFiles ?? throw new ArgumentNullException(nameof(generatedFiles));
        StatusEntries = (statusEntries ?? Array.Empty<GitStatusEntry>())
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToArray();
    }

    public GeneratedFilesReport GeneratedFiles { get; }

    public IReadOnlyList<GitStatusEntry> StatusEntries { get; }
}
```

---

## 4. Add Code Execution Core Models

Create:

```text
src/AiReliabilityEngineering.Core/CodeExecution/CodeExecutorKind.cs
src/AiReliabilityEngineering.Core/CodeExecution/CodeExecutorSelection.cs
src/AiReliabilityEngineering.Core/CodeExecution/CodeExecutionRequest.cs
src/AiReliabilityEngineering.Core/CodeExecution/CodeExecutionResult.cs
src/AiReliabilityEngineering.Core/CodeExecution/ICodeExecutor.cs
```

### CodeExecutorKind

```csharp
public enum CodeExecutorKind
{
    Fake,
    OpenCode,
    Codex
}
```

### CodeExecutorSelection

```csharp
public sealed record CodeExecutorSelection
{
    public CodeExecutorSelection(CodeExecutorKind kind)
    {
        Kind = kind;
    }

    public CodeExecutorKind Kind { get; }

    public static CodeExecutorSelection Fake { get; } =
        new(CodeExecutorKind.Fake);
}
```

### CodeExecutionRequest

```csharp
public sealed record CodeExecutionRequest
{
    public CodeExecutionRequest(
        string workspaceDirectory,
        string prompt,
        TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(workspaceDirectory))
        {
            throw new ArgumentException("Workspace directory is required.", nameof(workspaceDirectory));
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(prompt));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        WorkspaceDirectory = workspaceDirectory;
        Prompt = prompt;
        Timeout = timeout;
    }

    public string WorkspaceDirectory { get; }

    public string Prompt { get; }

    public TimeSpan Timeout { get; }
}
```

### CodeExecutionResult

```csharp
public sealed record CodeExecutionResult
{
    public CodeExecutionResult(
        bool succeeded,
        int exitCode,
        string standardOutput,
        string standardError,
        TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Succeeded = succeeded;
        ExitCode = exitCode;
        StandardOutput = standardOutput ?? string.Empty;
        StandardError = standardError ?? string.Empty;
        Duration = duration;
    }

    public bool Succeeded { get; }

    public int ExitCode { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public TimeSpan Duration { get; }
}
```

### ICodeExecutor

```csharp
public interface ICodeExecutor
{
    string Name { get; }

    Task<CodeExecutionResult> ExecuteAsync(
        CodeExecutionRequest request,
        CancellationToken cancellationToken);
}
```

---

# Required Orchestration Components

## 5. GeneratedFilesReporter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/GeneratedFilesReporter.cs
```

Purpose:

- enumerate files under workspace;
- exclude transient directories;
- return GeneratedFilesReport.

Rules:

- Use `runContext.Paths.WorkspaceDirectory`.
- Exclude:
  - `.git/`
  - `bin/`
  - `obj/`
  - `.vs/`
  - `.idea/`
- Return relative paths.
- Normalize separators.
- Sort deterministically.
- Do not scan outside workspace.

---

## 6. GitStatusParser

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/GitStatusParser.cs
```

Purpose:

- parse `git status --short` output.

Examples:

```text
?? src/GeneratedTool.Cli/Program.cs
 M src/GeneratedTool.Cli/Program.cs
A  tests/SmokeTests.cs
```

Rules:

- status is first two columns trimmed.
- path is remaining text trimmed.
- ignore blank lines.
- normalize separators.
- return GitStatusEntry list.

---

## 7. GitSnapshotReportWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/GitSnapshotReportWriter.cs
```

Writes:

```text
reports/generated-files.md
reports/generated-files.json
reports/git-status.md
```

Rules:

- Use `runContext.Paths.ReportsDirectory`.
- Ensure reports directory exists.
- Write deterministic Markdown/JSON.
- generated-files.json should contain file count, total size, and files.
- git-status.md should include status table.
- If git is unavailable, write a report that states git snapshot could not be collected.
- Return artifact refs.

---

## 8. GitWorkspaceSnapshotAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/GitWorkspaceSnapshotAgent.cs
```

The agent implements IAgent.

Dependencies:

- IToolExecutor for git commands;
- IRunLogger;
- GeneratedFilesReporter;
- GitStatusParser;
- GitSnapshotReportWriter.

Execution flow:

1. Check cancellation.
2. Validate context.
3. Ensure workspace directory exists.
4. If `.git` does not exist under workspace:
   - run `git init` in workspace.
5. Run `git status --short` in workspace.
6. Parse status output.
7. Generate files report.
8. Write generated-files.md, generated-files.json, git-status.md.
9. Return AgentResult.Success if reports were written.

Timeouts:

```text
git init          -> 30 seconds
git status --short -> 30 seconds
```

If git is not installed or git command fails:

- do not fail the whole run;
- write reports indicating git command failure;
- return AgentResult.Success if reports were written.

Reason: Git snapshot is finalization/reporting, not a critical generation step.

---

## 9. CodeExecutionPromptBuilder

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/CodeExecutionPromptBuilder.cs
```

Purpose:

- build a bounded prompt for external code agents from generated artifacts.

Inputs:

- specification.json;
- tasks.json;
- README.md;
- PLAN.md.

Rules:

- Read only from artifacts directory.
- Do not include secrets.
- Do not include environment variables.
- Keep prompt bounded.
- If files are too large, truncate each section to a fixed max length, e.g. 6000 characters.
- Prompt must explicitly say:
  - work only inside current workspace;
  - do not delete unrelated files;
  - keep generated project buildable;
  - run no network commands unless required by tool itself;
  - implement only small safe improvements;
  - do not change AIRE repository root.

Suggested prompt sections:

```text
# AIRE Coding Task

## Scope
You are operating inside the generated run workspace only.

## Project Specification
...

## Implementation Tasks
...

## Existing Documentation
...

## Requirements
- Keep build passing.
- Keep tests passing.
- Make small, safe changes.
- Do not modify files outside workspace.
```

---

## 10. CodeExecutionReportWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/CodeExecutionReportWriter.cs
```

Writes:

```text
reports/code-execution.md
```

Include:

- executor name;
- exit code;
- duration;
- stdout;
- stderr;
- success/failure.

Do not include secrets.

---

## 11. ExternalCodeAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/ExternalCodeAgent.cs
```

Implements IAgent.

Dependencies:

- TemplateCodeAgent or DotnetTemplateProjectWriter/WorkspaceArtifactReader;
- ICodeExecutor;
- CodeExecutionPromptBuilder;
- CodeExecutionReportWriter;
- IRunLogger.

Execution flow:

1. Check cancellation.
2. Create deterministic template workspace first.
3. Build code execution prompt from artifacts.
4. Create CodeExecutionRequest:
   - workspaceDirectory = runContext.Paths.WorkspaceDirectory;
   - timeout = 10 minutes;
   - prompt from builder.
5. Run ICodeExecutor.
6. Write reports/code-execution.md.
7. If executor failed, return AgentResult.Failure.
8. If executor succeeded, return AgentResult.Success.

Rules:

- Do not run build/test here.
- BuildTestAgent validates after Code step.
- Do not allow executor to run outside workspace.
- Do not pass secrets in prompt.
- Do not swallow OperationCanceledException.

---

# Required Infrastructure Components

## 12. FakeCodeExecutor

Create:

```text
src/AiReliabilityEngineering.Infrastructure/CodeExecution/FakeCodeExecutor.cs
```

Behavior:

- Name = `fake-code-executor`;
- does not modify files;
- returns success;
- deterministic stdout.

Used in tests and optional local profiles if needed.

---

## 13. OpenCodeExecutor

Create:

```text
src/AiReliabilityEngineering.Infrastructure/CodeExecution/OpenCodeExecutor.cs
```

Behavior:

- Name = `opencode`;
- uses IToolExecutor or ShellToolExecutor through composition;
- runs command in workspace;
- timeout = request.Timeout;
- captures stdout/stderr;
- maps result to CodeExecutionResult.

Do not hardcode a single OpenCode command if current CLI syntax is uncertain.

Instead, define command construction in one small method:

```csharp
protected virtual ToolExecutionRequest CreateToolRequest(CodeExecutionRequest request)
```

Recommended initial command:

```text
opencode run <prompt>
```

If the actual installed OpenCode CLI uses different syntax, this one method can be adjusted later.

Do not fail at construction time if OpenCode is not installed. Runtime command failure is reported in code-execution.md.

---

## 14. CodexExecutor

Create:

```text
src/AiReliabilityEngineering.Infrastructure/CodeExecution/CodexExecutor.cs
```

Behavior:

- Name = `codex`;
- uses IToolExecutor or ShellToolExecutor through composition;
- runs command in workspace;
- timeout = request.Timeout;
- captures stdout/stderr;
- maps result to CodeExecutionResult.

Do not hardcode risky flags.

Recommended initial command:

```text
codex exec <prompt>
```

If the installed Codex CLI syntax differs, command construction is isolated in:

```csharp
protected virtual ToolExecutionRequest CreateToolRequest(CodeExecutionRequest request)
```

Runtime command failure should be captured and reported, not crash the process.

---

## 15. CodeExecutorFactory

Create if useful:

```text
src/AiReliabilityEngineering.Infrastructure/CodeExecution/CodeExecutorFactory.cs
```

Inputs:

- CodeExecutorSelection;
- IToolExecutor.

Output:

- FakeCodeExecutor;
- OpenCodeExecutor;
- CodexExecutor.

Do not instantiate ShellToolExecutor in Orchestration.

CLI composition root wires:

```text
CodeExecutorSelection -> CodeExecutorFactory -> executor
```

---

# CLI and Profile Selection

## 16. Add Code Executor Selection Internally

Do not add a public `--code-executor` option yet unless necessary.

Map profiles:

```text
ai-demo-dotnet-opencode -> CodeExecutorKind.OpenCode
ai-demo-dotnet-codex    -> CodeExecutorKind.Codex
```

Keep the selection internal to AgentPipelineFactory/CompositionRoot.

Reason:

- profile already expresses executor choice;
- fewer CLI options;
- less user confusion.

---

## 17. Update AgentPipelineFactory

Add profiles:

```text
AiDemoDotnetReviewGit
AiDemoDotnetOpenCode
AiDemoDotnetCodex
```

Profile `AiDemoDotnetReviewGit`:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> TemplateCodeAgent
Testing       -> BuildTestAgent
Review        -> ArtifactReviewAgent
Finalize      -> GitWorkspaceSnapshotAgent
```

Profile `AiDemoDotnetOpenCode`:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> ExternalCodeAgent(OpenCodeExecutor)
Testing       -> BuildTestAgent
Review        -> ArtifactReviewAgent
Finalize      -> GitWorkspaceSnapshotAgent
```

Profile `AiDemoDotnetCodex`:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> ExternalCodeAgent(CodexExecutor)
Testing       -> BuildTestAgent
Review        -> ArtifactReviewAgent
Finalize      -> GitWorkspaceSnapshotAgent
```

AgentPipeline must support Finalize step.

---

## 18. Update CLI Help

Help should mention new profiles through existing profile help.

Supported profiles now include:

```text
fake
ai-requirements
ai-demo
ai-demo-dotnet
ai-demo-dotnet-review
ai-demo-dotnet-review-git
ai-demo-dotnet-opencode
ai-demo-dotnet-codex
```

Do not add code-executor option yet.

---

# Tests

## 19. Core tests

Add tests for:

- GeneratedFileEntry validation;
- GeneratedFilesReport sorting/count/size;
- GitStatusEntry validation;
- GitWorkspaceSnapshot sorting;
- CodeExecutionRequest validation;
- CodeExecutionResult validation;
- CodeExecutorSelection;
- WorkflowProfileParser new profiles.

---

## 20. Orchestration tests

Add tests for:

### GeneratedFilesReporter

- excludes bin/obj/.git;
- returns relative sorted files;
- normalizes separators.

### GitStatusParser

- parses untracked files;
- parses modified files;
- parses added files;
- ignores blank lines.

### GitSnapshotReportWriter

- writes generated-files.md;
- writes generated-files.json;
- writes git-status.md;
- JSON is valid.

### GitWorkspaceSnapshotAgent

- runs git init when .git missing;
- runs git status --short;
- writes reports;
- succeeds even if git command fails but reports are written;
- uses workspace directory;
- uses 30-second timeouts.

Use fake/recording IToolExecutor. Do not require real git in unit tests.

### CodeExecutionPromptBuilder

- includes specification/tasks/docs;
- truncates long sections;
- does not read outside artifacts.

### CodeExecutionReportWriter

- writes code-execution.md;
- includes executor name, exit code, stdout/stderr.

### ExternalCodeAgent

- creates template baseline first;
- calls ICodeExecutor with workspace directory;
- uses 10-minute timeout;
- writes code-execution.md;
- returns failure on executor failure;
- does not run build/test itself.

Use fake ICodeExecutor.

---

## 21. Infrastructure tests

Add tests for:

### FakeCodeExecutor

- returns success;
- does not require external commands.

### OpenCodeExecutor

- creates expected ToolExecutionRequest;
- uses workspace directory;
- passes timeout;
- maps tool result success/failure;
- does not call real OpenCode.

### CodexExecutor

- creates expected ToolExecutionRequest;
- uses workspace directory;
- passes timeout;
- maps tool result success/failure;
- does not call real Codex.

Use fake/recording IToolExecutor.

---

## 22. Profile/orchestrator tests

Add tests:

### ai-demo-dotnet-review-git

- completes with fake provider and fake tool executor;
- includes Finalize step;
- writes generated-files.md/json and git-status.md.

### ai-demo-dotnet-opencode

- builds pipeline with ExternalCodeAgent;
- uses OpenCode executor selection;
- unit/integration test uses FakeCodeExecutor or recording executor, not real OpenCode.

### ai-demo-dotnet-codex

- builds pipeline with ExternalCodeAgent;
- uses Codex executor selection;
- unit/integration test uses FakeCodeExecutor or recording executor, not real Codex.

Do not make automated tests require OpenCode/Codex installed.

---

# Documentation

## 23. Add docs/git-workspace-snapshot.md

Explain:

- local workspace git init/status;
- generated files report;
- no commits/pushes;
- reports generated.

## 24. Add docs/generated-files-report.md

Explain:

- what generated-files.md/json contain;
- excluded directories;
- deterministic ordering.

## 25. Add docs/code-executor-abstraction.md

Explain:

- ICodeExecutor;
- CodeExecutionRequest/Result;
- FakeCodeExecutor;
- future executors.

## 26. Add docs/opencode-codex-integration.md

Explain:

- OpenCode/Codex profiles are opt-in;
- tools must be installed/configured by developer;
- AIRE runs them only inside workspace;
- build/test validates after execution;
- no secrets in prompts.

Mention that Codex CLI is a local terminal coding agent capable of reading/changing/running code in the selected directory, and OpenCode is an open-source coding agent usable from terminal/desktop/IDE. Do not copy long external docs.

## 27. Add docs/demo-external-code-executors.md

Include commands:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review-git --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-opencode --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-codex --provider fake
```

Explain OpenCode/Codex commands require tools installed.

## 28. Update README.md

Add short section:

```markdown
## Git Snapshot and External Code Executors

AIRE can now generate Git/workspace reports and includes opt-in profiles for OpenCode and Codex executors.

Local Git snapshot demo:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review-git --provider fake
```

OpenCode and Codex profiles are opt-in and require the corresponding CLI tools to be installed and configured.
```

---

# Existing Workflow Must Remain Stable

These commands must still pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review-git --provider fake
```

Do not require OpenCode/Codex installed for normal tests.

Manual only:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-opencode --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-codex --provider fake
```

Cleanup still works:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Suggested Implementation Order for Codex

## Task 1: Add Finalize workflow step and new profiles

Update WorkflowStep, WorkflowProfile, parser, and pipeline support.

## Task 2: Add Git/generated-files core models

Add models and tests.

## Task 3: Add GeneratedFilesReporter and GitStatusParser

Add orchestration helpers and tests.

## Task 4: Add GitSnapshotReportWriter and GitWorkspaceSnapshotAgent

Use fake tool executor tests.

## Task 5: Add CodeExecution core abstractions

Add ICodeExecutor and related models/tests.

## Task 6: Add CodeExecutionPromptBuilder and CodeExecutionReportWriter

Add tests.

## Task 7: Add ExternalCodeAgent

Use fake code executor tests.

## Task 8: Add infrastructure code executors

Add FakeCodeExecutor, OpenCodeExecutor, CodexExecutor, CodeExecutorFactory.

## Task 9: Wire profiles in AgentPipelineFactory and CompositionRoot

Keep Orchestration infrastructure-agnostic.

## Task 10: Add docs and README updates

Run final verification.

---

# Acceptance Criteria

## Git Snapshot

- Generated files report is written.
- Git status report is written.
- Git command failure does not fail finalization if reports are written.
- No commits/pushes are performed.
- Unit tests do not require real git.

## Code Executor Abstraction

- ICodeExecutor exists.
- FakeCodeExecutor exists.
- CodeExecutionPromptBuilder exists.
- CodeExecutionReportWriter exists.
- ExternalCodeAgent exists.
- ExternalCodeAgent creates template baseline before running executor.
- ExternalCodeAgent uses 10-minute timeout.
- BuildTestAgent remains responsible for validation.

## OpenCode/Codex

- OpenCodeExecutor exists.
- CodexExecutor exists.
- Both run only inside workspace.
- Both use IToolExecutor/ShellToolExecutor through infrastructure composition.
- No automated tests require actual OpenCode/Codex.
- Runtime failures are captured in reports.

## Profiles

- ai-demo-dotnet-review-git exists.
- ai-demo-dotnet-opencode exists.
- ai-demo-dotnet-codex exists.
- Default profile remains fake.
- Existing profiles remain unchanged.

## Documentation

- git-workspace-snapshot.md exists.
- generated-files-report.md exists.
- code-executor-abstraction.md exists.
- opencode-codex-integration.md exists.
- demo-external-code-executors.md exists.
- README is updated.

## Verification

These pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review-git --provider fake
```

Manual optional profiles:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-opencode --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-codex --provider fake
```

---

# Notes for Codex

- Keep this step safety-first.
- Do not add Docker or Kubernetes.
- Do not add Git commits or pushes.
- Do not require OpenCode/Codex installed in automated tests.
- Do not pass secrets in prompts.
- Do not execute outside workspace.
- Keep default profile unchanged.
- Use temporary directories in tests.
- Save this file as UTF-8 without BOM.
