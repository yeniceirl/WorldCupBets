# Exploration: player-squad-sync — persisted player squad index synced from /admin

## Current State

`ApiSportsFootballPlayerSearchProvider.SearchAsync` (`src/WorldCupBets.Infrastructure/ExternalFootball/ApiSportsFootballPlayerSearchProvider.cs:19-50`) builds a squad index lazily via `HybridCache.GetOrCreateAsync` keyed by a constant key. On a cache miss, `BuildPlayerIndexAsync` (1) reads the persisted `ExternalFootballSnapshot` for team names, (2) calls `/teams?search={name}` to resolve each included team's API-Sports id, (3) calls `/players/squads?team={id}`, then flattens/dedupes in memory. Consequences: every API instance independently rebuilds the index on its own cache miss (duplicated external calls across instances), restarts/evictions trigger fresh API-Sports calls uncontrollably (burns the 100 req/day quota), and failures are silent (`EnsureSuccessStatusCode()` throws inside the cache factory — search just fails with no admin-visible diagnostics).

The existing football-data sync pattern is the template to mirror: `SyncFootballDataCommand` (empty record) → `SyncFootballDataHandler.Handle` calls `IFootballDataProvider.GetSnapshotAsync` then `IExternalFootballDataRepository.ReplaceSnapshotAsync` (delete-existing + insert-new, transactional via `SaveChangesAsync`) → returns `SyncFootballDataResultDto(ProviderName, TeamsCount, StadiumsCount, GroupsCount, MatchesCount, SyncedAtUtc)`. Endpoint is `POST /api/football-data/sync` with `[Authorize(Policy = "Admin")]` on the `/api/football-data` group. Frontend wires `MatchesService.syncFootballData()` to `AdminPageComponent.syncFootballData()` with an `isSyncingFootballData` signal and success/error message signals.

Persistence schema: one table per concept (`external_football_teams`, `_stadiums`, `_group_standings`, `_matches`), each with `(ProviderName, ExternalId)` unique index, `SyncedAtUtc` per row, full delete+insert on sync, EF configs in `Persistence/Configurations/`. Migration: `20260603033649_AddExternalFootballDataCache`.

DI (`InfrastructureServiceCollectionExtensions.AddExternalFootballData`): `IPlayerSearchProvider` is registered conditionally — `EmptyPlayerSearchProvider` when `ApiKey` is blank, else `ApiSportsFootballPlayerSearchProvider` wired with `HttpClient`, `ExternalFootballDataOptions`, `IExternalFootballDataRepository`, `HybridCache`.

## Affected Areas

- `src/WorldCupBets.Infrastructure/ExternalFootball/ApiSportsFootballPlayerSearchProvider.cs` — change `SearchAsync` from "build index on cache miss" to "query persisted DB rows"; team-resolution + squad-fetch logic moves into a new sync handler
- `src/WorldCupBets.Infrastructure/ExternalFootball/ApiSportsFootballOptions.cs` — `SquadCacheHours` becomes a thin perf-cache TTL or can be dropped
- `src/WorldCupBets.Domain/Entities/` — new `ExternalFootballPlayer` entity (mirror `ExternalFootballTeam`: `ProviderName`, `ExternalId`, `Name`, `NormalizedName`, `TeamExternalId`, `TeamName`, `Position`, `PhotoUrl`, `SyncedAtUtc`)
- `src/WorldCupBets.Infrastructure/Persistence/Configurations/` — new `ExternalFootballPlayerConfiguration` (table `external_football_players`, unique index `(ProviderName, ExternalId)`, secondary index on `NormalizedName`)
- `src/WorldCupBets.Infrastructure/Persistence/Migrations/` — new EF migration adding the table
- `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` — new `DbSet<ExternalFootballPlayer>`
- `src/WorldCupBets.Application/Abstractions/IExternalFootballDataRepository.cs` (or sibling repository) — needs a way to replace/read persisted player rows
- `src/WorldCupBets.Application/Features/FootballData/` — new `SyncPlayerSquadsCommand`/`Handler`/`ResultDto` mirroring the football-data sync trio
- `src/WorldCupBets.WebApi/Endpoints/FootballDataEndpoints.cs` — new `POST /api/football-data/players/sync` `[Authorize(Policy = "Admin")]`
- `src/WorldCupBets.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` — DI registration changes
- `frontend/src/app/features/matches/matches.service.ts` (or `admin.service.ts`) — new `syncPlayerSquads()` method
- `frontend/src/app/features/admin/admin-page.component.ts` — new "Sync players" button + result/error display mirroring `syncFootballData()`
- `frontend/.../*.models.ts` — new result DTO interface

## Approaches

### 1. New dedicated table + sync command/handler mirroring the football-data pattern (RECOMMENDED)

- Add `ExternalFootballPlayer` entity + `external_football_players` table (own migration); new `SyncPlayerSquadsCommand`/`Handler`/`ResultDto` that resolves team ids (caching the resolution to avoid redundant `/teams?search=` calls across syncs), fetches squads, replaces rows for the provider, returns counts + per-team errors + timestamp; rewrite `SearchAsync` as a pure DB query (optionally wrapped in a short HybridCache TTL purely as a read-through perf layer over DB reads, never over external calls)
- Pros: clean separation; mirrors an established, well-understood pattern almost verbatim; indexed DB query scales better than in-memory scan; survives restarts; trivially shared across instances; natural per-team partial-failure reporting
- Cons: requires new migration + table/repository — more new code than reusing existing schema
- Effort: Medium

### 2. Extend `ExternalFootballSnapshot`/`external_football_data` schema to also carry players, reusing the existing sync command

- Pros: reuses the existing trigger; one "sync" concept for the admin
- Cons: conflates two independently rate-limited providers behind one button — `IFootballDataProvider` (worldcup26.ir, free) vs. API-Sports (100 req/day quota). An admin clicking "Sync provider" expecting a free refresh would unknowingly burn API-Sports quota. Breaks `SyncFootballDataHandler`'s single responsibility and is more invasive (touches `ExternalFootballSnapshot`, `ReplaceSnapshotAsync`, `SyncFootballDataResultDto`, existing UI flow)
- Effort: Medium-High

## Recommendation

**Approach 1.** Keep API-Sports quota concerns explicit and isolated from the free worldcup26.ir sync; mirror the proven pattern almost verbatim; make `ApiSportsFootballPlayerSearchProvider` a pure DB-read, eliminating duplicated cross-instance external calls.

Key design points for the proposal stage:

- **Team-id resolution caching**: persist the resolved API-Sports team id alongside player rows (or in a small lookup table keyed by `(ProviderName, NameEn) -> ApiSportsTeamId`); skip `/teams?search=` on subsequent syncs when a mapping exists. With ~8 default included teams: first sync ≈ 16 requests (2/team), subsequent syncs ≈ 8 requests (1/team) — roughly halves the steady-state cost against the 100/day budget.
- **Partial failure handling**: continue through remaining teams if one fails; collect `(teamName, errorMessage)` into the result DTO; do not abort the whole sync for one bad team. Catch `HttpRequestException` (incl. 429) per team.
- **Result DTO** mirroring `SyncFootballDataResultDto`: `ProviderName`, `TeamsProcessedCount`, `PlayersIndexedCount`, `IReadOnlyList<PlayerSquadSyncErrorDto> Errors`, `SyncedAtUtc`.
- **Cache layer**: HybridCache only as a thin wrapper over the DB read (or drop it — indexed `NormalizedName` reads are cheap), never over the external API call.
- New migration `AddExternalFootballPlayers` mirroring `20260603033649_AddExternalFootballDataCache` shape/conventions.

## Risks

- **Stale data between syncs** is intentional (admin-triggered only) — but the UI must surface "last synced" prominently
- **Concurrent sync triggers**: delete+insert replace is not safe under overlapping syncs (race + duplicate API-Sports calls). Mitigate at minimum with client-side button-disable (existing `isSyncingFootballData` pattern); consider a server-side guard (recency check on `SyncedAtUtc` or a short Redis lock) if double-triggering is plausible
- **Empty `IncludedTeamNames`**: handler must report `TeamsProcessedCount: 0` clearly, not look like a silent success/error
- **Missing API key**: sync command/endpoint must guard the same way DI does today (swap to a no-op with a clear "not configured" result), not throw
- **HTTP 429 (rate limit)**: `EnsureSuccessStatusCode()` throws on 429 today — sync handler must catch per-team, distinguish "rate limited" from "team not found" in the error report, and likely abort the whole sync early on first 429 (signals quota exhaustion — continuing just generates more failed-request noise)
- **Players lacking IDs/names/photos**: keep the existing filter (`Id is not null && !string.IsNullOrWhiteSpace(Name)`); `PhotoUrl`/`Position` stay nullable
- **Migration risk**: low (additive table), but must follow exact naming/index conventions of existing `external_football_*` tables to avoid EF model-snapshot drift

## Open Questions for Proposal

1. Exact endpoint route/naming (`/api/football-data/players/sync` vs. under `/api/admin`)
2. Whether to keep any HybridCache layer over the DB read or drop it
3. Whether to add a server-side "sync in progress" guard now or defer it
4. Abort-on-first-429 vs. best-effort continue
