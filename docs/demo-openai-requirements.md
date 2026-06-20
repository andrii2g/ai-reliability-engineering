# Demo: OpenAI Requirements Agent

This demo runs `AiRequirementsAgent` with `OpenAiProvider`.

## Prerequisites

- `OPENAI_API_KEY` is set.
- A model name is selected.
- Network access is available.

## Command

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

## Expected Output

A run folder is created under `runs/`.

Review:

- `artifacts/specification.json`
- `artifacts/requirements.md`
- `logs/orchestrator.log`
- `run-state.json`

## Notes

The current `AiRequirementsAgent` still writes deterministic requirement artifacts using local normalization. The OpenAI call proves provider integration but is not yet used to generate the final `ProjectSpecification`.

Do not pass API keys through CLI arguments.
