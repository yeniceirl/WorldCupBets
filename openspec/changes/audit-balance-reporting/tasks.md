# Tasks: Audit Balance Reporting

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | 650-950 including tests and Angular UI |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 projections/contracts -> PR 2 queries/API/tests -> PR 3 frontend/verification |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|---|---|---|---|
| 1 | Projection contracts and EF read methods | PR 1 | Safe backend foundation with targeted repository tests. |
| 2 | Query handlers, admin endpoints, authorization | PR 2 | Depends on PR 1; include backend behavior tests. |
| 3 | Angular models/service, summary table, drill-down | PR 3 | Depends on API; include UI build and manual verification. |

## Phase 1: Projection Foundation

- [ ] 1.1 Add projection DTO/contracts under `src/WorldCupBets.Application/Features/Admin/` for audit summary rows, subledger items, and derived-report metadata labels.
- [ ] 1.2 Extend `IUserRepository`, `IMatchBetRepository`, `IMatchChallengeRepository`, `ITournamentPickRepository`, and `ITournamentSettlementRepository` with projection-oriented audit read methods only.
- [ ] 1.3 Implement EF projection queries in `src/WorldCupBets.Infrastructure/Persistence/Repositories/` so summary stays set-based and drill-down can fetch one user on demand.

## Phase 2: Derivation Queries and API

- [ ] 2.1 Create `GetAuditBalanceSummaryQuery` and `GetAuditUserSubledgerQuery` handlers in `src/WorldCupBets.Application/Features/Admin/` that reuse existing settlement math for match, challenge, champion, and special-pick derivation.
- [ ] 2.2 Encode explicit pending-reason mapping for unsettled match bets, open/matched challenges, unsettled champion picks, and always-pending special picks.
- [ ] 2.3 Wire `GET /api/admin/audit/balances` and `GET /api/admin/audit/users/{userId}` in `src/WorldCupBets.WebApi/Endpoints/AdminEndpoints.cs`, keeping responses clearly labeled as derived current-state reporting.
- [ ] 2.4 Extend endpoint registration and admin authorization metadata coverage so both audit endpoints remain protected by the `Admin` policy.

## Phase 3: Backend Tests

- [ ] 3.1 Add application tests for summary totals and subledger outcomes using `GetLeaderboardHandlerTests`, `RecordMatchResultHandlerTests`, `SettleChampionHandlerTests`, and `ChallengeHandlerTests` as parity sources.
- [ ] 3.2 Cover edge cases: no-activity user, no-winner match settlement, split-winner rounding, challenge refund/void/expire states, champion-settled vs special-pick-pending behavior.
- [ ] 3.3 Add authorization metadata assertions for the two new admin audit routes.

## Phase 4: Angular Admin Audit UI

- [ ] 4.1 Extend `frontend/src/app/features/admin/admin.models.ts` and `admin.service.ts` with summary/detail models and HTTP methods.
- [ ] 4.2 Add an audit balance component under `frontend/src/app/features/admin/` and embed it in `admin-page.component.ts` below the control-room cards.
- [ ] 4.3 Build the summary table with derived-state labeling, loading/error/empty states, and per-user drill-down that lazy-loads the subledger.
- [ ] 4.4 Render grouped detail rows with stake, outcome, pending amount, pending reason, and compact metadata for source context.

## Phase 5: Verification

- [ ] 5.1 Run targeted backend tests for admin audit handlers, payout parity, and endpoint authorization.
- [ ] 5.2 Run frontend build/tests for the admin feature and manually verify admin-only access, summary totals, and drill-down behavior against seeded scenarios.
