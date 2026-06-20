# PLAN.md - AIRE: Git Snapshot, Generated Files Report, Code Executor Abstraction, and OpenCode/Codex Integration

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

1. Finalize workflow step.
2. Git workspace snapshot.
3. Generated files report.
4. Code executor abstraction.
5. Fake code executor.
6. OpenCode executor integration.
7. Codex executor integration.
8. Guarded workflow profiles that keep execution inside the run workspace.

This plan intentionally keeps OpenCode/Codex execution opt-in only and avoids requiring the real tools in automated tests.

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
  Code          -> ExternalCodeAgent(OpenCode executor selected through injected factory)
  Testing       -> BuildTestAgent
  Review        -> ArtifactReviewAgent
  Finalize      -> GitWorkspaceSnapshotAgent

ai-demo-dotnet-codex
  Requirements  -> AiRequirementsAgent
  Documentation -> AiDocumentationAgent
  Planning      -> AiPlannerAgent
  Code          -> ExternalCodeAgent(Codex executor selected through injected factory)
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

External code executors are powerful. AIRE must enforce these rules:

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
11. OpenCode/Codex profiles are opt-in only.

Default profile remains `fake`.

---

## Critical Architecture Boundary Decision

`AgentPipelineFactory` lives in `AiReliabilityEngineering.Orchestration`.

`OpenCodeExecutor`, `CodexExecutor`, and `ShellToolExecutor` are infrastructure implementations and must live in `AiReliabilityEngineering.Infrastructure`.

Therefore:

- `AiReliabilityEngineering.Orchestration` must not reference `AiReliabilityEngineering.Infrastructure`.
- `AgentPipelineFactory` must accept delegates/factories for external dependencies.
- The CLI composition root wires those delegates to Infrastructure implementations.

Required `AgentPipelineFactory` dependency shape:

```csharp
public sealed class AgentPipelineFactory
{
    private readonly Func<AiProviderSelection, IAiProvider> _aiProviderFactory;
    private readonly Func<CodeExecutorSelection, ICodeExecutor> _codeExecutorFactory;
    private readonly Func<IToolExecutor> _toolExecutorFactory;

    public AgentPipelineFactory(
        Func<AiProviderSelection, IAiProvider> aiProviderFactory,
        Func<CodeExecutorSelection, ICodeExecutor> codeExecutorFactory,
        Func<IToolExecutor> toolExecutorFactory,
        TimeProvider? timeProvider = null)
    {
        _aiProviderFactory = aiProviderFactory ?? throw new ArgumentNullException(nameof(aiProviderFactory));
        _codeExecutorFactory = codeExecutorFactory ?? throw new ArgumentNullException(nameof(codeExecutorFactory));
        _toolExecutorFactory = toolExecutorFactory ?? throw new ArgumentNullException(nameof(toolExecutorFactory));
        ...
    }
}
```

Acceptable equivalent shapes:

```csharp
Func<CodeExecutorKind, ICodeExecutor>
Func<RunContext, CodeExecutorSelection, ICodeExecutor>
```

But the key rule is non-negotiable:

```text
AgentPipelineFactory must not instantiate or reference OpenCodeExecutor, CodexExecutor, ShellToolExecutor, or any Infrastructure namespace.
```

The CLI composition root should wire:

```text
CodeExecutorSelection.Fake     -> FakeCodeExecutor
CodeExecutorSelection.OpenCode -> OpenCodeExecutor
CodeExecutorSelection.Codex    -> CodexExecutor
IToolExecutor                  -> ShellToolExecutor
```

Tests should inject fake/recording delegates.

---

## Critical OpenCode/Codex Command Decision

The exact installed CLI syntax for OpenCode and Codex can differ by version and environment.

This plan requires executor implementations, but the command mapping is intentionally isolated and provisional.

### Required behavior

- Implement `OpenCodeExecutor` and `CodexExecutor`.
- Each executor maps `CodeExecutionRequest` to `ToolExecutionRequest`.
- Mapping is isolated in a single overridable/testable method:
  - `CreateToolRequest(CodeExecutionRequest request)`
- Automated tests verify mapping only.
- Automated tests must not invoke real OpenCode or Codex.
- Profiles may be used manually if the developer has the corresponding CLI installed and the provisional command works in their environment.

### Provisional default command mapping

Use these defaults as provisional initial mappings:

```text
OpenCodeExecutor:
  executable: opencode
  arguments: run <prompt-file-path>
  working directory: request.WorkspaceDirectory
  timeout: request.Timeout

CodexExecutor:
  executable: codex
  arguments: exec <prompt-file-path>
  working directory: request.WorkspaceDirectory
  timeout: request.Timeout
```

These are accepted for this implementation as provisional. They must be isolated so a future change can update command syntax in one method per executor.

Do not pass the full prompt text as a command-line argument. `ExternalCodeAgent` must write the bounded prompt to a deterministic file inside the generated workspace, then pass that file path through `CodeExecutionRequest`.

### Manual execution warning

Docs must clearly state:

- OpenCode/Codex profiles require the corresponding CLI installed and configured.
- Command syntax may need adjustment depending on installed tool version.
- Automated tests do not require these tools.

---

## Critical RunStatus Decision

This repo has per-step `RunStatus` values.

Add an explicit status:

```csharp
RunStatus.FinalizationCompleted
```

Update pipeline status mapping:

```text
StepStartedStatus(WorkflowStep.Finalize)   -> RunStatus.ReviewCompleted or current prior state
StepCompletedStatus(WorkflowStep.Finalize) -> RunStatus.FinalizationCompleted
```

After all steps finish, the terminal run status must still be:

```text
RunStatus.Completed
```

Required behavior:

- Finalize step result is recorded in `RunState.Steps`.
- When Finalize step succeeds, state may temporarily become `FinalizationCompleted`.
- After the pipeline completes, final state becomes `Completed`.
- Existing six-step profiles still finish as `Completed`.

Update `AgentPipeline.StepStartedStatus` and `AgentPipeline.StepCompletedStatus` explicitly.

---

## Critical Tool Exception Handling Decision

Current `ShellToolExecutor` may throw before returning `ToolExecutionResult` when a command cannot start.

This plan requires all finalization/external-code command failures to be converted into reports/results.

### GitWorkspaceSnapshotAgent

If `IToolExecutor.ExecuteAsync(...)` throws for git commands:

- catch the exception unless it is `OperationCanceledException`;
- create a failure-like Git command result internally;
- write reports stating git command failed;
- return `AgentResult.Success` if reports were written.

Git finalization is diagnostic and must not fail the whole run because git is unavailable.

### OpenCodeExecutor and CodexExecutor

If `IToolExecutor.ExecuteAsync(...)` throws:

- catch the exception unless it is `OperationCanceledException`;
- return `CodeExecutionResult` with:
  - `Succeeded = false`;
  - non-zero exit code, e.g. `1`;
  - stdout empty;
  - stderr containing safe exception message;
  - duration measured if possible.
- do not crash the process because executable is missing.

### ExternalCodeAgent

If `ICodeExecutor.ExecuteAsync(...)` returns failure:

- write reports/code-execution.md;
- return `AgentResult.Failure`.

If `ICodeExecutor.ExecuteAsync(...)` throws non-cancellation exception unexpectedly:

- catch it;
- write reports/code-execution.md if possible;
- return `AgentResult.Failure`.

Do not swallow `OperationCanceledException`.

---

## Critical Transient Git Status Decision

After `dotnet build` and `dotnet test`, workspace may contain transient `bin/` and `obj/` files.

Do not write `.gitignore` in this step.

Instead, filter transient paths from both:

- generated files report;
- git status report.

Transient path filters:

```text
.git/
bin/
obj/
.vs/
.idea/
```

The filter must apply to nested directories too, for example:

```text
src/GeneratedTool.Cli/bin/Debug/...
src/GeneratedTool.Cli/obj/...
tests/GeneratedTool.Cli.Tests/bin/...
tests/GeneratedTool.Cli.Tests/obj/...
```

`GitStatusParser` may parse everything, but `GitWorkspaceSnapshotAgent` or report writer must filter transient status entries before writing reports.

Recommended: create a shared helper:

```text
TransientWorkspacePathFilter
```

or keep a private helper in both reporters if simpler.

---

## Required Workflow Changes

## 1. Extend WorkflowStep

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

Existing six-step profiles continue working.

Update run-state status mapping as described above.

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

Supported profiles after this step:

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

---

# Required Core Models

## 3. Git Core Models

Create:

```text
src/AiReliabilityEngineering.Core/Git/GeneratedFileEntry.cs
src/AiReliabilityEngineering.Core/Git/GeneratedFilesReport.cs
src/AiReliabilityEngineering.Core/Git/GitStatusEntry.cs
src/AiReliabilityEngineering.Core/Git/GitWorkspaceSnapshot.cs
```

### GeneratedFileEntry

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

        RelativePath = relativePath.Replace('\\', '/');
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
        Path = path.Replace('\\', '/');
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
            .Where(entry => entry is not null)
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToArray();
    }

    public GeneratedFilesReport GeneratedFiles { get; }

    public IReadOnlyList<GitStatusEntry> StatusEntries { get; }
}
```

---

## 4. Code Execution Core Models

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
        string promptFilePath,
        TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(workspaceDirectory))
        {
            throw new ArgumentException("Workspace directory is required.", nameof(workspaceDirectory));
        }

        if (string.IsNullOrWhiteSpace(promptFilePath))
        {
            throw new ArgumentException("Prompt file path is required.", nameof(promptFilePath));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        WorkspaceDirectory = workspaceDirectory;
        PromptFilePath = promptFilePath;
        Timeout = timeout;
    }

    public string WorkspaceDirectory { get; }

    public string PromptFilePath { get; }

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

## 5. TransientWorkspacePathFilter

Create if useful:

```text
src/AiReliabilityEngineering.Orchestration/Agents/TransientWorkspacePathFilter.cs
```

Purpose:

- identify transient paths that should not appear in generated-files or git-status reports.

Rules:

- Normalize separators to `/`.
- Exclude any path segment equal to:
  - `.git`
  - `bin`
  - `obj`
  - `.vs`
  - `.idea`

Suggested API:

```csharp
public static class TransientWorkspacePathFilter
{
    public static bool IsTransient(string relativePath)
    {
        ...
    }
}
```

---

## 6. GeneratedFilesReporter

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
- Exclude transient paths using TransientWorkspacePathFilter.
- Return relative paths.
- Normalize separators.
- Sort deterministically.
- Do not scan outside workspace.

---

## 7. GitStatusParser

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
- Parser may parse transient paths; filtering is applied before report writing.

---

## 8. GitSnapshotReportWriter

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
- generated-files.json contains file count, total size, and files.
- git-status.md includes status table.
- Filter transient git status entries before writing.
- If git is unavailable, write a report that states git snapshot could not be collected.
- Return artifact refs.

---

## 9. GitWorkspaceSnapshotAgent

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
6. Catch non-cancellation tool exceptions and convert to reportable failure text.
7. Parse status output if available.
8. Filter transient status entries.
9. Generate files report.
10. Write generated-files.md, generated-files.json, git-status.md.
11. Return AgentResult.Success if reports were written.

Timeouts:

```text
git init            -> 30 seconds
git status --short  -> 30 seconds
```

If git is not installed or git command fails:

- do not fail the whole run;
- write reports indicating git command failure;
- return AgentResult.Success if reports were written.

Reason: Git snapshot is finalization/reporting, not a critical generation step.

---

## 10. CodeExecutionPromptBuilder

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
- Truncate each section to 6000 characters.
- The prompt is written to `.aire/code-execution-prompt.md` under the generated workspace.
- The prompt file path, not the prompt text, is passed to external executors.
- Prompt must explicitly say:
  - work only inside current workspace;
  - do not delete unrelated files;
  - keep generated project buildable;
  - run no network commands unless required by the tool itself;
  - implement only small safe improvements;
  - do not change AIRE repository root.

---

## 11. CodeExecutionReportWriter

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

## 12. ExternalCodeAgent

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
4. Write prompt to `.aire/code-execution-prompt.md` under `runContext.Paths.WorkspaceDirectory`.
5. Verify the prompt file path is inside `runContext.Paths.WorkspaceDirectory`.
6. Create CodeExecutionRequest:
   - workspaceDirectory = runContext.Paths.WorkspaceDirectory;
   - promptFilePath = generated workspace prompt file path;
   - timeout = 10 minutes;
7. Run ICodeExecutor.
8. Catch non-cancellation executor exceptions and convert to failed result/report.
9. Write reports/code-execution.md.
10. If executor failed, return AgentResult.Failure.
11. If executor succeeded, return AgentResult.Success.

Rules:

- Do not run build/test here.
- BuildTestAgent validates after Code step.
- Do not allow executor to run outside workspace.
- Do not pass secrets in prompt.
- Do not swallow OperationCanceledException.

---

# Required Infrastructure Components

## 13. FakeCodeExecutor

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

## 14. OpenCodeExecutor

Create:

```text
src/AiReliabilityEngineering.Infrastructure/CodeExecution/OpenCodeExecutor.cs
```

Behavior:

- Name = `opencode`;
- uses IToolExecutor supplied by composition;
- maps CodeExecutionRequest to ToolExecutionRequest using provisional mapping;
- catches IToolExecutor exceptions and returns CodeExecutionResult failure;
- never changes working directory outside request.WorkspaceDirectory.

Required method:

```csharp
internal ToolExecutionRequest CreateToolRequest(CodeExecutionRequest request)
```

or `protected virtual` if easier to test.

Provisional mapping:

```text
command: opencode
arguments: run <prompt-file-path>
workingDirectory: request.WorkspaceDirectory
timeout: request.Timeout
```

Automated tests assert this mapping but do not run real OpenCode.

---

## 15. CodexExecutor

Create:

```text
src/AiReliabilityEngineering.Infrastructure/CodeExecution/CodexExecutor.cs
```

Behavior:

- Name = `codex`;
- uses IToolExecutor supplied by composition;
- maps CodeExecutionRequest to ToolExecutionRequest using provisional mapping;
- catches IToolExecutor exceptions and returns CodeExecutionResult failure;
- never changes working directory outside request.WorkspaceDirectory.

Required method:

```csharp
internal ToolExecutionRequest CreateToolRequest(CodeExecutionRequest request)
```

or `protected virtual` if easier to test.

Provisional mapping:

```text
command: codex
arguments: exec <prompt-file-path>
workingDirectory: request.WorkspaceDirectory
timeout: request.Timeout
```

Automated tests assert this mapping but do not run real Codex.

---

## 16. CodeExecutorFactory

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

# Pipeline and CLI Composition

## 17. Internal Code Executor Selection

Do not add public `--code-executor` option yet.

Map profiles internally:

```text
ai-demo-dotnet-opencode -> CodeExecutorKind.OpenCode
ai-demo-dotnet-codex    -> CodeExecutorKind.Codex
```

The profile expresses executor choice.

---

## 18. Update AgentPipelineFactory

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
Code          -> ExternalCodeAgent(injected OpenCode ICodeExecutor)
Testing       -> BuildTestAgent
Review        -> ArtifactReviewAgent
Finalize      -> GitWorkspaceSnapshotAgent
```

Profile `AiDemoDotnetCodex`:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> ExternalCodeAgent(injected Codex ICodeExecutor)
Testing       -> BuildTestAgent
Review        -> ArtifactReviewAgent
Finalize      -> GitWorkspaceSnapshotAgent
```

AgentPipeline must support Finalize step.

---

## 19. Update CLI Help

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

## 20. Core tests

Add tests for:

- GeneratedFileEntry validation;
- GeneratedFilesReport sorting/count/size;
- GitStatusEntry validation;
- GitWorkspaceSnapshot sorting;
- CodeExecutionRequest validation;
- CodeExecutionResult validation;
- CodeExecutorSelection;
- WorkflowStep includes Finalize;
- RunStatus includes FinalizationCompleted;
- WorkflowProfileParser new profiles.

---

## 21. Orchestration tests

Add tests for:

### GeneratedFilesReporter

- excludes bin/obj/.git/.vs/.idea;
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
- JSON is valid;
- transient status entries are filtered.

### GitWorkspaceSnapshotAgent

- runs git init when .git missing;
- runs git status --short;
- writes reports;
- succeeds even if git command fails but reports are written;
- catches IToolExecutor exceptions and writes report;
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
- catches ICodeExecutor non-cancellation exceptions and writes report;
- does not run build/test itself.

Use fake ICodeExecutor.

---

## 22. Infrastructure tests

Add tests for:

### FakeCodeExecutor

- returns success;
- does not require external commands.

### OpenCodeExecutor

- creates expected ToolExecutionRequest using provisional command mapping;
- uses workspace directory;
- passes timeout;
- maps tool result success/failure;
- catches tool executor exception and returns failed CodeExecutionResult;
- does not call real OpenCode.

### CodexExecutor

- creates expected ToolExecutionRequest using provisional command mapping;
- uses workspace directory;
- passes timeout;
- maps tool result success/failure;
- catches tool executor exception and returns failed CodeExecutionResult;
- does not call real Codex.

Use fake/recording IToolExecutor.

---

## 23. Profile/orchestrator tests

Add explicit pipeline test seam:

- ai-demo-dotnet-opencode and ai-demo-dotnet-codex profiles must be tested with injected fake ICodeExecutor instances.
- Infrastructure tests separately assert OpenCodeExecutor/CodexExecutor command mapping.
- No test should require OpenCode/Codex installed.

Add tests:

### ai-demo-dotnet-review-git

- completes with fake provider and fake tool executor;
- includes Finalize step;
- writes generated-files.md/json and git-status.md.

### ai-demo-dotnet-opencode

- builds pipeline with ExternalCodeAgent;
- pipeline receives a fake ICodeExecutor through injected factory;
- run can complete in tests without real OpenCode.

### ai-demo-dotnet-codex

- builds pipeline with ExternalCodeAgent;
- pipeline receives a fake ICodeExecutor through injected factory;
- run can complete in tests without real Codex.

---

# Documentation

## 24. Add docs/git-workspace-snapshot.md

Explain:

- local workspace git init/status;
- generated files report;
- transient file filtering;
- no commits/pushes;
- reports generated.

## 25. Add docs/generated-files-report.md

Explain:

- what generated-files.md/json contain;
- excluded directories;
- deterministic ordering.

## 26. Add docs/code-executor-abstraction.md

Explain:

- ICodeExecutor;
- CodeExecutionRequest/Result;
- FakeCodeExecutor;
- future executors;
- why orchestration depends only on abstractions.

## 27. Add docs/opencode-codex-integration.md

Explain:

- OpenCode/Codex profiles are opt-in;
- tools must be installed/configured by developer;
- AIRE runs them only inside workspace;
- command mapping is provisional and isolated;
- build/test validates after execution;
- no secrets in prompts.

## 28. Add docs/demo-external-code-executors.md

Include commands:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review-git --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-opencode --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-codex --provider fake
```

Explain OpenCode/Codex commands require tools installed and may require command mapping adjustment depending on installed version.

## 29. Update README.md

Add short section:

```markdown
## Git Snapshot and External Code Executors

AIRE can generate Git/workspace reports and includes opt-in profiles for OpenCode and Codex executors.

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

## Task 1: Add Finalize workflow step, FinalizationCompleted status, and new profiles

Update WorkflowStep, RunStatus, AgentPipeline status mapping, WorkflowProfile, parser, and tests.

## Task 2: Add Git/generated-files core models

Add models and tests.

## Task 3: Add transient path filter, GeneratedFilesReporter, and GitStatusParser

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

## Finalize Step

- WorkflowStep includes Finalize.
- RunStatus includes FinalizationCompleted.
- AgentPipeline maps Finalize started/completed explicitly.
- Final run status after all steps remains Completed.
- Existing profiles still complete.

## Git Snapshot

- Generated files report is written.
- Git status report is written.
- Transient paths are filtered from generated files and git status reports.
- Git command failure does not fail finalization if reports are written.
- IToolExecutor exceptions are caught and converted into reports.
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
- ExternalCodeAgent catches non-cancellation executor exceptions and writes report.
- BuildTestAgent remains responsible for validation.

## OpenCode/Codex

- OpenCodeExecutor exists.
- CodexExecutor exists.
- Both map requests to provisional ToolExecutionRequest commands through isolated methods.
- Both run only inside workspace.
- Both use IToolExecutor/ShellToolExecutor through infrastructure composition.
- Both catch IToolExecutor exceptions and return failed CodeExecutionResult.
- No automated tests require actual OpenCode/Codex.
- Runtime failures are captured in reports.

## Architecture Boundary

- AgentPipelineFactory accepts code executor factory/delegate.
- AgentPipelineFactory does not reference Infrastructure types.
- Orchestration project does not reference Infrastructure project.
- CLI composition root wires Infrastructure executors.

## Profiles

- ai-demo-dotnet-review-git exists.
- ai-demo-dotnet-opencode exists.
- ai-demo-dotnet-codex exists.
- Default profile remains fake.
- Existing profiles remain unchanged.
- OpenCode/Codex profile tests use injected fake ICodeExecutor instances.
- Infrastructure tests separately assert executor command mapping.

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
