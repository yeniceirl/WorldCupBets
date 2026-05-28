# Technical Design: bootstrap-worldcupbets-architecture-scaffold

## Summary
Bootstrap a runnable day-one scaffold for WorldCupBets with strict layer boundaries, placeholder-only feature surfaces, and a local stack that proves composition without delivering business behavior.

## Architecture decisions

### Backend boundaries
- `WorldCupBets.Domain`: entities, value objects, domain events, base result/error types, and domain-facing abstractions only.
- `WorldCupBets.Application`: use-case contracts, commands/queries, validators, mapping registrations, and application service interfaces.
- `WorldCupBets.Infrastructure`: EF Core, repository implementations, Redis/cache adapters, JWT/Google auth adapters, and persistence/runtime extension methods.
- `WorldCupBets.WebApi`: minimal API endpoint placeholders, HTTP middleware, health checks, Swagger, and composition root.

### Contract placement
Service contracts move to `WorldCupBets.Application`, not Domain, to keep Domain focused on business model primitives and to avoid treating external workflows as domain concepts. Domain keeps only persistence-agnostic abstractions that are truly part of the model boundary.

## Folder layout

```text
/
├── src/
│   ├── WorldCupBets.Domain/
│   │   ├── Common/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   └── Events/
│   ├── WorldCupBets.Application/
│   │   ├── Abstractions/
│   │   ├── Behaviors/
│   │   ├── Contracts/
│   │   ├── Features/
│   │   └── DependencyInjection/
│   ├── WorldCupBets.Infrastructure/
│   │   ├── Authentication/
│   │   ├── Caching/
│   │   ├── Messaging/
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   └── DependencyInjection/
│   └── WorldCupBets.WebApi/
│       ├── Endpoints/
│       ├── Middleware/
│       ├── Configuration/
│       └── Extensions/
├── frontend/
│   ├── src/app/core/
│   ├── src/app/auth/
│   ├── src/app/features/matches/
│   ├── src/app/features/bets/
│   ├── src/app/features/leaderboard/
│   └── src/app/features/admin/
└── database/flyway/
```

## Composition root wiring
`Program.cs` in `WorldCupBets.WebApi` stays thin and calls layer-owned extension methods:
1. `AddApplication()` for validators, Mapster registrations, and Wolverine handler discovery.
2. `AddInfrastructure(configuration)` for DbContext, repositories, Redis, HybridCache, auth adapters, and Flyway/EF-related settings.
3. WebApi-local registration for Swagger, authentication/authorization middleware, exception handling, health checks, and endpoint mapping.

This keeps package references and concrete runtime choices out of Domain/Application while making startup readable.

## Auth wiring scope
Scaffold only the transport and integration seams:
- Backend: JWT bearer auth, Google token validation adapter interface/implementation skeleton, auth endpoint placeholder, and role policy registration.
- Frontend: auth shell, login callback placeholder, auth state service stub, and JWT interceptor skeleton.
- Out of scope: refresh-token flow, full user provisioning rules, persistent session UX, and protected feature implementation.

## EF Core and Flyway responsibilities
- EF Core owns schema evolution for tables, keys, indexes, and constraints.
- `Infrastructure/Persistence/AppDbContext` and `Configurations/` define future entity mapping.
- `Infrastructure/Persistence/Migrations/` exists but starts empty; startup may call `Database.Migrate()` so the pattern is wired without committing an initial migration.
- Flyway owns programmable SQL and repeatable assets only in `database/flyway/`.
- Docker Compose order: Postgres healthy → API starts and applies EF migrations if present → Flyway runs repeatables after API health.

This preserves the dual-migration split from the prompt while avoiding duplicate ownership of table schema.

## Frontend placeholder strategy
Use Angular standalone routing with:
- `core/` for app config, API base URL, interceptors, and shared guards.
- `auth/` for login/callback placeholder screens and auth wiring.
- Lazy feature folders for `matches`, `bets`, `leaderboard`, and `admin`, each exposing only shell route/components with placeholder copy.

Each placeholder route should compile, render, and clearly signal “scaffold only” to prevent accidental scope creep.

## Build and runtime assumptions
- Root repo is the project root and contains `WorldCupBets.sln`.
- .NET 10 SDK, Node/npm for Angular, Docker, and Docker Compose are available locally.
- Backend listens on its container port directly for Swagger; nginx proxies only `/api` and `/health` for SPA use.
- Redis is shared for HybridCache L2 and Wolverine transport.
- Environment secrets come from compose env vars or a local `.env`, not committed files.

## Data flow
1. Browser hits nginx-served SPA.
2. SPA calls `/api/*` through nginx to WebApi.
3. WebApi endpoint forwards request into Wolverine/Application handlers.
4. Handlers use Application/Domain abstractions; Infrastructure provides implementations.
5. Persistence uses EF Core + Postgres; cache/message fan-out uses Redis.
6. Flyway applies repeatable SQL objects separately from EF schema changes.

## Planned file changes
- Add root solution/build files: `WorldCupBets.sln`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, Docker assets.
- Add backend project skeletons and DI extension classes.
- Add Angular standalone scaffold plus nginx config.
- Add `database/flyway/` placeholder repeatable script or README-level marker.

## Validation approach
- `dotnet build WorldCupBets.sln`
- frontend install/build
- `docker compose config`
- smoke start of compose stack and verify SPA, `/health`, backend Swagger direct URL, and empty Flyway/EF startup path

## Rollout
Single scaffold change. No data migration rollout is needed because no initial EF migration is committed.

## Tradeoffs and risks
- Putting service contracts in Application improves use-case clarity but departs from the prompt’s suggestion that Domain may hold some interfaces; discipline is needed so repository/model abstractions stay in Domain when they are domain-facing.
- API-startup EF migration is convenient for local bootstrap but can be too implicit for production later.
- Redis dual use reduces infrastructure cost but couples cache and messaging availability.
- Keeping Swagger off nginx simplifies proxy rules but requires contributors to remember a separate backend URL.
- Placeholder-heavy frontend scaffolding can drift into pseudo-features unless labels and route shells stay explicit.
