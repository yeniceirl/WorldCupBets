# Apply Progress: Custom Match Challenges

## Mode

Standard mode. `openspec/config.yaml` has `strict_tdd: false`; no strict TDD evidence is required.

## Workload / PR Boundary

- Delivery strategy: auto-chain
- Chain strategy: feature-branch-chain
- Current work unit: PR 4 hardening/testing only
- Boundary: Challenge handler, persistence, and gated PostgreSQL concurrency test coverage. No new product features or frontend changes were added in this slice.
- Review budget impact: Focused test-only hardening slice; additions are isolated to application test files and SDD progress artifacts.

## Completed Tasks

- [x] 1.1 Create `src/WorldCupBets.Domain/Entities/MatchChallenge*.cs` with statuses, sides, stake escrow invariants, and terminal lifecycle guards.
- [x] 1.2 Create `src/WorldCupBets.Domain/Repositories/IMatchChallengeRepository.cs` for list, load-for-update, add, and active stake totals.
- [x] 1.3 Update `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` with challenge DbSets and versioned entity metadata.
- [x] 1.4 Add `src/WorldCupBets.Infrastructure/Persistence/Configurations/*Challenge*Configuration.cs` and deterministic `*AddMatchChallenges*` migration.
- [x] 1.5 Add `src/WorldCupBets.Infrastructure/Persistence/Repositories/MatchChallengeRepository.cs` and register it in `InfrastructureServiceCollectionExtensions.cs`.
- [x] 1.6 Update `src/WorldCupBets.Application/Features/Leaderboard/GetLeaderboardHandler.cs` to include active challenge escrow in pending totals.
- [x] 2.1 Create `src/WorldCupBets.Application/Features/Challenges/*` DTOs, commands, and queries for list, create, accept, settle, void, and expire.
- [x] 2.2 Implement create/accept handlers with serializable transactions, balance deduction, validation, self-accept rejection, and double-accept protection.
- [x] 2.3 Implement admin settle/void/expire handlers with payout/refund behavior and terminal-state rejection.
- [x] 2.4 Create `src/WorldCupBets.WebApi/Endpoints/ChallengesEndpoints.cs` with authenticated routes and admin-only lifecycle routes.
- [x] 2.5 Update `src/WorldCupBets.WebApi/Extensions/WebApplicationExtensions.cs` to map challenge endpoints.
- [x] 3.1 Create `frontend/src/app/features/challenges/*` models and service for list/create/accept/admin actions.
- [x] 3.2 Create the standalone challenges page with loading, error, empty, create, accept, and admin action states.
- [x] 3.3 Update `frontend/src/app/app.routes.ts` and `frontend/src/app/app.component.ts` with authenticated Challenges route/nav.
- [x] 3.4 Refresh wallet-facing state after challenge create, accept, settlement, void, or expiry responses.
- [x] 4.1 Add `tests/WorldCupBets.Application.Tests/*Challenge*Tests.cs` for valid/rejected creation and escrow rollback.
- [x] 4.2 Add tests for acceptance, self-accept rejection, matched/terminal rejection, and no extra escrow.
- [x] 4.3 Add tests for admin settlement payout and void/expire refunds.
- [x] 4.4 Add persistence coverage for mappings, indexes, repository listing, and active stake totals.
- [x] 4.5 Add gated PostgreSQL concurrency tests for double accept/settle when `WORLD_CUP_BETS_TEST_CONNECTION_STRING` exists.

## Verification

- `dotnet ef migrations add AddMatchChallenges --project "src/WorldCupBets.Infrastructure/WorldCupBets.Infrastructure.csproj" --startup-project "src/WorldCupBets.WebApi/WorldCupBets.WebApi.csproj" --output-dir "Persistence/Migrations"` — succeeded in PR 1.
- `dotnet build "src/WorldCupBets.WebApi/WorldCupBets.WebApi.csproj"` — passed with 0 warnings and 0 errors in PR 1 and after PR 2 changes.
- `dotnet test` — passed 107 tests in PR 1; later full-solution attempt aborted in the local snap .NET launcher with internal CLR error `0x80131506`.
- `dotnet test "tests/WorldCupBets.Application.Tests/WorldCupBets.Application.Tests.csproj"` — passed: 112 tests in PR 2.
- `npm run build` from `frontend/` — passed after PR 3 frontend changes.
- `dotnet test "tests/WorldCupBets.Application.Tests/WorldCupBets.Application.Tests.csproj"` — passed: 127 tests after PR 4 hardening tests.
- `dotnet test` — passed: 127 tests after PR 4 hardening tests.

## Deviations

None — implementation matches the Phase 1, Phase 2, Phase 3, and Phase 4 design boundaries. Text limits were set in the domain (`280` claim characters, `120` side characters) because the spec required length limits but did not define exact values.

## Issues

The prior full-solution `dotnet test` command aborted in the local snap .NET launcher during PR 3, but the PR 4 run of `dotnet test` passed. PostgreSQL repository/concurrency tests are gated and return early unless `WORLD_CUP_BETS_TEST_CONNECTION_STRING` points to a disposable test database.
