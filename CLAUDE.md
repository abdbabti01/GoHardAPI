# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GoHardAPI is an ASP.NET Core 8.0 Web API for a fitness tracking platform with an offline-first mobile client (`GoHardAPP/`, Flutter). It covers workout sessions, exercises, structured programs, goals, body metrics, nutrition tracking, social/messaging features, and AI chat coaching (multiple providers, configured via `appsettings.json`).

## Architecture & Boundaries

- Keep controllers thin; put reusable business logic in services.
- Use DTOs for every external request/response; never return EF entities directly.
- Get user identity only from validated JWT claims (`ClaimTypes.NameIdentifier` via `GetCurrentUserId()`) — never trust a client-supplied user id.
- Every user-scoped query or command must filter by the authenticated user's id:
  ```csharp
  var userId = GetCurrentUserId();
  var items = await _context.Items.Where(i => i.UserId == userId).ToListAsync();
  ```
- Use async EF Core calls (`ToListAsync`, `FirstOrDefaultAsync`, etc.) and pass `CancellationToken` through wherever the surrounding signature already supports it.

## Authentication

- JWT: HMAC SHA256, Issuer `GoHardAPI`, Audience `GoHardApp`, 720h expiry.
- Passwords hashed with BCrypt.Net-Next; verification is constant-time.
- `POST /api/auth/login` and `/api/auth/signup` are the only endpoints that don't require `[Authorize]`.

## Database

### Providers

- Local development: SQL Server (`MSI\MSSQLSERVER01`, database `TrainingAppDb`).
- Production (Railway): PostgreSQL, auto-selected from the `DATABASE_URL` environment variable.
- Any schema or query change must work correctly on both providers — verify before merging.

### EF Core rules

- Filter every query by `UserId` for user-owned data; no cross-user access via id manipulation.
- Use `.Include()` / `.ThenInclude()` for related data; watch for N+1 query patterns.
- Use `AsNoTracking()` for read-only queries that don't need change tracking.
- All `DateTime` fields are UTC (`DateTime.UtcNow`) — required for PostgreSQL compatibility.

### Migration safety

- Never delete or rewrite an already-applied migration.
- Never manipulate migration history automatically (no runtime inserts/deletes against `__EFMigrationsHistory`).
- Never apply a migration to production without explicit approval.
- Always inspect a generated migration before applying it.
- Standard flow:
  ```bash
  dotnet ef migrations add <MigrationName> --project GoHardAPI
  dotnet ef database update --project GoHardAPI
  ```

**Known unsafe technical debt:** Program.cs currently contains startup migration-history, schema-patching, and cascade-repair behavior that is known unsafe production technical debt. Do not extend, copy, or use this behavior as a pattern for new migrations. Removing or replacing it must be an explicitly requested, isolated `database-change` task with migration-history analysis, SQL Server/PostgreSQL compatibility review, rollback notes, tests, and architecture review — not something to change incidentally while doing unrelated work.

## Cross-Project Contract (GoHardAPP/)

- Any change to a route, request/response shape, or JSON field must be checked against `GoHardAPP/` for consumers (Dart models, serialization, repository calls, providers) and updated to match.
- Preserve JSON field names and nullability unless the task explicitly changes the contract.
- Consider compatibility with cached and unsynchronized mobile data when changing response shapes.

## Build & Verify

```bash
dotnet restore
dotnet build GoHardAPI.sln
dotnet test GoHardAPI.Tests/GoHardAPI.Tests.csproj
dotnet publish GoHardAPI/GoHardAPI.csproj -c Release -o ./publish-test
```

Run the API locally with `dotnet run` from `GoHardAPI/` (listens on `http://0.0.0.0:5121`); Swagger UI is at `/swagger` for interactive testing and JWT authorization.

## Definition of Done

- The requested behavior is implemented; unrelated code is untouched.
- Tests are added or updated for changed behavior and pass.
- Build and tests pass using the commands above.
- API contract changes are reflected in `GoHardAPP/` consumers.
- No secrets, generated artifacts, or unrelated files are included in the change.
