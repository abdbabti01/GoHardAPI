---
name: api-change
description: Implement controller, DTO, service, and repository changes for GoHardAPI endpoints — use when adding or modifying API endpoints, request/response contracts, or business logic.
---

# API Change

Applies to controller, DTO, service, and repository work. CLAUDE.md is canonical for stack conventions, JWT config, and entity relationships — read it first.

## Before editing
- Inspect existing controllers/services for the established pattern (thin controller, service holds logic).
- Identify all consumers: routes, DTOs, and — if `GoHardAPP/` is present — Flutter models/repositories/providers that call the changed endpoint.

## Implementation rules
- Keep controllers thin; put reusable business logic in services.
- Use async EF Core calls (`ToListAsync`, `FirstOrDefaultAsync`, etc.) and pass `CancellationToken` through wherever the surrounding method signature already supports it.
- Use DTOs for every external request/response; never return EF entities directly from new or modified endpoints.
- Get user identity only from validated JWT claims (`ClaimTypes.NameIdentifier` via `GetCurrentUserId()`); scope every user-owned query/command to that user.
- Preserve existing routes and JSON field names/nullability unless the task explicitly changes the contract.
- Do not touch migrations unless explicitly requested — that's `database-change`.

## Required checks before calling it done
- Authentication and authorization present on the endpoint.
- Ownership check on every user-scoped read/write (no cross-user access via id).
- No mass assignment (DTO binds only intended fields).
- No sensitive-field exposure (password hash, tokens, other users' PII) in responses.
- If `GoHardAPP/` exists, search it for consumers of the changed endpoint/DTO and update Dart models/serialization/repositories/tests to match.
- Run targeted tests for the changed area before handing off.

Use `review-change` before shipping; use `ship-change` to commit/push.
