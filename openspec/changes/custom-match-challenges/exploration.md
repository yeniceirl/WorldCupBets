## Exploration: custom-match-challenges

### Current State
The app has two established betting paths. Per-match winner bets use `MatchBet` rows keyed by user/match, deduct the match phase stake once, allow selection changes while the match betting window is open, and settle automatically from the official match result. Tournament bets now use generic `TournamentPick` rows, but the categories are still explicit (`Champion`, `BestPlayer`, `TopScorer`) and have one pick per user/category. CopaCoin balance is deducted at placement, pending stakes are re-added for display in leaderboard/My Bets, and settlement credits winners through application handlers inside serializable transactions.

There is no existing challenge/reto model or UI. The closest reusable patterns are: authenticated Minimal API endpoint groups, Wolverine command/query handlers, repository contracts over EF Core, one-page Angular standalone features behind feature services, wallet summary refresh after stake changes, and pending stake aggregation for leaderboard/My Bets.

### Affected Areas
- `src/WorldCupBets.Domain/Entities/MatchBet.cs` — current per-match fixed-market model; useful contrast but should not be overloaded for custom text challenges.
- `src/WorldCupBets.Domain/Entities/TournamentPick.cs` — generic-ish pick model, but category uniqueness and tournament scope make it a poor fit for many match-scoped user-created challenges.
- `src/WorldCupBets.Domain/Entities/User.cs` — CopaCoin affordability, deduction, crediting, and dead-rescue behavior must be reused for challenge escrow and payout.
- `src/WorldCupBets.Application/Features/Bets/PlaceMatchBetHandler.cs` — placement transaction, stake deduction, closed-window validation, and change semantics provide the main application pattern.
- `src/WorldCupBets.Application/Features/Matches/RecordMatchResultHandler.cs` — settlement/payout precedent; challenge settlement may need a separate admin/manual flow because custom claims cannot be derived from `Home/Draw/Away`.
- `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` and `Configurations/*` — new EF entities/configurations/migrations are required for challenges, positions, and settlement state.
- `src/WorldCupBets.WebApi/Endpoints/BetsEndpoints.cs` — current bet endpoints; challenges likely deserve a separate `/api/challenges` group to keep WebApi thin and avoid growing `BetsEndpoints` further.
- `src/WorldCupBets.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` — new repository contracts must be registered explicitly.
- `frontend/src/app/app.routes.ts` and `frontend/src/app/app.component.ts` — add an authenticated Challenges/Retos route and navigation entry.
- `frontend/src/app/features/matches/matches.service.ts` and `matches.models.ts` — existing HTTP/model location may be reused or split into a dedicated challenges service/model for clearer feature boundaries.
- `frontend/src/app/features/bets/bets-page.component.ts` — wallet/pending-stake display patterns and My Bets ledger may need to include challenge stakes.
- `tests/WorldCupBets.Application.Tests/*Bet*Tests.cs` — existing handler/unit-test style should be copied for challenge creation, acceptance, cancellation/expiry, and settlement rules.

### Approaches
1. **Free-text matched challenge market** — Creator writes a match-scoped claim, stake amount, and creator side; another bettor accepts the opposite side for the same stake. Settlement is manual/admin: mark creator side won, taker side won, void/refund, or expire/refund.
   - Pros: Preserves the user's core requirement (no predefined bet taxonomy), simple mental model, avoids unmaintainable custom rules, and fits CopaCoin escrow/payout patterns.
   - Cons: Requires moderation/settlement authority because the app cannot know whether "Messi scores 3 goals" happened unless someone decides it; needs clear lifecycle/statuses.
   - Effort: Medium

2. **Reusable generic challenge with structured outcome options** — Creator writes a prompt plus two custom labels (e.g., "Messi scores 3+" vs "He doesn't"), and other users take one available side; settlement picks one option.
   - Pros: More expressive than a hardcoded opposite side and supports non-binary wording while still avoiding predefined domain bet types.
   - Cons: Slightly more complex UI and data model; if multiple users can join both sides, payout math becomes a pool-market design instead of a simple head-to-head bet.
   - Effort: Medium/High

3. **Extend `TournamentPick` or `MatchBet` with custom categories/text** — Add custom text/category fields to existing bet tables and reuse existing endpoints.
   - Pros: Fewer new types at first glance.
   - Cons: Mixes unrelated invariants, breaks existing uniqueness assumptions (`user/match`, `user/category`), complicates settlement, and makes repositories/leaderboard logic harder to reason about.
   - Effort: High

### Recommendation
Use Approach 1 for v1: a new challenge aggregate separate from `MatchBet` and `TournamentPick`, with tables such as `match_challenges` and `match_challenge_positions`. Model a strict lifecycle: `Open`, `Matched`, `Settled`, `Voided`, `Expired`. Require creator/taker stakes to be escrowed immediately by deducting CopaCoins inside serializable transactions; pay the winning side both stakes on settlement, or refund both on void/expiry. Keep settlement manual/admin for v1 because custom natural-language claims are intentionally not machine-resolvable.

This gives users the flexible fun mechanic they asked for without corrupting the existing fixed-market bet models. It also creates a clean seam for later enhancements like comments, reporting, multiple takers, or AI-assisted settlement suggestions without depending on them now.

### Risks
- Free-text claims create moderation and abuse risks; v1 should limit length, require authenticated bettors, and expose admin void/settlement controls.
- Custom challenges cannot be settled automatically from current match results; product copy must make manual settlement/refund expectations explicit.
- Escrowed stakes must be included in leaderboard/My Bets pending totals, otherwise wallet display will look inconsistent.
- Race conditions are likely when two users try to accept the same open challenge; repository queries need locking/unique constraints and existing serializable transaction patterns.
- Review size can exceed the 400-line budget if backend model, migrations, API, Angular page, navigation, and tests ship together; tasks should plan reviewable slices.

### Ready for Proposal
Yes — propose a v1 custom match challenge feature with free-text, binary head-to-head challenges, escrowed equal stakes, authenticated challenge listing/creation/acceptance, admin/manual settlement or voiding, and pending stake integration. The orchestrator should confirm whether challenge settlement is admin-only for v1 and whether non-admin creators may request/mark outcomes later.
