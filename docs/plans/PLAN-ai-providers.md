# PLAN.md - AIRE Step 10.4: Add OpenAI Provider and Provider Selection

## Purpose

This plan is for Codex to add the first real AI provider integration to AIRE.

AIRE means AI Reliability Engineering.

Repository conventions:

- Repository name: ai-reliability-engineering
- Solution name: AiReliabilityEngineering.slnx
- CLI project: AiReliabilityEngineering.Cli
- CLI command name: aire
- PRD location: docs/PRD.md

The previous steps should already be implemented:

- stable fake workflow;
- cleanup command;
- AI provider contracts;
- FakeAiProvider;
- AiRequirementsAgent;
- workflow profiles;
- `fake` profile;
- `ai-requirements` profile;
- `--profile fake`;
- `--profile ai-requirements`.

The goal of this step is to add the first real AI provider while keeping AIRE safe, testable, deterministic by default, and local-first.

This step adds:

- `OpenAiProvider`;
- provider selection for the `run` command;
- `--provider fake|openai`;
- `--model <model>`;
- `OPENAI_API_KEY` environment-variable lookup;
- mocked HTTP tests;
- a small manual demo sample;
- documentation for the real-provider demo.

The default behavior must remain unchanged and must still use the fake provider unless the user explicitly selects OpenAI.

---

## High-Level Goal

Enable this command:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

Expected behavior:

1. `ai-requirements` profile selects `AiRequirementsAgent` for the requirements step.
2. `--provider openai` selects `OpenAiProvider`.
3. `OpenAiProvider` reads the API key from `OPENAI_API_KEY`.
4. `OpenAiProvider` calls the OpenAI Responses API.
5. `AiRequirementsAgent` still writes deterministic local requirement artifacts.
6. The run completes.
7. No later fake steps break the requirements artifacts.

Important: `AiRequirementsAgent` does not yet parse AI output into the final specification. It only proves that a real provider can be called safely through `IAiProvider`.

---

## Non-Goals

Do not implement these in this step:

- Anthropic provider
- Gemini provider
- Ollama provider
- OpenRouter provider
- OpenAI SDK dependency unless already used in the repo
- streaming
- tool calling
- file search
- web search
- image input
- audio input
- background responses
- OpenAI structured outputs enforcement
- JSON schema response format
- prompt template engine
- config file loading
- storing API keys
- accepting API keys as CLI arguments
- retry policies
- rate-limit backoff
- provider cost tracking
- token budget enforcement
- real AI-generated ProjectSpecification parsing
- AI DocumentationAgent
- AI PlannerAgent
- CodeAgent integration
- Codex executor
- OpenCode executor
- Docker
- Kubernetes

This step is only about adding the first real provider and letting existing `AiRequirementsAgent` call it.

---

## Important Safety Rules

### API key handling

Use only this environment variable:

```text
OPENAI_API_KEY
```

Do not accept API keys as command-line arguments.

Do not write API keys to:

- logs;
- run-state.json;
- artifacts;
- reports;
- test output;
- exception messages.

If the key is missing, return an `AiResponse.Failure(...)` with a safe message:

```text
OPENAI_API_KEY environment variable is not set.
```

Do not include the key value.

### Tests

Automated tests must not call the real OpenAI API.

All OpenAI provider tests must use a mocked/fake `HttpMessageHandler`.

Any real OpenAI call should be manual only and documented as such.

### Defaults

Default provider remains `fake`.

These commands must not require API keys:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider fake
```

Only this requires `OPENAI_API_KEY`:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider openai --model <model-name>
```

---

## Design Decision: Use Direct HttpClient

Implement `OpenAiProvider` using `HttpClient` directly.

Do not add an OpenAI SDK package in this step.

Reason:

- fewer dependencies;
- easier mocked HTTP tests;
- keeps provider mapping explicit;
- avoids SDK version coupling in the first integration step.

Future steps may replace or complement the HTTP implementation with an SDK provider if desired.

---

## Design Decision: Use the Responses API

`OpenAiProvider` should call:

```text
POST https://api.openai.com/v1/responses
```

This step uses a minimal text-generation request.

Do not implement streaming.

Do not implement tools.

Do not implement structured outputs.

Do not implement JSON schema response format yet.

For `AiOutputFormat.Json`, this provider may add a plain text instruction asking the model to return valid JSON, but it must not claim schema enforcement yet.

---

## Design Decision: Provider Is Real, Agent Output Remains Deterministic

`OpenAiProvider` is real.

`AiRequirementsAgent` output remains deterministic and local-normalized in this step.

That means the provider call proves integration, but final `specification.json` and `requirements.md` are still controlled by `RequirementsNormalizer` and `RequirementsArtifactWriter`.

This is intentional.

The next future step may parse real AI output into `ProjectSpecification`, but not now.

---

## Target Folder Structure

Add or update this structure:

```text
src/
|-- AiReliabilityEngineering.Core/
|   `-- Ai/
|       |-- AiProviderKind.cs
|       |-- AiProviderSelection.cs
|       `-- AiProviderSelectionParser.cs
|
|-- AiReliabilityEngineering.Infrastructure/
|   `-- Ai/
|       |-- OpenAi/
|       |   |-- OpenAiProvider.cs
|       |   |-- OpenAiProviderOptions.cs
|       |   |-- OpenAiResponseTextExtractor.cs
|       |   `-- OpenAiUsageMapper.cs
|       |-- AiProviderFactory.cs
|       `-- AiProviderFactoryOptions.cs
|
|-- tests/
|   |-- AiReliabilityEngineering.Core.Tests/
|   |   `-- Ai/
|   |       `-- AiProviderSelectionParserTests.cs
|   |
|   |-- AiReliabilityEngineering.Infrastructure.Tests/
|   |   `-- Ai/
|   |       `-- OpenAi/
|   |           |-- OpenAiProviderTests.cs
|   |           |-- OpenAiResponseTextExtractorTests.cs
|   |           `-- OpenAiUsageMapperTests.cs
|   |
|   `-- AiReliabilityEngineering.Cli.Tests/
|       `-- RunProviderCommandTests.cs
|
|-- samples/
|   `-- redis-ttl-audit.md
|
`-- docs/
    |-- openai-provider.md
    `-- demo-openai-requirements.md
```

If the repository already has a different folder layout, follow the existing style, but keep the same logical coverage.

---

# Required Core Changes

## 1. Extend AiProviderKind

Find:

```text
src/AiReliabilityEngineering.Core/Ai/AiProviderKind.cs
```

It currently should contain:

```csharp
public enum AiProviderKind
{
    Fake
}
```

Update to:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public enum AiProviderKind
{
    Fake,
    OpenAi
}
```

Rules:

- Add only OpenAi.
- Do not add Anthropic, Gemini, Ollama, or OpenRouter yet.

---

## 2. Add AiProviderSelection

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiProviderSelection.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Ai;
```

Purpose:

- represent provider selection for a run;
- keep provider CLI parsing separated from infrastructure provider construction.

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiProviderSelection
{
    public AiProviderSelection(
        AiProviderKind kind,
        string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("AI provider model is required.", nameof(model));
        }

        Kind = kind;
        Model = model;
    }

    public AiProviderKind Kind { get; }

    public string Model { get; }

    public static AiProviderSelection DefaultFake { get; } =
        new(AiProviderKind.Fake, AiProviderOptions.DefaultFake.Model);
}
```

Rules:

- Kind is required.
- Model is required.
- Model must not be null, empty, or whitespace.
- DefaultFake uses the existing fake model.
- Do not add API key or base URL here.

---

## 3. Add AiProviderSelectionParser

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiProviderSelectionParser.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Core.Ai;
```

Purpose:

- parse CLI provider names;
- normalize provider names;
- list supported provider CLI names.

Supported provider names:

```text
fake
openai
```

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public static class AiProviderSelectionParser
{
    public static bool TryParseProviderKind(
        string? value,
        out AiProviderKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            kind = AiProviderKind.Fake;
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "fake":
                kind = AiProviderKind.Fake;
                return true;

            case "openai":
                kind = AiProviderKind.OpenAi;
                return true;

            default:
                kind = AiProviderKind.Fake;
                return false;
        }
    }

    public static string ToCliName(AiProviderKind kind)
    {
        return kind switch
        {
            AiProviderKind.Fake => "fake",
            AiProviderKind.OpenAi => "openai",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static IReadOnlyList<string> SupportedCliNames { get; } =
        new[] { "fake", "openai" };
}
```

Rules:

- Null/empty/whitespace provider value maps to Fake.
- Unknown provider returns false.
- Names are lowercase.
- Do not throw for unknown user input in TryParseProviderKind.
- Throw only for unsupported enum values in ToCliName.

---

## 4. Extend RunRequest

Find the current `RunRequest`.

It should already include `WorkflowProfile`.

Update it to include provider selection.

Suggested shape:

```csharp
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Workflow;

public sealed record RunRequest(
    string IdeaFilePath,
    string RunsDirectory,
    WorkflowProfile Profile = WorkflowProfile.Fake,
    AiProviderSelection? AiProvider = null)
{
    public AiProviderSelection EffectiveAiProvider =>
        AiProvider ?? AiProviderSelection.DefaultFake;
}
```

Alternative acceptable shape:

```csharp
public sealed record RunRequest(
    string IdeaFilePath,
    string RunsDirectory,
    WorkflowProfile Profile,
    AiProviderSelection AiProvider);
```

But if using this shape, preserve compatibility with previous constructors through overloads or static factories.

Rules:

- Default provider must be fake.
- Existing code constructing `RunRequest` with two or three arguments should continue compiling if possible.
- Do not break previous tests unnecessarily.
- Do not store API keys in RunRequest.

---

# Required Infrastructure Changes

## 5. Add OpenAiProviderOptions

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/OpenAi/OpenAiProviderOptions.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;
```

Suggested implementation:

```csharp
namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

public sealed record OpenAiProviderOptions
{
    public OpenAiProviderOptions(
        string model,
        string apiKeyEnvironmentVariable = "OPENAI_API_KEY",
        Uri? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("OpenAI model is required.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(apiKeyEnvironmentVariable))
        {
            throw new ArgumentException("API key environment variable name is required.", nameof(apiKeyEnvironmentVariable));
        }

        Model = model;
        ApiKeyEnvironmentVariable = apiKeyEnvironmentVariable;
        Endpoint = endpoint ?? new Uri("https://api.openai.com/v1/responses");
    }

    public string Model { get; }

    public string ApiKeyEnvironmentVariable { get; }

    public Uri Endpoint { get; }
}
```

Rules:

- Default API key env var is `OPENAI_API_KEY`.
- Do not store the API key value here.
- Endpoint is configurable only for tests/future compatibility.
- Do not add base URL config file yet.

---

## 6. Add OpenAiResponseTextExtractor

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/OpenAi/OpenAiResponseTextExtractor.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;
```

Purpose:

- extract output text from OpenAI Responses API JSON;
- keep parsing separate and testable.

Suggested behavior:

1. Prefer top-level `output_text` if present and non-empty.
2. Otherwise walk `output[]`.
3. Inside each output item, walk `content[]`.
4. Extract text from:
   - `text`
   - or nested formats if present and easy to support.
5. Join text fragments with newline.
6. Return empty string if no text found.

Suggested signature:

```csharp
using System.Text.Json;

namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

public static class OpenAiResponseTextExtractor
{
    public static string ExtractText(JsonDocument document)
    {
        // implementation
    }
}
```

Rules:

- Must not throw for missing output fields.
- Invalid JSON parsing is handled before this method by caller.
- Keep parser tolerant because API response shape may include multiple item types.

---

## 7. Add OpenAiUsageMapper

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/OpenAi/OpenAiUsageMapper.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;
```

Purpose:

- map optional OpenAI usage fields to `AiUsage`.

Suggested behavior:

Look for:

```json
{
  "usage": {
    "input_tokens": 123,
    "output_tokens": 45,
    "total_tokens": 168
  }
}
```

Suggested signature:

```csharp
using System.Text.Json;
using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;

public static class OpenAiUsageMapper
{
    public static AiUsage? MapUsage(JsonDocument document)
    {
        // implementation
    }
}
```

Rules:

- Return null if no usage object exists.
- Return AiUsage with nullable fields if some values are missing.
- Do not throw for missing fields.
- Do not calculate total if not present.

---

## 8. Add OpenAiProvider

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/OpenAi/OpenAiProvider.cs
```

Namespace:

```csharp
namespace AiReliabilityEngineering.Infrastructure.Ai.OpenAi;
```

Required dependencies:

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiReliabilityEngineering.Core.Ai;
```

Suggested constructor:

```csharp
public sealed class OpenAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiProviderOptions _options;
    private readonly Func<string, string?> _environmentVariableReader;

    public OpenAiProvider(
        HttpClient httpClient,
        OpenAiProviderOptions options,
        Func<string, string?>? environmentVariableReader = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
    }

    public string Name => "openai";

    public async Task<AiResponse> GenerateAsync(
        AiRequest request,
        CancellationToken cancellationToken)
    {
        // implementation
    }
}
```

### Required GenerateAsync behavior

1. Call `cancellationToken.ThrowIfCancellationRequested()` at the start.
2. Validate `request` is not null.
3. Read API key from `_options.ApiKeyEnvironmentVariable`.
4. If API key is missing:
   - return `AiResponse.Failure("OPENAI_API_KEY environment variable is not set.", Name, _options.Model)`;
   - do not throw.
   - this is provider execution failure, not CLI argument failure.
5. Build a Responses API request payload.
6. Send POST request to `_options.Endpoint`.
7. Add headers:
   - Authorization: Bearer `<api-key>`
   - Content-Type: application/json
8. If HTTP status is not success:
   - read response body;
   - return `AiResponse.Failure(...)`;
   - include status code and a safe truncated error body;
   - do not include API key.
9. Parse JSON response.
10. Extract text using `OpenAiResponseTextExtractor`.
11. If text is empty:
   - return failure with message `OpenAI response did not contain output text.`
12. Map usage with `OpenAiUsageMapper`.
13. Return `AiResponse.Success(text, Name, _options.Model, usage)`.

### Model ownership

`AiRequirementsAgent` may still create `AiRequest` with `AiProviderOptions.DefaultFake` in this step.

`OpenAiProvider` must use `OpenAiProviderOptions.Model` as the selected OpenAI model for:

- the Responses API payload `model` value;
- the returned `AiResponse.Model`.

`OpenAiProvider` must not send `request.Options.Model` to OpenAI. In this step, `request.Options` is request metadata from the provider-neutral contract, while provider selection owns the concrete provider model.

### Request payload

Use minimal Responses API payload.

Suggested shape:

```json
{
  "model": "<selected-openai-model>",
  "input": "<combined prompt>"
}
```

The initial implementation intentionally sends string `input`, not message-array input.

Build the input string from `AiRequest.Messages`:

```text
[SYSTEM]
...

[USER]
...

[ASSISTANT]
...
```

If `AiRequest.OutputFormat == AiOutputFormat.Json`, append a final instruction:

```text
Return valid JSON only.
```

Important:

- Do not claim schema enforcement yet.
- Do not send message-array input in this step.
- Do not send JSON schema response_format yet.
- Do not send tools.
- Do not stream.
- Do not send structured-output options.

### Temperature / MaxOutputTokens

If possible and accepted by the API, include:

- temperature when `request.Options.Temperature` is not null;
- max output token field only if the exact expected API parameter name is already known from existing code/docs.

If there is any uncertainty, do not include these optional fields in this step.

The priority is a minimal working Responses API request.

### Error body truncation

Truncate provider error response body to avoid huge logs.

Suggested max length:

```text
2000 characters
```

Do not include secrets.

---

## 9. Update AiProviderFactoryOptions

Find:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/AiProviderFactoryOptions.cs
```

Update it to carry provider selection and model.

Suggested shape:

```csharp
using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed record AiProviderFactoryOptions
{
    public AiProviderFactoryOptions(AiProviderSelection selection)
    {
        Selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public AiProviderSelection Selection { get; }

    public static AiProviderFactoryOptions Default { get; } =
        new(AiProviderSelection.DefaultFake);
}
```

If current code uses `AiProviderKind ProviderKind`, update usages carefully.

---

## 10. Update AiProviderFactory

Find:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/AiProviderFactory.cs
```

Update it to support Fake and OpenAI.

Suggested constructor:

```csharp
public sealed class AiProviderFactory
{
    private readonly HttpClient _httpClient;
    private readonly Func<string, string?> _environmentVariableReader;

    public AiProviderFactory(
        HttpClient? httpClient = null,
        Func<string, string?>? environmentVariableReader = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
    }

    public IAiProvider Create(AiProviderFactoryOptions options)
    {
        // implementation
    }
}
```

Suggested Create implementation:

```csharp
return options.Selection.Kind switch
{
    AiProviderKind.Fake => new FakeAiProvider(),
    AiProviderKind.OpenAi => new OpenAiProvider(
        _httpClient,
        new OpenAiProviderOptions(options.Selection.Model),
        _environmentVariableReader),
    _ => throw new AiProviderException(...)
};
```

Rules:

- Fake provider remains default.
- OpenAiProvider uses selected model.
- Do not create network calls in the factory.
- Do not read API keys in the factory if avoidable; provider can read when called.
- Do not add config files.

---

# Required Orchestration Changes

## 11. Pass provider selection into pipeline creation

The current `AgentPipelineFactory` likely receives or creates `IAiProvider`.

Update it so the AI provider is built from the run request's `AiProviderSelection`.

Possible design:

```csharp
public sealed class AgentPipelineFactory
{
    private readonly Func<AiProviderSelection, IAiProvider> _aiProviderFactory;

    public AgentPipelineFactory(
        Func<AiProviderSelection, IAiProvider> aiProviderFactory,
        TimeProvider? timeProvider = null)
    {
        _aiProviderFactory = aiProviderFactory ?? throw new ArgumentNullException(nameof(aiProviderFactory));
        ...
    }

    public AgentPipeline Create(
        WorkflowProfile profile,
        AiProviderSelection aiProviderSelection,
        IRunLogger logger,
        IRunStateStore stateStore)
    {
        ...
    }
}
```

Then for `WorkflowProfile.AiRequirements`:

```csharp
var aiProvider = _aiProviderFactory(aiProviderSelection);
new AiRequirementsAgent(aiProvider, logger)
```

For `WorkflowProfile.Fake`, it does not need an AI provider, but passing provider selection is harmless.

Rules:

- Do not create OpenAiProvider directly in orchestration.
- Do not read environment variables in orchestration.
- Provider construction belongs in Infrastructure/CompositionRoot.

---

## 12. Update AireOrchestrator

Update the orchestrator call site.

Before:

```csharp
var pipeline = _pipelineFactory.Create(request.Profile, logger, stateStore);
```

After:

```csharp
var pipeline = _pipelineFactory.Create(
    request.Profile,
    request.EffectiveAiProvider,
    logger,
    stateStore);
```

Adjust exact names to match implemented RunRequest.

Rules:

- Run state behavior must not change.
- Failure behavior must not change.
- Default fake profile remains fake.

---

## 13. Update CompositionRoot

Update composition root so it can create providers from selection.

Conceptual example:

```csharp
public static IAiProvider CreateAiProvider(AiProviderSelection selection)
{
    var factory = new AiProviderFactory();
    return factory.Create(new AiProviderFactoryOptions(selection));
}

public static AgentPipelineFactory CreateAgentPipelineFactory()
{
    return new AgentPipelineFactory(CreateAiProvider);
}
```

If `HttpClient` should be reused, create it in composition root and pass it to the factory.

Keep it simple. Do not add HttpClientFactory unless already used.

Rules:

- No config file.
- No API key read in composition root.
- No real provider unless selected.

---

# Required CLI Changes

## 14. Add --provider option to run command

Add:

```text
--provider <provider>
```

Supported values:

```text
fake
openai
```

Default:

```text
fake
```

Examples:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider openai --model <model-name>
```

Behavior:

- Missing provider maps to fake.
- Unknown provider returns exit code 2.
- Error should mention supported providers.

Suggested error:

```text
Unsupported AI provider: unknown
Supported providers: fake, openai
```

---

## 15. Add --model option to run command

Add:

```text
--model <model>
```

Behavior:

- For provider fake:
  - if model omitted, use `fake-model`;
  - if model provided, use provided model if it passes validation.
- For provider openai:
  - if model omitted, return exit code 2 with error:
    ```text
    --model is required when --provider openai is used.
    ```
  - if model is blank/whitespace, return exit code 2.
  - if model is valid but `OPENAI_API_KEY` is missing, the CLI arguments are valid; the run should fail through the normal execution failure path with exit code 1.
- Do not hardcode a specific OpenAI model as default in this step.

Reason:

- model names can change over time;
- requiring explicit model avoids stale defaults.

---

## 16. Update CLI help text

Help output should mention:

```text
--provider
fake
openai
--model
```

Suggested provider option description:

```text
AI provider to use. Supported values: fake, openai. Default: fake.
```

Suggested model option description:

```text
AI model name. Required when --provider openai is used.
```

Also update the custom manual usage text printed by `CliCommandHandler` on parse errors or root invocation:

```text
aire run <idea-file> [--profile <profile>] [--provider <provider>] [--model <model>]
```

---

# Tests

## 17. Core provider parser tests

Add tests under:

```text
tests/AiReliabilityEngineering.Core.Tests/Ai/
```

### Test: null or empty provider maps to Fake

Cases:

```text
null
""
"   "
```

Expected:

- TryParseProviderKind returns true;
- kind is AiProviderKind.Fake.

### Test: fake parses

Input:

```text
fake
```

Expected:

- Fake.

### Test: openai parses

Input:

```text
openai
```

Expected:

- OpenAi.

### Test: parsing is case-insensitive

Inputs:

```text
FAKE
OpenAI
```

Expected:

- correct provider kinds.

### Test: unknown provider returns false

Input:

```text
unknown
```

Expected:

- false.

### Test: ToCliName returns lowercase names

Expected:

```text
Fake -> fake
OpenAi -> openai
```

### Test: AiProviderSelection rejects blank model

Expected:

- ArgumentException.

---

## 18. OpenAiResponseTextExtractor tests

Add tests under:

```text
tests/AiReliabilityEngineering.Infrastructure.Tests/Ai/OpenAi/
```

### Test: extracts top-level output_text

Input JSON:

```json
{
  "output_text": "hello"
}
```

Expected:

```text
hello
```

### Test: extracts output content text

Input JSON:

```json
{
  "output": [
    {
      "type": "message",
      "content": [
        {
          "type": "output_text",
          "text": "hello"
        }
      ]
    }
  ]
}
```

Expected:

```text
hello
```

### Test: joins multiple text fragments

Input JSON with two text fragments.

Expected:

```text
first
second
```

or the exact join behavior selected by implementation.

### Test: missing text returns empty string

Input JSON:

```json
{}
```

Expected:

```text
""
```

---

## 19. OpenAiUsageMapper tests

Add tests under:

```text
tests/AiReliabilityEngineering.Infrastructure.Tests/Ai/OpenAi/
```

### Test: maps full usage

Input:

```json
{
  "usage": {
    "input_tokens": 10,
    "output_tokens": 5,
    "total_tokens": 15
  }
}
```

Expected:

- InputTokens = 10
- OutputTokens = 5
- TotalTokens = 15

### Test: missing usage returns null

Input:

```json
{}
```

Expected:

- null.

### Test: partial usage maps nullable values

Input:

```json
{
  "usage": {
    "input_tokens": 10
  }
}
```

Expected:

- InputTokens = 10
- OutputTokens = null
- TotalTokens = null

---

## 20. OpenAiProvider tests

All tests must use mocked/fake HttpMessageHandler.

Do not call the real API.

Create a test handler that captures requests and returns controlled responses.

### Test: missing API key returns failure

Arrange:

- environmentVariableReader returns null.

Act:

- GenerateAsync.

Assert:

- response failed;
- error mentions `OPENAI_API_KEY`;
- no HTTP request was sent.

### Test: sends expected request

Arrange:

- environmentVariableReader returns fake key;
- handler returns success JSON with `output_text`.

Assert captured request:

- method POST;
- URL is `/v1/responses` or configured endpoint;
- Authorization header is Bearer fake key;
- JSON body contains selected model;
- JSON body contains input;
- input contains system/user message content.

### Test: successful response maps text and usage

Handler response:

```json
{
  "output_text": "hello from openai",
  "usage": {
    "input_tokens": 10,
    "output_tokens": 5,
    "total_tokens": 15
  }
}
```

Expected:

- response succeeded;
- content is `hello from openai`;
- provider is `openai`;
- model is selected model;
- usage values are mapped.

### Test: non-success HTTP returns failure

Handler response:

- status 400;
- body:
  ```json
  { "error": { "message": "bad request" } }
  ```

Expected:

- response failed;
- error contains status code;
- error contains safe body/message;
- no secret is included.

### Test: invalid JSON returns failure

Handler response:

```text
not json
```

Expected:

- response failed;
- error says invalid JSON or failed to parse OpenAI response.

### Test: missing output text returns failure

Handler response:

```json
{}
```

Expected:

- response failed;
- error says response did not contain output text.

### Test: cancellation is checked at start

Use already-canceled token.

Expected:

- OperationCanceledException.

---

## 21. AiProviderFactory tests

Update existing tests.

### Test: default creates FakeAiProvider

Expected unchanged.

### Test: openai selection creates OpenAiProvider

Arrange:

```csharp
var selection = new AiProviderSelection(AiProviderKind.OpenAi, "test-model");
```

Expected:

- provider is OpenAiProvider;
- provider.Name is `openai`.

### Test: factory rejects null options

Expected unchanged.

---

## 22. AgentPipelineFactory / Orchestrator tests

Update existing tests for provider selection.

### Test: ai-requirements with fake provider still works

Existing behavior should pass.

### Test: ai-requirements with openai provider selection uses provider factory

Use a test provider factory delegate that records the selection and returns a RecordingAiProvider.

Arrange:

- WorkflowProfile.AiRequirements
- AiProviderSelection OpenAi/test-model

Assert:

- provider factory received OpenAi selection;
- AiRequirementsAgent completed.

Do not call real OpenAI.

---

## 23. CLI provider tests

Add tests under:

```text
tests/AiReliabilityEngineering.Cli.Tests/
```

### Test: default provider remains fake

Command:

```text
run idea.md --profile ai-requirements
```

Expected:

- exit code 0;
- run completes without OPENAI_API_KEY.

### Test: explicit fake provider succeeds

Command:

```text
run idea.md --profile ai-requirements --provider fake
```

Expected:

- exit code 0.

### Test: openai provider without model fails

Command:

```text
run idea.md --profile ai-requirements --provider openai
```

Expected:

- exit code 2;
- error mentions `--model is required`.

### Test: unknown provider fails

Command:

```text
run idea.md --provider unknown
```

Expected:

- exit code 2;
- error mentions unsupported provider;
- error mentions fake and openai.

### Test: help mentions provider and model

Expected help output contains:

```text
--provider
--model
fake
openai
```

### Optional CLI integration test with mocked provider

If current CLI composition cannot inject mocked OpenAiProvider, do not add real HTTP CLI tests.

Do not write a CLI test that requires real `OPENAI_API_KEY`.

---

# Sample Demo

## 24. Add sample idea file

Create:

```text
samples/redis-ttl-audit.md
```

Suggested content:

```markdown
# Redis TTL Audit Tool

Create a small CLI tool that scans Redis keys and reports keys that have no TTL.

The tool should group keys by prefix, estimate how many keys never expire, and generate a Markdown report.

The tool must be read-only and must not delete or modify Redis keys.
```

---

# Documentation

## 25. Add docs/openai-provider.md

Create:

```text
docs/openai-provider.md
```

Content should explain:

```markdown
# OpenAI Provider

AIRE includes OpenAiProvider as the first real AI provider.

It implements IAiProvider and calls the OpenAI Responses API.

## API Key

Set the API key through the OPENAI_API_KEY environment variable.

Do not pass API keys through CLI arguments.

## Usage

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

## Scope

This provider currently supports non-streaming text requests only.

It does not support tools, streaming, structured outputs, or file inputs yet.
```

Do not include a real API key.

---

## 26. Add docs/demo-openai-requirements.md

Create:

```text
docs/demo-openai-requirements.md
```

Content should explain the manual demo:

```markdown
# Demo: OpenAI Requirements Agent

This demo runs AiRequirementsAgent with OpenAiProvider.

## Prerequisites

- OPENAI_API_KEY is set.
- A model name is selected.
- Network access is available.

## Command

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

## Expected Output

A run folder is created under runs/.

Review:

- artifacts/specification.json
- artifacts/requirements.md
- logs/orchestrator.log
- run-state.json

## Notes

The current AiRequirementsAgent still writes deterministic requirement artifacts using local normalization. The OpenAI call proves provider integration but is not yet used to generate the final ProjectSpecification.
```

---

## 27. Update docs/workflow-profiles.md

Add provider examples:

```markdown
Run ai-requirements with fake provider:

```bash
aire run samples/redis-ttl-audit.md --profile ai-requirements --provider fake
```

Run ai-requirements with OpenAI provider:

```bash
aire run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```
```

---

## 28. Update docs/wiki.md

If exists, add entries:

```markdown
## AI Provider Selection

Provider selection chooses which IAiProvider implementation is used by AI-aware agents.

Current providers:

- fake
- openai
```

---

## 29. Update README.md

Add a short section:

```markdown
## Real Provider Demo

AIRE can run the AI requirements workflow with OpenAI:

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

The default provider remains fake and does not require API keys.
```

Keep README brief.

---

# Existing Workflow Must Remain Stable

After this step, these commands must pass without API keys:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider fake
```

This command should fail cleanly with exit code 2 because `--model` is required:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider openai
```

This command has valid CLI arguments but should fail through the normal execution failure path with exit code 1 if `OPENAI_API_KEY` is not set:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider openai --model <model-name>
```

The missing-key error must mention only that `OPENAI_API_KEY` is not set. It must not include any secret value.

Manual OpenAI demo command requires:

```text
OPENAI_API_KEY
```

and explicit:

```text
--model <model-name>
```

---

# Suggested Implementation Order for Codex

Follow this order.

## Task 1: Add provider selection models

Create/update:

```text
src/AiReliabilityEngineering.Core/Ai/AiProviderKind.cs
src/AiReliabilityEngineering.Core/Ai/AiProviderSelection.cs
src/AiReliabilityEngineering.Core/Ai/AiProviderSelectionParser.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 2: Extend RunRequest

Add `AiProviderSelection`.

Keep default fake provider.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 3: Add OpenAI parsing helpers

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/OpenAi/OpenAiResponseTextExtractor.cs
src/AiReliabilityEngineering.Infrastructure/Ai/OpenAi/OpenAiUsageMapper.cs
```

Add tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 4: Add OpenAiProvider

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/OpenAi/OpenAiProviderOptions.cs
src/AiReliabilityEngineering.Infrastructure/Ai/OpenAi/OpenAiProvider.cs
```

Add mocked HTTP tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 5: Update AiProviderFactory

Support Fake and OpenAi.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 6: Update pipeline factory and orchestrator

Pass provider selection from RunRequest to AgentPipelineFactory.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 7: Update CLI

Add:

```text
--provider
--model
```

Add CLI tests.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 8: Add sample and docs

Create/update:

```text
samples/redis-ttl-audit.md
docs/openai-provider.md
docs/demo-openai-requirements.md
docs/workflow-profiles.md
docs/wiki.md if exists
README.md
```

Run final verification.

---

# Acceptance Criteria

The task is complete when all criteria are satisfied.

## Core

- AiProviderKind includes Fake and OpenAi.
- AiProviderSelection exists.
- AiProviderSelection validates model.
- AiProviderSelectionParser supports fake and openai.
- Parser tests pass.
- RunRequest carries provider selection.
- RunRequest defaults to Fake provider.

## Infrastructure

- OpenAiProvider exists.
- OpenAiProvider implements IAiProvider.
- OpenAiProvider uses HttpClient.
- OpenAiProvider calls POST /v1/responses.
- OpenAiProvider reads API key from OPENAI_API_KEY.
- OpenAiProvider does not log or expose API key.
- OpenAiProvider returns failure if API key is missing.
- OpenAiProvider maps successful output text.
- OpenAiProvider maps usage when present.
- OpenAiProvider returns failure on HTTP errors.
- OpenAiProvider returns failure on invalid JSON.
- OpenAiProvider tests use mocked HTTP only.
- AiProviderFactory creates FakeAiProvider by default.
- AiProviderFactory creates OpenAiProvider for OpenAi selection.

## Orchestration

- AgentPipelineFactory receives provider selection.
- AiRequirements profile can use selected provider.
- Fake profile remains unchanged.
- No real provider is created unless selected.
- No API key is required for fake/default runs.

## CLI

- `--provider fake` works.
- `--provider openai --model <model-name>` is accepted.
- `--provider openai` without model fails with exit code 2.
- unknown provider fails with exit code 2.
- help mentions provider and model.
- default provider remains fake.

## Tests

- Core provider selection tests pass.
- OpenAI parser tests pass.
- OpenAI provider mocked HTTP tests pass.
- Provider factory tests pass.
- Orchestrator tests pass.
- CLI tests pass.
- Existing tests still pass.
- No automated test calls the real OpenAI API.

## Documentation

- samples/redis-ttl-audit.md exists.
- docs/openai-provider.md exists.
- docs/demo-openai-requirements.md exists.
- README includes a brief real provider demo.
- Docs warn not to pass API keys via CLI.
- Docs say OpenAI demo requires OPENAI_API_KEY and explicit --model.

## Verification

These commands pass without API keys:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider fake
```

This command fails cleanly with exit code 2 because model is required:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md --profile ai-requirements --provider openai
```

Manual real-provider demo:

```bash
export OPENAI_API_KEY="..."
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-requirements --provider openai --model <model-name>
```

Cleanup passes when run against disposable data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Notes for Codex

- Keep this step focused on OpenAI provider and provider selection.
- Do not add other providers.
- Do not add SDK packages.
- Do not add config files.
- Do not accept API keys in CLI.
- Do not log API keys.
- Do not add streaming.
- Do not add structured outputs yet.
- Do not change default provider from fake.
- Do not change default profile from fake.
- Do not make tests call the real OpenAI API.
- Keep outputs deterministic where tests depend on output.
- Use temporary directories in tests.
- Save this file as UTF-8 without BOM.
