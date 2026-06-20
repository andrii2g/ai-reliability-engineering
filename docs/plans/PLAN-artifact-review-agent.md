# PLAN.md - AIRE: ArtifactReviewAgent and Final Run Summary

## Purpose

This plan is for Codex to implement the next AIRE milestone: a deterministic final review layer for generated runs.

AIRE means AI Reliability Engineering.

Repository conventions:

- Repository name: ai-reliability-engineering
- Solution name: AiReliabilityEngineering.slnx
- CLI project: AiReliabilityEngineering.Cli
- CLI command name: aire
- PRD location: docs/PRD.md

Previous steps should already be implemented:

- stable fake workflow;
- cleanup command;
- workflow profiles;
- `fake` profile;
- `ai-requirements` profile;
- `ai-demo` profile;
- `ai-demo-dotnet` profile;
- AI provider contracts;
- FakeAiProvider;
- OpenAiProvider;
- AiRequirementsAgent;
- AiDocumentationAgent;
- AiPlannerAgent;
- TemplateCodeAgent;
- BuildTestAgent;
- generated .NET workspace demo;
- build/test reports;
- samples/redis-ttl-audit.md.

The goal of this step is to replace the final fake review in the generated .NET demo workflow with a deterministic review agent that summarizes what AIRE produced and whether the run output looks complete.

This step must not introduce real AI review yet.

---

## High-Level Goal

Add a new workflow profile:

```text
ai-demo-dotnet-review
```

Expected pipeline:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> TemplateCodeAgent
Testing       -> BuildTestAgent
Review        -> ArtifactReviewAgent
```

Expected command:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```

Expected additional review outputs:

```text
reports/final-review.md
reports/workspace-summary.md
```

The profile should complete successfully with `--provider fake`.

With `--provider openai --model <model-name>`, this profile is valid manual usage and earlier AI-aware agents may call OpenAI. The run may still fail through the normal failed-run path if OpenAI output does not match the required documentation markers or planner JSON shape. ArtifactReviewAgent must not compensate for malformed upstream AI output.

---

## Why This Step Exists

AIRE can now produce:

```text
requirements -> docs -> plan -> template code -> build/test reports
```

The remaining weak point is:

```text
Review -> FakeReviewerAgent
```

Before adding Codex/OpenCode or other external code agents, AIRE needs a stable final review layer that answers:

- What was generated?
- Are all expected artifacts present?
- Did the workspace project exist?
- Did build/test reports exist?
- What should the developer inspect next?
- Are there warnings or missing files?

This review layer becomes the foundation for future agentic coding workflows.

---

## Non-Goals

Do not implement these in this step:

- AI-powered ReviewerAgent;
- OpenAI review;
- Codex executor;
- OpenCode executor;
- Git integration;
- Git diff reports;
- PR creation;
- Docker;
- Kubernetes;
- SQLite;
- dashboard;
- retry/fix loop;
- deep compiler output parsing;
- code quality scoring;
- security scanning;
- dependency vulnerability scanning;
- real source code analysis;
- LLM-based review comments.

This step is only about deterministic review of generated artifacts and workspace files.

---

## Design Decision: Deterministic Review First

ArtifactReviewAgent must not call AI.

It should deterministically inspect the run directory and produce Markdown reports.

Future direction:

```text
ArtifactReviewAgent -> deterministic baseline
AiReviewAgent       -> future AI-assisted review
GitReviewAgent      -> future diff/status-aware review
```

The deterministic review should be safe, fast, and testable.

---

## Design Decision: Add a New Profile Instead of Changing Existing Profiles

Do not change existing profiles.

Existing profiles must remain:

```text
fake
ai-requirements
ai-demo
ai-demo-dotnet
```

Add new profile:

```text
ai-demo-dotnet-review
```

This allows comparing:

```text
ai-demo-dotnet        -> fake review
ai-demo-dotnet-review -> deterministic artifact review
```

Later, `ai-demo-dotnet-review` may become the preferred demo profile, but do not remove the previous profile in this step.

---

## Expected Reports

ArtifactReviewAgent must write:

```text
reports/final-review.md
reports/workspace-summary.md
```

### final-review.md

Required sections:

```markdown
# Final Review

## Run Output

## Required Artifacts

## Requirements

## Documentation

## Planning

## Workspace

## Build and Test Reports

## Warnings

## Suggested Next Steps
```

### workspace-summary.md

Required sections:

```markdown
# Workspace Summary

## Workspace Root

## Generated Files

## Project Files

## Test Files

## Reports
```

The workspace summary should include a deterministic file list or tree for generated workspace files.

---

## Required File Checks

The review agent should check for these files.

### Required artifact files

```text
artifacts/specification.json
artifacts/requirements.md
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json
```

### Required workspace files

```text
workspace/Directory.Packages.props
workspace/GeneratedTool.slnx
workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
workspace/src/GeneratedTool.Cli/Program.cs
workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs
```

### Required report files

```text
reports/build.md
reports/tests.md
```

The agent should not fail the run just because a required artifact is missing. Instead, it should:

- record missing files as warnings;
- write the review reports;
- return success if review reports were written successfully.

Reason:

The review agent is diagnostic. Earlier pipeline steps should already fail when critical generation/build/test steps fail.

However, if the review agent cannot write reports, it should return AgentResult.Failure.

---

## Target Folder Structure

Add or update this structure:

```text
src/
|-- AiReliabilityEngineering.Core/
|   `-- Review/
|       |-- RequiredArtifactCheck.cs
|       |-- ArtifactReviewResult.cs
|       `-- WorkspaceSummary.cs
|
|-- AiReliabilityEngineering.Orchestration/
|   `-- Agents/
|       |-- ArtifactReviewAgent.cs
|       |-- RequiredArtifactChecker.cs
|       |-- WorkspaceSummaryBuilder.cs
|       `-- ReviewReportWriter.cs
|
|-- tests/
|   |-- AiReliabilityEngineering.Core.Tests/
|   |   `-- Review/
|   |       |-- RequiredArtifactCheckTests.cs
|   |       |-- ArtifactReviewResultTests.cs
|   |       `-- WorkspaceSummaryTests.cs
|   |
|   `-- AiReliabilityEngineering.Orchestration.Tests/
|       `-- Agents/
|           |-- ArtifactReviewAgentTests.cs
|           |-- RequiredArtifactCheckerTests.cs
|           |-- WorkspaceSummaryBuilderTests.cs
|           `-- ReviewReportWriterTests.cs
|
`-- docs/
    |-- artifact-review-agent.md
    `-- demo-final-review.md
```

If the repository already uses a different folder layout, follow the existing style, but keep the same logical coverage.

---

# Required Core Models

## 1. RequiredArtifactCheck

Create:

```text
src/AiReliabilityEngineering.Core/Review/RequiredArtifactCheck.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Review;
```

Purpose:

- represent whether one expected file exists.

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Review;

public sealed record RequiredArtifactCheck
{
    public RequiredArtifactCheck(
        string relativePath,
        bool exists,
        string category)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path is required.", nameof(relativePath));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        RelativePath = relativePath.Replace('\\', '/');
        Exists = exists;
        Category = category;
    }

    public string RelativePath { get; }

    public bool Exists { get; }

    public string Category { get; }
}
```

Categories should be stable strings:

```text
artifact
workspace
report
```

---

## 2. WorkspaceSummary

Create:

```text
src/AiReliabilityEngineering.Core/Review/WorkspaceSummary.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Review;
```

Purpose:

- hold a deterministic list of generated workspace files.

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Review;

public sealed record WorkspaceSummary
{
    public WorkspaceSummary(
        string workspaceRoot,
        IReadOnlyList<string> files)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
        }

        if (files is null)
        {
            throw new ArgumentNullException(nameof(files));
        }

        if (files.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Workspace files must not contain empty entries.", nameof(files));
        }

        WorkspaceRoot = workspaceRoot;
        Files = files
            .Select(path => path.Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    public string WorkspaceRoot { get; }

    public IReadOnlyList<string> Files { get; }
}
```

Rules:

- Store paths relative to workspace root when possible.
- Normalize path separators to `/`.
- Sort files deterministically.

---

## 3. ArtifactReviewResult

Create:

```text
src/AiReliabilityEngineering.Core/Review/ArtifactReviewResult.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Review;
```

Purpose:

- aggregate review inputs and warnings.

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Review;

public sealed record ArtifactReviewResult
{
    public ArtifactReviewResult(
        IReadOnlyList<RequiredArtifactCheck> checks,
        WorkspaceSummary workspaceSummary,
        IReadOnlyList<string> warnings)
    {
        if (checks is null)
        {
            throw new ArgumentNullException(nameof(checks));
        }

        if (checks.Any(check => check is null))
        {
            throw new ArgumentException("Checks must not contain null entries.", nameof(checks));
        }

        WorkspaceSummary = workspaceSummary ?? throw new ArgumentNullException(nameof(workspaceSummary));
        Warnings = (warnings ?? Array.Empty<string>())
            .Where(warning => !string.IsNullOrWhiteSpace(warning))
            .ToArray();

        Checks = checks.ToArray();
    }

    public IReadOnlyList<RequiredArtifactCheck> Checks { get; }

    public WorkspaceSummary WorkspaceSummary { get; }

    public IReadOnlyList<string> Warnings { get; }

    public IReadOnlyList<RequiredArtifactCheck> Missing =>
        Checks.Where(check => !check.Exists).ToArray();

    public bool HasMissingRequiredFiles => Missing.Count > 0;
}
```

---

# Required Orchestration Components

## 4. RequiredArtifactChecker

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/RequiredArtifactChecker.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- check expected files in a run directory.

Suggested signature:

```csharp
public sealed class RequiredArtifactChecker
{
    public IReadOnlyList<RequiredArtifactCheck> Check(RunContext runContext)
    {
        // implementation
    }
}
```

Rules:

- Use actual `RunContext` and `RunPaths`.
- Do not scan arbitrary directories.
- Use fixed required file list from this plan.
- Return checks with normalized relative paths.
- Do not throw if files are missing.
- Throw ArgumentNullException if runContext is null.

Required file list:

```text
artifacts/specification.json
artifacts/requirements.md
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json
workspace/Directory.Packages.props
workspace/GeneratedTool.slnx
workspace/src/GeneratedTool.Cli/GeneratedTool.Cli.csproj
workspace/src/GeneratedTool.Cli/Program.cs
workspace/tests/GeneratedTool.Cli.Tests/GeneratedTool.Cli.Tests.csproj
workspace/tests/GeneratedTool.Cli.Tests/SmokeTests.cs
reports/build.md
reports/tests.md
```

Categories:

```text
artifact -> paths under artifacts/
workspace -> paths under workspace/
report -> paths under reports/
```

---

## 5. WorkspaceSummaryBuilder

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/WorkspaceSummaryBuilder.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- produce a deterministic workspace file summary.

Suggested signature:

```csharp
public sealed class WorkspaceSummaryBuilder
{
    public WorkspaceSummary Build(RunContext runContext)
    {
        // implementation
    }
}
```

Rules:

- Use `runContext.Paths.WorkspaceDirectory`.
- If workspace directory does not exist, return summary with an empty file list.
- Enumerate files recursively.
- Return paths relative to workspace directory.
- Normalize separators to `/`.
- Sort deterministically.
- Exclude transient build outputs if they exist:
  - `bin/`
  - `obj/`
  - `.vs/`
  - `.idea/`
- Do not scan outside workspace.
- Throw ArgumentNullException if runContext is null.

---

## 6. ReviewReportWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/ReviewReportWriter.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

Purpose:

- write:
  - `reports/final-review.md`
  - `reports/workspace-summary.md`

Suggested signature:

```csharp
public sealed class ReviewReportWriter
{
    public Task<IReadOnlyList<ArtifactRef>> WriteAsync(
        ArtifactReviewResult result,
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        // write reports
    }
}
```

Rules:

- Use `runContext.Paths.ReportsDirectory`.
- Ensure reports directory exists.
- Write deterministic Markdown.
- Do not include timestamps.
- Do not include random IDs.
- Do not include absolute paths except the workspace root in the `## Workspace Root` section of `workspace-summary.md`.
- Return artifact refs for both files.
- Do not overwrite build.md or tests.md.

### final-review.md content requirements

Must include:

```markdown
# Final Review

## Run Output

## Required Artifacts

## Requirements

## Documentation

## Planning

## Workspace

## Build and Test Reports

## Warnings

## Suggested Next Steps
```

Required Artifacts section should contain a Markdown table:

```markdown
| Category | Path | Status |
|---|---|---|
| artifact | artifacts/specification.json | OK |
| report | reports/build.md | Missing |
```

Warnings section:

- if no warnings, write `No warnings.`;
- if missing files exist, list them.

Suggested next steps should mention:

- review generated artifacts;
- inspect build/test reports;
- inspect generated workspace;
- next future step may be Git snapshot or Codex/OpenCode integration.

### workspace-summary.md content requirements

Must include:

```markdown
# Workspace Summary

## Workspace Root

## Generated Files

## Project Files

## Test Files

## Reports
```

The `## Workspace Root` section may include the absolute value of `runContext.Paths.WorkspaceDirectory`.

Generated Files section should list all workspace files from `WorkspaceSummary.Files`.

All generated file entries must be relative to `runContext.Paths.WorkspaceDirectory`, use `/` separators, and be sorted with ordinal ordering.

Project Files should list files under:

```text
src/
```

Test Files should list files under:

```text
tests/
```

Reports should mention:

```text
reports/build.md
reports/tests.md
reports/final-review.md
reports/workspace-summary.md
```

---

## 7. ArtifactReviewAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/ArtifactReviewAgent.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Orchestration.Agents;
```

The agent should implement existing `IAgent`.

Suggested dependencies:

```csharp
public sealed class ArtifactReviewAgent : IAgent
{
    private readonly IRunLogger _logger;
    private readonly RequiredArtifactChecker _artifactChecker;
    private readonly WorkspaceSummaryBuilder _workspaceSummaryBuilder;
    private readonly ReviewReportWriter _reportWriter;

    public ArtifactReviewAgent(
        IRunLogger logger,
        RequiredArtifactChecker? artifactChecker = null,
        WorkspaceSummaryBuilder? workspaceSummaryBuilder = null,
        ReviewReportWriter? reportWriter = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artifactChecker = artifactChecker ?? new RequiredArtifactChecker();
        _workspaceSummaryBuilder = workspaceSummaryBuilder ?? new WorkspaceSummaryBuilder();
        _reportWriter = reportWriter ?? new ReviewReportWriter();
    }

    public string Name => "ArtifactReviewAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // implementation
    }
}
```

### Required execution flow

1. Check cancellation at the start.
2. Validate context is not null.
3. Log start event.
4. Check required artifacts.
5. Build workspace summary.
6. Build warnings:
   - one warning per missing required file;
   - optional warning if workspace is empty.
7. Write final review reports.
8. Log completion.
9. Return AgentResult.Success with report artifact refs.

Failure behavior:

- If report writing fails, return AgentResult.Failure.
- Do not fail only because required files are missing.
- Do not swallow OperationCanceledException.

---

# Workflow Profile Changes

## 8. Extend WorkflowProfile

Add:

```text
ai-demo-dotnet-review
```

Enum value:

```csharp
AiDemoDotnetReview
```

Current enum likely has:

```csharp
Fake,
AiRequirements,
AiDemo,
AiDemoDotnet
```

Update to:

```csharp
Fake,
AiRequirements,
AiDemo,
AiDemoDotnet,
AiDemoDotnetReview
```

---

## 9. Update WorkflowProfileParser

Support CLI name:

```text
ai-demo-dotnet-review
```

Required supported names after this step:

```text
fake
ai-requirements
ai-demo
ai-demo-dotnet
ai-demo-dotnet-review
```

Rules:

- Missing profile maps to Fake.
- Unknown profile returns false.
- ToCliName maps AiDemoDotnetReview to `ai-demo-dotnet-review`.
- SupportedCliNames includes `ai-demo-dotnet-review`.

---

## 10. Update AgentPipelineFactory

For `WorkflowProfile.AiDemoDotnetReview`, create pipeline:

```text
Requirements  -> AiRequirementsAgent
Documentation -> AiDocumentationAgent
Planning      -> AiPlannerAgent
Code          -> TemplateCodeAgent
Testing       -> BuildTestAgent
Review        -> ArtifactReviewAgent
```

Use selected provider for AI agents.

Use `IToolExecutor` for BuildTestAgent.

Rules:

- Fake profile remains unchanged.
- AiRequirements profile remains unchanged.
- AiDemo profile remains unchanged.
- AiDemoDotnet profile remains unchanged.
- AiDemoDotnetReview replaces only the review step with ArtifactReviewAgent.
- Orchestration project must remain infrastructure-agnostic.

---

# Tests

## 11. Core review model tests

Add tests under:

```text
tests/AiReliabilityEngineering.Core.Tests/Review/
```

Tests for RequiredArtifactCheck:

- accepts valid values;
- rejects blank relative path;
- rejects blank category;
- normalizes path separators.

Tests for WorkspaceSummary:

- accepts valid values;
- rejects blank workspace root;
- rejects null file list;
- rejects blank file entries;
- normalizes path separators;
- sorts files deterministically.

Tests for ArtifactReviewResult:

- accepts valid checks and workspace summary;
- rejects null checks;
- rejects null workspace summary;
- Missing contains only missing files;
- HasMissingRequiredFiles is true when any check is missing;
- warnings ignore null/blank values.

---

## 12. RequiredArtifactChecker tests

Use real RunContext and RunPaths with temp directories.

Required tests:

- returns all expected checks;
- marks existing files as present;
- marks missing files as missing;
- categories are artifact/workspace/report;
- relative paths are normalized;
- does not throw when files are missing.

---

## 13. WorkspaceSummaryBuilder tests

Use real RunContext and RunPaths with temp directories.

Required tests:

- returns empty list when workspace directory is missing;
- lists files recursively;
- returns paths relative to workspace;
- normalizes separators;
- sorts files deterministically;
- excludes bin and obj directories.

---

## 14. ReviewReportWriter tests

Required tests:

- writes reports/final-review.md;
- writes reports/workspace-summary.md;
- final-review.md contains required sections;
- final-review.md contains required artifact table;
- final-review.md lists missing files as warnings;
- workspace-summary.md contains required sections;
- returned artifact refs include both files;
- does not overwrite build.md or tests.md.

---

## 15. ArtifactReviewAgent tests

Use real RunContext and RunPaths.

Required tests:

- succeeds when required artifacts exist;
- writes final-review.md;
- writes workspace-summary.md;
- returns artifact refs;
- succeeds even when some required files are missing;
- missing files appear in final-review.md warnings;
- cancellation propagates OperationCanceledException;
- report writing failure returns AgentResult.Failure if testable.

---

## 16. Workflow profile tests

Update WorkflowProfileParser tests:

- parses `ai-demo-dotnet-review`;
- ToCliName maps AiDemoDotnetReview to `ai-demo-dotnet-review`;
- SupportedCliNames includes `ai-demo-dotnet-review`.

Update AgentPipelineFactory tests:

- AiDemoDotnetReview pipeline contains:
  - AiRequirementsAgent
  - AiDocumentationAgent
  - AiPlannerAgent
  - TemplateCodeAgent
  - BuildTestAgent
  - ArtifactReviewAgent
- step order remains:
  - Requirements
  - Documentation
  - Planning
  - Code
  - Testing
  - Review
- AiDemoDotnet profile still uses FakeReviewerAgent.

---

## 17. Orchestrator ai-demo-dotnet-review tests

Add test:

```text
ai-demo-dotnet-review profile completes with fake provider and fake tool executor
```

Arrange:

- RunRequest with WorkflowProfile.AiDemoDotnetReview and Fake provider.
- Use test pipeline/composition that injects fake tool executor returning successful build/test results.
- Run orchestrator.

Assert:

- run succeeds;
- final status is Completed;
- artifacts/specification.json exists;
- artifacts/requirements.md exists;
- artifacts/README.md exists;
- artifacts/PLAN.md exists;
- artifacts/tasks.json exists;
- workspace/Directory.Packages.props exists;
- workspace/GeneratedTool.slnx exists;
- workspace generated project files exist;
- reports/build.md exists;
- reports/tests.md exists;
- reports/final-review.md exists;
- reports/workspace-summary.md exists;
- run-state step agents include:
  - AiRequirementsAgent
  - AiDocumentationAgent
  - AiPlannerAgent
  - TemplateCodeAgent
  - BuildTestAgent
  - ArtifactReviewAgent

Do not make this orchestration unit test depend on real dotnet process execution.

---

## 18. CLI tests

Update CLI tests:

- help mentions `ai-demo-dotnet-review`;
- parser accepts `ai-demo-dotnet-review`;
- profile dispatch passes `WorkflowProfile.AiDemoDotnetReview` to the run delegate;
- CLI unit tests must not depend on real `dotnet build` or `dotnet test` execution;
- full generated artifact assertions must remain in orchestration tests with a fake `IToolExecutor`;
- manual verification may run the real CLI command with `--profile ai-demo-dotnet-review --provider fake`.

Do not make automated tests flaky by depending on external environment unexpectedly.

---

# Documentation

## 19. Add docs/artifact-review-agent.md

Create:

```text
docs/artifact-review-agent.md
```

Explain:

- ArtifactReviewAgent performs deterministic review;
- it does not call AI;
- it checks required artifacts;
- it builds workspace summary;
- it writes final-review.md and workspace-summary.md;
- missing files are warnings, not automatic review failures.

---

## 20. Add docs/demo-final-review.md

Create:

```text
docs/demo-final-review.md
```

Include command:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```

Expected review files:

```text
reports/final-review.md
reports/workspace-summary.md
```

Explain that this profile is the most complete local fake-provider demo.

---

## 21. Update docs/workflow-profiles.md

Add:

```markdown
## ai-demo-dotnet-review

Runs AI-aware requirements, documentation, and planning agents, generates a deterministic .NET CLI workspace, validates it with build/test commands, and writes deterministic final review reports.

```bash
aire run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```
```

---

## 22. Update docs/wiki.md

If exists, add:

```markdown
## ArtifactReviewAgent

Performs deterministic review of generated artifacts and workspace files.

## ai-demo-dotnet-review Profile

Runs the current most complete local demo pipeline, ending with ArtifactReviewAgent.
```

---

## 23. Update README.md

Add a short section:

```markdown
## Final Review Demo

Run the most complete local demo:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```

The run produces:

- requirements artifacts
- documentation artifacts
- planning artifacts
- generated .NET workspace
- build/test reports
- final review reports

Review:

- reports/final-review.md
- reports/workspace-summary.md
```

---

# Existing Workflow Must Remain Stable

These commands must still pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```

OpenAI manual demo is valid usage, but it may fail through the normal failed-run path if upstream OpenAI output does not match the required documentation markers or planner JSON shape. ArtifactReviewAgent must not compensate for those upstream AI contract failures.

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider openai --model <model-name>
```

Cleanup command should still work when used against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Suggested Implementation Order for Codex

Follow this order.

## Task 1: Add Core review models

Create:

```text
src/AiReliabilityEngineering.Core/Review/RequiredArtifactCheck.cs
src/AiReliabilityEngineering.Core/Review/WorkspaceSummary.cs
src/AiReliabilityEngineering.Core/Review/ArtifactReviewResult.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 2: Add RequiredArtifactChecker

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/RequiredArtifactChecker.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 3: Add WorkspaceSummaryBuilder

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/WorkspaceSummaryBuilder.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 4: Add ReviewReportWriter

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/ReviewReportWriter.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 5: Add ArtifactReviewAgent

Create:

```text
src/AiReliabilityEngineering.Orchestration/Agents/ArtifactReviewAgent.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 6: Add ai-demo-dotnet-review workflow profile

Update:

```text
WorkflowProfile
WorkflowProfileParser
AgentPipelineFactory
CLI help/tests if needed
```

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 7: Add orchestration/CLI coverage and docs

Create/update docs and README.

Run final verification.

---

# Acceptance Criteria

The task is complete when all criteria are satisfied.

## Core

- RequiredArtifactCheck exists and validates inputs.
- WorkspaceSummary exists and validates inputs.
- ArtifactReviewResult exists and validates inputs.
- Core review tests pass.

## Review Agent

- RequiredArtifactChecker exists.
- WorkspaceSummaryBuilder exists.
- ReviewReportWriter exists.
- ArtifactReviewAgent exists.
- ArtifactReviewAgent implements IAgent.
- ArtifactReviewAgent does not call AI.
- ArtifactReviewAgent writes reports/final-review.md.
- ArtifactReviewAgent writes reports/workspace-summary.md.
- ArtifactReviewAgent returns success even when expected files are missing, as long as reports are written.
- Missing files appear as warnings.
- Tests pass.

## Workflow

- WorkflowProfile includes AiDemoDotnetReview.
- CLI profile name is `ai-demo-dotnet-review`.
- ai-demo-dotnet-review profile uses:
  - AiRequirementsAgent
  - AiDocumentationAgent
  - AiPlannerAgent
  - TemplateCodeAgent
  - BuildTestAgent
  - ArtifactReviewAgent
- ai-demo-dotnet profile remains unchanged and still uses FakeReviewerAgent.
- Other existing profiles remain unchanged.

## Demo

- `--profile ai-demo-dotnet-review --provider fake` completes successfully.
- The run contains:
  - reports/final-review.md
  - reports/workspace-summary.md

## Documentation

- docs/artifact-review-agent.md exists.
- docs/demo-final-review.md exists.
- docs/workflow-profiles.md mentions ai-demo-dotnet-review.
- README includes final review demo.
- Docs explain review is deterministic and does not call AI.

## Verification

These commands pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```

Cleanup passes against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Notes for Codex

- Keep this step focused on deterministic final review.
- Do not implement AI review.
- Do not add Codex or OpenCode.
- Do not add Docker.
- Do not add Kubernetes.
- Do not add Git integration.
- Do not parse complex compiler/test output.
- Do not change existing profiles.
- Add only the new ai-demo-dotnet-review profile.
- Use temporary directories in tests.
- Preserve existing stable CLI behavior.
- Save this file as UTF-8 without BOM.
