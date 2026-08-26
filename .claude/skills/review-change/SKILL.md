---
name: review-change
description: Review the current branch against origin/main using targeted read-only subagents before shipping — use before committing or opening a PR for GoHardAPI changes.
---

# Review Change

## Main session (do first, no delegation)
- Diff the current branch against `origin/main`; read the actual diff.
- Run/read relevant fresh test output for the changed area.
- Build the exact file list and relevant diff hunks to hand to any reviewer — don't ask a reviewer to rediscover the diff.

## Reviewer selection (max 2 agents)
Select only by what changed — never invoke an agent whose category doesn't match:
- Controller, auth, authorization, user-data, upload, or sensitive-data changes → `security-reviewer`.
- Route, DTO, request/response, or status-code changes → `api-contract-reviewer`.
- EF Core, services, startup, dependency lifetime, or database changes → `architecture-reviewer`.
- Docs/config/CI-only changes → no subagent; do a concise main-session review instead.
- If nothing matches a category, don't force an unrelated agent onto the review.

Reviewers are read-only (plan mode, `Read`/`Grep`/`Glob` only) — pass them the file list, diff content, and test results directly; they cannot run commands or explore beyond that.

## Reporting
- Bucket findings as blocking / non-blocking / informational.
- Call out any test that could still pass if the underlying behavior were broken (weak coverage).
- End with an explicit **safe to ship** or **unsafe to ship** verdict.
