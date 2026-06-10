# Proposal: Custom Match Challenges

## Intent

Users need personalized match-scoped retos with friends beyond fixed winner bets or explicit tournament picks. Add free-text, head-to-head CopaCoin challenges without overloading `MatchBet` or `TournamentPick`.

## Scope

### In Scope
- Match-scoped challenges with free-text claim, equal stake, creator side, and opposite taker side.
- Lifecycle: `Open`, `Matched`, `Settled`, `Voided`, `Expired`.
- Immediate CopaCoin escrow, payout/refund settlement, and pending-stake inclusion.
- Authenticated listing, creation, acceptance, and admin settlement/voiding.
- Basic authenticated Angular challenge page/route/service with wallet refresh behavior.

### Out of Scope
- Extending `MatchBet` or `TournamentPick` for custom claims.
- Multi-taker pools, comments, reports, AI settlement, self-settlement, or automated resolution.
- Public unauthenticated challenge participation.

## Capabilities

### New Capabilities
- `match-challenges`: Match-scoped custom reto creation, acceptance, escrow, lifecycle, manual settlement, refunding, and pending stake reporting.

### Modified Capabilities
- None.

## Approach

Create a separate challenge aggregate and persistence model (`match_challenges`, `match_challenge_positions`) with repository contracts and Wolverine handlers. Use existing serializable transaction and `User` CopaCoin debit/credit patterns. Add a thin `/api/challenges` endpoint group and an authenticated standalone Angular feature. Settlement is admin/manual in v1 because free-text claims are not resolvable from match results.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/WorldCupBets.Domain/Entities` | New | Challenge aggregate, positions, statuses. |
| `src/WorldCupBets.Application/Features` | New | Create, list, accept, expire, void, settle handlers. |
| `src/WorldCupBets.Infrastructure/Persistence` | Modified | EF configs, DbSet, migration, repositories. |
| `src/WorldCupBets.WebApi/Endpoints` | New | Thin challenges endpoint group. |
| `frontend/src/app` | Modified | Route/nav plus challenges feature service/page. |
| `tests/WorldCupBets.Application.Tests` | New | Lifecycle, escrow, race, and settlement tests. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Free-text abuse/moderation | Med | Length limits, authenticated users, admin void. |
| Acceptance race | Med | Serializable transactions and uniqueness/locking. |
| Wallet inconsistency | Med | Include challenge escrow in pending totals. |
| Review size exceeds 400 lines | High | Plan backend/API/frontend in reviewable slices. |

## Rollback Plan

Disable/remove `/api/challenges` route and UI navigation, then revert challenge migrations/entities/handlers. If data exists, refund non-settled escrow before dropping challenge tables.

## Dependencies

- Existing authentication, admin policy, CopaCoin user balance methods, match data, EF migrations.

## Assumptions and Decision Gaps

- V1 settlement is admin-only; non-admin outcome requests are deferred.
- Expiry timing and moderation copy need product confirmation during spec/design.

## Success Criteria

- [ ] Users can create and accept binary match retos with equal escrowed CopaCoin stakes.
- [ ] Admins can settle, void, or expire challenges with correct payout/refund behavior.
- [ ] Challenge stakes appear in pending totals and wallet displays stay consistent.
