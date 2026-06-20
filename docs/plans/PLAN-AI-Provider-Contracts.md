# PLAN.md - AIRE Step 10.1: Add AI Provider Contracts and FakeAiProvider

## Purpose

This plan is for Codex to add the first AI integration foundation to AIRE.

AIRE means AI Reliability Engineering.

Repository conventions:

- Repository name: ai-reliability-engineering
- Solution name: AiReliabilityEngineering.slnx
- CLI project: AiReliabilityEngineering.Cli
- CLI command name: aire
- PRD location: docs/PRD.md

The current AIRE skeleton should already be stable and tested:

- CLI command: run
- CLI command: cleanup
- fake orchestration pipeline
- fake agents
- run folder creation
- run-state persistence
- logging
- cleanup tests
- CLI tests
- orchestration tests

The goal of this step is to add a provider-neutral AI abstraction layer without connecting to any real cloud or local AI provider yet.

This step must keep the system fully local, deterministic, testable, and working without API keys or network access.

---

## High-Level Goal

Add AI provider contracts and a deterministic FakeAiProvider.

After this step, the repository should contain:

- provider-neutral AI contracts in Core;
- deterministic constructor/factory validation for AI request models;
- a deterministic FakeAiProvider in Infrastructure;
- a small provider factory;
- tests for AI request/response models;
- tests for FakeAiProvider behavior;
- documentation describing the AI provider abstraction;
- no real OpenAI, Anthropic, Gemini, Ollama, Codex, or OpenCode integration yet.

The existing fake workflow must continue working exactly as before.

---

## Non-Goals

Do not implement these in this step:

- OpenAI provider
- Anthropic provider
- Gemini provider
- Ollama provider
- OpenRouter provider
- Codex executor
- OpenCode executor
- MCP integration
- real prompt execution
- real AI-powered RequirementsAgent
- real AI-powered DocumentationAgent
- real AI-powered PlannerAgent
- real AI-powered ReviewerAgent
- configuration file loading
- API key lookup
- HTTP calls
- retry policies
- token budgeting
- streaming
- tool calling
- structured output enforcement through provider APIs
- JSON schema validation package
- build/test runner
- Git integration
- dashboard

This step is only about stable internal contracts and the fake provider.

---

## Design Principle

Agents must not depend directly on any concrete AI vendor SDK.

Correct dependency direction:

```text
Agents
  -> IAiProvider
      -> FakeAiProvider
      -> future OpenAiProvider
      -> future OllamaProvider
      -> future AnthropicProvider
```

Incorrect dependency direction:

```text
Agents
  -> OpenAI SDK directly
Agents
  -> Anthropic SDK directly
Agents
  -> Ollama HTTP API directly
```

The provider abstraction belongs in Core because agents and orchestration should depend on contracts, not infrastructure implementations.

Concrete providers belong in Infrastructure.

---

## Validation Decision

This plan uses constructor/factory-level validation for AI model objects.

That means:

- AiMessage rejects null Content.
- AiProviderOptions rejects null, empty, or whitespace Model.
- AiRequest rejects null Messages.
- AiRequest rejects an empty message list.
- AiRequest rejects null message entries.
- AiRequest rejects null Options.
- AiResponse.Success rejects null content/provider/model.
- AiResponse.Failure rejects null/blank error message and null provider/model.

Because Core model constructors/factories reject invalid shapes, FakeAiProvider does not need to handle impossible null request shapes. FakeAiProvider should still return AiResponse.Failure for valid but semantically unsupported provider-level failures if such cases are introduced later.

Cancellation remains exception-based:

- FakeAiProvider must call cancellationToken.ThrowIfCancellationRequested() at the start of GenerateAsync.
- Passing an already-canceled token must throw OperationCanceledException.
- This is the only required cancellation behavior in this step.

---

## Target Folder Structure

Add or update this structure:

```text
src/
|-- AiReliabilityEngineering.Core/
|   `-- Ai/
|       |-- IAiProvider.cs
|       |-- AiRequest.cs
|       |-- AiResponse.cs
|       |-- AiMessage.cs
|       |-- AiRole.cs
|       |-- AiUsage.cs
|       |-- AiOutputFormat.cs
|       |-- AiProviderKind.cs
|       |-- AiProviderOptions.cs
|       `-- AiProviderException.cs
|
|-- AiReliabilityEngineering.Infrastructure/
|   `-- Ai/
|       |-- FakeAiProvider.cs
|       |-- AiProviderFactory.cs
|       `-- AiProviderFactoryOptions.cs
|
|-- tests/
|   |-- AiReliabilityEngineering.Core.Tests/
|   |   `-- Ai/
|   |       |-- AiRequestTests.cs
|   |       |-- AiResponseTests.cs
|   |       |-- AiMessageTests.cs
|   |       `-- AiProviderOptionsTests.cs
|   |
|   `-- AiReliabilityEngineering.Infrastructure.Tests/
|       `-- Ai/
|           |-- FakeAiProviderTests.cs
|           `-- AiProviderFactoryTests.cs
|
`-- docs/
    `-- ai-providers.md
```

If `AiReliabilityEngineering.Infrastructure.Tests` does not exist yet:

1. Create the test project.
2. Add it to `AiReliabilityEngineering.slnx`.
3. Add a project reference to `src/AiReliabilityEngineering.Infrastructure/`.
4. Add a project reference to `src/AiReliabilityEngineering.Core/` if it is not transitively available or if tests directly use Core types.
5. Ensure `dotnet test AiReliabilityEngineering.slnx` runs the new infrastructure tests.

If the repository already has a different tests layout, follow the existing style, but keep the same logical coverage and ensure the test project is included in the solution.

---

## Required Core Contracts

Add these files under:

```text
src/AiReliabilityEngineering.Core/Ai/
```

Use namespace:

```csharp
namespace AiReliabilityEngineering.Core.Ai;
```

---

## 1. IAiProvider

Create:

```text
src/AiReliabilityEngineering.Core/Ai/IAiProvider.cs
```

Required contract:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public interface IAiProvider
{
    string Name { get; }

    Task<AiResponse> GenerateAsync(
        AiRequest request,
        CancellationToken cancellationToken);
}
```

Rules:

- `Name` must be stable and lowercase-friendly.
- Normal provider failures should return `AiResponse.Failure(...)`.
- Cancellation should respect `CancellationToken`.
- Do not throw for normal provider errors.
- Throw only for programming errors or cancellation.

---

## 2. AiRole

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiRole.cs
```

Required enum:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public enum AiRole
{
    System,
    User,
    Assistant
}
```

This allows a provider-neutral message model.

---

## 3. AiMessage

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiMessage.cs
```

Use an explicit constructor so null content is rejected.

Required implementation shape:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiMessage
{
    public AiMessage(AiRole role, string content)
    {
        Role = role;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public AiRole Role { get; }

    public string Content { get; }
}
```

Rules:

- Content must not be null.
- Empty content is allowed in this step.
- Whitespace content is allowed in this step.
- Do not add provider-specific fields.

---

## 4. AiOutputFormat

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiOutputFormat.cs
```

Required enum:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public enum AiOutputFormat
{
    Text,
    Json
}
```

Do not add provider-specific modes yet.

---

## 5. AiProviderKind

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiProviderKind.cs
```

Required enum:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public enum AiProviderKind
{
    Fake
}
```

Only add `Fake` in this step.

Do not add OpenAI, Anthropic, Gemini, or Ollama enum values yet. Provider kinds should represent implemented providers only.

---

## 6. AiUsage

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiUsage.cs
```

Required record:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiUsage(
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens);
```

Rules:

- Usage values are optional because some providers may not return all values.
- Fake provider may return deterministic fake usage.
- TotalTokens should not be auto-calculated.
- Keep it as simple data.

---

## 7. AiProviderOptions

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiProviderOptions.cs
```

Use an explicit constructor so invalid model names are rejected.

Required implementation shape:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiProviderOptions
{
    public AiProviderOptions(
        string model,
        double? temperature = null,
        int? maxOutputTokens = null)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("AI provider model is required.", nameof(model));
        }

        Model = model;
        Temperature = temperature;
        MaxOutputTokens = maxOutputTokens;
    }

    public string Model { get; }

    public double? Temperature { get; }

    public int? MaxOutputTokens { get; }

    public static AiProviderOptions DefaultFake { get; } =
        new("fake-model", temperature: 0, maxOutputTokens: null);
}
```

Rules:

- Model is required.
- Model must not be null, empty, or whitespace.
- Do not add API key fields.
- Do not add base URL fields yet.
- Provider-specific options can come later.

---

## 8. AiRequest

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiRequest.cs
```

Use an explicit constructor so invalid request shape is rejected.

Required implementation shape:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiRequest
{
    public AiRequest(
        IReadOnlyList<AiMessage> messages,
        AiOutputFormat outputFormat,
        AiProviderOptions options,
        string? jsonSchema = null)
    {
        if (messages is null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        if (messages.Count == 0)
        {
            throw new ArgumentException("AI request must contain at least one message.", nameof(messages));
        }

        if (messages.Any(message => message is null))
        {
            throw new ArgumentException("AI request messages must not contain null entries.", nameof(messages));
        }

        Messages = messages.ToArray();
        OutputFormat = outputFormat;
        Options = options ?? throw new ArgumentNullException(nameof(options));
        JsonSchema = jsonSchema;
    }

    public IReadOnlyList<AiMessage> Messages { get; }

    public AiOutputFormat OutputFormat { get; }

    public AiProviderOptions Options { get; }

    public string? JsonSchema { get; }

    public static AiRequest FromPrompts(
        string systemPrompt,
        string userPrompt,
        AiOutputFormat outputFormat,
        AiProviderOptions options,
        string? jsonSchema = null)
    {
        return new AiRequest(
            new[]
            {
                new AiMessage(AiRole.System, systemPrompt),
                new AiMessage(AiRole.User, userPrompt)
            },
            outputFormat,
            options,
            jsonSchema);
    }
}
```

Rules:

- Prefer messages over separate SystemPrompt/UserPrompt properties.
- Keep `JsonSchema` as nullable string for now.
- Do not validate JSON schema content yet.
- Do not depend on any JSON schema package.
- Store a defensive copy of messages using `ToArray()`.

Because AiMessage rejects null content, `FromPrompts(null, ...)` or `FromPrompts(..., null, ...)` should throw ArgumentNullException from AiMessage.

---

## 9. AiResponse

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiResponse.cs
```

Use factory methods and a private constructor if possible.

Required implementation shape:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiResponse
{
    private AiResponse(
        bool succeeded,
        string content,
        string? errorMessage,
        AiUsage? usage,
        string provider,
        string model)
    {
        Succeeded = succeeded;
        Content = content;
        ErrorMessage = errorMessage;
        Usage = usage;
        Provider = provider;
        Model = model;
    }

    public bool Succeeded { get; }

    public string Content { get; }

    public string? ErrorMessage { get; }

    public AiUsage? Usage { get; }

    public string Provider { get; }

    public string Model { get; }

    public static AiResponse Success(
        string content,
        string provider,
        string model,
        AiUsage? usage = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(model);

        return new AiResponse(
            true,
            content,
            null,
            usage,
            provider,
            model);
    }

    public static AiResponse Failure(
        string errorMessage,
        string provider,
        string model)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("AI error message is required.", nameof(errorMessage));
        }

        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(model);

        return new AiResponse(
            false,
            string.Empty,
            errorMessage,
            null,
            provider,
            model);
    }
}
```

Rules:

- Successful responses must have `Succeeded = true`.
- Failed responses must have `Succeeded = false`.
- Failed responses must have empty content.
- ErrorMessage must be non-empty for failure.
- Do not throw for expected provider failures inside providers; providers should use `AiResponse.Failure(...)`.
- Constructor should not be public if factory methods are used.

---

## 10. AiProviderException

Create:

```text
src/AiReliabilityEngineering.Core/Ai/AiProviderException.cs
```

Required implementation:

```csharp
namespace AiReliabilityEngineering.Core.Ai;

public sealed class AiProviderException : Exception
{
    public AiProviderException(string message)
        : base(message)
    {
    }

    public AiProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

This is mainly for future programming or provider adapter errors.

Do not use exceptions for normal fake provider failures.

---

# Required Infrastructure Implementation

Add these files under:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/
```

Use namespace:

```csharp
namespace AiReliabilityEngineering.Infrastructure.Ai;
```

---

## 11. FakeAiProvider

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/FakeAiProvider.cs
```

Required behavior:

- Implements `IAiProvider`.
- Name returns `fake`.
- Does not call the network.
- Does not read environment variables.
- Is deterministic.
- Supports `AiOutputFormat.Text`.
- Supports `AiOutputFormat.Json`.
- Respects cancellation by calling `cancellationToken.ThrowIfCancellationRequested()` at the start of GenerateAsync.
- Does not need to check for null AiRequest, null Messages, null Options, blank Model, or null messages because Core constructors reject those invalid shapes.
- May throw ArgumentNullException if `request` itself is null, because that is a programming error.

Required implementation outline:

```csharp
using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed class FakeAiProvider : IAiProvider
{
    public string Name => "fake";

    public Task<AiResponse> GenerateAsync(
        AiRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        var content = request.OutputFormat switch
        {
            AiOutputFormat.Text => CreateTextResponse(request),
            AiOutputFormat.Json => CreateJsonResponse(),
            _ => throw new AiProviderException($"Unsupported AI output format: {request.OutputFormat}")
        };

        var usage = new AiUsage(
            InputTokens: EstimateTokens(request.Messages.Sum(message => message.Content.Length)),
            OutputTokens: EstimateTokens(content.Length),
            TotalTokens: null);

        return Task.FromResult(AiResponse.Success(
            content,
            Name,
            request.Options.Model,
            usage));
    }

    private static string CreateTextResponse(AiRequest request)
    {
        var userMessage = request.Messages.LastOrDefault(message => message.Role == AiRole.User)?.Content ?? string.Empty;

        return $"Fake AI response for: {userMessage}";
    }

    private static string CreateJsonResponse()
    {
        return """
        {
          "provider": "fake",
          "status": "ok",
          "message": "Fake AI JSON response"
        }
        """.Trim();
    }

    private static int EstimateTokens(int characters)
    {
        return Math.Max(1, characters / 4);
    }
}
```

Adjust syntax if the project language version does not support raw string literals.

Important:

- Keep output deterministic.
- Do not include timestamps or random IDs in fake AI response.
- Tests should be able to compare exact output.

---

## 12. AiProviderFactoryOptions

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/AiProviderFactoryOptions.cs
```

Required record:

```csharp
using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed record AiProviderFactoryOptions(
    AiProviderKind ProviderKind)
{
    public static AiProviderFactoryOptions Default { get; } =
        new(AiProviderKind.Fake);
}
```

Keep it minimal.

---

## 13. AiProviderFactory

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/AiProviderFactory.cs
```

Required implementation:

```csharp
using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Infrastructure.Ai;

public sealed class AiProviderFactory
{
    public IAiProvider Create(AiProviderFactoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.ProviderKind switch
        {
            AiProviderKind.Fake => new FakeAiProvider(),
            _ => throw new AiProviderException(
                $"Unsupported AI provider kind: {options.ProviderKind}")
        };
    }
}
```

Rules:

- Only Fake provider is supported in this step.
- Do not add real provider placeholders unless required by tests.
- Do not read config files.
- Do not read environment variables.
- Do not use dependency injection yet unless the project already uses it.

---

# Composition Root Update

If the current repository has a composition root, update it minimally to make `FakeAiProvider` available.

For example, if there is:

```text
src/AiReliabilityEngineering.Cli/CompositionRoot.cs
```

or similar, add a method like:

```csharp
public static IAiProvider CreateAiProvider()
{
    var factory = new AiProviderFactory();
    return factory.Create(AiProviderFactoryOptions.Default);
}
```

Do not wire it into fake agents yet unless it is useful and does not change behavior.

The current workflow should still use the existing fake agents after this step.

---

# Documentation

## 14. Add docs/ai-providers.md

Create:

```text
docs/ai-providers.md
```

Content should explain:

- why AIRE uses provider abstraction;
- current provider: FakeAiProvider;
- future providers: OpenAI, Ollama, Anthropic, Gemini;
- no API keys are required in this step;
- no network calls are made in this step;
- agents should depend on IAiProvider, not provider SDKs;
- concrete providers belong in Infrastructure;
- contracts belong in Core;
- AI request models validate invalid shapes at construction time.

Suggested outline:

```markdown
# AI Providers

AIRE uses a provider-neutral AI abstraction so workflow agents do not depend directly on a specific AI vendor SDK.

## Current Provider

### FakeAiProvider

The fake provider is deterministic, local-only, and used for tests and early workflow development.

It does not call the network and does not require API keys.

## Contract Validation

AI request models reject invalid shapes at construction time. For example, AiRequest requires at least one message, AiMessage requires non-null content, and AiProviderOptions requires a non-empty model name.

## Future Providers

Future providers may include:

- OpenAI
- Ollama
- Anthropic
- Gemini

These providers should be implemented behind IAiProvider.

## Design Rule

Agents must depend on IAiProvider.

Concrete providers belong in AiReliabilityEngineering.Infrastructure.
```

---

## 15. Update docs/wiki.md

If `docs/wiki.md` exists, add a short section:

```markdown
## AI Provider

An AI Provider is an adapter that converts AIRE's provider-neutral AI request into a concrete model call.

Current provider:

- FakeAiProvider: deterministic local provider used for tests and early development.

AI request contracts validate invalid shapes at construction time.

Future providers may include OpenAI, Ollama, Anthropic, and Gemini.
```

If `docs/wiki.md` does not exist, do not create it in this step unless the repository already expects it.

---

## 16. Update README.md

Add a short note:

```markdown
## AI Provider Layer

AIRE now contains the first provider-neutral AI abstraction.

The current implementation includes only a FakeAiProvider. It is deterministic, local-only, and does not require API keys or network access.

AI request contracts validate invalid shapes at construction time.

Real providers such as OpenAI, Ollama, Anthropic, and Gemini will be added later behind the same IAiProvider contract.
```

Keep this brief.

---

# Tests

## 17. Core AI model tests

Add tests under:

```text
tests/AiReliabilityEngineering.Core.Tests/Ai/
```

If folders do not exist, create them.

### Test: AiMessage rejects null content

Assert:

- `new AiMessage(AiRole.User, null!)` throws ArgumentNullException.

### Test: AiMessage allows empty content

Assert:

- `new AiMessage(AiRole.User, string.Empty)` succeeds.

### Test: AiProviderOptions rejects null, empty, or whitespace model

Assert these throw:

```csharp
new AiProviderOptions(null!)
new AiProviderOptions("")
new AiProviderOptions("   ")
```

Expected:

- null throws ArgumentException or ArgumentNullException, depending on implementation;
- empty and whitespace throw ArgumentException.

Prefer consistent ArgumentException for null/empty/whitespace if implemented through string.IsNullOrWhiteSpace.

### Test: AiProviderOptions.DefaultFake is usable

Assert:

- model is `fake-model`;
- temperature is 0;
- max output tokens is null.

### Test: AiRequest.FromPrompts creates system and user messages

Arrange:

```csharp
var request = AiRequest.FromPrompts(
    "system",
    "user",
    AiOutputFormat.Text,
    AiProviderOptions.DefaultFake);
```

Assert:

- request has two messages;
- first message role is System;
- first message content is `system`;
- second message role is User;
- second message content is `user`;
- output format is Text;
- options model is `fake-model`.

### Test: AiRequest rejects null messages list

Assert:

```csharp
new AiRequest(null!, AiOutputFormat.Text, AiProviderOptions.DefaultFake)
```

throws ArgumentNullException.

### Test: AiRequest rejects empty messages list

Assert:

```csharp
new AiRequest(Array.Empty<AiMessage>(), AiOutputFormat.Text, AiProviderOptions.DefaultFake)
```

throws ArgumentException.

### Test: AiRequest rejects null message entries

Assert a messages list containing null throws ArgumentException.

### Test: AiRequest rejects null options

Assert:

```csharp
new AiRequest(
    new[] { new AiMessage(AiRole.User, "hello") },
    AiOutputFormat.Text,
    null!)
```

throws ArgumentNullException.

### Test: AiRequest defensively copies messages

Arrange:

- create a mutable List<AiMessage>;
- create AiRequest from the list;
- mutate the original list.

Assert:

- request.Messages remains unchanged.

### Test: AiResponse.Success creates successful response

Assert:

- Succeeded is true;
- Content is expected;
- ErrorMessage is null;
- Provider is expected;
- Model is expected.

### Test: AiResponse.Success rejects null content/provider/model

Assert null arguments throw ArgumentNullException.

### Test: AiResponse.Failure creates failed response

Assert:

- Succeeded is false;
- Content is empty;
- ErrorMessage is expected;
- Provider is expected;
- Model is expected.

### Test: AiResponse.Failure rejects blank error message

Assert null, empty, or whitespace error message throws ArgumentException or ArgumentNullException according to implementation.

---

## 18. Infrastructure test project wiring

If `tests/AiReliabilityEngineering.Infrastructure.Tests/` does not exist, create it.

The test project must:

- be added to AiReliabilityEngineering.slnx;
- reference `src/AiReliabilityEngineering.Infrastructure/`;
- reference `src/AiReliabilityEngineering.Core/` if test code directly imports Core AI types;
- use the same test framework as the rest of the repository;
- be included when running `dotnet test AiReliabilityEngineering.slnx`.

Required verification:

```bash
dotnet test AiReliabilityEngineering.slnx
```

must execute the infrastructure tests.

---

## 19. FakeAiProvider tests

Add tests under:

```text
tests/AiReliabilityEngineering.Infrastructure.Tests/Ai/
```

### Test: text response is deterministic

Arrange:

```csharp
var provider = new FakeAiProvider();
var request = AiRequest.FromPrompts(
    "You are a fake provider.",
    "Create documentation.",
    AiOutputFormat.Text,
    AiProviderOptions.DefaultFake);
```

Act twice.

Assert:

- both responses succeeded;
- both contents are equal;
- content is exactly `Fake AI response for: Create documentation.`;
- provider is `fake`;
- model is `fake-model`;
- usage is not null.

### Test: JSON response is deterministic and parseable

Arrange request with:

```csharp
AiOutputFormat.Json
```

Act.

Assert:

- response succeeded;
- content is valid JSON using System.Text.Json;
- JSON contains `provider` equal to `fake`;
- JSON contains `status` equal to `ok`.

### Test: cancellation is checked at start

Arrange:

- create a valid request;
- create an already-canceled CancellationToken.

Act:

```csharp
await provider.GenerateAsync(request, canceledToken);
```

Assert:

- OperationCanceledException is thrown.

This test must use an already-canceled token to verify the explicit start-of-method cancellation check.

### Test: null request throws programming error

Assert:

```csharp
await provider.GenerateAsync(null!, CancellationToken.None);
```

throws ArgumentNullException.

---

## 20. AiProviderFactory tests

Add tests under:

```text
tests/AiReliabilityEngineering.Infrastructure.Tests/Ai/
```

### Test: default factory creates FakeAiProvider

Arrange:

```csharp
var factory = new AiProviderFactory();
```

Act:

```csharp
var provider = factory.Create(AiProviderFactoryOptions.Default);
```

Assert:

- provider is not null;
- provider.Name is `fake`;
- provider type is FakeAiProvider.

### Test: factory rejects null options

Assert:

```csharp
factory.Create(null!)
```

throws ArgumentNullException.

---

# Existing Workflow Must Remain Stable

After adding AI contracts and FakeAiProvider, this must still work:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

Expected:

- fake workflow still runs;
- run-state still becomes Completed;
- existing fake artifacts are still created;
- no AI provider is required to run the current fake workflow;
- no API key is required;
- no network call is made.

---

# Suggested Implementation Order for Codex

Follow this order.

## Task 1: Add Core AI contracts

Create:

```text
src/AiReliabilityEngineering.Core/Ai/IAiProvider.cs
src/AiReliabilityEngineering.Core/Ai/AiRole.cs
src/AiReliabilityEngineering.Core/Ai/AiMessage.cs
src/AiReliabilityEngineering.Core/Ai/AiOutputFormat.cs
src/AiReliabilityEngineering.Core/Ai/AiProviderKind.cs
src/AiReliabilityEngineering.Core/Ai/AiUsage.cs
src/AiReliabilityEngineering.Core/Ai/AiProviderOptions.cs
src/AiReliabilityEngineering.Core/Ai/AiRequest.cs
src/AiReliabilityEngineering.Core/Ai/AiResponse.cs
src/AiReliabilityEngineering.Core/Ai/AiProviderException.cs
```

Run:

```bash
dotnet build
```

## Task 2: Add FakeAiProvider

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/FakeAiProvider.cs
```

Run:

```bash
dotnet build
```

## Task 3: Add provider factory

Create:

```text
src/AiReliabilityEngineering.Infrastructure/Ai/AiProviderFactoryOptions.cs
src/AiReliabilityEngineering.Infrastructure/Ai/AiProviderFactory.cs
```

Run:

```bash
dotnet build
```

## Task 4: Update composition root minimally

If there is an existing composition root, add provider creation method.

Do not change the default fake workflow behavior.

Run:

```bash
dotnet build
```

## Task 5: Add Core tests

Create or update Core test files.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 6: Add Infrastructure tests

Create infrastructure test project if missing.

Add required project references.

Add tests for FakeAiProvider and AiProviderFactory.

Run:

```bash
dotnet test AiReliabilityEngineering.slnx
```

## Task 7: Update documentation

Update:

```text
README.md
docs/ai-providers.md
docs/wiki.md if it exists
```

Run:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
```

## Task 8: Verify existing CLI behavior

Run:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

Cleanup only if local runs are disposable:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Acceptance Criteria

The task is complete when all criteria are satisfied.

## Core contracts

- IAiProvider exists.
- AiRequest exists.
- AiResponse exists.
- AiMessage exists.
- AiRole exists.
- AiUsage exists.
- AiOutputFormat exists.
- AiProviderKind exists with Fake.
- AiProviderOptions exists with DefaultFake.
- AiProviderException exists.
- AiMessage rejects null content.
- AiProviderOptions rejects null, empty, or whitespace model.
- AiRequest rejects null, empty, or invalid message lists.
- AiRequest rejects null options.
- AiRequest stores a defensive copy of messages.
- AiResponse factory methods validate invalid inputs.

## Infrastructure

- FakeAiProvider exists.
- FakeAiProvider implements IAiProvider.
- FakeAiProvider is deterministic.
- FakeAiProvider supports Text output.
- FakeAiProvider supports Json output.
- FakeAiProvider checks cancellation at the start of GenerateAsync.
- FakeAiProvider does not call network.
- FakeAiProvider does not require API keys.
- AiProviderFactory exists.
- Default factory options create FakeAiProvider.

## Infrastructure test project

- AiReliabilityEngineering.Infrastructure.Tests exists if it did not already.
- It is added to AiReliabilityEngineering.slnx.
- It references Infrastructure and Core as needed.
- It runs as part of `dotnet test AiReliabilityEngineering.slnx`.

## Tests

- Core AI model tests pass.
- FakeAiProvider tests pass.
- AiProviderFactory tests pass.
- Existing CLI/orchestration/cleanup tests still pass.

## Documentation

- README mentions the AI provider layer.
- docs/ai-providers.md exists.
- docs/wiki.md is updated if it exists.
- Documentation clearly states that only FakeAiProvider exists in this step.
- Documentation clearly states that no API keys or network calls are required.
- Documentation explains that invalid AI request shapes are rejected at construction time.

## Existing workflow

These commands pass:

```bash
dotnet build
dotnet test AiReliabilityEngineering.slnx
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/idea.md
```

Cleanup command passes when executed against disposable run data:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- cleanup
```

---

# Notes for Codex

- Keep the implementation small.
- Do not introduce real provider dependencies.
- Do not add OpenAI SDK.
- Do not add Anthropic SDK.
- Do not add Gemini SDK.
- Do not add Ollama HTTP code.
- Do not add API key handling yet.
- Do not add config loading yet.
- Do not change fake workflow behavior.
- Do not move existing orchestration classes unless necessary.
- Keep tests deterministic.
- Use temporary directories in tests when file system access is needed.
- Save this file as UTF-8 without BOM.
