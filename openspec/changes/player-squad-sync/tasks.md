# Tasks: Player Squad Sync

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~550-700 (new entity+config+migration+repo+command/handler/dto+provider rewrite+endpoint+DI+frontend+tests) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (backend domain+persistence+repo+tests) → PR 2 (sync command/handler/provider+endpoint+DI+tests) → PR 3 (search rewrite+frontend+tests) |
| Delivery strategy | ask-on-risk |
| Chain strategy | stacked-to-develop |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-develop
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain entity + EF config + migration + repository (port+adapter) + repo tests | PR 1 | Base = develop. Self-contained persistence slice; verifiable via migration apply + repo unit tests |
| 2 | Sync command/handler/result-DTO + squad provider port/adapter + endpoint + DI wiring + handler/integration tests | PR 2 | Base = PR 1 branch. Depends on Unit 1 repo; delivers the admin sync feature end-to-end on backend |
| 3 | Search-provider rewrite (drop HybridCache) + frontend service/models/component + e2e/integration coverage | PR 3 | Base = PR 2 branch. Depends on persisted data path from Units 1-2; closes the loop for admins and searchers |

Test command (backend): `dotnet test tests/WorldCupBets.Application.Tests/WorldCupBets.Application.Tests.csproj --configuration Release`
Test command (frontend): `ng test` / Playwright via `npx playwright test` (per CI). Strict TDD not flagged in sdd-init (testing capabilities exist but no `strict_tdd: true`); follow Standard Mode — write tests alongside or immediately after each unit.

## Phase 1: Domain & Persistence Foundation (Spec: Persisted player search §req3, §req4 config guard groundwork)

- [x] 1.1 Create `src/WorldCupBets.Domain/Entities/ExternalFootballPlayer.cs` mirroring `ExternalFootballTeam` (ProviderName, ExternalId, Name, NormalizedName, TeamExternalId, TeamName, Position?, PhotoUrl?, SyncedAtUtc; private ctor + static `Create`)
- [x] 1.2 Create `src/WorldCupBets.Application/Abstractions/IExternalFootballPlayerRepository.cs` (ReplacePlayersAsync, SearchAsync(providerName, normalizedQuery), GetTeamIdMapAsync(providerName))
- [x] 1.3 Create `Infrastructure/Persistence/Configurations/ExternalFootballPlayerConfiguration.cs` (table `external_football_players`, unique index (ProviderName,ExternalId), secondary index NormalizedName, HasMaxLength conventions matching `ExternalFootballTeamConfiguration`)
- [x] 1.4 Add `DbSet<ExternalFootballPlayer>` to `Persistence/AppDbContext.cs`; generate EF migration `AddExternalFootballPlayers` (+ Designer + snapshot update)
- [x] 1.5 Create `Infrastructure/Persistence/Repositories/ExternalFootballPlayerRepository.cs` implementing replace (delete+insert), `SearchAsync` via indexed `NormalizedName` `Contains`, and `GetTeamIdMapAsync` (read-before-replace cache source)
- [x] 1.6 Write repo unit tests: replace semantics, `SearchAsync` Contains-filter + ranking input, `GetTeamIdMapAsync` returns persisted TeamExternalId map (Spec scenario: "Search reads from persisted rows", "Search before any sync has run")

## Phase 2: Sync Command, Provider & Endpoint (Spec: Admin-triggered sync §req1, Abort-on-429 §req2, Config guards §req5) — COMPLETE

- [x] 2.1 Create `Application/Abstractions/IPlayerSquadProvider.cs` (ResolveTeamIdAsync — throws `ApiSportsRateLimitException` on 429; GetSquadAsync)
- [x] 2.2 Create `Infrastructure/ExternalFootball/ApiSportsPlayerSquadProvider.cs` implementing the port against API-Sports `/teams?search=` and `/players/squads`
- [x] 2.3 Create `Application/Features/FootballData/SyncPlayerSquadsCommand.cs` (empty record), `SyncPlayerSquadsResultDto.cs` (ProviderName, TeamsProcessedCount, PlayersIndexedCount, Errors: `IReadOnlyList<PlayerSquadSyncErrorDto>`, SyncedAtUtc, NotConfigured), and `PlayerSquadSyncErrorDto` (TeamName, Message, RateLimited)
- [x] 2.4 Create `Application/Features/FootballData/SyncPlayerSquadsHandler.cs`: missing API key → NotConfigured:true no-op; empty `IncludedTeamNames` → TeamsProcessedCount:0; per team — reuse cached TeamExternalId else resolve, fetch squad, on 429 add rate-limited error + break + replace gathered + return, other `HttpRequestException` → per-team error + continue; on completion replace persisted rows via repo
- [x] 2.5 Add `POST /api/football-data/players/sync` to `WebApi/Endpoints/FootballDataEndpoints.cs` with `[Authorize(Policy = "Admin")]`, dispatching `SyncPlayerSquadsCommand` and returning `SyncPlayerSquadsResultDto`
- [x] 2.6 Wire DI in `InfrastructureServiceCollectionExtensions.cs`: register `IExternalFootballPlayerRepository`, `IPlayerSquadProvider`/`ApiSportsPlayerSquadProvider`; remove `SquadCacheHours` from `ApiSportsFootballOptions.cs`
- [x] 2.7 Write handler unit tests (fake provider + in-memory repo): 429 abort mid-sync, not-configured no-op, zero-teams clean result, team-id cache reuse (skip resolve when persisted TeamExternalId exists), per-team non-429 error continues (Spec scenarios: "Successful sync replaces persisted rows", "Non-admin cannot trigger sync", "Per-team failure...without aborting", "First 429 stops further processing", "Non-429 errors do not trigger abort", "Missing API key produces a clear no-op result", "Empty included-team list reports zero processed teams")
- [x] 2.8 Write endpoint integration test: `POST /players/sync` rejects non-admin callers and never invokes the provider (Spec scenario: "Non-admin cannot trigger sync") — covered via `EndpointAuthorizationMetadataTests` (asserts `/api/football-data/players/sync` requires the `Admin` policy and metadata-only mapping never executes handlers)

## Phase 3: Search Rewrite & Frontend Integration (Spec: Persisted search §req3, Admin feedback §req4) — COMPLETE

- [x] 3.1 Rewrite `ApiSportsFootballPlayerSearchProvider.SearchAsync` to drop HybridCache/index-build/team-resolution entirely and delegate to `IExternalFootballPlayerRepository.SearchAsync`, keeping existing starts-with-word/alphabetical/take-10 ranking in C#
- [x] 3.2 Write/update search-provider unit tests verifying no HTTP calls occur and ranking matches prior behavior on persisted-row fixtures (Spec scenarios: "Search reads from persisted rows", "Search before any sync has run")
- [x] 3.3 Add `syncPlayerSquads()` to `frontend/src/app/.../matches.service.ts`; add `SyncPlayerSquadsResult` and `PlayerSquadSyncError` to `matches.models.ts`
- [x] 3.4 Update `admin-page.component.ts`: add sync-players button, `isSyncingPlayers` signal (client-side disable-while-syncing guard), and result/error/last-synced display mirroring `syncFootballData()` (Spec scenarios: "Admin sees success summary after sync", "Last-synced timestamp persists across reloads")
- [x] 3.5 Write/update frontend component test or Playwright e2e covering: trigger sync, see TeamsProcessedCount/PlayersIndexedCount/Errors/SyncedAtUtc, and persisted last-synced timestamp across reload

## Phase 4: Cleanup — COMPLETE

- [x] 4.1 Remove any now-dead `SquadCacheHours` references/config entries and confirm no remaining lazy-fetch code paths in the player search flow
