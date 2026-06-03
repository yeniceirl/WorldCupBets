# Tasks: Complete CopaCoin V1 Business Logic

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 700-1,100 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 foundation → PR 2 match → PR 3 champion → PR 4 leaderboard/UI → PR 5 verify |
| Delivery strategy | ask-on-risk |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Foundation/settlement model | PR 1 | Domain, contracts, EF migration. |
| 2 | Match settlement | PR 2 | Result command and payout rules. |
| 3 | Champion settlement | PR 3 | Jackpot-backed champion payout. |
| 4 | Leaderboard/UI | PR 4 | Balance APIs and UI. |
| 5 | Verification | PR 5 | Regression and cleanup. |

## Phase 1: Foundation / Settlement Model

- [x] 1.1 Modify `src/WorldCupBets.Domain/Entities/Match.cs` with official result and `SettledAtUtc`; test closed-window eligibility.
- [x] 1.2 Modify `src/WorldCupBets.Domain/Entities/User.cs` with safe CopaCoin credit helper; test balance update.
- [x] 1.3 Create `src/WorldCupBets.Domain/Entities/TournamentSettlement.cs` for jackpot, champion result, timestamp, and undistributed remainder.
- [x] 1.4 Update `src/WorldCupBets.Domain/Repositories/` contracts for settlement loads, leaderboard, and singleton settlement.
- [x] 1.5 Update `AppDbContext`, configurations, repositories, migration, and snapshot for settlement fields/table.

## Phase 2: Match Result and Settlement

- [x] 2.1 Create `RecordMatchResultCommand` and handler to reject open windows and store Team A/Draw/Team B.
- [x] 2.2 Add match payout logic: winners split losers, all-correct returns stake, nobody-correct refunds half.
- [x] 2.3 Credit integer remainders and nobody-correct residuals to `TournamentSettlement.ChampionJackpotCc`.
- [x] 2.4 Add tests for record-after-close, reject-before-close, remainder, all-correct, nobody-correct, and idempotency.
- [x] 2.5 Wire `POST /api/matches/{id}/result` in `src/WorldCupBets.WebApi/Endpoints/MatchesEndpoints.cs`.

## Phase 3: Champion Settlement

- [x] 3.1 Create `SettleChampionCommand`, handler, and DTOs in `src/WorldCupBets.Application/Features/Bets/`.
- [x] 3.2 Implement payout: winners recover 50 CC and split losing stakes plus jackpot; remainder stays undistributed.
- [x] 3.3 Add tests for jackpot availability, winner payout, remainder recording, and idempotency.
- [x] 3.4 Wire admin `POST /api/bets/champion/settlement` in `src/WorldCupBets.WebApi/Endpoints/BetsEndpoints.cs`.

## Phase 4: Leaderboard / UI

- [x] 4.1 Create `src/WorldCupBets.Application/Features/Leaderboard/*` query/handler/DTO ordered by balance descending.
- [x] 4.2 Expose `GET /api/leaderboard` and test highest-first and equal-balance scenarios.
- [x] 4.3 Update `frontend/src/app/features/matches/*` to display result/settlement status and admin result entry.
- [x] 4.4 Replace placeholder leaderboard with service-backed loading, empty, and error states.

## Phase 5: Verification

- [x] 5.1 Run backend tests and add missing regression tests for all OpenSpec scenarios.
- [x] 5.2 Run frontend checks/tests; manually verify flows if no runner exists.
- [x] 5.3 Review migration determinism and keep endpoints thin.
