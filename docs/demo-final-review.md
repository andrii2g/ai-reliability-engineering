# Final Review Demo

Run the most complete local fake-provider demo:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review --provider fake
```

Expected review files:

```text
reports/final-review.md
reports/workspace-summary.md
```

This profile runs requirements, documentation, planning, deterministic .NET workspace generation, build/test validation, and deterministic final review.
