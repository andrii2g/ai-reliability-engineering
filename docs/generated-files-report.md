# Generated Files Report

The generated files report summarizes files under the run workspace.

Paths are relative to `workspace/`, use `/` separators, and are sorted deterministically. The JSON report includes file count, total size in bytes, and file entries.

Transient build and tool directories are excluded:

- `.git/`
- `bin/`
- `obj/`
- `.vs/`
- `.idea/`

Reports are written by the finalization step for profiles that include `GitWorkspaceSnapshotAgent`.
