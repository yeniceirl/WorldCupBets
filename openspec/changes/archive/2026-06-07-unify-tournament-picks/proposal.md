# Proposal: Refactorizar apuestas de torneo para unificar Champion, BestPlayer y TopScorer en TournamentPick

## Intent

Problem: tournament picks are split across `ChampionBet`/`champion_bets` and `SpecialPlayerBet`/`special_player_bets`, duplicating one-pick and stake logic while representing one domain concept. Unify them as explicit `TournamentPick` / `tournament_picks` with category-specific rules kept in domain/application code.

## Scope

### In Scope
- Add minimum `TournamentPick` model/table with `Category` (`Champion`, `BestPlayer`, `TopScorer`), `SelectedText`, optional `ExternalId`, stake, user, and placed timestamp.
- Migrate data from `champion_bets` and `special_player_bets` into `tournament_picks`; reverse migration splits rows back.
- Replace champion/special repositories with category-aware tournament-pick repository methods.
- Update champion/player place-bet flows, markets, leaderboard/My Bets pending stake aggregation, and champion settlement filtering.
- Update/add backend tests for one pick per user per category, migration mapping, pending stakes, and champion-only settlement.

### Out of Scope
- Public route/DTO/frontend renames; keep existing API contracts unless implementation proves impossible.
- Settling `BestPlayer` or `TopScorer` picks.
- New tournament pick categories or configurable stake amounts.

## Capabilities

### New Capabilities
- `tournament-picks`: unified tournament-pick behavior, category rules, persistence, pending stakes, and champion settlement interaction.

### Modified Capabilities
- None; no existing `openspec/specs/` capabilities found.

## Approach

Use Approach 1 from exploration: internal unification with stable external API. Map champion picks as `Category=Champion`, `SelectedText=TeamName`, `ExternalId=null`; map player picks as `Category=BestPlayer|TopScorer`, `SelectedText=PlayerName`, `ExternalId=ExternalPlayerId`. Enforce unique `(UserId, Category)`. Keep per-category validation in handlers/domain, not separate tables.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/WorldCupBets.Domain/Entities` | Modified | Replace old entities/category with `TournamentPick` and `TournamentPickCategory`. |
| `src/WorldCupBets.Domain/Repositories` | Modified | Introduce `ITournamentPickRepository`; remove split bet repositories. |
| `src/WorldCupBets.Infrastructure/Persistence` | Modified | DbSet, EF config, repository, DI, deterministic migration/snapshot. |
| `src/WorldCupBets.Application/Features/Bets` | Modified | Place/market/settlement handlers use category-aware repository. |
| `src/WorldCupBets.Application/Features/Leaderboard` | Modified | Pending stake aggregation remains behavior-compatible. |
| `tests/WorldCupBets.Application.Tests` | Modified | Update stubs/fixtures and add category-specific assertions. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| EF migration/snapshot inflates review size | High | Keep API/frontend untouched; isolate persistence changes. |
| Champion settlement includes player picks | Medium | Repository method filters `Category=Champion`; test it. |
| Pending stakes regress after champion settlement | Medium | Add leaderboard/My Bets focused tests. |

## Rollback Plan

Revert code and migration. If applied, run the reverse migration to recreate `champion_bets` and `special_player_bets` from `tournament_picks`, then drop `tournament_picks`.

## Dependencies

- Existing EF migration workflow and current test suite.

## Success Criteria

- [ ] One pick per user per category is enforced in domain/handler and DB.
- [ ] Existing champion/special APIs behave the same externally.
- [ ] Existing data migrates without losing selected team/player, category, stake, user, or timestamp.
- [ ] Champion settlement processes only champion picks.
- [ ] Verification: `dotnet test WorldCupBets.sln`; run `npm run build` from `frontend/` only if frontend is touched.
- [ ] Review forecast: Medium/High risk against 400 changed lines because EF migration/snapshot may dominate; chained PR decision should be revisited in tasks.
