# Proposal: Bootstrap WorldCupBets architecture scaffold

## Problem Statement
The repository does not yet contain the agreed day-one architecture scaffold for WorldCupBets. We need a concrete, reviewable bootstrap plan that establishes the root layout, backend/frontend project boundaries, runtime wiring, and migration approach before feature work begins.

## Intent
Create a runnable architectural scaffold for the WorldCupBets solution that reflects the project brief and clarified decisions, while intentionally stopping at placeholders and skeletons instead of feature implementation.

## Scope
- Keep the current repository root as the project root.
- Add `src/`, `frontend/`, `database/`, and `WorldCupBets.sln` at the root.
- Scaffold the four-project backend Clean Architecture layout.
- Place service contracts in `WorldCupBets.Application`.
- Set up a runnable day-one Docker/runtime flow for backend, frontend, PostgreSQL, Redis, and Flyway.
- Configure EF Core migration support without generating an initial migration.
- Scaffold Angular app structure, auth shell, and route/folder placeholders for non-auth features.
- Configure nginx to proxy only `/api` and `/health`; Swagger remains direct on the backend.

## Affected Areas
- Root solution and shared build/config files.
- `src/WorldCupBets.Domain`
- `src/WorldCupBets.Application`
- `src/WorldCupBets.Infrastructure`
- `src/WorldCupBets.WebApi`
- `database/flyway`
- `frontend`
- Docker and nginx runtime configuration.

## Proposed Changes
1. Create the solution scaffold and project references for Domain, Application, Infrastructure, and WebApi.
2. Add shared repository/build conventions (`Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`).
3. Establish minimal backend skeletons for Result/Error, base entity support, lookup model, application contracts/middleware stubs, infrastructure persistence/cache/auth wiring, and WebApi composition root.
4. Configure WebApi startup for Wolverine, FluentValidation, Mapster, HybridCache, Redis, JWT auth, Swagger, global exception handling, and `/health`.
5. Add Docker assets so the stack is runnable on day one, including backend image, frontend image, compose orchestration, and Flyway placeholder SQL.
6. Scaffold Angular standalone application structure with auth shell, JWT interceptor skeleton, lazy routes, and placeholder feature folders/routes for matches, bets, leaderboard, and admin.
7. Configure frontend nginx for SPA fallback plus proxying of `/api` and `/health` only.
8. Prepare EF Core migrations infrastructure in the expected path, but do not create an initial migration file.

## Risks
- Day-one runtime wiring spans multiple technologies, so bootstrap misconfiguration could delay first-run success.
- Keeping Swagger off nginx means local developer docs use the backend port directly, which must be documented consistently.
- Placeholder-heavy frontend scaffolding may tempt later work to bypass agreed feature boundaries unless naming and folders are clear.

## Rollback
- Remove the scaffolded solution, frontend, database, and container files introduced by this change.
- Revert root-level build/config additions.
- No data rollback is needed because no EF migration will be generated in this change.

## Success Criteria
- The repository contains the agreed root structure and OpenSpec-approved scaffold plan.
- Backend projects and references reflect the intended Clean Architecture boundaries, with service contracts in Application.
- Docker/runtime assets are defined for a runnable day-one stack.
- EF Core is configured for future migrations, but no initial migration is present.
- Frontend auth skeleton exists and other feature areas remain placeholders only.
- nginx proxies `/api` and `/health` only, while Swagger remains directly exposed by the backend.
