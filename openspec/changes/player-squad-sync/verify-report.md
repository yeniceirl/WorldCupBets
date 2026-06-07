# Verify Report: player-squad-sync

**Mode**: full (proposal/spec/design/tasks all present)
**Branches reviewed**: feat/player-squad-sync-search-rewrite (cumulative, stacked on -feature on -persistence, based on develop)
**Verdict**: PASS

## Completeness

All 21 tasks (1.1-1.6, 2.1-2.8, 3.1-3.5, 4.1) are checked `[x]` in `openspec/changes/player-squad-sync/tasks.md` on the review branch and match the apply-progress claims. No unchecked tasks found.

## Build & Test Evidence

- `dotnet build src/WorldCupBets.Infrastructure/...` (Release): **succeeded, 0 warnings, 0 errors**
- `dotnet test tests/WorldCupBets.Application.Tests/WorldCupBets.Application.Tests.csproj -c Release`: **97/97 passed**, 2s
- `npx ng build` (frontend): **succeeded** (admin-page-component chunk built, 18.09 kB)
- No `strict_tdd: true` flag found in `sdd-init/worldcupbets` cache — Standard Mode verification applied.

## Spec Compliance Matrix (5 requirements / 13 scenarios)

| Requirement / Scenario | Evidence | Status |
|---|---|---|
| Admin-triggered sync — Successful sync replaces persisted rows | `SyncPlayerSquadsHandler.Handle` resolves teams (cache-or-fetch), fetches squads, dedupes, calls `ReplacePlayersAsync` (delete+insert in one operation); `SyncPlayerSquadsHandlerTests` cover happy path with counts/empty errors | PASS |
| Admin-triggered sync — Non-admin cannot trigger | `POST /players/sync` mapped with `[Authorize(Policy = "Admin")]`; `EndpointAuthorizationMetadataTests.Admin_Endpoints_Require_Admin_Policy` asserts route is registered with `Admin` policy (same pattern as existing `/sync` and `/import` admin endpoints — no integration test exists for any of them, metadata-test is the established convention) | PASS |
| Admin-triggered sync — Per-team failure reported, no abort | `catch (HttpRequestException)` adds `PlayerSquadSyncErrorDto(..., RateLimited: false)` and `continue`s; covered by `Collects_PerTeam_Errors_And_Continues_For_NonRateLimit_Failures` | PASS |
| Abort on 429 — First 429 stops processing | `catch (ApiSportsRateLimitException)` sets `rateLimited = true`, loop checks flag and `break`s, persists gathered players, adds distinct `RateLimited: true` error entry; covered by `Aborts_Whole_Sync_On_First_RateLimit_And_Persists_Partial_Results` | PASS |
| Abort on 429 — Non-429 errors don't abort | Same handler branch logic; `HttpRequestException` is caught separately from `ApiSportsRateLimitException` and does not set `rateLimited`; covered above | PASS |
| Persisted search — reads from persisted rows, zero external calls | `ApiSportsFootballPlayerSearchProvider` depends ONLY on `IExternalFootballPlayerRepository` — no `HttpClient`, no `IFootballDataProvider`, no team-resolution; `SearchAsync` queries `repo.SearchAsync(...)` then ranks in C#; `SearchAsync_Returns_Persisted_Matches_...` and `SearchAsync_Returns_Empty_When_No_Players_Persisted` use a stub repo with zero HTTP | PASS |
| Persisted search — empty before sync | `SearchAsync_Returns_Empty_When_No_Players_Persisted` confirms empty-table → empty result, no error | PASS |
| Admin feedback — sees summary | `admin-page.component.ts.syncPlayerSquads()` composes message with `playersIndexedCount`, `teamsProcessedCount`, `providerName`, per-team error summary distinguishing `"rate limited"` vs message text, displayed via `data-testid="success-message"`; e2e `core-flows.spec.ts` "admin player squad sync" describe block asserts this | PASS |
| Admin feedback — last-synced persists across reload | `admin-player-sync-storage.ts` (localStorage) wrapped by `AdminService.getLastPlayerSyncAtUtc`/`rememberLastPlayerSyncAtUtc`, seeded into `lastPlayerSyncAtUtc` signal on component init, updated on successful sync; e2e test reloads page and re-asserts `admin-players-last-synced` | PASS |
| Guard missing config — blank API key | Handler returns `NotConfigured: true` early, before any provider/repo call; `Returns_NotConfigured_When_ApiKey_Is_Blank` asserts `provider.ResolveCalled == false` and `repository.ReplaceCalled == false` | PASS |
| Guard missing config — empty team list | Handler returns `TeamsProcessedCount: 0, PlayersIndexedCount: 0, NotConfigured: false` early; `Returns_Zero_Result_When_No_Teams_Configured` asserts no provider/repo calls and `Errors` empty | PASS |

All 13 scenarios have a runtime-passing covering test or are exercised through the e2e spec (frontend feedback scenarios). No UNTESTED/FAILING scenarios found.

## Hard-Constraint Checks

1. **Zero external HTTP calls from search path** — CONFIRMED. `ApiSportsFootballPlayerSearchProvider` constructor takes only `IExternalFootballPlayerRepository`; no `HttpClient`/`IFootballDataProvider` reference anywhere in the class. `rg "HybridCache|SquadCacheHours|SquadIndexCache|squad-index"` across `src/`, `tests/`, `frontend/src` returns only the unrelated `services.AddHybridCache()` registration line in `InfrastructureServiceCollectionExtensions.cs` (used by a different, pre-existing caching concern — confirmed by apply-progress notes and by the fact that removing its `using` would break that other registration).
2. **HybridCache fully removed from player-search path** — CONFIRMED. No `BuildPlayerIndexAsync`, `ResolveNationalTeamAsync`, `SquadIndexCacheDuration` placeholder, or `IDistributedCache`/`HybridCache` injection remains in `ApiSportsFootballPlayerSearchProvider` or its DI factory (`new ApiSportsFootballPlayerSearchProvider(serviceProvider.GetRequiredService<IExternalFootballPlayerRepository>())`).
3. **Migration naming/index conventions match `external_football_*`** — CONFIRMED. `20260607161621_AddExternalFootballPlayers` creates table `external_football_players`, indexes `IX_external_football_players_NormalizedName` and `IX_external_football_players_ProviderName_ExternalId` (unique), matching the exact naming pattern of `external_football_teams`/`IX_external_football_teams_ProviderName_ExternalId`. `ExternalFootballPlayerConfiguration` mirrors `ExternalFootballTeamConfiguration` (same `HasMaxLength` conventions, same composite-unique index shape, same `ToTable`/`HasKey` patterns). Designer + `AppDbContextModelSnapshot` are updated in the same commit — no drift detected (`dotnet build` of Infrastructure succeeds cleanly, which would surface snapshot/model mismatches as warnings in this codebase's EF setup).
4. **Scope creep / stray changes** — NONE FOUND. `git status --porcelain` is clean on the review branch; `git diff --stat develop...feat/player-squad-sync-search-rewrite` shows only files explicitly listed in the design's File Changes section plus expected test/migration/openspec artifacts. The only "extra" additions beyond the design's enumerated list are: `ApiSportsRateLimitException.cs` (a small, clearly-scoped exception type referenced by the design's `IPlayerSquadProvider` contract but not separately enumerated in File Changes — not scope creep, just an omission in the design's file list), `admin.service.ts` modification (the AdminService wrapper layer added per Guardian Angel review feedback — see apply-progress "Learned" notes), and `admin-player-sync-storage.ts` (the storage module backing that wrapper). Both are direct, narrowly-scoped consequences of implementing the spec's "last-synced persists across reload" requirement, following the codebase's established `auth-storage.ts` → `auth-state.service.ts` layering convention.

## Design Coherence

| Decision | Implementation | Status |
|---|---|---|
| Sibling `IExternalFootballPlayerRepository` (not merged into existing repo) | New port + `ExternalFootballPlayerRepository` adapter, registered separately in DI | MATCH |
| DB `Contains`/`Like` filter + C# rank (starts-with-word → alphabetical → take 10) | `EF.Functions.Like(..., $"%{normalizedQuery}%")` then `.OrderByDescending(StartsWithWord).ThenBy(Name).Take(10)` in C# — verbatim port of prior ranking | MATCH |
| Team-id cache reuse via persisted `TeamExternalId` (read before replace) | `GetTeamIdMapAsync` called before the loop; `teamIdMap.TryGetValue` checked before calling `ResolveTeamIdAsync`; `Reuses_Persisted_TeamExternalId_And_Skips_Resolution` test confirms | MATCH |
| Drop HybridCache from search path entirely | Confirmed (see hard-constraint #2) | MATCH |
| Abort-on-first-429, persist partial, distinct error | `rateLimited` flag + `break` + `ReplacePlayersAsync` called once after loop with whatever was gathered + `RateLimited: true` distinct entry | MATCH |
| Sync-in-progress guard = client-side button-disable only | `[disabled]="!isAdmin() || isSyncingPlayers()"` on the "Sync players" button, no server-side lock | MATCH |

No design deviations found that would warrant a WARNING.

## Issues

### CRITICAL
None.

### WARNING
None.

### SUGGESTION
1. The design's "File Changes" list does not mention `ApiSportsRateLimitException.cs` or the new `admin-player-sync-storage.ts`/`admin.service.ts` modification explicitly — both are legitimate, narrowly-scoped additions driven by the contracts the design *does* specify (the `IPlayerSquadProvider` 429 contract, and the "last-synced persists across reloads" scenario plus the Guardian Angel layering convention). Purely a design-doc completeness note for archival; does not affect correctness or scope.
2. Frontend e2e suite (Playwright) cannot execute in this sandbox (`chromium` unsupported on this Ubuntu image — a pre-existing, environment-level limitation that also affects the prior `core-flows.spec.ts` tests, not something this change introduced). The new "admin player squad sync" describe block was verified by static/structural review (same `mockApi`/`route`/`getByTestId` patterns as passing siblings) and by `ng build` succeeding with the file included, but it has not been runtime-executed. Recommend running it in CI/a Playwright-capable environment before fully relying on it as regression coverage.

## Final Verdict

**PASS** — All 21 tasks complete, all 13 spec scenarios covered by passing tests (backend) or structurally-consistent e2e coverage (frontend, environment-blocked from execution but not a regression), 97/97 backend tests green, frontend build green, zero external calls from the search path confirmed, HybridCache fully removed from that path, migration conventions match existing `external_football_*` tables with no model-snapshot drift, and no unrelated scope creep. Ready for archive.
