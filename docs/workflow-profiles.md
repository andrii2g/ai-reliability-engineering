# Workflow Profiles

A workflow profile selects which agents are used in an AIRE run.

## fake

The default profile.

It uses fake agents for all workflow steps.

Command:

```bash
aire run samples/idea.md
```

or:

```bash
aire run samples/idea.md --profile fake
```

## ai-requirements

This profile uses `AiRequirementsAgent` for the requirements step and fake agents for all later steps.

Command:

```bash
aire run samples/idea.md --profile ai-requirements
```

This profile still uses `FakeAiProvider`. It does not require API keys and does not call the network.

Run `ai-requirements` with the fake provider explicitly:

```bash
aire run samples/redis-ttl-audit.md --profile ai-requirements --provider fake
```

Run `ai-requirements` with the OpenAI provider:

```bash
aire run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

The OpenAI provider requires `OPENAI_API_KEY` to be set in the environment.

## ai-demo

This profile uses AI-aware requirements, documentation, and planning agents, then fake code/test/review agents.

Run with the fake provider:

```bash
aire run samples/redis-ttl-audit.md --profile ai-demo --provider fake
```

Run with the OpenAI provider:

```bash
aire run samples/redis-ttl-audit.md --profile ai-demo --provider openai --model <model-name>
```

The OpenAI run may fail normally if provider output does not match the required documentation markers or planner JSON shape.

## ai-demo-dotnet

This profile runs AI-aware requirements, documentation, and planning agents, then generates a deterministic .NET CLI workspace project and validates it with build/test commands.

Run with the fake provider:

```bash
aire run samples/redis-ttl-audit.md --profile ai-demo-dotnet --provider fake
```

The generated source code is template-based and not AI-generated yet.

## ai-demo-dotnet-review

This profile runs AI-aware requirements, documentation, and planning agents, generates a deterministic .NET CLI workspace, validates it with build/test commands, and writes deterministic final review reports.

Run with the fake provider:

```bash
aire run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```

The review is deterministic and does not call AI.
