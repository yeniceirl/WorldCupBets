## Exploration: audit-balance-reporting

### Current State
The app already exposes a bettor leaderboard that calculates each user's available balance from `User.CurrentBalanceCc`, pending match stakes from unsettled `MatchBet` rows, pending challenge stakes from active `MatchChallengePosition` rows, pending champion stakes until champion settlement, and pending special player stakes. Admin UI is a single standalone Angular page under `/admin` that uses `MatchesService` plus `AdminService`; the backend maps `/api/admin` as an Admin-only route group, while settlement endpoints live under matches/bets/challenges with Admin authorization.

There is no immutable accounting ledger or per-bet settlement record. Won/lost/pending can be derived from current entities for match bets and match challenges, and partially for champion picks after champion settlement. Special player picks are still pending because no settlement flow exists. Exact historical balance movement audit would require a new transaction ledger, not only read-side queries.

### Affected Areas
- `src/WorldCupBets.WebApi/Endpoints/AdminEndpoints.cs` — best place to add Admin-only audit endpoints such as `/api/admin/audit/balances` and `/api/admin/audit/users/{id}`.
- `src/WorldCupBets.Application/Features/Admin/` or new `Features/Audit/` — add read-side query handlers/DTOs for major and subledger reports.
- `src/WorldCupBets.Domain/Repositories/IUserRepository.cs` — needs a way to list users with ids, display names, emails, current balances, and rescue fields for audit rows.
- `src/WorldCupBets.Domain/Repositories/IMatchBetRepository.cs` and `src/WorldCupBets.Infrastructure/Persistence/Repositories/MatchBetRepository.cs` — add projections including match result/status/team names to compute won/lost/pending match bet lines.
- `src/WorldCupBets.Domain/Repositories/ITournamentPickRepository.cs` and `TournamentPickRepository.cs` — add projections for champion and special picks, including settlement state where available.
- `src/WorldCupBets.Domain/Repositories/IMatchChallengeRepository.cs` and `MatchChallengeRepository.cs` — add projections for active/settled/voided/expired challenge positions and pending reason/status.
- `frontend/src/app/features/admin/admin.service.ts` and `admin.models.ts` — add typed audit API calls and models.
- `frontend/src/app/features/admin/admin-page.component.ts` or new `frontend/src/app/features/admin/audit-*` components — add the major table and clickable subledger detail while keeping admin UI state focused.
- `frontend/src/app/app.routes.ts` and `frontend/src/app/app.component.ts` — optional if audit becomes `/admin/audit`; not needed if embedded in the existing `/admin` page.
- `tests/WorldCupBets.Application.Tests/GetLeaderboardHandlerTests.cs` — useful reference for balance/pending calculations.
- `tests/WorldCupBets.Application.Tests/EndpointAuthorizationMetadataTests.cs` — must include any new Admin audit endpoint if it should explicitly require Admin policy.

### Approaches
1. **Read-only derived audit report** — Add Admin-only query endpoints that derive major and subledger rows from the current `Users`, `MatchBets`, `TournamentPicks`, `TournamentSettlements`, and `MatchChallenges` tables.
   - Pros: minimal production risk, no migration, reuses leaderboard and My Bets calculation patterns, good fit for the requested audit page.
   - Cons: not a true immutable ledger; exact historical balance movements and some won/lost payout amounts are inferred, not recorded.
   - Effort: Medium

2. **Introduce an accounting transaction ledger** — Persist debits, credits, settlement payouts, jackpot movements, rescues, and reversals as immutable accounting entries, then build audit UI from that ledger.
   - Pros: strongest accounting/audit model, exact historical trace, future-proof for reconciliation.
   - Cons: high-risk migration/backfill, requires touching every balance mutation path, larger review surface for production.
   - Effort: High

3. **Frontend-only composition from existing APIs** — Reuse leaderboard, matches, champion, and special endpoints to assemble admin audit data in Angular.
   - Pros: fastest visible UI spike, almost no backend work.
   - Cons: misses all users' private bet details, leaks bettor-scoped assumptions into admin, cannot produce reliable subledgers.
   - Effort: Low but not recommended

### Recommendation
Use the read-only derived audit report as the SDD scope. Add backend Admin-only audit endpoints with explicit DTOs for: (1) major rows per user containing available balance, pending stake total, derived total balance, won total, lost total, and pending total; (2) subledger rows per user grouped by bet source (`MatchBet`, `Champion`, `BestPlayer`, `TopScorer`, `Challenge`) with status, stake, derived result, payout/credit when safely derivable, and a human-readable pending reason.

Keep exact accounting-ledger work out of this change unless the user confirms they need historical reconciliation. The page should label the report as current-state audit/derived balances to avoid promising accounting-grade immutability.

### Risks
- Current persistence does not store balance transactions or per-bet payout records; exact `won`/`lost` totals may require recomputing settlement math or adding a ledger.
- Special player bets currently have no settlement source, so they remain pending with reasons like `Waiting for tournament special settlement`.
- Champion settlement only records the champion and aggregate jackpot/undistributed amounts; per-user champion payout is inferable but not recorded.
- Admin subledger endpoints expose sensitive user/bet data and must stay under Admin authorization with endpoint metadata tests updated.
- Large reports can become N+1-heavy if implemented with entity loading instead of read-side projections.

### Ready for Proposal
Yes — propose a current-state Admin audit report, not a full accounting ledger. Tell the user the safe first slice can deliver the major/subledger page from existing data, but true accounting-grade historical audit requires a separate ledger/backfill change.
