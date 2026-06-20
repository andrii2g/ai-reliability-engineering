# AI Requirements Agent

`AiRequirementsAgent` is the first AI-aware workflow agent in AIRE.

It depends on `IAiProvider`, not on a concrete AI vendor SDK.

In the current step, it is tested with `FakeAiProvider` or test providers only. It does not call OpenAI, Ollama, Anthropic, Gemini, Codex, or OpenCode.

The agent reads the copied input idea file, calls `IAiProvider`, normalizes the idea into `ProjectSpecification`, and writes:

- `artifacts/specification.json`
- `artifacts/requirements.md`

The default CLI workflow is not changed in this step.
