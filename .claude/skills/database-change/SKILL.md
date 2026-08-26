---
name: database-change
description: Plan and implement EF Core model, DbContext, or migration changes for GoHardAPI — use for schema changes, new migrations, or provider-specific (SQL Server/PostgreSQL) database work.
---

# Database Change

CLAUDE.md documents provider switching, EF Core rules, and migration safety — read it first, especially the Database section.

## Start here
1. Investigate: read `Program.cs` provider-selection logic, `Data/TrainingContext.cs`, and existing migration history before writing any code.
2. Write a short plan covering what changes, why, which provider(s) it affects, and the rollback approach — before editing.

## Constraints
- Support both SQL Server (local) and PostgreSQL/Railway (production); confirm the change is valid on both.
- Never delete, rewrite, or auto-repair an already-applied migration.
- Never add new startup logic that manipulates migration history, deletes schema, or automatically repairs schema/data.
- Existing Program.cs migration-history, schema-patch, and cascade-repair behavior is known unsafe technical debt. Do not extend or copy that pattern.
- Removing or replacing that existing behavior is allowed only when explicitly requested by the user. Treat it as an isolated remediation task with:
  - investigation and a written plan before editing,
  - migration-history analysis (what state each environment is actually in),
  - SQL Server/PostgreSQL compatibility review,
  - rollback and deployment notes,
  - relevant tests,
  - mandatory `architecture-reviewer` review before shipping.
- Never apply a migration to production yourself, and never execute destructive or production database actions without explicit approval.
- Keep database work isolated — don't bundle it with unrelated controller/service changes.

## Required output
- Rollback notes (how to reverse the migration safely).
- Deployment notes (ordering, whether it's breaking for running instances).
- Compatibility notes (mobile cached data, existing rows, nullable defaults).

Stop and ask before any destructive operation (dropping columns/tables, data-loss migrations, manual production DB commands). Hand off to `review-change` (architecture-reviewer) before `ship-change`.
