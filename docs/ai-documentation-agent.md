# AI Documentation Agent

`AiDocumentationAgent` generates project documentation artifacts from `artifacts/specification.json`.

It calls `IAiProvider` for integration with the selected provider and writes:

- `artifacts/README.md`
- `artifacts/PLAN.md`

With the fake provider, the agent still calls the provider but writes deterministic local documentation from the specification.

With real providers, the response must include `---README---` and `---PLAN---` markers. If the markers are missing, the run fails through the normal failed-run path.

This agent does not generate or modify source code.
