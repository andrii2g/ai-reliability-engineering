# AI Planner Agent

`AiPlannerAgent` generates implementation planning artifacts from `artifacts/specification.json`.

It calls `IAiProvider` and expects planning JSON with a `tasks` array. It writes:

- `artifacts/tasks.json`

With the fake provider, the agent still calls the provider but writes deterministic local tasks from the specification.

With real providers, invalid JSON or invalid task shape fails the run through the normal failed-run path.

This agent does not generate or modify source code.
