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
