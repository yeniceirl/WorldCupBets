# Tasks: Custom Match Challenges

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 900-1,300 including tests/UI/migration |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 foundation → PR 2 handlers/API → PR 3 frontend → PR 4 hardening |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain, EF persistence, migration, pending totals | PR 1 | base = feature/tracker branch; include persistence tests |
| 2 | Application handlers and WebApi endpoints | PR 2 | base = PR 1 branch; covers create/list/accept/admin lifecycle |
| 3 | Angular challenges feature | PR 3 | base = PR 2 branch; route/nav/service/page/wallet refresh |
| 4 | Concurrency/admin polish | PR 4 | base = PR 3 branch; optional gated tests and parity cleanup |

## Phase 1: Backend Foundation

- [x] 1.1 Create `src/WorldCupBets.Domain/Entities/MatchChallenge*.cs` with statuses, sides, stake escrow invariants, and terminal lifecycle guards.
- [x] 1.2 Create `src/WorldCupBets.Domain/Repositories/IMatchChallengeRepository.cs` for list, load-for-update, add, and active stake totals.
- [x] 1.3 Update `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` with challenge DbSets and versioned entity metadata.
- [x] 1.4 Add `src/WorldCupBets.Infrastructure/Persistence/Configurations/*Challenge*Configuration.cs` and deterministic `*AddMatchChallenges*` migration.
- [x] 1.5 Add `src/WorldCupBets.Infrastructure/Persistence/Repositories/MatchChallengeRepository.cs` and register it in `InfrastructureServiceCollectionExtensions.cs`.
- [x] 1.6 Update `src/WorldCupBets.Application/Features/Leaderboard/GetLeaderboardHandler.cs` to include active challenge escrow in pending totals.

## Phase 2: Application and API

- [x] 2.1 Create `src/WorldCupBets.Application/Features/Challenges/*` DTOs, commands, and queries for list, create, accept, settle, void, and expire.
- [x] 2.2 Implement create/accept handlers with serializable transactions, balance deduction, validation, self-accept rejection, and double-accept protection.
- [x] 2.3 Implement admin settle/void/expire handlers with payout/refund behavior and terminal-state rejection.
- [x] 2.4 Create `src/WorldCupBets.WebApi/Endpoints/ChallengesEndpoints.cs` with authenticated routes and admin-only lifecycle routes.
- [x] 2.5 Update `src/WorldCupBets.WebApi/Extensions/WebApplicationExtensions.cs` to map challenge endpoints.

## Phase 3: Frontend Feature

- [x] 3.1 Create `frontend/src/app/features/challenges/*` models and service for list/create/accept/admin actions.
- [x] 3.2 Create the standalone challenges page with loading, error, empty, create, accept, and admin action states.
- [x] 3.3 Update `frontend/src/app/app.routes.ts` and `frontend/src/app/app.component.ts` with authenticated Challenges route/nav.
- [x] 3.4 Refresh wallet-facing state after challenge create, accept, settlement, void, or expiry responses.

## Phase 4: Testing and Hardening

- [x] 4.1 Add `tests/WorldCupBets.Application.Tests/*Challenge*Tests.cs` for valid/rejected creation and escrow rollback.
- [x] 4.2 Add tests for acceptance, self-accept rejection, matched/terminal rejection, and no extra escrow.
- [x] 4.3 Add tests for admin settlement payout and void/expire refunds.
- [x] 4.4 Add persistence coverage for mappings, indexes, repository listing, and active stake totals.
- [x] 4.5 Add gated PostgreSQL concurrency tests for double accept/settle when `WORLD_CUP_BETS_TEST_CONNECTION_STRING` exists.
