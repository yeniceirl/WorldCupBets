# Verification Report

**Change**: unify-tournament-picks  
**Version**: N/A  
**Mode**: Strict TDD

### Completeness

| Metric | Value |
|--------|-------|
| Tasks total | 18 |
| Tasks complete | 18 |
| Tasks incomplete | 0 |

### Build & Tests Execution

**Build**: ✅ Passed via `dotnet test WorldCupBets.sln` build phase

```text
dotnet test WorldCupBets.sln
WorldCupBets.Domain -> net10.0
WorldCupBets.Application -> net10.0
WorldCupBets.Infrastructure -> net10.0
WorldCupBets.WebApi -> net10.0
WorldCupBets.Application.Tests -> net10.0
```

**Tests**: ✅ 79 passed / ❌ 0 failed / ⚠️ 0 skipped

```text
dotnet test WorldCupBets.sln
Passed!  - Failed: 0, Passed: 79, Skipped: 0, Total: 79, Duration: 3 s
```

**Frontend build**: ➖ Not run — `git diff --name-only -- frontend` returned no changed frontend files.

**Coverage**: ➖ Not available — no coverage package/tool is configured in the test project.

### TDD Compliance

| Check | Result | Details |
|-------|--------|---------|
| TDD Evidence reported | ✅ | `apply-progress.md` contains a TDD Cycle Evidence table. |
| All tasks have tests | ✅ | 18/18 completed tasks have reported test coverage or verification evidence. |
| RED confirmed (tests exist) | ✅ | Referenced test files exist in `tests/WorldCupBets.Application.Tests/`. |
| GREEN confirmed (tests pass) | ✅ | Full suite passed: 79/79. |
| Triangulation adequate | ✅ | Category semantics, duplicate picks, markets, settlement, pending stakes, and migration shape have multiple cases. |
| Safety Net for modified files | ✅ | Apply evidence reports focused safety-net runs before behavior/persistence cleanup. |

**TDD Compliance**: 6/6 checks passed

---

### Test Layer Distribution

| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit / model metadata / migration script | 29+ | 6 | xUnit, EF Core metadata/migrator |
| Integration | 3 | 1 | xUnit + optional PostgreSQL via `WORLD_CUP_BETS_TEST_CONNECTION_STRING` |
| E2E | 0 | 0 | Not used for this backend-only change |
| **Total** | **32+ related cases** | **7 related files** | |

---

### Changed File Coverage

Coverage analysis skipped — no coverage tool detected.

---

### Assertion Quality

**Assertion quality**: ✅ All reviewed assertions verify real behavior or concrete metadata/script output. No tautologies, ghost loops, or smoke-only tests found in the new tournament-pick test files.

---

### Quality Metrics

**Linter**: ➖ Not available  
**Type Checker**: ✅ `dotnet test WorldCupBets.sln` compiled all projects successfully

### Spec Compliance Matrix

| Requirement | Scenario | Test | Result |
|-------------|----------|------|--------|
| TournamentPick domain semantics | Champion stores team | `TournamentPickRulesTests.CreateChampion_Stores_Team_As_Champion_Selection_Without_External_Id` | ✅ COMPLIANT |
| TournamentPick domain semantics | Player stores player | `TournamentPickRulesTests.CreatePlayer_Stores_Player_Category_Text_And_Optional_External_Id` | ✅ COMPLIANT |
| One pick per user per category | First pick is accepted | `PlaceChampionBetHandlerTests.Handle_Places_Champion_Bet_And_Deducts_User_Balance`; `PlaceSpecialPlayerBetHandlerTests.Handle_Places_Special_Player_Bet_And_Deducts_User_Balance` | ✅ COMPLIANT |
| One pick per user per category | Duplicate is rejected | `PlaceChampionBetHandlerTests.Handle_Rejects_Duplicate_Champion_Bet_For_Same_User`; `PlaceSpecialPlayerBetHandlerTests.Handle_Rejects_Duplicate_Special_Player_Bet_For_Same_Category`; EF unique index metadata test | ✅ COMPLIANT |
| Migration preserves existing picks | Champion rows migrate | `TournamentPickPersistenceTests.AddTournamentPicks_Migration_Copies_And_Drops_Split_Tournament_Bet_Tables` + migration inspection | ✅ COMPLIANT |
| Migration preserves existing picks | Special player rows migrate | `TournamentPickPersistenceTests.AddTournamentPicks_Migration_Copies_And_Drops_Split_Tournament_Bet_Tables` + migration inspection | ✅ COMPLIANT |
| Place flows remain compatible | Champion bet | `PlaceChampionBetHandlerTests.Handle_Places_Champion_Bet_And_Deducts_User_Balance` | ✅ COMPLIANT |
| Place flows remain compatible | Player bet | `PlaceSpecialPlayerBetHandlerTests.Handle_Places_Special_Player_Bet_And_Deducts_User_Balance`; `Handle_Rejects_Champion_Category_For_Player_Bets` | ✅ COMPLIANT |
| Markets expose pick state | Category state | `PlaceChampionBetHandlerTests.Market_Maps_Current_Champion_Pick_Selected_Text_To_Team_Name`; `PlaceSpecialPlayerBetHandlerTests.Market_Maps_Player_Tournament_Picks_To_Existing_Player_Bet_Dtos` | ✅ COMPLIANT |
| Pending stakes remain compatible | Pending stakes aggregate | `GetLeaderboardHandlerTests.Handle_Shows_Pending_Stake_Separately_From_Realized_Balance` | ✅ COMPLIANT |
| Pending stakes remain compatible | Settled champion not pending | `GetLeaderboardHandlerTests.Handle_Keeps_Player_Picks_Pending_When_Champion_Is_Settled` | ✅ COMPLIANT |
| Champion settlement filters by category | Player categories excluded | `SettleChampionHandlerTests.Handle_Settles_Only_Champion_Tournament_Picks` | ✅ COMPLIANT |
| Verification coverage | Backend verification command | `dotnet test WorldCupBets.sln` | ✅ COMPLIANT |

**Compliance summary**: 13/13 scenarios compliant

### Correctness (Static Evidence)

| Requirement | Status | Notes |
|------------|--------|-------|
| Unified model/table | ✅ Implemented | `TournamentPick`, `TournamentPickCategory`, `AppDbContext.TournamentPicks`, and `TournamentPickConfiguration` are present. |
| Category filtering | ✅ Implemented | Repository methods filter by explicit `TournamentPickCategory`; settlement uses `ListChampionForSettlementAsync()`. |
| One-pick uniqueness | ✅ Implemented | Handlers check `ExistsForUserAndCategoryAsync`; EF unique index exists on `(UserId, Category)`. |
| Data migration | ✅ Implemented | `Up` creates `tournament_picks`, copies champion/special rows, then drops old tables; `Down` recreates and splits back. |
| Pending stakes | ✅ Implemented | Leaderboard includes match stakes, champion picks only before champion settlement, and player categories regardless of champion settlement. |
| API compatibility | ✅ Implemented | Existing routes and DTO record names remain; player endpoint rejects `Champion`. |
| Accidental commits | ✅ None found | Working tree has uncommitted changes only; `git log -5` still ends at `c1a53e4 feat(bets): improve player autocomplete provider`; no new apply/verify commit was created. |

### Coherence (Design)

| Decision | Followed? | Notes |
|----------|-----------|-------|
| Replace split domain model with `TournamentPick` | ✅ Yes | Old active entities/repositories/configurations removed; historical migrations remain. |
| Use `SelectedText` + nullable `ExternalId` | ✅ Yes | Factories and DTO mapping use these fields. |
| Keep public API stable | ✅ Yes | Routes `/api/bets/champion`, `/api/bets/special`, `/api/bets/special/player` are unchanged. |
| Single `ITournamentPickRepository` | ✅ Yes | DI registers only `ITournamentPickRepository` for tournament picks. |
| Settlement champion-only | ✅ Yes | Repository filters `Category == Champion`; test covers mixed categories. |
| Backend-only verification | ✅ Yes | No frontend files changed; frontend build skipped as specified. |

### Issues Found

**CRITICAL**: None.

**WARNING**: None.

**SUGGESTION**:
- Strengthen migration tests by asserting the exact copied columns (`UserId`, `TeamName`/`PlayerName`, `ExternalPlayerId`, `StakeAmountCc`, `PlacedAtUtc`) rather than only broad script fragments. Static inspection confirms the current migration is correct.

### Verdict

PASS

The implementation satisfies the tournament-pick specification and `dotnet test WorldCupBets.sln` passes 79/79 after the unrelated diffs were stashed. The prior unrelated-change warning is resolved; no frontend files changed, no frontend build was required, and no commits were created.

### Re-verification After Stash

| Check | Result | Evidence |
|-------|--------|----------|
| Unrelated diffs removed from worktree | ✅ | `git status --short` no longer lists `src/WorldCupBets.Domain/Common/Result.cs`, `src/WorldCupBets.Infrastructure/ExternalFootball/ApiSportsFootballOptions.cs`, or `src/WorldCupBets.WebApi/WorldCupBets.WebApi.csproj`. |
| Backend tests | ✅ | `dotnet test WorldCupBets.sln` passed 79/79. |
| Frontend build | ➖ | Skipped because `git diff --name-only -- frontend` returned no changed frontend files. |
| Accidental commits | ✅ | `git log --oneline -5` shows latest commit `c1a53e4 feat(bets): improve player autocomplete provider`; no verification commit was created. |
