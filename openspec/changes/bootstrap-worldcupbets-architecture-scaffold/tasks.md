# Implementation Tasks: bootstrap-worldcupbets-architecture-scaffold

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 700-1,100 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 bootstrap/repo skeleton → PR 2 backend core/application → PR 3 infrastructure/web API → PR 4 frontend scaffold → PR 5 containers/runtime docs |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

## Notes
- `openspec/config.yaml` reports no reliable test runner and `strict_tdd: false`; use build/smoke/manual verification tasks instead of RED/GREEN test-first sequencing.
- Keep each PR scaffold-only: no initial EF migration, no feature-complete workflows, no nginx proxying beyond `/api` and `/health`.
- Aim for ~150-300 changed lines per PR by preferring placeholders, README markers, and thin DI/composition seams.

## Task Plan

1. **PR 1 — Establish repository/bootstrap skeleton**
   - Files: `WorldCupBets.sln`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, `src/`, `frontend/`, `database/flyway/README.md` or equivalent marker.
   - Work: create the root scaffold, empty backend project directories/files, and shared repo conventions without adding runtime wiring yet.
   - Verify: `dotnet sln list` shows the intended solution shell once projects exist; root tree contains `src/`, `frontend/`, and `database/`.
   - Rollback boundary: revert root-level scaffold/config files only.

2. **PR 2 — Scaffold Domain and Application layer seams**
   - Files: `src/WorldCupBets.Domain/**/*.cs`, `src/WorldCupBets.Application/**/*.cs`, `src/WorldCupBets.Application/DependencyInjection/*.cs`, corresponding `*.csproj` files.
   - Work: add minimal project files, core domain primitives (`Common/`, `Entities/`, `ValueObjects/`, `Events/`), application abstractions/contracts/feature placeholders, and `AddApplication()` registration stub. Keep service contracts in `WorldCupBets.Application`.
   - Verify: `dotnet build WorldCupBets.sln`; inspect project references so Domain has no Infrastructure/WebApi dependency and Application depends only on Domain.
   - Rollback boundary: revert Domain/Application projects without touching runtime/container files.

3. **PR 3 — Scaffold Infrastructure and persistence/auth/cache adapters**
   - Files: `src/WorldCupBets.Infrastructure/**/*.cs`, especially `Authentication/`, `Caching/`, `Messaging/`, `Persistence/AppDbContext.cs`, `Persistence/Configurations/`, `Persistence/Migrations/`, `DependencyInjection/*.cs`, plus `WorldCupBets.Infrastructure.csproj`.
   - Work: add infrastructure DI extension, placeholder EF Core context/configuration path, Redis/HybridCache seams, JWT bearer and Google validation adapter skeletons, and explicit empty migrations path with no initial migration file.
   - Verify: `dotnet build WorldCupBets.sln`; confirm `Persistence/Migrations/` exists and contains no generated migration.
   - Rollback boundary: revert Infrastructure project only.

4. **PR 4 — Add Web API composition root and placeholder endpoints**
   - Files: `src/WorldCupBets.WebApi/Program.cs`, `Endpoints/**/*.cs`, `Middleware/**/*.cs`, `Configuration/**/*.cs`, `Extensions/**/*.cs`, `WorldCupBets.WebApi.csproj`.
   - Work: wire thin `Program.cs`, call `AddApplication()` and `AddInfrastructure(configuration)`, add Swagger/auth/authorization/health/global exception placeholder setup, and map scaffold-only auth/health placeholder endpoints.
   - Verify: `dotnet build WorldCupBets.sln`; manual startup smoke should expose `/health` and direct Swagger backend URL while keeping endpoint payloads obviously placeholder-only.
   - Rollback boundary: revert Web API project files only.

5. **PR 5 — Add Angular standalone scaffold and auth shell placeholders**
   - Files: `frontend/package.json`, `frontend/angular.json`, `frontend/src/main.ts`, `frontend/src/app/core/**`, `frontend/src/app/auth/**`, `frontend/src/app/features/matches/**`, `bets/**`, `leaderboard/**`, `admin/**`.
   - Work: scaffold the standalone app shell, app config/router, auth state and JWT interceptor skeletons, and lazy placeholder routes/components for the four feature areas with explicit scaffold-only copy.
   - Verify: frontend install/build command defined by generated Angular workspace succeeds; route inspection shows lazy placeholders for `matches`, `bets`, `leaderboard`, and `admin` only.
   - Rollback boundary: revert `frontend/` only.

6. **PR 6 — Add container/runtime/bootstrap wiring**
   - Files: `docker-compose.yml` or `compose.yaml`, backend `Dockerfile`, frontend `Dockerfile`, frontend/nginx config such as `frontend/nginx.conf`, `.env.example`, `database/flyway/**/*`, optional bootstrap README paths.
   - Work: define local services for backend, frontend, PostgreSQL, Redis, and Flyway; constrain nginx proxying to `/api` and `/health`; keep Swagger direct on backend; add Flyway repeatable-script placeholder or marker only.
   - Verify: `docker compose config`; manual review confirms nginx only proxies `/api` and `/health`, SPA fallback remains enabled, and service order documents Postgres → API → Flyway intent.
   - Rollback boundary: revert container/bootstrap/database runtime files only.

7. **Final integration pass — keep the chain review-safe**
   - Discovery targets: all files added in PRs 1-6, plus `openspec/changes/bootstrap-worldcupbets-architecture-scaffold/design.md` and `specs/architecture-scaffold/spec.md`.
   - Work: before each apply step, compare planned diff size against the 300-line target, trim placeholder content where possible, and defer nonessential niceties (extra docs, sample payloads, extra helper types) into follow-up changes if any PR drifts upward.
   - Verify: each PR remains independently buildable/reviewable; no PR introduces initial EF migration, feature logic, or expanded nginx proxy scope.
   - Rollback boundary: drop the specific oversize PR from the chain without invalidating already-merged earlier scaffold layers.
