# Demo: AI Artifacts

This demo generates requirements, documentation, and planning artifacts from an idea file.

## Fake Provider

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider fake
```

## OpenAI Provider

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo --provider openai --model <model-name>
```

The OpenAI command calls the real provider. It may fail normally if the model response does not include the required documentation markers or valid planner JSON.

## Expected Artifacts

```text
artifacts/specification.json
artifacts/requirements.md
artifacts/README.md
artifacts/PLAN.md
artifacts/tasks.json
```

Source code generation is not implemented yet.
