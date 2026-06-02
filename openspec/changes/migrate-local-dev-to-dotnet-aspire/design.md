# Design: Migrate Local Development to .NET Aspire

## Technical Approach

Local development now starts from `src/WorldCupBets.AppHost`, an Aspire AppHost that orchestrates PostgreSQL, Redis, the existing Web API, and the Angular dev server. Aspire owns service startup, dependency ordering, endpoints, health visibility, and local configuration injection. The browser still talks to relative `/api` paths; Angular's dev-server proxy forwards those requests to the Aspire-provided API endpoint. This satisfies the local-development spec without defining production deployment.

## Architecture Decisions

| Decision | Choice | Alternatives considered | Tradeoff / rationale |
|---|---|---|---|
| Canonical local entrypoint | Use `WorldCupBets.AppHost` as the only documented full-stack startup path. | Keep Docker Compose/nginx alongside Aspire. | One path reduces debugging ambiguity and matches the requirement to remove Compose/nginx from local dev; rollback remains possible through version control. |
| Secrets and local config | Read AppHost user-secrets/env vars, fail fast for `DB_PASSWORD` and `JWT_SECRET`, pass `Jwt__Secret`, Google client id, and dev-login flags downstream. | Let WebApi/frontend discover all local configuration independently. | Centralizes local wiring and gives clear missing-secret errors. JWT length is still enforced by `JwtTokenGenerator` at token generation time. |
| Frontend hosting | Run `npm start` via `AddNpmApp`; `scripts/start-dev.js` generates `.generated/env.js` and starts `ng serve` with `proxy.conf.js`. | Build frontend into WebApi or keep nginx container. | Preserves Angular hot reload and relative API URLs while making the frontend visible in Aspire. Requires Node/npm availability in the AppHost process environment. |
| Database migration policy | WebApi applies EF Core migrations on startup; Flyway is manual only for local SQL artifacts. | Restore Flyway as automatic local schema path. | Keeps the application model authoritative for normal local schema setup while preserving a manual escape hatch for views/procedures. |
| Production boundary | Do not use this change to define production orchestration. | Treat Aspire as the production plan. | Avoids mixing local observability work with deployment architecture decisions. |

## Data Flow

```text
Developer ──dotnet run──> AppHost dashboard
  AppHost ──starts/wires──> PostgreSQL + Redis
  AppHost ──config/env────> WebApi ──EF migrations──> PostgreSQL
  AppHost ──ASPIRE_API_URL> Angular dev server
Browser ──/api/*─────────> Angular proxy ───────────> WebApi
```

## File Changes

| File | Action | Description |
|---|---|---|
| `src/WorldCupBets.AppHost/Program.cs` | Create | Defines Aspire resources, required local secrets, fixed local ports, API health check, dependency waits, and frontend npm process. |
| `src/WorldCupBets.AppHost/WorldCupBets.AppHost.csproj` | Create | AppHost project using Aspire AppHost, NodeJs, PostgreSQL, Redis, and user-secrets packages. |
| `WorldCupBets.sln` | Modify | Includes the AppHost project in the solution. |
| `frontend/scripts/start-dev.js` | Create | Generates runtime frontend env and launches Angular dev server with the proxy. |
| `frontend/proxy.conf.js` | Create | Proxies `/api` and `/health` to `ASPIRE_API_URL`, defaulting to `http://localhost:5000`. |
| `frontend/angular.json` | Modify | Serves generated `env.js` from `.generated` as a frontend asset. |
| `frontend/package.json` | Modify | Makes `npm start` run the Aspire-compatible dev startup script. |
| `docs/local-development.md` | Modify | Documents Aspire startup, required secrets, ports, proxy behavior, migration policy, and Rider/npm caveat. |
| `src/WorldCupBets.WebApi/Program.cs` | Existing behavior | API startup applies migrations before mapping endpoints. |
| `src/WorldCupBets.Infrastructure/Authentication/JwtTokenGenerator.cs` | Existing behavior | Validates JWT secret presence and minimum HS256 byte length. |
| local Compose/nginx/Dockerfile artifacts | Delete | Removed as local development entrypoints. |

## Interfaces / Contracts

Local configuration contract:

- Required for AppHost: `DB_PASSWORD`, `JWT_SECRET`.
- Optional: `DB_USERNAME` (`app` default), `GOOGLE_CLIENT_ID` (empty default), `ENABLE_DEV_LOGIN` (`true` default).
- AppHost-to-API: `Jwt__Secret`, `Google__ClientId`, Aspire connection strings `DefaultConnection` and `Redis`.
- AppHost-to-frontend: `ASPIRE_API_URL`, `GOOGLE_CLIENT_ID`, `ENABLE_DEV_LOGIN`, `PORT`.

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | JWT secret validation remains strict. | Existing code path; add focused unit coverage if test projects are introduced. |
| Integration | AppHost starts Postgres, Redis, API, frontend; API waits for dependencies and reports `/health`. | Manual verification through `dotnet run --project src/WorldCupBets.AppHost` and Aspire dashboard. |
| E2E | Frontend loads on `4200`, dev login works, `/matches` calls route through `/api`. | Browser smoke test against the Aspire-started stack. |

## Migration / Rollout

No data migration required. Developers set local secrets once, run the AppHost, and stop using Compose/nginx for local development. Production rollout is excluded.

## Open Questions

None.
