# Design: Custom Match Challenges

## Technical Approach

Add a separate match-challenge aggregate, repository, Wolverine handlers, Minimal API group, and standalone Angular feature. The design follows existing `MatchBet`/`TournamentPick` patterns: domain entities own invariants, Application handlers use repository contracts and serializable transactions, WebApi endpoints stay thin, EF Core owns mappings/migrations, and frontend calls stay behind feature services.

## Architecture Decisions

| Option | Tradeoff | Decision |
|---|---|---|
| New `MatchChallenge`/`MatchChallengePosition` instead of extending bet tables | More files/migration, but avoids breaking `user/match` and `user/category` uniqueness rules | Create dedicated tables and contracts |
| Escrow by deducting `User.CurrentBalanceCc` immediately | Requires pending-stake aggregation, but matches existing CopaCoin behavior | Deduct on create/accept; credit on settle/refund |
| Admin/manual lifecycle handlers | Slower operations, but free-text claims cannot be derived from official match result | `Settle`, `Void`, and `Expire` require Admin |
| Dedicated `/api/challenges` group and frontend feature | Adds route/service, but keeps `BetsEndpoints` and matches service from growing further | Create `ChallengesEndpoints` and `features/challenges` |
| Serializable transaction plus entity version | Some concurrent attempts may conflict, but current code already treats PostgreSQL serialization/unique violations as conflicts | Use same transaction factory and add challenge to versioned types |

## Data Flow

Create/accept:

    Angular ChallengesPage -> /api/challenges -> Wolverine handler
      -> IUserRepository + IMatchRepository + IMatchChallengeRepository
      -> serializable transaction -> deduct balance -> save challenge/position

Settle/refund:

    Admin endpoint -> handler -> load challenge with participants
      -> credit winner or refund active positions -> terminal status -> save

Pending stake:

    Leaderboard/My Bets summary -> challenge repository active stake totals
      -> add to existing match/tournament pending totals

## File Changes

| File | Action | Description |
|---|---|---|
| `src/WorldCupBets.Domain/Entities/MatchChallenge*.cs` | Create | Aggregate, position side, status, winner side, lifecycle methods |
| `src/WorldCupBets.Domain/Repositories/IMatchChallengeRepository.cs` | Create | List, load for update, active stake totals, add |
| `src/WorldCupBets.Application/Features/Challenges/*` | Create | Commands/queries/DTOs and handlers for list, create, accept, settle, void, expire |
| `src/WorldCupBets.Application/Features/Leaderboard/GetLeaderboardHandler.cs` | Modify | Include challenge pending stakes |
| `src/WorldCupBets.Application/Features/Users/*Summary*` | Modify | Expose pending challenge stake only if My Bets needs wallet parity |
| `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs` | Modify | DbSets and versioned challenge type |
| `src/WorldCupBets.Infrastructure/Persistence/Configurations/*Challenge*Configuration.cs` | Create | Tables, enum conversions, numeric stake columns, indexes |
| `src/WorldCupBets.Infrastructure/Persistence/Repositories/MatchChallengeRepository.cs` | Create | EF implementation with includes for participants/match |
| `src/WorldCupBets.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | Modify | Register repository |
| `src/WorldCupBets.Infrastructure/Persistence/Migrations/*AddMatchChallenges*` | Create | Deterministic schema migration |
| `src/WorldCupBets.WebApi/Endpoints/ChallengesEndpoints.cs` | Create | Authenticated challenge routes; admin lifecycle routes |
| `src/WorldCupBets.WebApi/Extensions/WebApplicationExtensions.cs` | Modify | Map challenge endpoints |
| `frontend/src/app/features/challenges/*` | Create | Models, service, page with list/create/accept/admin actions |
| `frontend/src/app/app.routes.ts`, `frontend/src/app/app.component.ts` | Modify | Authenticated Challenges route/nav |
| `tests/WorldCupBets.Application.Tests/*Challenge*Tests.cs` | Create | Handler, persistence, and concurrency coverage |

## Interfaces / Contracts

API routes: `GET /api/challenges?matchId=`, `POST /api/challenges`, `POST /api/challenges/{id}/accept`, `POST /api/challenges/{id}/settlement`, `POST /api/challenges/{id}/void`, `POST /api/challenges/{id}/expire`.

Request fields: `matchId`, `claimText`, `stakeAmountCc`, and settlement `winnerSide` (`Creator`/`Taker`). The creator backs the claim; the taker accepts the implicit opposite side. Text is trimmed and length-limited in handlers/domain; stake must be positive.

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | create/accept validations, self-accept rejection, terminal lifecycle, payout/refund | xUnit handler tests with in-file stubs matching current style |
| Persistence | EF mapping, indexes, repository active totals | Npgsql model/migration tests like `TournamentPickPersistenceTests` |
| Concurrency | double accept and double settlement do not double deduct/pay | Optional PostgreSQL test gated by `WORLD_CUP_BETS_TEST_CONNECTION_STRING` |
| Frontend | Basic route/service behavior | Manual verification unless project adds Angular tests |

## Migration / Rollout

Add two new tables with no destructive migration. Roll out backend first, then API/admin lifecycle, then frontend. Rollback requires disabling `/api/challenges`, refunding non-terminal escrow if data exists, then reverting migration/UI.

## Delivery Plan / Review Slices

1. Backend domain+persistence+tests: entities, repository, EF config/migration, pending totals; target <=400 changed lines excluding generated migration if reviewed separately.
2. Application+API handlers: list/create/accept and admin settle/void/expire endpoint group; target <=400 lines.
3. Frontend feature: challenges service/models/page/route/nav and wallet refresh; target <=400 lines.
4. Settlement/admin hardening follow-up if needed: concurrency integration tests, admin UI polish, My Bets pending parity; target <=400 lines.

## Open Questions

- [ ] Exact expiry rule is still product-defined; design supports manual admin expiry in v1.
- [ ] Decide whether generated EF migration counts against the 400-line review budget or ships in its own slice.
