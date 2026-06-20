# Code Executor Abstraction

AIRE uses `ICodeExecutor` for external coding-agent integration.

Core contracts:

- `ICodeExecutor`
- `CodeExecutionRequest`
- `CodeExecutionResult`
- `CodeExecutorSelection`
- `CodeExecutorKind`

Orchestration depends only on these abstractions. Infrastructure provides concrete executors:

- `FakeCodeExecutor`
- `OpenCodeExecutor`
- `CodexExecutor`

`ExternalCodeAgent` creates the deterministic .NET workspace first, writes `.aire/code-execution-prompt.md` inside that workspace, and passes the prompt file path to the selected executor. Build and test validation still happens later through `BuildTestAgent`.
