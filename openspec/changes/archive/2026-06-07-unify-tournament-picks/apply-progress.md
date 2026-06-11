# Apply Progress: unify-tournament-picks

## Mode

Strict TDD — orchestrator override with `dotnet test` runner.

## Delivery Boundary

- Strategy: chained PR slice, stacked-to-develop.
- Previous slices completed:
  - Domain foundation for unified tournament picks.
  - EF mapping/repository/additive migration foundation for `TournamentPick` while preserving existing champion/special consumers.
  - Application handlers, markets, champion settlement, endpoint enum parsing, and leaderboard pending-stake consumers migrated to `ITournamentPickRepository` while preserving public API/DTO names.
- Slice completed: final cleanup/replacement migration. Obsolete split domain/persistence contracts and DbSets were removed; the migration now copies from `champion_bets` / `special_player_bets` into `tournament_picks`, drops old tables, and restores/splits rows on rollback.
- Start state: code read/wrote `TournamentPick`, but obsolete split entities/repositories/DbSets/configurations and additive migration artifacts remained.
- End state: `TournamentPick` is the only active tournament-pick persistence model; migration and model snapshot represent the expected replacement shape.
- Review boundary: destructive persistence cleanup and migration verification only. No API/frontend route or DTO rename.

## Completed Tasks

- [x] 1.1 Create `src/WorldCupBets.Domain/Entities/TournamentPickCategory.cs` with `Champion`, `BestPlayer`, `TopScorer`.
- [x] 1.2 Create `src/WorldCupBets.Domain/Entities/TournamentPick.cs` with factories `CreateChampion` and `CreatePlayer` enforcing category-specific selection rules.
- [x] 1.3 Create `src/WorldCupBets.Domain/Repositories/ITournamentPickRepository.cs` with category-aware methods from `design.md`.
- [x] 1.4 Remove replaced domain contracts/entities: `ChampionBet`, `SpecialPlayerBet`, `SpecialPlayerBetCategory`, `IChampionBetRepository`, `ISpecialPlayerBetRepository` after consumers are migrated.
- [x] 2.1 Add `src/WorldCupBets.Infrastructure/Persistence/Configurations/TournamentPickConfiguration.cs` mapping `tournament_picks`, string category, lengths, FK, and unique `(UserId, Category)` index.
- [x] 2.2 Update `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` to expose `TournamentPicks` and remove old bet DbSets.
- [x] 2.3 Create `TournamentPickRepository` and update `InfrastructureServiceCollectionExtensions.cs` to register only `ITournamentPickRepository`.
- [x] 2.4 Generate deterministic migration replacing `champion_bets`/`special_player_bets` with `tournament_picks`; `Up` copies rows, `Down` recreates/splits old tables.
- [x] 2.5 Review `AppDbContextModelSnapshot.cs` for only expected table/index/entity changes.
- [x] 3.1 Update `PlaceChampionBetHandler.cs` and `GetChampionBetMarketHandler.cs` to use `TournamentPickCategory.Champion` and map `SelectedText` to existing DTOs.
- [x] 3.2 Update `PlaceSpecialPlayerBetCommand.cs`, `PlaceSpecialPlayerBetHandler.cs`, and `GetSpecialBetMarketHandler.cs` to use player categories and reject `Champion` for `/special/player`.
- [x] 3.3 Update `BetsEndpoints.cs` enum parsing/imports while preserving route, request, and response contracts.
- [x] 3.4 Update `SettleChampionHandler` flow to use `ListChampionForSettlementAsync()` so player picks remain unsettled.
- [x] 3.5 Update `GetLeaderboardHandler.cs` pending-stake aggregation to include each unsettled tournament pick once. `GetCurrentUserSummaryHandler` has no pending-stake fields in the current DTO, so no code change was required there.
- [x] 4.1 Update handler test fakes in `tests/WorldCupBets.Application.Tests/*BetHandlerTests.cs` for `ITournamentPickRepository` and duplicate-per-category scenarios.
- [x] 4.2 Add/adjust `SettleChampionHandlerTests.cs` for mixed categories: only `Champion` settles.
- [x] 4.3 Add/adjust `GetLeaderboardHandlerTests.cs` and `GetCurrentUserSummaryHandlerTests.cs` for pending player picks after champion settlement.
- [x] 4.4 Update `PostgresConcurrencyHardeningTests.cs` or migration-focused coverage for unique `(UserId, Category)` and copied row shape.
- [x] 4.5 Run `dotnet test WorldCupBets.sln`; run `npm run build` in `frontend/` only if frontend files changed.

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/WorldCupBets.Application.Tests/TournamentPickRulesTests.cs` | Unit | N/A (new) | ✅ `dotnet test WorldCupBets.sln --filter TournamentPickRulesTests` failed with missing `TournamentPickCategory` | ✅ 5/5 passed after adding enum | ✅ Covered all three categories through Champion and player factory cases | ✅ No additional refactor needed |
| 1.2 | `tests/WorldCupBets.Application.Tests/TournamentPickRulesTests.cs` | Unit | N/A (new) | ✅ Tests referenced missing `TournamentPick` factories first | ✅ 5/5 passed after adding entity factories | ✅ Champion trims selection and clears external id; player categories trim text/id and reject `Champion` | ✅ Factory validation kept explicit and minimal |
| 1.3 | `tests/WorldCupBets.Application.Tests/TournamentPickRulesTests.cs` | Unit | N/A (new) | ✅ Test referenced missing `ITournamentPickRepository` contract first | ✅ 5/5 passed after adding contract | ➖ Structural contract verification only; no branching behavior | ✅ Method names match design contract |
| 2.1 | `tests/WorldCupBets.Application.Tests/TournamentPickPersistenceTests.cs` | Unit/model metadata | ✅ 5/5 existing `TournamentPickRulesTests` passed before modifying persistence files | ✅ Persistence tests failed with missing `AppDbContext.TournamentPicks` and `TournamentPickRepository` | ✅ 3/3 initial persistence tests passed after adding mapping, DbSet, and repository | ✅ 5/5 after empty-category repository cases | ✅ Generated EF migration/snapshot after green; tests remained green |
| 3.1 | `tests/WorldCupBets.Application.Tests/PlaceChampionBetHandlerTests.cs` | Unit | ✅ 17/17 focused existing handler tests passed before modifying files | ✅ Focused handler test run failed because tests passed `ITournamentPickRepository` while handlers still required `IChampionBetRepository` | ✅ Focused suite passed after champion place/market handlers used `TournamentPickCategory.Champion` and `SelectedText` | ✅ Duplicate champion pick and market mapping cover different code paths | ✅ Kept legacy DTO names and response mapping stable |
| 3.2 | `tests/WorldCupBets.Application.Tests/PlaceSpecialPlayerBetHandlerTests.cs` | Unit | ✅ 17/17 focused existing handler tests passed before modifying files | ✅ Focused handler test run failed because command/tests used `TournamentPickCategory` while command still used `SpecialPlayerBetCategory` | ✅ Focused suite passed after player place/market handlers used `TournamentPick` and rejected `Champion` | ✅ Happy path, duplicate category, short player name, closed betting, invalid `Champion` category, and market mapping covered | ✅ Handler keeps category validation explicit before factory creation |
| 3.3 | `tests/WorldCupBets.Application.Tests/PlaceSpecialPlayerBetHandlerTests.cs` + full build | Unit/build | ✅ 17/17 focused existing handler tests passed before modifying files | ✅ Command category type change forced WebApi compile updates for endpoint parsing | ✅ Focused and full `dotnet test` passed after endpoint parsing switched to `TournamentPickCategory` and rejected `Champion` | ✅ Endpoint keeps the existing string category request contract while preserving player-only categories | ✅ No route/DTO rename |
| 3.4 | `tests/WorldCupBets.Application.Tests/SettleChampionHandlerTests.cs` | Unit | ✅ 17/17 focused existing handler tests passed before modifying files | ✅ Focused handler test run failed because settlement tests passed `ITournamentPickRepository` while handler still required `IChampionBetRepository` | ✅ Focused suite passed after settlement called `ListChampionForSettlementAsync()` and compared `SelectedText` | ✅ Mixed category test proves player picks are excluded from winner/loser counts and payouts | ✅ Settlement math left unchanged |
| 3.5 | `tests/WorldCupBets.Application.Tests/GetLeaderboardHandlerTests.cs` | Unit | ✅ 17/17 focused existing handler tests passed before modifying files | ✅ Focused handler test run failed because leaderboard tests passed one `ITournamentPickRepository` while handler still required split repositories | ✅ Focused suite passed after leaderboard queried champion and player categories from `ITournamentPickRepository` | ✅ Unsettled all-category pending stakes and settled champion/player-pending scenarios covered | ✅ Kept current-user summary unchanged because it has no pending-stake API surface |
| 1.4 / 2.2 / 2.3 | `tests/WorldCupBets.Application.Tests/TournamentPickPersistenceTests.cs` | Unit/model metadata | ✅ 8/8 migration-focused safety net passed before cleanup | ✅ Added test failed while `ChampionBet` / `SpecialPlayerBet` were still mapped and `ChampionBets` / `SpecialPlayerBets` DbSets still existed | ✅ 8/8 passed after deleting split domain contracts/configs/repositories and old DI registrations | ✅ Test asserts both entity mappings and DbSet properties are gone | ✅ No adapter layer retained; `ITournamentPickRepository` remains the only tournament-pick repository registration |
| 2.4 | `tests/WorldCupBets.Application.Tests/TournamentPickPersistenceTests.cs` | Unit/migration script | ✅ 8/8 migration-focused safety net passed before replacing migration | ✅ Added migration script tests failed because `AddTournamentPicks` only created/dropped `tournament_picks` without copy/drop or rollback split | ✅ 8/8 passed after regenerating and adjusting migration with copy/drop Up and recreate/split Down | ✅ Forward script covers champion and special-player sources; down script covers both rollback targets | ✅ Migration reviewed for data-preserving order: create → copy → drop; rollback create → split → drop |
| 2.5 / 4.4 | `tests/WorldCupBets.Application.Tests/TournamentPickPersistenceTests.cs` | Unit/model metadata + migration script | ✅ 8/8 focused tests passed before snapshot verification | ✅ Snapshot test would fail if obsolete entities remained mapped; script tests would fail if row-copy shape regressed | ✅ 8/8 focused tests passed with snapshot containing `TournamentPick` only for active tournament picks | ✅ Unique `(UserId, Category)`, copy sources, drop targets, and rollback split all asserted | ✅ Snapshot generated by EF and reviewed for expected replacement shape |
| 4.5 | `WorldCupBets.sln` | Full suite | ✅ Focused persistence suite passed before full run | ✅ Not applicable — verification command task | ✅ `dotnet test WorldCupBets.sln` passed 79/79 | ➖ Verification command only | ✅ No frontend files changed, so frontend build was not run |

## Test Summary

- Total tests written/updated: 18 cumulative domain/persistence/application tests across all apply slices.
- Total tests passing: 79/79 full suite.
- Layers used: Unit/model metadata, migration script checks, and handler unit tests.
- Approval tests: Existing handler and persistence tests acted as behavior safety net before refactoring consumers and cleanup.
- Pure functions created: 0.

## Tests Run

- `dotnet test WorldCupBets.sln --filter "TournamentPickPersistenceTests|PostgresConcurrencyHardeningTests"` — safety net passed, 8/8.
- `dotnet test WorldCupBets.sln --filter TournamentPickPersistenceTests` — RED failed, 3/8, proving obsolete mappings/DbSets and additive migration behavior still existed.
- `dotnet test WorldCupBets.sln --filter TournamentPickPersistenceTests` — after cleanup and migration replacement, passed, 8/8.
- `dotnet test WorldCupBets.sln` — final suite passed, 79/79.
- Frontend build not run because no frontend files changed.

## Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `src/WorldCupBets.Domain/Entities/ChampionBet.cs` | Deleted | Removed obsolete split champion entity. |
| `src/WorldCupBets.Domain/Entities/SpecialPlayerBet.cs` | Deleted | Removed obsolete split special-player entity. |
| `src/WorldCupBets.Domain/Entities/SpecialPlayerBetCategory.cs` | Deleted | Removed obsolete split player category enum. |
| `src/WorldCupBets.Domain/Repositories/IChampionBetRepository.cs` | Deleted | Removed obsolete champion repository contract. |
| `src/WorldCupBets.Domain/Repositories/ISpecialPlayerBetRepository.cs` | Deleted | Removed obsolete special-player repository contract. |
| `src/WorldCupBets.Infrastructure/Persistence/Configurations/ChampionBetConfiguration.cs` | Deleted | Removed obsolete champion EF configuration. |
| `src/WorldCupBets.Infrastructure/Persistence/Configurations/SpecialPlayerBetConfiguration.cs` | Deleted | Removed obsolete special-player EF configuration. |
| `src/WorldCupBets.Infrastructure/Persistence/Repositories/ChampionBetRepository.cs` | Deleted | Removed obsolete champion repository implementation. |
| `src/WorldCupBets.Infrastructure/Persistence/Repositories/SpecialPlayerBetRepository.cs` | Deleted | Removed obsolete special-player repository implementation. |
| `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` | Modified | Removed old `ChampionBets` and `SpecialPlayerBets` DbSets; retained `TournamentPicks`. |
| `src/WorldCupBets.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | Modified | Removed old split repository DI registrations; retained `ITournamentPickRepository`. |
| `src/WorldCupBets.Infrastructure/Persistence/Migrations/20260607070825_AddTournamentPicks.cs` | Created | Replaced additive migration with data-preserving copy/drop Up and recreate/split Down. |
| `src/WorldCupBets.Infrastructure/Persistence/Migrations/20260607070825_AddTournamentPicks.Designer.cs` | Created | Regenerated EF migration designer from unified model. |
| `src/WorldCupBets.Infrastructure/Persistence/Migrations/20260607065110_AddTournamentPicks.cs` | Deleted | Removed obsolete additive migration. |
| `src/WorldCupBets.Infrastructure/Persistence/Migrations/20260607065110_AddTournamentPicks.Designer.cs` | Deleted | Removed obsolete additive migration designer. |
| `src/WorldCupBets.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` | Modified | Regenerated snapshot so active model contains `TournamentPick` and no split bet entities. |
| `tests/WorldCupBets.Application.Tests/TournamentPickPersistenceTests.cs` | Modified | Added cleanup and migration script coverage for obsolete mappings, forward copy/drop, rollback split, and unique index. |
| `openspec/changes/unify-tournament-picks/tasks.md` | Modified | Marked all completed tasks and recorded resolved stacked-to-develop strategy. |
| `openspec/changes/unify-tournament-picks/apply-progress.md` | Modified | Merged cumulative progress with final cleanup TDD evidence. |

## Deviations from Design

None — implementation matches design. Public route/DTO names remain compatible while internal persistence is unified.

## Issues Found

None.

## Remaining Tasks

- None for apply. Ready for SDD verify.
