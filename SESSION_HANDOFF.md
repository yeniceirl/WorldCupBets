# Session Handoff

## Current Status

The local development workflow has been migrated to .NET Aspire and is now running from Rider.

Aspire is the canonical local entrypoint. The old Docker Compose/nginx local path was removed.

## Quick Start

Start the stack from Rider using the `WorldCupBets.AppHost` project.

The AppHost reads local secrets from .NET user-secrets:

```bash
dotnet user-secrets set DB_PASSWORD "123" --project src/WorldCupBets.AppHost
dotnet user-secrets set JWT_SECRET "local-dev-jwt-secret-32-chars-ok" --project src/WorldCupBets.AppHost
```

Expected Aspire resources:

- `postgres`
- `redis`
- `api`
- `frontend`

## Decisions Made

| Topic | Decision |
|-------|----------|
| Local orchestration | Use .NET Aspire instead of Docker Compose. |
| Frontend local hosting | Run Angular as an Aspire-managed Node/npm process. |
| Browser-to-API routing | Keep frontend calls relative (`/api/...`) and use Angular dev-server proxy. |
| Database schema | EF Core migrations run automatically when the API starts in development. |
| Flyway | Keep manual/local only for SQL artifacts such as views or stored procedures. |
| Local secrets | Use AppHost user-secrets, not committed `launchSettings.json` values. |
| Production deployment | Still intentionally undecided. Design later. |

## Resolved Issues

- Rider can start the full Aspire stack.
- `npm` lookup from Rider was solved through Rider environment setup, not AppHost workaround code.
- `dev-login` now works.
- JWT secret requirement is 32+ UTF-8 bytes for HS256 with the current IdentityModel packages.
- Wolverine 6 requires explicit service-location policy for the current handler/repository/EF setup.

## Important Files

- `src/WorldCupBets.AppHost/Program.cs` — Aspire topology and resource wiring.
- `src/WorldCupBets.AppHost/WorldCupBets.AppHost.csproj` — AppHost project and user-secrets ID.
- `src/WorldCupBets.WebApi/Program.cs` — Wolverine configuration.
- `src/WorldCupBets.Infrastructure/Authentication/JwtTokenGenerator.cs` — JWT secret validation.
- `frontend/scripts/start-dev.js` — Angular dev startup wrapper.
- `frontend/proxy.conf.js` — dev-server proxy for `/api` and `/health`.
- `docs/local-development.md` — current local development instructions.
- `openspec/changes/migrate-local-dev-to-dotnet-aspire/proposal.md` — Aspire migration proposal.

## Next Session Plan

1. Verify current user-facing flow once from Rider:
   - login with development login
   - load `/matches`
   - confirm summary, champion market, and match list load without 500s
2. Clean the worktree into reviewable units:
   - Aspire migration/local dev cleanup
   - Wolverine/package upgrade fixes
   - unrelated product/backend/frontend changes, if any
3. Complete the OpenSpec artifacts for the Aspire migration:
   - `spec.md`
   - `design.md`
   - `tasks.md`
4. Decide whether to commit the Aspire migration as one work unit or split it from unrelated package/product changes.

## Watch Outs

- Do not reintroduce Docker Compose as local fallback unless there is a new explicit decision.
- Do not store `DB_PASSWORD` or `JWT_SECRET` in committed files.
- If Rider fails to find `npm`, check the AppHost run configuration environment before changing AppHost code.
- If JWT errors return, first check the effective secret length in the AppHost/API environment.
