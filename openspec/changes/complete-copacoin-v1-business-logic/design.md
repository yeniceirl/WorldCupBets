# Design: Complete CopaCoin V1 Business Logic

## Technical Approach

Extend existing Domain entities with result/settlement state, keep payout rules in Application handlers, persist through EF repositories, and expose only thin Wolverine `InvokeAsync` endpoints. Settlement is synchronous, transactional, and idempotent: result entry loads the match, bets, users, and jackpot row in one EF transaction, exits without mutation when already settled, then applies integer CopaCoin payouts and saves once.

## Architecture Decisions

| Decision | Choice | Alternatives considered | Rationale |
|---|---|---|---|
| Settlement ownership | Application handlers orchestrate; Domain entities expose small methods such as result/settled markers and `User.CreditBalance`. | Put rules in WebApi or EF repositories. | Matches existing handler pattern and keeps endpoints thin while avoiding EF concerns in Domain. |
| Idempotency | Persist `Match.OfficialResult`, `Match.SettledAtUtc`, and singleton `TournamentSettlement` champion state. | Infer settlement from balances or create payout ledger first. | Smallest correct state that prevents double settlement; ledger can be added later if audit needs grow. |
| Jackpot | Add singleton `TournamentSettlement` with `ChampionJackpotCc`, `ChampionSettledAtUtc`, `ChampionTeamName`, `UndistributedJackpotCc`. | Store jackpot on every match or config table. | Centralizes cross-match accounting and final champion settlement. |
| Integer division | Use integer `/` and `%`; match split remainders go to champion jackpot, nobody-correct remainder per bet goes to jackpot, final champion remainder remains undistributed. | Decimal balances or random remainder assignment. | Deterministic and matches specs/product rules. |

## Data Flow

```text
Admin endpoint -> Wolverine command -> Application handler
  -> repositories/AppDbContext transaction
  -> Match/User/TournamentSettlement state
  -> SaveChanges -> DTO response

Leaderboard endpoint -> GetLeaderboardHandler -> IUserRepository.ListLeaderboardAsync -> balances desc
```

## File Changes

| File | Action | Description |
|---|---|---|
| `src/WorldCupBets.Domain/Entities/{Match,User}.cs` | Modify | Add official result/settlement markers and safe balance credit. |
| `src/WorldCupBets.Domain/Entities/TournamentSettlement.cs` | Create | Singleton jackpot and champion settlement state. |
| `src/WorldCupBets.Domain/Repositories/{IMatchRepository,IMatchBetRepository,IChampionBetRepository,IUserRepository,ITournamentSettlementRepository}.cs` | Modify/Create | Add settlement-loading and leaderboard contracts. |
| `src/WorldCupBets.Application/Features/{Matches,Bets,Leaderboard}/*` | Create/Modify | Result entry, match settlement, champion settlement, leaderboard DTOs/handlers. |
| `src/WorldCupBets.Infrastructure/Persistence/{AppDbContext,Configurations,Repositories,Migrations}/*` | Modify/Create | EF mapping, transaction-backed repository methods, migration/model snapshot. |
| `src/WorldCupBets.WebApi/Endpoints/{MatchesEndpoints,BetsEndpoints}.cs` | Modify | Admin result/champion settlement endpoints and leaderboard GET endpoint. |
| `frontend/src/app/features/{matches,leaderboard}/*` | Modify/Create | Show results/settlement status and replace placeholder leaderboard with API data. |
| `tests/WorldCupBets.Application.Tests/*` | Modify/Create | Rule and handler coverage. |

## Interfaces / Contracts

```csharp
public sealed record RecordMatchResultCommand(int MatchId, MatchBetSelection OfficialResult);
public sealed record SettleChampionCommand(string ChampionTeamName);
public sealed record LeaderboardItemDto(int Rank, string DisplayName, int CurrentBalanceCc);
```

Admin endpoints require `Admin`: `POST /api/matches/{id}/result`, `POST /api/bets/champion/settlement`. Bettor leaderboard endpoint: `GET /api/leaderboard` or `/api/bets/leaderboard` using existing route conventions; prefer `/api/leaderboard` if adding a dedicated endpoint group.

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | Match result close-window rule, winner/loser split, all-correct, nobody-correct, division remainders, champion remainder. | xUnit domain/rule tests with in-memory entities. |
| Handler | Idempotent repeated match/champion settlement and balance/jackpot mutations. | Stub repositories mirroring current Application tests; add EF-backed integration if transaction behavior is difficult to stub. |
| API/UI | Thin endpoint status mapping; leaderboard loading/empty/error states. | Minimal WebApi tests if available; Angular component/service tests only where existing tooling supports it. |

## Migration / Rollout

Create one deterministic EF migration adding nullable match result fields, settlement timestamps, and `TournamentSettlements`. Seed/ensure singleton row in repository on first use. Existing balances remain unchanged. Rollback reverts migration or restores pre-settlement backup.

## Open Questions

- None
