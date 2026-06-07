# Tasks: Refactorizar apuestas de torneo para unificar Champion, BestPlayer y TopScorer en TournamentPick

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 600-900 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 domain+EF migration → PR 2 handlers+settlement+pending stakes → PR 3 tests+verification cleanup |
| Delivery strategy | chained PRs approved |
| Chain strategy | stacked-to-develop |

Decision needed before apply: Resolved — chained PRs, stacked-to-develop
Chained PRs recommended: Yes
Chain strategy: stacked-to-develop
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Introduce `TournamentPick` persistence and deterministic migration | PR 1 | Foundation; includes EF snapshot/migration review. |
| 2 | Switch application behavior to category-aware repository | PR 2 | Depends on PR 1; keeps external APIs stable. |
| 3 | Complete focused tests and verification | PR 3 | Depends on PR 2; may fold into earlier PRs if size permits. |

## Phase 1: Domain and Repository Foundation

- [x] 1.1 Create `src/WorldCupBets.Domain/Entities/TournamentPickCategory.cs` with `Champion`, `BestPlayer`, `TopScorer`.
- [x] 1.2 Create `src/WorldCupBets.Domain/Entities/TournamentPick.cs` with factories `CreateChampion` and `CreatePlayer` enforcing category-specific selection rules.
- [x] 1.3 Create `src/WorldCupBets.Domain/Repositories/ITournamentPickRepository.cs` with category-aware methods from `design.md`.
- [x] 1.4 Remove replaced domain contracts/entities: `ChampionBet`, `SpecialPlayerBet`, `SpecialPlayerBetCategory`, `IChampionBetRepository`, `ISpecialPlayerBetRepository` after consumers are migrated.

## Phase 2: EF Mapping and Migration

- [x] 2.1 Add `src/WorldCupBets.Infrastructure/Persistence/Configurations/TournamentPickConfiguration.cs` mapping `tournament_picks`, string category, lengths, FK, and unique `(UserId, Category)` index.
- [x] 2.2 Update `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` to expose `TournamentPicks` and remove old bet DbSets.
- [x] 2.3 Create `TournamentPickRepository` and update `InfrastructureServiceCollectionExtensions.cs` to register only `ITournamentPickRepository`.
- [x] 2.4 Generate deterministic migration replacing `champion_bets`/`special_player_bets` with `tournament_picks`; `Up` copies rows, `Down` recreates/splits old tables.
- [x] 2.5 Review `AppDbContextModelSnapshot.cs` for only expected table/index/entity changes.

Slice 2 note: repository, `TournamentPicks` DbSet, DI registration, and additive `AddTournamentPicks` migration foundation were added, but tasks 2.2-2.4 remain unchecked because old DbSets/repository registrations and replacing data migration are intentionally deferred until consumers migrate in the next slice.

## Phase 3: Handlers, Queries, Settlement, Pending Stakes

- [x] 3.1 Update `PlaceChampionBetHandler.cs` and `GetChampionBetMarketHandler.cs` to use `TournamentPickCategory.Champion` and map `SelectedText` to existing DTOs.
- [x] 3.2 Update `PlaceSpecialPlayerBetCommand.cs`, `PlaceSpecialPlayerBetHandler.cs`, and `GetSpecialBetMarketHandler.cs` to use player categories and reject `Champion` for `/special/player`.
- [x] 3.3 Update `BetsEndpoints.cs` enum parsing/imports while preserving route, request, and response contracts.
- [x] 3.4 Update `SettleChampionHandler` flow to use `ListChampionForSettlementAsync()` so player picks remain unsettled.
- [x] 3.5 Update `GetLeaderboardHandler.cs` and `GetCurrentUserSummaryHandler.cs` pending-stake aggregation to include each unsettled tournament pick once.

Slice 3 note: `GetLeaderboardHandler` now aggregates tournament-pick pending stakes by explicit categories. `GetCurrentUserSummaryHandler` has no pending-stake fields or repository dependencies in the current public API, so no code change was required there to preserve DTO behavior.

## Phase 4: Tests and Verification

- [x] 4.1 Update handler test fakes in `tests/WorldCupBets.Application.Tests/*BetHandlerTests.cs` for `ITournamentPickRepository` and duplicate-per-category scenarios.
- [x] 4.2 Add/adjust `SettleChampionHandlerTests.cs` for mixed categories: only `Champion` settles.
- [x] 4.3 Add/adjust `GetLeaderboardHandlerTests.cs` and `GetCurrentUserSummaryHandlerTests.cs` for pending player picks after champion settlement.
- [x] 4.4 Update `PostgresConcurrencyHardeningTests.cs` or migration-focused coverage for unique `(UserId, Category)` and copied row shape.
- [x] 4.5 Run `dotnet test WorldCupBets.sln`; run `npm run build` in `frontend/` only if frontend files changed.

Slice 4 note: final cleanup removed obsolete split domain/persistence contracts, replaced the additive migration with a copy/drop migration plus rollback split, regenerated the model snapshot, and added migration-focused script coverage. No frontend files were changed, so no frontend build was required.
