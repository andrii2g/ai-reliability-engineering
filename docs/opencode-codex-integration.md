# OpenCode and Codex Integration

AIRE includes opt-in profiles for local external coding agents:

- `ai-demo-dotnet-opencode`
- `ai-demo-dotnet-codex`

These profiles require the matching CLI tool to be installed and configured by the developer. They are not used by default, and automated tests do not require either tool.

External executors run only inside the generated run workspace. AIRE does not pass provider API keys or environment variables in the prompt.

The initial command mapping is provisional and isolated in each executor:

- OpenCode: `opencode run <prompt-file-path>`
- Codex: `codex exec <prompt-file-path>`

Installed tool versions may require command mapping changes later. The mapping is intentionally isolated so it can be adjusted without changing orchestration.
