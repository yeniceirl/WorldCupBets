## Exploration: Refactorizar apuestas de torneo para unificar Champion, BestPlayer y TopScorer en TournamentPick

### Current State

The backend currently models tournament picks as two separate concepts:

- `ChampionBet` stored in `champion_bets`, with `UserId`, `TeamName`, `StakeAmountCc`, and `PlacedAtUtc`.
- `SpecialPlayerBet` stored in `special_player_bets`, with `UserId`, `Category`, `PlayerName`, `ExternalPlayerId`, `StakeAmountCc`, and `PlacedAtUtc`; `Category` is currently `BestPlayer | TopScorer`.

One-pick enforcement exists at persistence and handler levels, but split by table: `champion_bets` has unique `UserId`; `special_player_bets` has unique `(UserId, Category)`. Both place flows use serializable transactions, deduct 50 CC, apply dead-rescue eligibility, and close at `IMatchRepository.GetChampionBettingClosesAtUtcAsync()`.

The public API is also split: `/api/bets/champion` for champion market/place, `/api/bets/special` and `/api/bets/special/player` for player pick market/place. The frontend consumes both shapes and renders them together on `/bets` as “Tournament picks”.

Champion settlement currently depends only on `IChampionBetRepository.ListForSettlementAsync()` and `ChampionBet.TeamName`; player picks are not settled today. Leaderboard pending stakes combine match stakes, unsettled champion stakes, and all special-player stakes. My Bets computes pending tournament stake from the champion market plus special market.

### Affected Areas

- `src/WorldCupBets.Domain/Entities/ChampionBet.cs` — replace/remove with unified `TournamentPick` model.
- `src/WorldCupBets.Domain/Entities/SpecialPlayerBet.cs` — replace/remove with unified `TournamentPick` model.
- `src/WorldCupBets.Domain/Entities/SpecialPlayerBetCategory.cs` — broaden/rename to `TournamentPickCategory` with `Champion`, `BestPlayer`, `TopScorer`.
- `src/WorldCupBets.Domain/Repositories/IChampionBetRepository.cs` — likely replaced by category-aware `ITournamentPickRepository` methods.
- `src/WorldCupBets.Domain/Repositories/ISpecialPlayerBetRepository.cs` — likely replaced by the same repository.
- `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` — replace `DbSet<ChampionBet>` and `DbSet<SpecialPlayerBet>` with `DbSet<TournamentPick>`.
- `src/WorldCupBets.Infrastructure/Persistence/Configurations/ChampionBetConfiguration.cs` — remove/replace mapping.
- `src/WorldCupBets.Infrastructure/Persistence/Configurations/SpecialPlayerBetConfiguration.cs` — remove/replace mapping.
- `src/WorldCupBets.Infrastructure/Persistence/Repositories/ChampionBetRepository.cs` — replace with category-filtered methods, especially champion settlement and stake aggregation.
- `src/WorldCupBets.Infrastructure/Persistence/Repositories/SpecialPlayerBetRepository.cs` — merge into unified repository.
- `src/WorldCupBets.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` — update repository registrations.
- `src/WorldCupBets.Infrastructure/Persistence/Migrations/*` and `AppDbContextModelSnapshot.cs` — require a deterministic migration from `champion_bets` + `special_player_bets` to `tournament_picks`.
- `src/WorldCupBets.Application/Features/Bets/PlaceChampionBetHandler.cs` — create a `TournamentPick` with category `Champion` and selected text as team name.
- `src/WorldCupBets.Application/Features/Bets/PlaceSpecialPlayerBetHandler.cs` — create a `TournamentPick` with category `BestPlayer` or `TopScorer` and selected text/external id.
- `src/WorldCupBets.Application/Features/Bets/GetChampionBetMarketHandler.cs` — read current user pick by category `Champion`.
- `src/WorldCupBets.Application/Features/Bets/GetSpecialBetMarketHandler.cs` — list current user picks by categories `BestPlayer`/`TopScorer`.
- `src/WorldCupBets.Application/Features/Bets/SettleChampionHandler.cs` — settle only category `Champion` picks, comparing selected text to champion team.
- `src/WorldCupBets.Application/Features/Leaderboard/GetLeaderboardHandler.cs` — aggregate pending tournament pick stakes; exclude champion after champion settlement while keeping player picks pending.
- `src/WorldCupBets.WebApi/Endpoints/BetsEndpoints.cs` — can preserve current routes/DTO contracts for minimum frontend impact while calling renamed/unified application concepts internally.
- `tests/WorldCupBets.Application.Tests/PlaceChampionBetHandlerTests.cs` — update stubs/types for unified repository.
- `tests/WorldCupBets.Application.Tests/PlaceSpecialPlayerBetHandlerTests.cs` — update category/entity/repository names and one-per-category checks.
- `tests/WorldCupBets.Application.Tests/GetLeaderboardHandlerTests.cs` — update pending stake stubs and verify champion settlement exclusion still works.
- `tests/WorldCupBets.Application.Tests/SettleChampionHandlerTests.cs` — update settlement fixtures to unified champion picks.
- `tests/WorldCupBets.Application.Tests/PostgresConcurrencyHardeningTests.cs` — update EF seeding and repository use.
- `frontend/src/app/features/matches/matches.models.ts` — frontend only needs changes if API contracts are renamed; can stay untouched for minimum change.
- `frontend/src/app/features/matches/matches.service.ts` — can stay untouched if existing endpoints remain.
- `frontend/src/app/features/bets/bets-page.component.ts` — can stay untouched if response DTOs remain stable.
- `frontend/e2e/core-flows.spec.ts` — can stay untouched if API contracts remain stable; otherwise update mocked route shapes.

### Approaches

1. **Unified domain and table, stable API contracts** — Introduce `TournamentPick` / `tournament_picks` internally while keeping existing endpoint URLs and DTO shapes for now.
   - Pros: Aligns persistence/domain with the requested model, minimizes frontend churn, keeps review scope tighter, preserves current UX and API behavior.
   - Cons: Some application DTO names remain legacy (`ChampionBetMarketDto`, `SpecialBetMarketDto`) unless renamed in a separate cleanup.
   - Effort: Medium.

2. **Full rename through API and frontend** — Rename backend DTOs/routes/client types around `TournamentPick` in one pass.
   - Pros: Cleaner vocabulary end-to-end.
   - Cons: Larger diff, more E2E updates, more risk for little behavior gain; likely exceeds the “minimum and clean” preference.
   - Effort: Medium/High.

3. **Only repository abstraction, keep both tables** — Add a façade repository but leave `champion_bets` and `special_player_bets` as-is.
   - Pros: Smallest code movement.
   - Cons: Does not satisfy the core goal of avoiding separate tables for conceptually equal tournament picks.
   - Effort: Low, but not recommended.

### Recommendation

Proceed with Approach 1. Model `TournamentPick` explicitly with `UserId`, `Category`, `SelectedText`, `ExternalId`, `StakeAmountCc`, and `PlacedAtUtc`, mapped to `tournament_picks` with a unique `(UserId, Category)` index. Keep category-specific rules in handlers: champion validates against team options and has no external id; BestPlayer/TopScorer validate player text/external id. Keep existing public API contracts initially to avoid unnecessary frontend changes.

Migration direction: create `tournament_picks`, copy `champion_bets` as category `Champion` with `SelectedText = TeamName` and `ExternalId = null`, copy `special_player_bets` using existing category and `SelectedText = PlayerName`, then drop the old tables. The reverse migration can recreate old tables and split rows by category.

### Risks

- Existing production data migration must preserve bet IDs only if external references exist; no references were found in explored code, but confirm before implementation.
- Settlement must filter only `Champion` picks, otherwise player picks would be incorrectly included in champion jackpot settlement.
- Pending leaderboard stakes must still exclude champion picks after champion settlement but keep BestPlayer/TopScorer pending until their own future settlement exists.
- Renaming DTOs/routes in the same change would expand frontend and E2E scope; keep contracts stable unless proposal explicitly accepts that cost.
- EF migration snapshot changes will be relatively large even if behavior is small.

### Ready for Proposal

Yes — propose a minimum-change refactor: internal domain/persistence unification to `TournamentPick`, stable external API, category-specific validation in handlers, and targeted tests around one pick per user per category, champion settlement filtering, migration data copy, and leaderboard pending stake aggregation.

Verification/build commands discovered: backend `dotnet test WorldCupBets.sln`; frontend `npm run build` from `frontend/` if frontend is touched; E2E `npm run e2e` from `frontend/` when route/contract behavior changes.
