# AI Providers

AIRE uses a provider-neutral AI abstraction so workflow agents do not depend directly on a specific AI vendor SDK.

## Current Providers

### FakeAiProvider

The fake provider is deterministic, local-only, and used for tests and early workflow development.

It does not call the network and does not require API keys.

### OpenAiProvider

The OpenAI provider calls the OpenAI Responses API through `HttpClient`.

It is selected explicitly with:

```bash
aire run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

It reads the API key from `OPENAI_API_KEY`. Do not pass API keys through CLI arguments.

## Contract Validation

AI request models reject invalid shapes at construction time. For example, `AiRequest` requires at least one message, `AiMessage` requires non-null content, and `AiProviderOptions` requires a non-empty model name.

## Future Providers

Future providers may include:

- Ollama
- Anthropic
- Gemini

These providers should be implemented behind `IAiProvider`.

## Design Rule

Agents must depend on `IAiProvider`.

Concrete providers belong in `AiReliabilityEngineering.Infrastructure`.
