# External Code Executor Demos

Local Git snapshot demo:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-review-git --provider fake
```

Optional external executor demos:

```bash
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-opencode --provider fake
dotnet run --project src/AiReliabilityEngineering.Cli -- run samples/redis-ttl-audit.md --profile ai-demo-dotnet-codex --provider fake
```

OpenCode and Codex demos require the corresponding CLI tool to be installed and configured. Command syntax may require adjustment depending on the installed version.
