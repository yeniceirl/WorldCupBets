# Design: Audit Balance Reporting

## Technical Approach

Add a read-only Admin audit slice that explains current balances from existing state, not a new ledger. Keep WebApi thin in `src/WorldCupBets.WebApi/Endpoints/AdminEndpoints.cs`, add Wolverine queries/DTOs under `src/WorldCupBets.Application/Features/Admin/`, and extend repository projection contracts in `src/WorldCupBets.Domain/Repositories/*` plus EF implementations in `src/WorldCupBets.Infrastructure/Persistence/Repositories/*`. Angular stays inside `frontend/src/app/features/admin/` using `AdminService` and either a new `audit-balance-report.component.ts` embedded in `admin-page.component.ts` or a routed `audit-balance-page.component.ts` at `/admin/audit`; the lower-risk choice is a child component inside the existing `/admin` page.

## Architecture Decisions

| Decision | Why | Impact |
|---|---|---|
| Derived audit report, not immutable ledger | Existing writes only mutate `User.CurrentBalanceCc`; there is no transaction history to replay | Safe, read-only scope but explicit "derived" labeling is mandatory |
| Reuse existing Admin route group | `AdminEndpoints` already owns `/api/admin` with `RequireAuthorization("Admin")` | Add `GET /api/admin/audit/balances` and `GET /api/admin/audit/users/{userId}` |
| Use projection queries, not entity loading | Current repositories already expose aggregate reads for leaderboard pending totals | Prevent N+1 and keep report feasible for all users |
| Separate summary and detail DTOs | Major table and subledger have different shapes and payload sizes | Faster initial page load and cleaner frontend state |
| Keep UI under existing admin feature | Current admin UI is one standalone feature at `/admin` | Lowest routing and navigation risk |

## Backend Design

Queries:
- `GetAuditBalanceSummaryQuery` -> one row per user.
- `GetAuditUserSubledgerQuery` -> one user plus ordered detail lines.

DTOs:
- `AuditBalanceSummaryRowDto`: `userId`, `displayName`, `email`, `availableBalanceCc`, `pendingTotalCc`, `derivedTotalBalanceCc`, `wonTotalCc`, `lostTotalCc`, `rescueDebtCc`, `rescueCount`.
- `AuditUserSubledgerDto`: user summary fields plus `items`.
- `AuditLedgerItemDto`: `sourceType`, `sourceId`, `label`, `placedAtUtc`, `stakeAmountCc`, `status`, `result`, `creditAmountCc`, `lossAmountCc`, `pendingAmountCc`, `pendingReason`, `metadata`.

Repository additions should be projection-oriented, for example:
- `IUserRepository`: audit user list projection.
- `IMatchBetRepository`: match bet audit rows including team names, `OfficialResult`, `SettledAtUtc`, and stake.
- `IMatchChallengeRepository`: challenge audit rows including status, winner side, participant side, claim text, and timestamps.
- `ITournamentPickRepository`: champion/special pick audit rows including category, selected text, stake, and placement time.
- `ITournamentSettlementRepository`: champion settlement snapshot for jackpot and settled champion.

Derivation rules:
- `availableBalanceCc` = `User.CurrentBalanceCc`.
- `pendingTotalCc` = unsettled match stakes + active challenge stakes (`Open`/`Matched`) + champion stakes if `ChampionSettledAtUtc` is null + all special player stakes.
- `derivedTotalBalanceCc` = `availableBalanceCc + pendingTotalCc`.
- Match bet `won/lost`: use `RecordMatchResultHandler` math. Winner credit = `stake + floor(losingPool / winners)` when losers exist, refund-only when no losers; loser loss = full stake except no-winner settlement where loss = residual half and refund half is credit.
- Challenge `won/lost`: settled winner credit = sum of both stakes; settled loser loss = stake; voided/expired/open/matched remain pending/refunded as applicable.
- Champion `won/lost`: when settled, winning credit = `stake + floor((losingChampionPool + ChampionJackpotCc) / winners)` and losers lose stake. Before settlement, all champion picks are pending.
- Special player picks: always pending in this change because no settlement flow exists.

Pending reason classification:
- Match bet: `Waiting for official match result`.
- Challenge open: `Waiting for another bettor to accept`.
- Challenge matched: `Waiting for admin challenge settlement`.
- Champion pick: `Waiting for champion settlement`.
- Special pick: `Waiting for tournament special settlement`.

## Frontend Design

Keep models and HTTP calls in `frontend/src/app/features/admin/admin.models.ts` and `admin.service.ts`. Add an audit table component under `frontend/src/app/features/admin/` with expandable or click-through user detail. Place it below current control-room cards inside `admin-page.component.ts` to avoid route churn; if the page becomes noisy, move only the audit UI to `/admin/audit` and keep `/admin` as the control room.

## Performance and Authorization

Run summary as set-based grouped projections and fetch subledger per user on demand. Do not load `User`, `Match`, `MatchBet`, `TournamentPick`, and `MatchChallenge` entities into memory for every row. Keep both endpoints under `/api/admin` group authorization and extend `tests/WorldCupBets.Application.Tests/EndpointAuthorizationMetadataTests.cs`. Add application tests for derivation edge cases using `GetLeaderboardHandlerTests.cs` as the pattern source.

## Highest-Risk Areas

| Area | Risk |
|---|---|
| Match/champion payout reconstruction | Rounding and jackpot remainder must exactly mirror existing handlers |
| "Won" vs current balance semantics | Current balance is mutated state, so report totals must avoid double-counting credited stake |
| Challenge pending/refund states | Open, matched, voided, expired, and settled need distinct audit meanings |
| Large user lists | Summary must stay projection-based and detail must lazy load |

## Tradeoff vs True Ledger

This design is low risk because it adds no writes or migrations, but it cannot provide immutable historical reconciliation. A true ledger would be the right architecture for accounting-grade audit, yet it would require touching every balance mutation path and backfilling history.

## Open Questions

- Should the first frontend slice embed audit inside `/admin`, or reserve `/admin/audit` immediately for cleaner separation?
- Should `voided`/`expired` challenge rows show as `refunded` in the subledger result column or remain a separate terminal status label?
