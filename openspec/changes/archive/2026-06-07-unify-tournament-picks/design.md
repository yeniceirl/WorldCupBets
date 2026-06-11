# Design: Refactorizar apuestas de torneo para unificar Champion, BestPlayer y TopScorer en TournamentPick

## Technical Approach

Unify tournament-level picks internally behind `TournamentPick` / `tournament_picks`, while keeping current champion/special API routes and DTOs stable. Handlers remain the place for category-specific rules; persistence enforces the shared invariant: one pick per user per category.

## Architecture Decisions

| Decision | Choice | Alternatives considered | Rationale |
|---|---|---|---|
| Domain model | Replace `ChampionBet`, `SpecialPlayerBet`, and `SpecialPlayerBetCategory` with `TournamentPick` and `TournamentPickCategory` (`Champion`, `BestPlayer`, `TopScorer`). | Generic “bet type” table; keep two tables behind façade. | Preserves the requested domain name and avoids another catch-all abstraction while removing duplicated tables. |
| Naming | Use `SelectedText` for team/player display text and nullable `ExternalId` for player provider id. | `TeamName`/`PlayerName` pairs on same entity. | One selected tournament outcome concept; handlers map legacy DTO fields to category rules. |
| API compatibility | Keep `/api/bets/champion`, `/api/bets/special`, `/api/bets/special/player` and existing DTO records. | Rename routes/DTOs to tournament picks now. | Minimum clean change; avoids frontend churn and review expansion. |
| Repository | Introduce `ITournamentPickRepository`; remove split repository registrations. | Keep legacy repository interfaces as adapters. | A single contract makes settlement and pending stake filters explicit and testable. |

## Data Flow

```text
Champion route ─→ PlaceChampionBetHandler ─┐
Special route  ─→ PlaceSpecialPlayerBetHandler ─→ ITournamentPickRepository ─→ tournament_picks
Settlement     ─→ SettleChampionHandler(Category=Champion only) ────────────┘
Leaderboard    ─→ pending match stakes + pending tournament-pick stakes
```

## Domain Model and Invariants

`TournamentPick` fields: `Id`, `UserId`, `User`, `Category`, `SelectedText`, `ExternalId`, `StakeAmountCc`, `PlacedAtUtc`.

Factory methods should make category differences visible:
- `CreateChampion(userId, teamName, stake, placedAtUtc)` sets `Category=Champion`, trims `SelectedText`, and forces `ExternalId=null`.
- `CreatePlayer(userId, category, playerName, externalPlayerId, stake, placedAtUtc)` accepts only `BestPlayer`/`TopScorer`, trims text/id, and preserves nullable external id.

Handlers keep existing rule differences: champion validates against team names; player categories validate player name length and `/special/player` must reject `Champion` even though it exists in the enum.

## EF Core Mapping, Index, and Migration

Create `TournamentPickConfiguration` mapped to `tournament_picks`:
- `Category`: string conversion, max length 40, required.
- `SelectedText`: max length 160, required. This covers current team max 100 and player max 160.
- `ExternalId`: max length 80, nullable.
- `StakeAmountCc`: `numeric(18,2)`, required.
- unique index on `(UserId, Category)`.
- cascade FK to `users`.

Migration plan: create `tournament_picks`, copy `champion_bets` with `Category='Champion'`, `SelectedText=TeamName`, `ExternalId=null`; copy `special_player_bets` with existing `Category`, `SelectedText=PlayerName`, `ExternalId=ExternalPlayerId`; then drop old tables. Do not preserve old ids because no code references bet ids. Down migration recreates both old tables, splits rows by category, then drops `tournament_picks`.

## Repository Contract and Handler Integration

```csharp
public interface ITournamentPickRepository
{
    Task<bool> ExistsForUserAndCategoryAsync(int userId, TournamentPickCategory category, CancellationToken ct = default);
    Task<TournamentPick?> GetByUserAndCategoryAsync(int userId, TournamentPickCategory category, CancellationToken ct = default);
    Task<IReadOnlyList<TournamentPick>> ListByUserAndCategoriesAsync(int userId, IReadOnlyCollection<TournamentPickCategory> categories, CancellationToken ct = default);
    Task<IReadOnlyList<TournamentPick>> ListChampionForSettlementAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(IReadOnlyCollection<TournamentPickCategory> categories, CancellationToken ct = default);
    Task AddAsync(TournamentPick pick, CancellationToken ct = default);
}
```

Update static handlers to depend on `ITournamentPickRepository`; keep result DTO mapping stable (`TeamName` from `SelectedText`, player fields from `SelectedText`/`ExternalId`). Update DI to register `TournamentPickRepository` only.

## Settlement and Pending Stake Queries

`SettleChampionHandler` calls `ListChampionForSettlementAsync()`, which filters `Category == Champion` and includes `User`. Leaderboard uses one repository twice logically: champion pending stakes only when champion is not settled, plus player pending stakes for `BestPlayer` and `TopScorer` regardless of champion settlement.

## File Changes

| File | Action | Description |
|---|---|---|
| `src/WorldCupBets.Domain/Entities/TournamentPick.cs` | Create | Unified entity and factories. |
| `src/WorldCupBets.Domain/Entities/TournamentPickCategory.cs` | Create | `Champion`, `BestPlayer`, `TopScorer`. |
| `src/WorldCupBets.Domain/Repositories/ITournamentPickRepository.cs` | Create | Category-aware repository contract. |
| old champion/special entity, enum, repository, config files | Delete | Replaced by unified model. |
| application bet/leaderboard handlers | Modify | Swap repository and map legacy DTO names. |
| `AppDbContext`, DI, EF migration/snapshot | Modify | DbSet, mapping, migration, service registration. |
| affected application tests | Modify | Update stubs and assertions. |

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | one pick per category, special endpoint rejects `Champion`, category factory rules | xUnit handler/entity tests. |
| Unit | champion settlement excludes player categories | settlement test with mixed picks. |
| Unit | leaderboard pending stakes: champion excluded after settlement, player picks remain pending | focused `GetLeaderboardHandlerTests`. |
| Integration | EF unique index and migration copy shape | update PostgreSQL hardening test; inspect generated migration. |

Verification: `dotnet test WorldCupBets.sln`. Run `npm run build` from `frontend/` only if API/frontend files change.

## Migration / Rollout

No feature flag. Rollout is one migration plus code deployment. Main risk is migration/snapshot diff size and data-copy correctness; review generated SQL carefully before apply.

## Open Questions

- [ ] None blocking.
