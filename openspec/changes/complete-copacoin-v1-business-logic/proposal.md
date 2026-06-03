# Proposal: Complete CopaCoin V1 Business Logic

## Intent

Problem: CopaCoin V1 can collect bets and close windows, but cannot record results, settle bets, account for the champion jackpot, or show a real CopaCoin leaderboard. This completes `docs/product/reglamento-v1.md` so balances reflect outcomes.

## Scope

### In Scope
- Add match result model/entry and idempotent settlement.
- Settle match bets: winners recover stake, split losing pool, all-correct returns only stake, nobody-correct returns 50% and sends 50% to champion jackpot.
- Track champion jackpot accounting and settle champion bets at tournament end.
- Replace placeholder leaderboard behavior with CopaCoin balance ordering.
- Expose WebApi endpoints and update matches/leaderboard UI flows.

### Out of Scope
- Exact scores, player-goal bets, head-to-head bets, classification bets.
- Advanced tie-breakers beyond current CC balance ordering.
- Non-V1 gamification such as badges or achievements.

## Capabilities

### New Capabilities
- `match-results`: official result entry and immutable/idempotent settlement trigger for matches.
- `match-bet-settlement`: V1 payout rules for all normal and special match-bet cases.
- `champion-settlement`: champion jackpot accounting and final champion-bet payout.
- `copacoin-leaderboard`: leaderboard ordered by current CopaCoin balance.

### Modified Capabilities
- None; `openspec/specs/` is currently empty.

## Approach

Extend Domain entities with result/settlement state and jackpot accounting, add Application handlers for result entry and settlements, persist through repositories/EF migrations, expose thin WebApi endpoints, and adapt Angular feature services/components. Settlement handlers should be transactional and idempotent to prevent double payouts.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/WorldCupBets.Domain/Entities/{User,Match,MatchBet,ChampionBet}.cs` | Modified | Result, settlement, balance, and jackpot state. |
| `src/WorldCupBets.Application/Features/Bets/*` | Modified/New | Match/champion settlement handlers and DTOs. |
| `src/WorldCupBets.Infrastructure/Persistence/Repositories/*` | Modified | Queries and transactional persistence for settlement. |
| `src/WorldCupBets.WebApi/Endpoints/{BetsEndpoints,MatchesEndpoints}.cs` | Modified | Admin result/settlement and leaderboard-facing APIs. |
| `frontend/src/app/features/{matches,leaderboard}/*` | Modified | Result display/entry and real CopaCoin ranking. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Double settlement corrupts balances | Med | Persist settlement markers and enforce idempotency in transactions. |
| Rounding/division edge cases | Med | Specify deterministic integer CC allocation rules in specs. |
| Jackpot source ambiguity | Low | Model champion jackpot contributions explicitly. |

## Rollback Plan

Revert code and migration. If deployed, restore DB backup or run a compensating migration to remove settlement fields and reset balances from pre-settlement snapshots.

## Dependencies

- Product rules in `docs/product/reglamento-v1.md`.

## Success Criteria

- [ ] Match results can be entered and settled once.
- [ ] All V1 match special cases update balances and champion jackpot correctly.
- [ ] Champion bets settle from base pool plus jackpot.
- [ ] Leaderboard orders users by current CopaCoin balance.
