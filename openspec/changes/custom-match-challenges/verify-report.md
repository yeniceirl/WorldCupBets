## Verification Report

**Change**: custom-match-challenges  
**Version**: N/A  
**Mode**: Standard (`openspec/config.yaml` has `strict_tdd: false`)  
**Artifact Store Mode**: hybrid  
**Verification Date**: 2026-06-09  
**Re-verify Context**: after adding match betting-window enforcement for challenge creation/acceptance and creator-only cancellation for open challenges.

### Completeness

| Metric | Value |
|--------|-------|
| Tasks total | 18 |
| Tasks complete | 18 |
| Tasks incomplete | 0 |
| Spec scenarios checked | 11 |
| Spec scenarios compliant | 11 |
| Review budget | 400 changed lines; planned delivery remains auto-chain / feature-branch-chain |

### Build & Tests Execution

**Backend tests**: ✅ Passed
```text
Command: dotnet test
Result: Passed! - Failed: 0, Passed: 133, Skipped: 0, Total: 133, Duration: 4 s
```

**Frontend build**: ✅ Passed
```text
Command: npm run build (from frontend/)
Result: Application bundle generation complete. Output: /home/yrl/repos/bet/frontend/dist/frontend
```

**OpenSpec validation**: ⚠️ Not executed
```text
Command: openspec validate custom-match-challenges --strict
Result: /bin/bash: line 1: openspec: command not found
```

**Coverage**: ➖ Not available; no coverage command is configured in `openspec/config.yaml`.

### Spec Compliance Matrix

| Requirement | Scenario | Test / Evidence | Result |
|-------------|----------|-----------------|--------|
| Custom Challenge Creation | Creator opens a valid challenge | `ChallengeHandlerTests.Create_Deducts_Creator_Balance_And_Stores_Open_Challenge`; `CreateChallengeHandler`; `MatchChallenge.Create` | ✅ COMPLIANT |
| Custom Challenge Creation | Creation is rejected | `Create_Rejects_Invalid_Match_Without_Escrow`, `Create_Rejects_Invalid_Text_Without_Escrow`, `Create_Rejects_Insufficient_Balance_Without_Escrow` | ✅ COMPLIANT |
| Custom Challenge Creation | Creation is rejected after the match window closes | `Create_Rejects_Closed_Match_Betting_Window_Without_Escrow`; `CreateChallengeHandler` checks `match.IsBettingOpenAt(DateTime.UtcNow)` before escrow | ✅ COMPLIANT |
| Challenge Listing and Acceptance | Taker accepts an open challenge | `ChallengeHandlerTests.Accept_Deducts_Taker_Balance_And_Matches_Challenge`; `AcceptChallengeHandler` | ✅ COMPLIANT |
| Challenge Listing and Acceptance | Acceptance race or self-acceptance is rejected | `Accept_Rejects_Self_Accept_Without_Deducting_Balance`, `Accept_Rejects_Already_Matched_Challenge_Without_Extra_Escrow`, `Accept_Rejects_Terminal_Challenge_Without_Extra_Escrow`; gated PostgreSQL double-accept test source is present | ✅ COMPLIANT |
| Challenge Listing and Acceptance | Acceptance is rejected after the match window closes | `Accept_Rejects_Closed_Match_Betting_Window_Without_Deducting_Balance`; `AcceptChallengeHandler` checks the challenge match window before taker escrow | ✅ COMPLIANT |
| Creator Challenge Cancellation | Creator cancels an open challenge | `Cancel_Refunds_Open_Challenge_For_Creator`; `POST /api/challenges/{id}/cancel` sends authenticated user id to `CancelChallengeHandler` | ✅ COMPLIANT |
| Creator Challenge Cancellation | Cancellation is rejected | `Cancel_Rejects_Non_Creator_Without_Refund`, `Cancel_Rejects_Matched_Challenge_Without_Refund`; handler returns `challenges.not_creator` or `challenges.not_open` before refund | ✅ COMPLIANT |
| Manual Lifecycle Settlement | Admin settles a matched challenge | `Settle_Pays_Full_Escrow_To_Winning_Side`, `Settle_Rejects_Invalid_Winner_Side_Without_Payout`; admin-only settlement endpoint remains guarded | ✅ COMPLIANT |
| Manual Lifecycle Settlement | Admin voids or expires a challenge | `Void_Refunds_Open_Challenge_Creator_Escrow`, `Expire_Refunds_Matched_Challenge_Participants`, `Void_Rejects_Terminal_Challenge_Without_Second_Refund`; admin-only void/expire endpoints remain guarded | ✅ COMPLIANT |
| Pending Stake Reporting | Pending stake includes active challenge escrow | `GetLeaderboardHandlerTests.Handle_Shows_Pending_Stake_Separately_From_Realized_Balance`; `PostgresConcurrencyHardeningTests` repository active-total evidence; `MatchChallengeRepository.ListActiveStakeAmountsByUserAsync` | ✅ COMPLIANT |

**Compliance summary**: 11/11 scenarios compliant.

### Correctness (Static Evidence)

| Requirement | Status | Notes |
|------------|--------|-------|
| Match betting-window enforcement on create | ✅ Implemented | `CreateChallengeHandler` loads the match and rejects `!match.IsBettingOpenAt(DateTime.UtcNow)` before `MatchChallenge.Create`, balance deduction, or repository add. |
| Match betting-window enforcement on accept | ✅ Implemented | `AcceptChallengeHandler` checks open/non-creator state, then loads the match and rejects a closed window before taker affordability, balance deduction, or `challenge.Accept`. |
| Creator-only cancellation | ✅ Implemented | `CancelChallengeHandler` requires `challenge.CreatorPosition.UserId == command.CreatorUserId` and `Status == Open` before crediting the creator escrow and voiding the challenge. |
| Separate challenge aggregate | ✅ Implemented | `MatchChallenge`, `MatchChallengePosition`, status/side enums, repository contract, EF configs, migration, and repository exist. |
| Immediate creator/taker escrow | ✅ Implemented | Create/accept handlers use serializable transactions, affordability checks, balance deduction, and save/commit behavior. |
| Manual admin lifecycle | ✅ Implemented | Settle/void/expire handlers and admin-only API routes exist; no auto-settlement path from match results was found. |
| Terminal-state guards | ✅ Implemented | Domain and handler checks reject invalid transitions and repeated terminal operations. |
| Pending stake totals | ✅ Implemented | Leaderboard pending totals include active challenge positions and exclude terminal statuses. |
| Frontend route/service/page | ✅ Implemented | Angular challenges service/page exposes create, accept, cancel, admin lifecycle actions, wallet refresh behavior, and build passes. |

### Coherence (Design)

| Decision | Followed? | Notes |
|----------|-----------|-------|
| Dedicated tables/contracts instead of extending bet tables | ✅ Yes | Challenge entities, repository, EF configuration, and migration are separate. |
| Deduct on create/accept; credit on settle/refund | ✅ Yes | Balance mutation follows existing CopaCoin patterns; cancellation is implemented as creator refund + `Voided` terminal status. |
| Admin/manual lifecycle handlers | ✅ Yes | Settlement, void, and expiry are admin-authorized API operations; user cancellation is a separate creator-only route for open challenges. |
| Dedicated `/api/challenges` group and Angular feature | ✅ Yes | Thin endpoint group plus frontend service/page/route are present. |
| Serializable transactions plus versioned entity | ✅ Yes | Mutation handlers begin serializable transactions; challenge entity is configured with a concurrency token. |

### Issues Found

**CRITICAL**: None.

**WARNING**:
- PostgreSQL concurrency tests are gated by `WORLD_CUP_BETS_TEST_CONNECTION_STRING`; no disposable PostgreSQL connection was provided during this verification, so race coverage remains source-present but environment-gated.
- `openspec validate custom-match-challenges --strict` could not run because the `openspec` CLI is not installed in this environment.
- The full change remains above the 400-line review budget by plan; deliver through the existing auto-chain / feature-branch-chain slices rather than one oversized PR.

**SUGGESTION**:
- Add endpoint-level integration tests for the new `POST /api/challenges/{id}/cancel` route and closed-window API responses if the project later adds WebApi test coverage; current runtime coverage is handler-level plus source inspection of endpoint mapping.

### Verdict

PASS WITH WARNINGS

The match betting-window enforcement and creator-only open-challenge cancellation are implemented, covered by passing handler tests, coherent with the SDD artifacts, and the backend/frontend commands passed. Remaining warnings are environment/tooling and delivery-size constraints, not implementation blockers.
