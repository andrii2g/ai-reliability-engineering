# Git Workspace Snapshot

The `ai-demo-dotnet-review-git` profile adds a finalization step after review.

`GitWorkspaceSnapshotAgent` works only inside the generated run workspace. It may run `git init` when the workspace has no `.git` directory, then runs `git status --short` and writes reports under `reports/`.

Generated reports:

- `reports/generated-files.md`
- `reports/generated-files.json`
- `reports/git-status.md`

The snapshot is reporting-only. It does not commit, push, create branches, or create pull requests.

Transient paths are filtered from generated file and git status reports:

- `.git/`
- `bin/`
- `obj/`
- `.vs/`
- `.idea/`

If Git is unavailable or a Git command fails, finalization still succeeds when reports can be written. The report records the command failure.
