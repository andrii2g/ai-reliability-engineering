# ArtifactReviewAgent

`ArtifactReviewAgent` performs deterministic review of generated run output.

It does not call AI.

The agent checks required artifacts, builds a workspace file summary, and writes:

```text
reports/final-review.md
reports/workspace-summary.md
```

Missing files are warnings, not automatic review failures. Earlier pipeline steps are responsible for failing the run when generation, build, or test execution fails.

`workspace-summary.md` may include the absolute workspace root only in the `Workspace Root` section. Generated file entries are relative to the workspace root, use `/` separators, and are sorted deterministically.
