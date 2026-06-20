# OpenAI Provider

AIRE includes `OpenAiProvider` as the first real AI provider.

It implements `IAiProvider` and calls the OpenAI Responses API with a minimal non-streaming text request.

## API Key

Set the API key through the `OPENAI_API_KEY` environment variable.

Do not pass API keys through CLI arguments.

## Usage

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

## Scope

This provider currently supports non-streaming text requests only.

It does not support tools, streaming, structured outputs, or file inputs yet.

`AiRequirementsAgent` still writes deterministic requirement artifacts using local normalization. The OpenAI call proves provider integration but is not yet used to generate the final `ProjectSpecification`.
