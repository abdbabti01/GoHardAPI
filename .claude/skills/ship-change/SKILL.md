---
name: ship-change
description: Commit, verify, and push GoHardAPI changes via Git — use when the user asks to commit, ship, or open a PR for reviewed changes.
---

# Ship Change

Claude performs Git operations directly for this skill.

## Before staging
- Inspect current branch and working tree (`git status`, `git branch --show-current`).
- Never commit on `main` or `master` — stop and ask if that's the current branch.
- Identify exactly which files are intended vs. unrelated tracked/untracked files that must be preserved untouched.

## Staging
- Stage only the exact intended files by name.
- Never use: `git add .`, `git add -A`, `git commit -a`, `git reset --hard`, or force push.
- Run `git diff --check` and read the staged diff before committing.

## Verification
- **Production-code changes:**
  ```
  dotnet build GoHardAPI.sln
  dotnet test GoHardAPI.Tests/GoHardAPI.Tests.csproj
  dotnet publish GoHardAPI/GoHardAPI.csproj -c Release -o ./publish-test
  ```
  Delete `./publish-test` only after confirming publish succeeded — it's reproducible output, nothing else.
- **Docs/config-only changes:** validate frontmatter/config syntax and inspect the diff; a full build isn't required unless application behavior is affected.

## Stop conditions
Stop and report instead of proceeding if: tests fail, an unexpected file is staged, the remote rejects the push, or completing the push would require force.

## Finishing
- Commit and push normally.
- Open a PR only if GitHub CLI is installed and authenticated; otherwise skip and report it.
- Never merge unless explicitly requested.
- Report: branch, commit hash, exact files changed, verification performed, remote status, final `git status`.
